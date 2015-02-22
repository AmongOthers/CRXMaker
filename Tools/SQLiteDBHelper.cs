using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Text;

namespace Tools
{
    /// <summary> 
    /// 说明：这是一个针对System.Data.SQLite的数据库常规操作封装的通用类。 
    /// </summary> 
    public class SQLiteDBHelper
    {
        private string mConnectionString = string.Empty;
        private ReaderWriterObjectLocker mDBlocker = new ReaderWriterObjectLocker();

        public SQLiteDBHelper(string dbPath, string password = null)
        {
            this.mConnectionString = "Data Source=" + dbPath + ";datetimeformat=JulianDay;Pooling=True;Max Pool Size=100";
            if (!String.IsNullOrEmpty(password))
            {
                this.mConnectionString += ";Password=" + password;
            }
        }

        /// <summary> 
        /// 创建SQLite数据库文件 
        /// </summary> 
        /// <param name="dbPath">要创建的SQLite数据库文件路径</param> 
        public static void CreateDB(string dbPath, string sql)
        {
            using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + dbPath))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
        }

        public SQLiteConnection CreateSQLiteConnection()
        {
            return new SQLiteConnection(mConnectionString);
        }

        public static bool HasPwd(string dbPath, string password)
        {
            try
            {
                var connWithPwd = "Data Source=" + dbPath + ";datetimeformat=JulianDay;Password=" + password + ";";
                using (SQLiteConnection conn = new SQLiteConnection(connWithPwd))
                {
                    conn.Open();
                    using (SQLiteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT name FROM [sqlite_master]";
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch
            {
            }
            return false;
        }

        public static void ChangePassword(string dbPath, string oldPassword, string newPassword = "")
        {
            try
            {
                var connWithPwd = "Data Source=" + dbPath + ";datetimeformat=JulianDay;Password=" + oldPassword + ";";
                using (SQLiteConnection conn = new SQLiteConnection(connWithPwd))
                {
                    conn.Open();
                    conn.ChangePassword(newPassword);
                    Logger.Logger.GetLogger(typeof(SQLiteDBHelper).Name).Debug("修改app.db密码成功");
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(SQLiteDBHelper).Name).Error("修改app.db密码失败", ex);
            }
        }

        /// <summary> 
        /// 对SQLite数据库执行增删改操作，返回受影响的行数。 
        /// </summary> 
        /// <param name="sql">要执行的增删改的SQL语句</param> 
        /// <returns></returns> 
        public int ExecuteNonQuery(string sql)
        {
            using (mDBlocker.WriteLock())
            {
                int affectedRows = 0;
                using (SQLiteConnection connection = new SQLiteConnection(mConnectionString))
                {
                    connection.Open();
                    using (DbTransaction transaction = connection.BeginTransaction())
                    {
                        using (SQLiteCommand command = new SQLiteCommand(connection))
                        {
                            command.CommandText = sql;
                            affectedRows = command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                }
                return affectedRows;
            }
        }

        /// <summary> 
        /// 对SQLite数据库执行增删改操作，返回受影响的行数。 
        /// </summary> 
        /// <param name="sql">要执行的增删改的SQL语句</param> 
        /// <param name="parameters">执行增删改语句所需要的参数，参数必须以它们在SQL语句中的顺序为准</param> 
        /// <returns></returns> 
        public int ExecuteNonQuery(string sql, SQLiteParameter[] parameters)
        {
            using (mDBlocker.WriteLock())
            {
                int affectedRows = 0;
                using (SQLiteConnection connection = new SQLiteConnection(mConnectionString))
                {
                    connection.Open();
                    using (DbTransaction transaction = connection.BeginTransaction())
                    {
                        using (SQLiteCommand command = new SQLiteCommand(connection))
                        {
                            command.CommandText = sql;
                            if (parameters != null)
                            {
                                command.Parameters.AddRange(parameters);
                            }
                            affectedRows = command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                }
                return affectedRows;
            }
        }

        /// <summary> 
        /// 执行一个查询语句，返回一个关联的SQLiteDataReader实例 
        /// </summary> 
        /// <param name="sql">要执行的查询语句</param> 
        /// <param name="parameters">执行SQL查询语句所需要的参数，参数必须以它们在SQL语句中的顺序为准</param> 
        /// <returns></returns> 
        public SQLiteDataReader ExecuteReader(string sql)
        {
            using (mDBlocker.ReadLock())
            {
                SQLiteConnection connection = new SQLiteConnection(mConnectionString);
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                connection.Open();
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
        }

        /// <summary> 
        /// 执行一个查询语句，返回一个关联的SQLiteDataReader实例 
        /// </summary> 
        /// <param name="sql">要执行的查询语句</param> 
        /// <param name="parameters">执行SQL查询语句所需要的参数，参数必须以它们在SQL语句中的顺序为准</param> 
        /// <returns></returns> 
        public SQLiteDataReader ExecuteReader(string sql, SQLiteParameter[] parameters)
        {
            using (mDBlocker.ReadLock())
            {
                SQLiteConnection connection = new SQLiteConnection(mConnectionString);
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }
                connection.Open();
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
        }

        /// <summary> 
        /// 执行一个查询语句，返回一个包含查询结果的DataTable 
        /// </summary> 
        /// <param name="sql">要执行的查询语句</param> 
        /// <returns></returns> 
        public DataTable ExecuteDataTable(string sql)
        {
            using (mDBlocker.ReadLock())
            {
                using (SQLiteConnection connection = new SQLiteConnection(mConnectionString))
                {
                    using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                        DataTable data = new DataTable();
                        adapter.Fill(data);
                        return data;
                    }
                }
            }
        }

        /// <summary> 
        /// 执行一个查询语句，返回一个包含查询结果的DataTable 
        /// </summary> 
        /// <param name="sql">要执行的查询语句</param> 
        /// <param name="parameters">执行SQL查询语句所需要的参数，参数必须以它们在SQL语句中的顺序为准</param> 
        /// <returns></returns> 
        public DataTable ExecuteDataTable(string sql, SQLiteParameter[] parameters)
        {
            using (mDBlocker.ReadLock())
            {
                using (SQLiteConnection connection = new SQLiteConnection(mConnectionString))
                {
                    using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                    {
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                        DataTable data = new DataTable();
                        adapter.Fill(data);
                        return data;
                    }
                }
            }
        }
        /// <summary> 
        /// 执行一个查询语句，返回查询结果的第一行第一列 
        /// </summary> 
        /// <param name="sql">要执行的查询语句</param> 
        /// <param name="parameters">执行SQL查询语句所需要的参数，参数必须以它们在SQL语句中的顺序为准</param> 
        /// <returns></returns> 
        public Object ExecuteScalar(string sql, SQLiteParameter[] parameters)
        {
            using (mDBlocker.ReadLock())
            {
                using (SQLiteConnection connection = new SQLiteConnection(mConnectionString))
                {
                    using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                    {
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                        DataTable data = new DataTable();
                        adapter.Fill(data);
                        if (data.Rows.Count > 0)
                        {
                            return data.Rows[0][0];
                        }
                    }
                }
                return String.Empty;
            }
        }
        /// <summary> 
        /// 查询数据库中的所有数据类型信息 
        /// </summary> 
        /// <returns></returns> 
        public DataTable GetSchema()
        {
            using (mDBlocker.ReadLock())
            {
                using (SQLiteConnection connection = new SQLiteConnection(mConnectionString))
                {
                    connection.Open();
                    DataTable data = connection.GetSchema("TABLES");
                    connection.Close();
                    return data;
                }
            }
        }
    }
}
