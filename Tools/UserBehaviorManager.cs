using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Threading;
using System.Net;
 
namespace Tools
{
    public class UserBehaviorManager
    {
        const string USER_BEHAVIOR_URL = "http://www.phone580.com/fbsapi/api/fzssj/saveReport";// "http://10.20.1.47:8082/fbs/api/fzssj/saveReport"; 
        volatile bool mIsWorking = false;
        volatile HttpStatusCode mStatusCode;
        SQLiteDBHelper mDbHelper;
        DateTime mStartupTime;
        string mDeviceId = String.Empty;
        string mClientVersionId = String.Empty;
        string mClientName = String.Empty;
        Queue<string> mData = new Queue<string>();
        static string MONITOR_FILEPATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "monitor.fbs");
        static Object mLock = new Object();
        static UserBehaviorManager mInstance = null;

        private void initDb()
        {
            try
            {
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserBehavior.db");
                //如果不存在改数据库文件，则创建该数据库文件 
                if (!File.Exists(dbPath))
                {
                    var dirPath = Path.GetDirectoryName(dbPath);
                    if (!Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }
                    var sql = String.Format("CREATE TABLE Behavior(ID INTEGER,Data text({0}),CreateDate text(20),DeviceId text(20),ClientVersionId text(20),VersionName text(20),flag INTEGER,PRIMARY KEY ('ID' ASC));", 10485760); //最大值
                    SQLiteDBHelper.CreateDB(dbPath, sql);
                }
                mDbHelper = new SQLiteDBHelper(dbPath);
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("CreateDB SqliteDb", ex);
            }
        }

        public static UserBehaviorManager GetInstance()
        {
            lock (mLock)
            {
                if (mInstance == null)
                {
                    lock (mLock)
                    {
                        mInstance = new UserBehaviorManager();
                    }
                }
            }
            return mInstance;
        }

        public UserBehaviorManager()
        {
            mStartupTime = DateTime.Now;
        }

        public void Start(string clientVersionId, string clientName)
        {
            //ThreadPool.QueueUserWorkItem((_) =>
            //{
            //    if (mIsWorking)
            //    {
            //        Logger.Logger.GetLogger(this).Info("用户行为统计已启动");
            //        return;
            //    }
            //    initDb();
            //    try
            //    {
            //        Logger.Logger.GetLogger(this).Info("启动用户行为统计");
            //        mStartupTime = DateTime.Now;
            //        mClientVersionId = clientVersionId;
            //        mClientName = clientName;
            //        mIsWorking = true;

            //        UserBehaviorManager.GetInstance().AddUserBehavior(new BehaviorData()
            //        {
            //            WidgetName = String.Empty,
            //            PageName = String.Empty,
            //            Location = 0,
            //            Breakdown = "0",
            //            DownloadSuccess = "0",
            //            Startup = 1
            //        });
            //        //404状态表示后台停了这个接口，没必要写到数据库里面
            //        while (mIsWorking && mStatusCode != HttpStatusCode.NotFound)
            //        {
            //            //够10条数据才写到数据库
            //            if (mData.Count >= 10)
            //            {
            //                addToDatabase();
            //                if (checkBehavior())
            //                {
            //                    submitBehavior();
            //                }
            //            }
            //            Thread.Sleep(2000);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Logger.Logger.GetLogger(this).Error("循环检查用户行为队列出错", ex);
            //    }
            //});
        }

        public void Stop()
        {
            //Logger.Logger.GetLogger(this).Info("停止用户行为统计");
            //if (mIsWorking)
            //{
            //    UserBehaviorManager.GetInstance().AddUserBehavior(new BehaviorData()
            //    {
            //        WidgetName = (DateTime.Now - mStartupTime).Minutes.ToString(),
            //        PageName = "CONN-TIME",
            //        Location = 0,
            //        Breakdown = "0",
            //        DownloadSuccess = "0",
            //        Startup = 0
            //    });
            //    mIsWorking = false;
            //    addToDatabase();
            //    mData.Clear();
            //}
        }


        /// <summary>
        /// 添加用户行为统计
        /// </summary>
        /// <param name="data"></param>
        public void AddUserBehavior(string data)
        {
            if (mIsWorking && mStatusCode != HttpStatusCode.NotFound)
            {
                mData.Enqueue(data);
            }
        }
        /// <summary>
        /// 添加用户行为统计
        /// </summary>
        /// <param name="behaviorData"></param>
        public void AddUserBehavior(BehaviorData behaviorData, bool isBreakdown = false)
        {
            try
            {
                if (mIsWorking && mStatusCode != HttpStatusCode.NotFound)
                {
                    if (behaviorData == null)
                    {
                        Logger.Logger.GetLogger(this).Info("添加用户行为对象为null");
                        return;
                    }
                    var data = String.Format("{{'WidgetName':'{0}','PageName':'{1}','Location':{2},'Breakdown':'{3}','DownloadSuccess':'{4}','STARTUP':{5},'createDate':'{6}'}}",
                       behaviorData.WidgetName, behaviorData.PageName, behaviorData.Location, behaviorData.Breakdown, behaviorData.DownloadSuccess, behaviorData.Startup,behaviorData.CreateDate);
                    mData.Enqueue(data);

                    if (isBreakdown)
                    {
                        Logger.Logger.GetLogger(this).Info("程序崩溃 begin添加用户行为到数据库");
                        addToDatabase();
                        Logger.Logger.GetLogger(this).Info("程序崩溃 end添加用户行为到数据库");
                    }
                }         
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("添加用户行为出错", ex);
            }
        }

