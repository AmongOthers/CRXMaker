using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

namespace Tools
{
    public class ReportSetting
    {
        public string MailFrom { get; set; }
        public string MailTo { get; set; }
        public string MailSubjet { get; set; }
        public string SmtpHost { get; set; }
        public string MailUser { get; set; }
        public string MailPwd { get; set; }
        public string WarningText { get; set; }
        public string ReportDescText { get; set; }
        public string MessageBoxText { get; set; }

        private static ReportSetting mInstance = null;

        public static ReportSetting getInstance()
        {
            if (mInstance == null)
            {
                mInstance = new ReportSetting();
            }
            return mInstance;
        }

        private ReportSetting()
        {
            Dictionary<string, string> desc_dic = new Dictionary<string, string>();
            try
            {
                using (FileStream fs = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DebugReportSetting.lx"), FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string key = null;
                        string value = null;

                        while (true)
                        {
                            string line = sr.ReadLine();
                            if (line == null)
                            {
                                if (!string.IsNullOrEmpty(key) || !string.IsNullOrEmpty(value))
                                {
                                    desc_dic.Add(key, value);
                                }
                                break;
                            }
                            line = line.Trim();
                            if (line.Length == 0 || line.StartsWith("#"))
                            {
                                continue;
                            }

                            if (line.StartsWith("["))
                            {
                                if (!string.IsNullOrEmpty(key) || !string.IsNullOrEmpty(value))
                                {
                                    desc_dic.Add(key, value);
                                }
                                key = parse_key(line);
                                value = "";
                            }
                            else
                            {
                                value += line;
                            }
                        }
                    }
                }

                foreach (PropertyInfo info in typeof(ReportSetting).GetProperties())
                {
                    if (desc_dic.ContainsKey(info.Name))
                    {
                        info.SetValue(this, desc_dic[info.Name], null);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("读取邮件配置出错",ex);
            }
        }

        private string parse_key(string line)
        {
            int index = line.IndexOf("]");
            if (index <=0 )
            {
                return null;
            }
            return line.Substring(1, index - 1);
        }
    }
}
