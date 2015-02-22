using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace Tools
{
    public class TabletTip
    {
        const string TABTIP = "TabTip";
        const string OSK = "osk";
        public static void Disable()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback((_) => {
                try
                {
                    var regPath = @"Software\Microsoft\TabletTip\1.7";
                    using (var reg = Registry.CurrentUser.OpenSubKey(regPath, true))
                    {
                        if (reg != null)
                        {
                            reg.SetValue("EdgeTargetPosition", 0x768f,RegistryValueKind.DWord);
                            reg.SetValue("EnableEdgeTarget", 0, RegistryValueKind.DWord);
                        }
                    }

                    KillProcess(TABTIP);
                    KillProcess(OSK);
                }
                catch (Exception ex)
                {
                    Logger.Logger.GetLogger(typeof(TabletTip).Name).Error("禁用虚拟键盘", ex);
                }
            }));
        }

        public static void Enable()
        {
            try
            {
                Logger.Logger.GetLogger(typeof(TabletTip).Name).Info("启用虚拟键盘");
                var regPath = @"Software\Microsoft\TabletTip\1.7";
                using (var reg = Registry.CurrentUser.OpenSubKey(regPath, true))
                {
                    if (reg != null)
                    {
                        reg.SetValue("EdgeTargetPosition", 0x76ec, RegistryValueKind.DWord);
                        reg.SetValue("EnableEdgeTarget", 1, RegistryValueKind.DWord);
                    }
                }

                KillProcess(TABTIP);
                KillProcess(OSK);
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(TabletTip).Name).Error("启用虚拟键盘出错", ex);
            }
        }

        private static void KillProcess(string processName)
        {
            try
            {
                Process[] list = Process.GetProcessesByName(processName);
                foreach (var item in list)
                {
                    item.Kill();
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(TabletTip).Name).Error("关闭进程：" + processName, ex);
            }
        }
    }
}