        private bool addToDatabase()
        {
            try
            {
                if (mData.Count == 0)
                {
                    return false;
                }

                if (String.IsNullOrEmpty(mDeviceId))
                {
                    mDeviceId = getDeviceId();
                }
                #region 添加到数据库

                var sb = new StringBuilder();
                while (mData.Count > 0)
                {
                    sb.Append(mData.Dequeue());
                    sb.Append(",");
                }
                sb.Remove(sb.Length -1,1);
                SQLiteParameter[] parameters = {
                                new SQLiteParameter("@Data", DbType.String),
                                new SQLiteParameter("@CreateDate", DbType.String),
                                new SQLiteParameter("@DeviceId", DbType.String),
                                new SQLiteParameter("@ClientVersionId", DbType.String),
                                new SQLiteParameter("@VersionName", DbType.String),
                                new SQLiteParameter("@flag", DbType.UInt32)};
                parameters[0].Value = sb.ToString();
                parameters[1].Value = DateTime.Now.ToString("yyyy-MM-dd");
                parameters[2].Value = mDeviceId;
                parameters[3].Value = mClientVersionId;
                parameters[4].Value = mClientName;
                parameters[5].Value = 0;
                var sql = "insert into Behavior(Data,CreateDate,DeviceId,ClientVersionId,VersionName,flag)values(@Data,@CreateDate,@DeviceId,@ClientVersionId,@VersionName,@flag);";
                return mDbHelper.ExecuteNonQuery(sql, parameters) > 0;

                #endregion
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("添加用户行为到数据库出错", ex);
            }
            return false;
        }

        private bool checkBehavior()
        {
            try
            {
                var sql = "select count(id) from Behavior where flag = 0";
                var obj = mDbHelper.ExecuteScalar(sql,null);
                if (obj != null)
                {
                    var count = Convert.ToInt32(obj);
                    return count >= 250;
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("检查用户行为个数出错", ex);
            }
            return false;
        }

        private bool updateBehavior(string ids)
        {
            try
            {
                var sql = String.Format("delete from Behavior where ID in({0}) or flag = 1;",ids);
                return mDbHelper.ExecuteNonQuery(sql, null) > 0;
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("更新用户行为标志为已提交出错", ex);
            }
            return false;
        }

        private void submitBehavior()
        {
            var idsList = new List<string>();
            try
            {
                if (mStatusCode == HttpStatusCode.NotFound)
                {
                    return;
                }
                var sql = "select * from Behavior where flag = 0 LIMIT 250";
                var dt = mDbHelper.ExecuteDataTable(sql);
                var sb = new StringBuilder();
                foreach (DataRow dr in dt.Rows)
                {
                    if (!idsList.Contains(dr["ID"].ToString()))
                    {
                        idsList.Add(dr["ID"].ToString());
                        sb.AppendFormat("{0},", dr["Data"].ToString());
                    }
                }
                if (sb.Length > 0)
                {
                    sb.Remove(sb.Length - 1, 1);
                    var dict = new Dictionary<string, object>();
                    dict.Add("data", String.Format("[{0}]", sb.ToString()));
                    dict.Add("createDate", DateTime.Now.ToString("yyyy-MM-dd"));
                    dict.Add("deviceId", mDeviceId);
                    dict.Add("clientVersionId", mClientVersionId);
                    dict.Add("versionName", mClientName);
                    using (var response = RequestHelper.Post(USER_BEHAVIOR_URL, dict))
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            if (idsList.Count > 0)
                            {
                                var idsStr = String.Join(",", idsList.ToArray());
                                var result = updateBehavior(idsStr);
                            }
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                //404页面错误也当作正确
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Message.Contains("404"))
                {
                    mStatusCode = HttpStatusCode.NotFound;
                    if (idsList.Count > 0)
                    {
                        var idsStr = String.Join(",", idsList.ToArray());
                        var result = updateBehavior(idsStr);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("提交用户行为到后台出错", ex);
            }         
        }

        private string getDeviceId()
        {
            try
            {
                if (File.Exists(MONITOR_FILEPATH))
                {
                    var json = File.ReadAllText(MONITOR_FILEPATH, Encoding.Default);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (dict != null && dict.ContainsKey("id"))
                    {
                        return dict["id"];
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("获取设备ID失败..", ex);
            }
            return String.Empty;
        }
    }

    public class BehaviorData
    {
        public string WidgetName { get; set; }
        public string PageName { get; set; }
        public int Location { get; set; }
        public string Breakdown { get; set; }
        public string DownloadSuccess { get; set; }
        public int Startup { get; set; }
        private string _CreateDate;
        public string CreateDate
        {
            get
            {
                _CreateDate = DateTime.Now.ToString("yyyy-MM-dd");
                return _CreateDate;
            }
        }
    }
}
