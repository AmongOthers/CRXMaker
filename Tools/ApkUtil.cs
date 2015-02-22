using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Tools
{
    public class ApkUtil
    {
        public class ApkInfo : LxErrno
        {
            public ApkInfo(LxErrno errno)
                : base(errno)
            {
            }
            public static Regex PACKAGE_REGEX = new Regex("package: name='(.*?)'");
            public static Regex LAUNCHABLE_ACTIVITY_REGEX = new Regex("launchable activity name='(.*?)'");
            public class ApkInfoData
            {
                public string PackageName;
                public string[] LaunchableActivitys;
            }
            private ApkInfoData mData = new ApkInfoData();
            public object Data
            {
                get
                {
                    return mData;
                }
            }
            public ApkInfoData ExactData
            {
                get
                {
                    return mData;
                }
            }
        }
        public static ApkInfo hackApk(string apkPath)
        {
            ApkInfo result = null;
            string args = string.Format("dump badging {0}", apkPath);
            if (File.Exists("aapt.exe"))
            {
                ExecutorControl control;
                CommandHelper.excute("aapt.exe", args, ExecutorControl.INDEFINITELY, out control);
                control.waitUtilOutputFinished();
                CommandErrno errno = control.ShellErrno;
                if (errno.ExitCode == 0)
                {
                    string output = (string)errno.Output;
                    if (!string.IsNullOrEmpty(output))
                    {
                        result = new ApkInfo(errno);
                        Match tempMatch = ApkInfo.PACKAGE_REGEX.Match(output);
                        if (tempMatch.Success)
                        {
                            result.ExactData.PackageName = tempMatch.Groups[1].Value;
                        }
                        MatchCollection tempMatches = ApkInfo.LAUNCHABLE_ACTIVITY_REGEX.Matches(output);
                        if (tempMatches.Count > 0)
                        {
                            result.ExactData.LaunchableActivitys = new string[tempMatches.Count];
                            int i = 0;
                            foreach (Match temp in tempMatches)
                            {
                                string component = temp.Groups[1].Value;
                                int lastDot = component.LastIndexOf('.');
                                component = component.Insert(lastDot, "/");
                                result.ExactData.LaunchableActivitys[i++] = component;
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}
