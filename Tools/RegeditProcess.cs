using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Threading;

namespace Tools
{
    public class RegeditProcess
    {
        public static void RegeditSave()
        {
            string path = System.Environment.CurrentDirectory;
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.StandardInput.WriteLine("cd " + path);
            path = path + "\\regedit";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            if (!File.Exists(path + "\\usb.hiv"))
            {
                p.StandardInput.WriteLine(String.Format(@"psexec.exe /accepteula  -d -s reg save HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\USB {0}\usb.hiv ", path));
            }
            if (!File.Exists(path + "\\umb.hiv"))
            {
                p.StandardInput.WriteLine(String.Format(@"psexec.exe /accepteula  -d -s reg save HKEY_LOCAL_MACHINE\system\CurrentControlSet\enum\wpdbusenumroot\umb {0}\umb.hiv", path));
            }
            if (!File.Exists(path + "\\volume.hiv"))
            {
                p.StandardInput.WriteLine(String.Format(@"psexec.exe /accepteula  -d -s reg save HKEY_LOCAL_MACHINE\system\CurrentControlSet\enum\storage\Volume {0}\volume.hiv", path));
            }
            if (!File.Exists(path + "\\usbflags.hiv"))
            {
                p.StandardInput.WriteLine(String.Format(@"psexec.exe /accepteula  -d -s reg save HKEY_LOCAL_MACHINE\system\CurrentControlSet\control\usbflags {0}\usbflags.hiv", path));
            }
            if (!File.Exists(path + "\\usbstor.hiv"))
            {
                p.StandardInput.WriteLine(String.Format(@"psexec.exe /accepteula  -d -s reg save HKEY_LOCAL_MACHINE\system\CurrentControlSet\enum\usbstor {0}\usbstor.hiv", path));
            }
            p.StandardInput.WriteLine("exit");
            p.WaitForExit();
        }

        public static void RegeditRestore()
        {
            string path = System.Environment.CurrentDirectory;
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.StandardInput.WriteLine("cd " + path);
            string savePath = path + "\\regedit";
            if (Directory.Exists(savePath))
            {
                if (Directory.GetFiles(savePath).Length == 0)
                {
                    return;
                }
                p.StandardInput.WriteLine("restore " + savePath);
                p.WaitForExit();
                p.Start();
                p.StandardInput.WriteLine("cd " + path);
                p.StandardInput.WriteLine("restores " + savePath);
                p.WaitForExit();
                try
                {
                    Directory.Delete(savePath, true);
                }
                catch (System.Exception ex)
                {
                    Logger.Logger.GetLogger(typeof(RegeditProcess)).Error(ex);
                }
            }
        }
    }
}
