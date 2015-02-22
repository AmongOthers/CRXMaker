using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public class DbHelper
    {
        private static Dictionary<string, ICommonDb> mDbDict;
        private static object mLock = new object();
        static DbHelper()
        {
            mDbDict = new Dictionary<string, ICommonDb>();
        }
        public static bool Put(string section, string key, string value)
        {
            try
            {
                lock (mLock)
                {
                    if (!mDbDict.ContainsKey(section))
                    {
                        var db = CommonDbFactory.Create(section,CommonDbTypeMode.RaptorDb);
                        mDbDict.Add(section, db);
                    }
                    mDbDict[section].Put(section,key, value);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(DbHelper).Name).ErrorFormat("保存{0}数据库的{1}键值时出错:{2}", section, key, ex);
            }
            return false;
        }
        public static string Get(string section, string key)
        {
            try
            {
                lock (mLock)
                {
                    if (mDbDict.ContainsKey(section))
                    {
                        return mDbDict[section].Get(section, key);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(DbHelper).Name).ErrorFormat("读取{0}数据库的{1}键值时出错:{2}", section, key, ex);
            }
            return String.Empty;
        }

        public static bool Clear(string section)
        {
            try
            {
                lock (mLock)
                {
                    if (mDbDict.ContainsKey(section))
                    {
                        mDbDict[section].Clear(section);
                        return mDbDict.Remove(section);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(DbHelper).Name).ErrorFormat("清除{0}数据库时出错:{1}", section, ex);
            }
            return false;
        }
    }
}
