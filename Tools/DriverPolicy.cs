using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Tools
{
    public static class DriverPolicy
    {
        public static void SettingDriverPolicy(bool isDisable)
        {
            var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"DriverPolicy.exe");
            try
            {
                Logger.Logger.GetLogger("DriverPolicy").DebugFormat("file:{0} args:{1}", fileName, isDisable);
                var p = new Process();
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.FileName = fileName;
                p.StartInfo.Arguments = isDisable == true ? "1" : "0";
                p.Start();
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger("DriverPolicy").Error("驱动安装优化", ex);
            }
        }
    }
}
