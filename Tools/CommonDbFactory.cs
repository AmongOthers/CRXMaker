using System;
using System.Collections.Generic;
using System.Text;
using RaptorDB;

namespace Tools
{
    public enum CommonDbTypeMode
    {
        SqliteDb,
        IniFileDb,
        RaptorDb
    }

    public class CommonDbFactory
    {
        public static ICommonDb Create(string dbPath, CommonDbTypeMode dbType)
        {
            switch (dbType)
            {
                case CommonDbTypeMode.SqliteDb:
                    return new SqliteDb(dbPath);
                case CommonDbTypeMode.IniFileDb:
                    return new IniFileDb(dbPath);
                case CommonDbTypeMode.RaptorDb:
                    return new RaptorDb(dbPath);
            }
            throw new ArgumentException("没有实现当前类型的数据库");
        }
    }
}
