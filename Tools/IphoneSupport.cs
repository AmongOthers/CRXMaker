using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tools
{
    public abstract class IphoneSupport
    {
        /// <summary>
        /// 检查苹果组件
        /// </summary>
        /// <returns></returns>
        public abstract bool DetectIphoneSupport();

        /// <summary>
        /// 判断当前安装到客户端电脑上的版本是最新版本
        /// </summary>
        /// <param name="newestVersion">最新的版本</param>
        /// <param name="currentVersion">当前安装到客户端电脑上的版本</param>
        /// <returns>当前安装到客户端电脑上的版本是最新版本返回:true</returns>
        public abstract bool IsNewestVersion();

        protected string getCurrentVersion(string regPath)
        {
            if (String.IsNullOrEmpty(regPath))
            {
                throw new ArgumentNullException();
            }

            try
            {
                using (var reg = Registry.LocalMachine.OpenSubKey(regPath, false))
                {
                    if (reg != null)
                    {
                        var regValue = reg.GetValue("Version");
                        if (regValue != null)
                        {
                            Logger.Logger.GetLogger(typeof(DetectIphoneComponent).Name).Info(regPath + " Version " + regValue);
                            return regValue.ToString();
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.Logger.GetLogger(typeof(DetectIphoneComponent).Name).Error("Get Iphone Component CurrentVersion", ex);
            }
            return String.Empty;
        }

        protected bool isNewestVersion(string newestVersion, string currentVersion)
        {
            if (String.IsNullOrEmpty(newestVersion) || String.IsNullOrEmpty(currentVersion))
            {
                throw new ArgumentNullException();
            }

            try
            {
                return compareVersionString(currentVersion, newestVersion) >= 0;
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(DetectIphoneComponent).Name).Error("苹果组件版本比较出错", ex);
            }
            return false;
        }

        private int compareVersionString(string left, string right)
        {
            string[] leftVerArr = left.Split('.');
            string[] rightVersionArr = right.Split('.');
            int leftLength = leftVerArr.Length;
            int rightLength = rightVersionArr.Length;
            int leftInt, rightInt;
            for (int i = 0; i < leftLength; i++)
            {
                if (i == rightLength)
                {
                    return 1;
                }
                else
                {
                    leftInt = int.Parse(leftVerArr[i]);
                    rightInt = int.Parse(rightVersionArr[i]);
                    if (leftInt < rightInt)
                    {
                        return -1;
                    }
                    else if (leftInt > rightInt)
                    {
                        return 1;
                    }
                }
            }
            if (leftLength == rightLength)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
    }

    public class AppleApplicationSupport : IphoneSupport
    {
        private const string mNewestVersion = "3.0";
        private const string mIphoneComponentPath = @"SOFTWARE\Apple Inc.\Apple Application Support";
        public string AppleApplicationSupportInstallDir { get; set; }

        public override bool DetectIphoneSupport()
        {
            try
            {
                using (var reg = Registry.LocalMachine.OpenSubKey(mIphoneComponentPath, false))
                {
                    if (reg != null)
                    {
                        var regValue = reg.GetValue("InstallDir");
                        if (regValue != null)
                        {
                            AppleApplicationSupportInstallDir = regValue.ToString();
                            Logger.Logger.GetLogger(typeof(DetectIphoneComponent).Name).Info("Apple Application Support InstallDir " + AppleApplicationSupportInstallDir);
                            return true;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.Logger.GetLogger(typeof(DetectIphoneComponent).Name).Error("Apple Application Not Support", ex);
            }
            return false;
        }

        public override bool IsNewestVersion()
        {
            try
            {
                var currentVersion = getCurrentVersion(mIphoneComponentPath);
                return isNewestVersion(mNewestVersion, currentVersion);
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(DetectIphoneComponent).Name).ErrorFormat("AppleApplicationSupport IsNewestVersion", ex);
            }
            return false;
        }
    }

    public class AppleMobileDeviceSupport : IphoneSupport
    {
        private const string mNewestVersion = "7.1.0.32";
        private const string mIphoneComponentPath = @"SOFTWARE\Apple Inc.\Apple Mobile Device Support\Shared";

        public override bool DetectIphoneSupport()
        {
            try
            {
                using (var reg = Registry.LocalMachine.OpenSubKey(mIphoneComponentPath, false))
                {
                    if (reg != null)
                    {
                        var iTunesMobileDeviceDLLFilePath = reg.GetValue("iTunesMobileDeviceDLL").ToString();
                        if (iTunesMobileDeviceDLLFilePath != null)
                        {
                            Logger.Logger.GetLogger(typeof(DetectIphoneComponent).Name).Info("ITunesMobileDeviceDLLFilePath " + iTunesMobileDeviceDLLFilePath);
                            if (File.Exists(iTunesMobileDeviceDLLFilePath))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.Logger.GetLogger(typeof(DetectIphoneComponent).Name).ErrorFormat("AppleMobileDeviceSupport", ex);
            }
            return false;
        }

        public override bool IsNewestVersion()
        {
            try
            {
                var versionRegPath = @"SOFTWARE\Apple Inc.\Apple Mobile Device Support";
                var currentVersion = getCurrentVersion(versionRegPath);
                return isNewestVersion(mNewestVersion, currentVersion);
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(DetectIphoneComponent).Name).ErrorFormat("AppleMobileDeviceSupport IsNewestVersion", ex);
            }
            return false;
        }
    }
}
