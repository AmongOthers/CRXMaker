using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    [Flags]
    public enum ApkInstallLocationMode
    {
        /// <summary>
        /// 不添加安装参数 1
        /// </summary>
        InstallerWithoutAnyParameter = 1,
        /// <summary>
        /// 安装到SD卡 2
        /// </summary>
        SdInstaller = 2,
        /// <summary>
        /// 添加-F安装参数 4
        /// </summary>
        MemoryInstallerWithFParameter = 4
    }
}
