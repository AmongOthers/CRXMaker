using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public class FtpTool
    {
        /// <summary>
        /// 用系统的资源管理器打开Ftp地址
        /// </summary>
        /// <param name="ftpUrl">Ftp地址</param>
        /// <returns></returns>
        public static bool OpenFtpByExplorer(string ftpUrl)
        {
            try
            {
                ftpUrl = ftpUrl.Trim();
                if (!ftpUrl.StartsWith("ftp://", StringComparison.InvariantCultureIgnoreCase))
                {
                    ftpUrl = "ftp://" + ftpUrl;
                }
                System.Diagnostics.Process.Start("Explorer.exe", ftpUrl);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
