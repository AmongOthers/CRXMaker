using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Tools
{
    public class DriveCleanup
    {
        public const string REMOVE_USB = @"devcon remove @USB\*";
        public const string RESCAN = "devcon rescan";
        public const string RESTART_SYSTEM = "shutdown -f -r -t 0";
        public const string CLOSE_SYSTEM = "shutdown -f -s -t 0";
        public static void ExecuteCommand(string command)
        {
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardInput = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    if (p.Start())
                    {
                        p.StandardInput.WriteLine("cd " + AppDomain.CurrentDomain.BaseDirectory);
                        p.StandardInput.WriteLine(command);
                        p.StandardInput.WriteLine("exit");
                        p.WaitForExit();                        
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(DriveCleanup).Name).Error("清理USB驱动痕迹异常", ex);
            }
        }

        /// <summary>
        /// 清理其他残留痕迹
        /// </summary>
        public static void RemoveWpdDevice()
        {
            try
            {
                var regPath = String.Empty;
                if (Tools.CheckWinVersion.Detect3264() == "32")
                {
                   regPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\DeviceHandlers";
                }
                else
                {
                    regPath = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\DeviceHandlers";
                }    
                using (var reg = Registry.LocalMachine.OpenSubKey(regPath, true))
                {
                    if (reg != null)
                    {
                       var subKeyNames = reg.GetSubKeyNames();
                       foreach (var item in subKeyNames)
                       {
                           if (item.Contains("WpdDeviceHandler_USB#VID_"))
                           {
                               reg.DeleteSubKeyTree(item);
                           }
                       }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(DriveCleanup).Name).Error("清理系统WpdDevice残留驱动痕迹", ex);
            }
        }

        /// <summary>
        ///  清理驱动安装日志
        /// </summary>
        public static void CleanupDriveLog()
        {
            try
            {
                var windir = String.Format(@"{0}\inf", Environment.GetEnvironmentVariable("windir"));
                var devPath = String.Format(@"{0}\setupapi.dev.log",windir);
                var appPath = String.Format(@"{0}\setupapi.app.log",windir);
                var wmiprovPath = String.Format(@"{0}\wbem\Logs\wmiprov.log",windir);
                if (File.Exists(devPath))
                {
                    File.Delete(devPath);
                }
                if (File.Exists(appPath))
                {
                    File.Delete(appPath);
                }
                if (File.Exists(wmiprovPath))
                {
                    File.Delete(wmiprovPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(DriveCleanup).Name).Error("清理驱动安装日志", ex);
            }
        }

        /// <summary>
        /// 检测USB驱动数据
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static bool CheckUsbMaxSize(int size = 100)
        {
            try
            {
                int usbCount = 0;
                //HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Enum\USB
                var regPath = @"SYSTEM\CurrentControlSet\Enum\USB";

                Logger.Logger.GetLogger(typeof(DriveCleanup).Name).InfoFormat("begin checkUsbMaxSize {0}", regPath);

                using (var reg = Registry.LocalMachine.OpenSubKey(regPath, false))
                {
                    if (reg != null)
                    {
                        var subKeyNames = reg.GetSubKeyNames();
                        Logger.Logger.GetLogger(typeof(DriveCleanup).Name).InfoFormat("{0} subKeyNames count {1}", regPath, subKeyNames.Length);

                        var list = (from key in subKeyNames where key.ToUpper().Contains("VID_") && key.ToUpper().Contains("PID_") && key.Length == 17 select key).ToList();
                        foreach (var usbName in list)
                        {
                            var subRegPath = @"SYSTEM\CurrentControlSet\Enum\USB\" + usbName;
                            var subReg = Registry.LocalMachine.OpenSubKey(subRegPath, false);
                            if (subReg != null)
                            {
                                usbCount += subReg.SubKeyCount;
                            }
                            if (usbCount >= size)
                            {
                                Logger.Logger.GetLogger(typeof(DriveCleanup).Name).InfoFormat("end checkUsbMaxSize {0} Usb Count >= {1}", regPath, usbCount);
                                return true;
                            }
                        }
                    }
                }   
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(typeof(DriveCleanup).Name).Error("检查注册表接入手机个数异常" + ex);
            }
            return false;
        }
    }
}
