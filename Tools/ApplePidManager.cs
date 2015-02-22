using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tools
{
    public class ApplePidManager
    {
        private const string PID_DOCUMENT = "apple_pids.txt";
        private List<string> mApplePidList;

        private static ApplePidManager _instance = null;
        private static object mLock = new object();
        public static ApplePidManager GetInstance()
        {
            if (_instance == null)
            {
                lock (mLock)
                {
                    if (_instance == null)
                    {
                        _instance = new ApplePidManager();
                    }
                }
            }
            return _instance;
        }

        private ApplePidManager()
        { }

        public bool CheckContainCurrenPid(string pid)
        {
            return mApplePidList.Contains(pid.ToLower());
        }

        public void InitApplePidList()
        {
            mApplePidList = new List<string>();
            try
            {
                Logger.Logger.GetLogger(this).Info("开始初始化苹果pid列表");

                var myDocumentsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), PID_DOCUMENT);
                var appFolderFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PID_DOCUMENT);

                if (!File.Exists(myDocumentsFilePath) && File.Exists(appFolderFilePath))
                {
                    File.Copy(appFolderFilePath, myDocumentsFilePath);
                    Logger.Logger.GetLogger(this).Info("copy苹果pid列表到my documents文件夹下完成");
                }

                if (File.Exists(myDocumentsFilePath))
                {
                    using (var fs = new FileStream(myDocumentsFilePath, FileMode.Open))
                    {
                        using (var sr = new StreamReader(fs))
                        {
                            string line = null;
                            while ((line = sr.ReadLine()) != null)
                            {
                                if (!String.IsNullOrEmpty(line.Trim()) && !mApplePidList.Contains(line.ToLower()))
                                {
                                    mApplePidList.Add(line.ToLower());
                                }
                            }
                            Logger.Logger.GetLogger(this).Info("初始化苹果pid列表完成");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("初始化苹果pid列表时出错", ex);
            }
        }

        public void AddApplePidToFile(string pid)
        {
            try
            {
                Logger.Logger.GetLogger(this).Info("开始更新苹果pid列表");

                if (!mApplePidList.Contains(pid.ToLower()))
                {
                    mApplePidList.Add(pid.ToLower());

                    var myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    if (Directory.Exists(myDocumentsPath))
                    {
                        var filePath = Path.Combine(myDocumentsPath, PID_DOCUMENT);
                        if (File.Exists(filePath))
                        {
                            using (var fs = new FileStream(filePath, FileMode.Append))
                            {
                                using (var sw = new StreamWriter(fs))
                                {
                                    sw.WriteLine(pid);
                                }
                            }
                        }
                    }
                }
                Logger.Logger.GetLogger(this).Info("更新苹果pid列表完成");
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("更新苹果pid列表时出错", ex);
            }
        }
    }
}
