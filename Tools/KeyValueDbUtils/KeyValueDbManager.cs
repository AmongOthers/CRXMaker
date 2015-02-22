using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Data.SQLite;
using System.Data;

namespace Tools.KeyValueDbUtils
{
    public class KeyValueDbManager : IDisposable
    {
        private bool mIsWorking;

        private SQLiteDBHelper mKeyValueDbHelper;
        private ReaderWriterObjectLocker mKeyValueDbHelperLocker;

        private Dictionary<string, string> mKeyValueDic;
        private ReaderWriterObjectLocker mKeyValueDicLocker;

        private int mCurOperateThreadState;//0表示未工作，1表示工作中


        public KeyValueDbManager(string dbPath, string password)
        {
            //先检查数据库文件是否正常
            checkDatabase(dbPath, password);
            mIsWorking = true;
            mKeyValueDbHelper = new SQLiteDBHelper(dbPath, password);
            mKeyValueDbHelperLocker = new ReaderWriterObjectLocker();
            mKeyValueDic = new Dictionary<string, string>();
            mKeyValueDicLocker = new ReaderWriterObjectLocker();
            mCurOperateThreadState = 0;
        }

        private void checkDatabase(string dbPath, string password)
        {
            // CREATE TABLE IF NOT EXISTS [KeyValueTable] ( [Key] text PRIMARY KEY NOT NULL, [Value] text)
            try
            {
                //这里不应该通过判断文件是否存在，来决定是否创建表（因为文件存在也有可能是一个不正确的文件）
                using (SQLiteConnection conn = new SQLiteConnection("Data Source=" + dbPath))
                {
                    conn.Open();
                    string strSql = "CREATE TABLE IF NOT EXISTS [KeyValueTable] ( [Key] text PRIMARY KEY NOT NULL, [Value] text)";
                    using (SQLiteCommand cmd = new SQLiteCommand(strSql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    //密码不为空时，才设置
                    if (!String.IsNullOrEmpty(password))
                    {
                        conn.ChangePassword(password);
                    }
                }
            }
            catch (SQLiteException ex)
            {
                //这个异常有可能是一个正常消息,所以不以错误输出
                Logger.Logger.GetLogger(this).Info("如果数据库已经存在，则可能会打印这消息（因为正常数据库可能有密码，所以打印这消息）", ex);
            }
        }

        public string GetValue(string key)
        {
            string value = null;
            try
            {
                string strSql = "select * from KeyValueTable where Key='" + key + "'";
                using (mKeyValueDbHelperLocker.ReadLock())
                {
                    using (SQLiteDataReader dr = mKeyValueDbHelper.ExecuteReader(strSql))
                    {
                        if (dr.Read())
                        {
                            value = dr["Value"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("获取数据库信息时出错（KeyValueTable）", ex);
                value = null;
            }
            return value;
        }

        public Dictionary<string, string> GetValues()
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            try
            {
                string strSql = "select * from KeyValueTable";
                using (mKeyValueDbHelperLocker.ReadLock())
                {
                    using (SQLiteDataReader dr = mKeyValueDbHelper.ExecuteReader(strSql))
                    {
                        while (dr.Read())
                        {
                            string key = dr["key"].ToString();
                            string value = dr["Value"].ToString();
                            dic.Add(key, value);//key是主键，不会出现相同
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("获取所有数据库信息（KeyValueTable）", ex);
            }
            return dic;
        }

        public void InsertOrUpdateKeyValue(string key, string value)
        {
            using (mKeyValueDicLocker.WriteLock())
            {
                mKeyValueDic.Remove(key);
                mKeyValueDic.Add(key, value);
            }
            checkAndStartOperateThread();
        }

        public void TryDeleteKeyValue(List<string> keyList)
        {
            if (keyList != null && keyList.Count != 0)
            {
                try
                {
                    using (mKeyValueDbHelperLocker.WriteLock())
                    {
                        using (SQLiteConnection conn = mKeyValueDbHelper.CreateSQLiteConnection())
                        {
                            conn.Open();
                            IDbTransaction trans = conn.BeginTransaction();
                            using (SQLiteCommand cmd = new SQLiteCommand(conn))
                            {
                                StringBuilder deleteTaskCondition = new StringBuilder();
                                for (int i = 0; i < keyList.Count; )
                                {
                                    if (deleteTaskCondition.Length != 0)
                                    {
                                        deleteTaskCondition.Append(" OR ");
                                    }
                                    deleteTaskCondition.Append(" Key='");
                                    deleteTaskCondition.Append(keyList[i]);
                                    deleteTaskCondition.Append("' ");
                                    i++;
                                    if (i == keyList.Count || i % 100 == 0)
                                    {
                                        cmd.CommandText = "delete from KeyValueTable where " + deleteTaskCondition.ToString();
                                        cmd.ExecuteNonQuery();
                                        deleteTaskCondition = new StringBuilder();
                                    }
                                }
                            }
                            trans.Commit();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Logger.GetLogger(this).Error("插入或更新数据库信息时出错（KeyValueTable）", ex);
                }
            }
        }

        public void ClearAllKeyValue()
        {
            try
            {
                string strSql = "delete from KeyValueTable";
                using (mKeyValueDbHelperLocker.WriteLock())
                {
                    mKeyValueDbHelper.ExecuteNonQuery(strSql);
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("清空数据库信息时出错（KeyValueTable）", ex);
            }
        }

        public void Dispose()
        {
            mIsWorking = false;
        }

        private void checkAndStartOperateThread()
        {
            if (Interlocked.Exchange(ref mCurOperateThreadState, 1) == 0)//如果已经工作中，就不用再重新启动线程
            {
                ThreadPool.QueueUserWorkItem((_) =>
                {
                    Dictionary<string, string> failedDic = new Dictionary<string, string>();
                    while (mIsWorking)
                    {
                        Dictionary<string, string> waitingOperateDic;
                        using (mKeyValueDicLocker.ReadLock())
                        {
                            if (mKeyValueDic.Count == 0 && failedDic.Count == 0)
                            {
                                //没有任务时，可以退出了
                                Interlocked.Exchange(ref mCurOperateThreadState, 0);
                                return;
                            }
                            waitingOperateDic = new Dictionary<string, string>(mKeyValueDic);
                            //把没成功处理的数据重新添加进待更新列表
                            foreach (KeyValuePair<string, string> keyValuePair in failedDic)
                            {
                                string key = keyValuePair.Key;
                                string value = keyValuePair.Value;
                                //如果Key已经存在，说明waitingConfigDic里已经包含最新的数据了，所以不用再添加进去了
                                if (!waitingOperateDic.ContainsKey(key))
                                {
                                    waitingOperateDic.Add(key, value);
                                }
                            }
                            mKeyValueDic.Clear();
                            failedDic.Clear();
                        }
                        try
                        {
                            using (mKeyValueDbHelperLocker.WriteLock())
                            {
                                using (SQLiteConnection conn = mKeyValueDbHelper.CreateSQLiteConnection())
                                {
                                    conn.Open();
                                    IDbTransaction trans = conn.BeginTransaction();
                                    using (SQLiteCommand cmd = new SQLiteCommand(conn))
                                    {
                                        foreach (KeyValuePair<string, string> keyValuePair in waitingOperateDic)
                                        {
                                            string key = keyValuePair.Key;
                                            string value = keyValuePair.Value;
                                            try
                                            {
                                                int count = 0;
                                                cmd.CommandText = "select count(*) as c from KeyValueTable where Key='" + key + "'";
                                                //先查询数据库中是否已经存在记录
                                                using (SQLiteDataReader dr = cmd.ExecuteReader())
                                                {
                                                    if (dr.Read())
                                                    {
                                                        count = dr.GetInt32(0);
                                                    }
                                                }
                                                string strSql = null;
                                                if (count != 0)
                                                {
                                                    strSql = "update KeyValueTable set Value='" + value + "' where Key='" + key + "'";
                                                }
                                                else
                                                {
                                                    strSql = "insert into KeyValueTable(Key,Value) values('" + key + "','" + value + "')";
                                                }
                                                cmd.CommandText = strSql;
                                                cmd.ExecuteNonQuery();
                                            }
                                            catch (Exception e)
                                            {
                                                if (!failedDic.ContainsKey(key))
                                                {
                                                    failedDic.Add(key, value);
                                                }
                                                Logger.Logger.GetLogger(this).Error("插入、更新数据库信息出错,稍后重新执行", e);
                                            }
                                        }
                                    }
                                    trans.Commit();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Logger.GetLogger(this).Fatal("插入、更新数据库信息出错（这是一个严重异常,可能导致配置信息缺失或不是最新）", ex);
                        }
                    }
                });
            }
        }

        ~KeyValueDbManager()
        {
            Dispose();
        }
    }
}
