using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace Tools
{
    public class ExceptionSetup
    {
        public static void CatchMeHere(string appName, string version, bool isSilent = true)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "4SLOG");
            setup(logPath, appName, version, isSilent);
        }

        private static string sLogPath;
        private static string sAppName;
        private static string sVersion;
        private static bool sIsSilent;
        public static void setup(string logPath, string appName, string version, bool isSilent)
        {
            sLogPath = logPath;
            sAppName = appName;
            sVersion = version;
            sIsSilent = isSilent;
            //处理未捕获的异常   
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            //处理UI线程异常   
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            //处理非UI线程异常   
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            string str = string.Format("应用程序线程错误:{0}", e);
            Exception error = e.Exception as Exception;
            reportError(str, error);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string str = string.Format("应用程序错误:{0}", e);
            Exception error = e.ExceptionObject as Exception;
            reportError(str, error);
        }

        private static void reportError(string str, Exception error)
        {
            string strDateInfo = "出现应用程序未处理的异常：" + DateTime.Now.ToString() + "\r\n";
            if (error != null)
            {
                str = errorToString(error);
            }
            Logger.Logger.GetLogger(typeof(ExceptionSetup)).Fatal(str);
            UserBehaviorManager.GetInstance().AddUserBehavior(new BehaviorData()
            {
                WidgetName = String.Empty,
                PageName = String.Empty,
                Location = 0,
                Breakdown = "1",
                DownloadSuccess = "0",
                Startup = 0
            },true);
            ShowReportDialog(sAppName, sVersion);
        }

        private static String errorToString(Exception error)
        {
            if (error != null)
            {
                String str = String.Format("异常类型：{0}\r\n异常消息：{1}\r\n异常信息：{2}\r\n 内部异常: {3}",
                     error.GetType().FullName, error.Message, error.StackTrace, errorToString(error.InnerException));
                return str;
            }
            return "null";
        }

        public static void ShowReportDialog(string appName, string version)
        {
            if (!String.IsNullOrEmpty(MonitorInfo.GetInstance().ClientVerName))
            {
                appName = MonitorInfo.GetInstance().ClientVerName;
            }

            if (!String.IsNullOrEmpty(MonitorInfo.GetInstance().UserId) &&
                !String.IsNullOrEmpty(MonitorInfo.GetInstance().UserName))
            {
                appName = String.Format("{0}-{1}-{2}", appName, MonitorInfo.GetInstance().UserId, MonitorInfo.GetInstance().UserName);
            }

            string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DebugReportTool.exe");
            string args = string.Format("\"{0}\" \"{1}\" \"{2}\"", appName, version, sIsSilent.ToString().ToLower());
            Logger.Logger.GetLogger("ExceptionSetup").InfoFormat("args:{0}", args);
            if (File.Exists(fileName))
            {
                Process p = new Process();
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.FileName = fileName;
                p.StartInfo.Arguments = args;
                p.Start();
            }
            Environment.Exit(0x111);
        }
    }
}
