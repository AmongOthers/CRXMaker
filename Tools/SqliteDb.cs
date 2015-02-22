using System;
using System.Data.SQLite;
using System.Data;
using System.IO;

namespace Tools
{
    public class SqliteDb : ICommonDb
    {
        SQLiteDBHelper db;
        public SqliteDb(string dbPath)
        {
            try
            {
                //如果不存在改数据库文件，则创建该数据库文件 
                if (!File.Exists(dbPath))
                {
                    var fullPath = Path.GetDirectoryName(dbPath);
                    if (!Directory.Exists(fullPath))
                    {
                        Directory.CreateDirectory(fullPath);
                    }
                    var sql = String.Format("CREATE TABLE keyValue(section text(1024),key text(1024),value text({0}))", 10485760); //最大值
                    SQLiteDBHelper.CreateDB(dbPath, sql);
                }
                db = new SQLiteDBHelper(dbPath);
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("CreateDB SqliteDb", ex);
            }
        }

        public SqliteDb(string dbPath,string password)
        {
            try
            {
                //如果不存在改数据库文件，则创建该数据库文件 
                if (!File.Exists(dbPath))
                {
                    var fullPath = Path.GetDirectoryName(dbPath);
                    if (!Directory.Exists(fullPath))
                    {
                        Directory.CreateDirectory(fullPath);
                    }
                    var sql = String.Format("CREATE TABLE keyValue(section text(1024),key text(1024),value text({0}))", 10485760); //最大值
                    SQLiteDBHelper.CreateDB(dbPath, sql);
                    SQLiteDBHelper.ChangePassword(dbPath,String.Empty,password);
                }
                db = new SQLiteDBHelper(dbPath, password);
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("CreateDB SqliteDb", ex);
            }
        }

        public string Get(string section, string key)
        {
            try
            {
                SQLiteParameter[] parameters = {
                    new SQLiteParameter("@section", DbType.String),
                    new SQLiteParameter("@key", DbType.String)};
                parameters[0].Value = section;
                parameters[1].Value = key;
                object obj = db.ExecuteScalar("select value from keyValue where section = @section and [key] = @key;", parameters);
                if (obj != null)
                {
                    return obj.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error(String.Format("Get section={0} key={1}", section, key), ex);
            }           
            return String.Empty;
        }

        public bool Put(string section, string key, string value)
        {
            try
            {
                SQLiteParameter[] parameters = {
                    new SQLiteParameter("@section", DbType.String),
                    new SQLiteParameter("@key", DbType.String),
                    new SQLiteParameter("@value", DbType.String)};
                parameters[0].Value = section;
                parameters[1].Value = key;
                parameters[2].Value = value;
                object obj = db.ExecuteScalar("SELECT count(*) from keyValue where section = @section and [key] = @key;", parameters);
                if(obj != null && Convert.ToInt32(obj) > 0)
                {
                    return db.ExecuteNonQuery("UPDATE keyValue set value = @value where section = @section and [key] = @key;", parameters) > 0;
                }
                else
                {
                    return db.ExecuteNonQuery("INSERT into keyValue(section,[key],value) VALUES( @section,@key,@value);",parameters) > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error(String.Format("Put section={0} key={1} value={2}", section, key, value), ex);
            }
            return false;
        }

        public bool Clear(string section)
        {
            try
            {
                SQLiteParameter[] parameters = {
                    new SQLiteParameter("@section", DbType.String)};
                parameters[0].Value = section;
                return db.ExecuteNonQuery("DELETE from keyValue where section = @section;", parameters) > 0;
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error(String.Format("Put section={0}", section), ex);
            }
            return false;
        }
    }
}
