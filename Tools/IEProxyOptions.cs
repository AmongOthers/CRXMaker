using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Tools
{
    public class IEProxyOptions
    {
        private const int INTERNET_OPTION_REFRESH = 0x000025;
        private const int INTERNET_OPTION_SETTINGS_CHANGED = 0x000027;
        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lPBuffer, int lpdwBufferLength);

        /// <summary>
        /// IE代理 自动检测设置
        /// </summary>
        /// <param name="set">true选中,flase不选中</param>
        public static void IEAutoDetectProxy(bool set)
        {
            try
            {
                if (CheckIEProxyEnable())
                {
                    Logger.Logger.GetLogger(typeof(IEProxyOptions).Name).Info("修改IE代理自动检测设置为:" + set);
                    RegistryKey RegKey = Registry.CurrentUser.OpenSubKey(@"Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections", true);
                    byte[] defConnection = (byte[])RegKey.GetValue("DefaultConnectionSettings");
                    var val = defConnection[8];
                    if (val >= 8)
                    {
                        defConnection[8] = Convert.ToByte(val - 8);
                    }
                    RegKey.SetValue("DefaultConnectionSettings", defConnection);
                    refreshIE();//不关闭IE都能使IE设置生效或失效
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(IEProxyOptions).Name).Error(ex);
            }
        }

        public static bool CheckIEProxyEnable()
        {
            try
            {
                string internet_Settings_Path = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
                using (RegistryKey regInternetSettings = Registry.CurrentUser.OpenSubKey(internet_Settings_Path, true))//获得IE设置
                {
                    int proxy_enable = Convert.ToInt32(regInternetSettings.GetValue("ProxyEnable"));
                    if (proxy_enable == 0)
                    {
                        Logger.Logger.GetLogger(typeof(IEProxyOptions).Name).Info("检测到IE代理已启用");
                        return false;
                    }
                    else
                    {
                        Logger.Logger.GetLogger(typeof(IEProxyOptions).Name).Info("检测到IE代理没启用");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {             
                Logger.Logger.GetLogger(typeof(IEProxyOptions).Name).Error(ex);
                return false;
            }
        }

        public static bool CheckIEAutoDetectProxy()
        {
            try
            {
                RegistryKey RegKey = Registry.CurrentUser.OpenSubKey(@"Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections", true);
                byte[] defConnection = (byte[])RegKey.GetValue("DefaultConnectionSettings");
                var val = defConnection[8];
                if (val >= 8)
                {
                    Logger.Logger.GetLogger(typeof(IEProxyOptions).Name).Info("检测到IE代理 自动检测设置 项是选中的");
                    return true;
                }
                else
                {
                    Logger.Logger.GetLogger(typeof(IEProxyOptions).Name).Info("检测到IE代理 自动检测设置 项没选中");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(IEProxyOptions).Name).Error(ex);
                return false;
            }
        }

        public static void PopIEProxyDialog()
        {
            try
            {
                System.Diagnostics.Process.Start("inetcpl.cpl", ",4");
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(IEProxyOptions).Name).Error(ex);
            }
        }

        private static void refreshIE()
        {
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }
    }
}
