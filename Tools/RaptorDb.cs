using System;
using System.Collections.Generic;
using System.Text;
using RaptorDB;
using System.IO;

namespace Tools
{
    public class RaptorDb : ICommonDb
    {
        private RaptorDB<string> mDb;
        private string mDbName;
        private string mDbPath;
        private const string DB_DATAFOLDER = "./Data";

        public RaptorDb(string dbName)
        {
            mDbName = dbName;
            mDbPath = String.Format("{0}/{1}", DB_DATAFOLDER, dbName);
            mDb = RaptorDB<string>.Open(String.Format("{0}/{1}", mDbPath, dbName), false);
        }

        public bool Put(string section,string key, string value)
        {
            try
            {
                mDb.Set(key, value);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).ErrorFormat("保存{0}数据的{1}键值时出错:{2}", mDbName, key, ex);
            }
            return false;
        }

        public string Get(string section, string key)
        {
            try
            {
                string value;
                if (mDb.Get(key, out value))
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).ErrorFormat("读取{0}数据的{1}键值时出错:{2}", mDbName, key, ex);
            }
            return String.Empty;
        }

        public bool Clear(string section)
        {
            try
            {
                if (mDb != null)
                {
                    mDb.Shutdown();
                    mDb.Dispose();
                    deleteDbFile();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).InfoFormat("清除{0}数据库时出错:{1}", mDbName, ex);
            }
            return false;
        }

        private void deleteDbFile()
        {
            try
            {
                if (Directory.Exists(mDbPath))
                {
                    Directory.Delete(mDbPath, true);
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).InfoFormat("删除{0}数据库文件时出错:{1}", mDbName, ex);
            }
        }
    }
}
