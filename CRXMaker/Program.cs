using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tools;
using Tools.AppInfoUtils;

namespace CRXMaker
{
    class Program
    {
        static void Main(string[] args)
        {
            string apkPath;
            bool isTablet = false;
            if (args.Length < 1)
            {
                apkPath = @"origin\LoveLive 学园偶像祭.apk";
            }
            else
            {
                apkPath = args[0];
                isTablet = false;
                if (args.Length > 1)
                {
                    isTablet = args[1].Equals("tablet") || args[1].Equals("t");
                }
            }
            
            AppInfoTool tool = new AppInfoTool();
            ApkOrIpaInfo info =  tool.Apk.GetApkInfo(apkPath);
            ExecutorControl executorControl;
            CommandErrno errno;
            string chromeosApkAgrs;
            if (!isTablet)
            {
                chromeosApkAgrs = String.Format("C:\\Users\\Administrator\\AppData\\Roaming\\npm\\node_modules\\chromeos-apk\\chromeos-apk --scale --name \"{0}\" \"{1}\"", info.AppName, apkPath);
            }
            else
            {
                chromeosApkAgrs = String.Format("C:\\Users\\Administrator\\AppData\\Roaming\\npm\\node_modules\\chromeos-apk\\chromeos-apk -tablet --scale --name \"{0}\" \"{1}\"", info.AppName, apkPath);
            }
            errno = CommandHelper.excute(@"node.exe", chromeosApkAgrs, -1, out executorControl, Encoding.UTF8);
            //替换图标
            tool.Apk.ExtractApkIcon(apkPath, info.AppIcon, info.PackageName + ".android", "icon");
            var outputzipFileName = String.Format("{0}v{1}.zip", info.AppName.Replace(":", "-"), info.AppVersion);
            string zipArgs = String.Format(@"a {0} {1}", outputzipFileName, info.PackageName + ".android");
            Console.WriteLine(String.Format("zipArgs: {0}", zipArgs));
            errno = CommandHelper.excute(@"C:\Program Files\WinRAR\WinRAR.exe", zipArgs, -1, out executorControl, Encoding.UTF8);
            File.Move(outputzipFileName, outputzipFileName.Replace("zip", "dacrx"));
        }
    }
}
