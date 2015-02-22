using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public class IniFileDb : ICommonDb
    {
        INIFile db;
        public IniFileDb(string dbPath)
        {
            db = new INIFile(dbPath);
        }

        public bool Put(string section, string key, string value)
        {
            try
            {
                db.WriteString(section, key, value);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error(db.FilePath, ex);
            }
            return false;
        }

        public string Get(string section, string key)
        {
            try
            {
               return db.ReadString(section, key);
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error(db.FilePath, ex);
            }
            return String.Empty; ;
        }

        public bool Clear(string section)
        {
            try
            {
                return db.EraseSection(section);
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error(db.FilePath, ex);
            }
            return false;
        }
    }
}
