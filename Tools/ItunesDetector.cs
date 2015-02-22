using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Tools
{
    public static class ItunesDetector
    {
        public static bool CheckItunes()
        {
            if (IntPtr.Size == 4)
            {
                //32
                return CheckOnOS32();
            }
            else
            {
                //64
                return CheckOnOS64();
            }
        }

        private static bool CheckOnOS32()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Apple Computer, Inc.\iPod\RegisteredApps\4");
            if (key == null)
                return false;

            object value = key.GetValue("path");
            if (value == null)
                return false;

            var path = value.ToString();
            if (!System.IO.File.Exists(path))
                return false;

            //var version = new Version(FileVersionInfo.GetVersionInfo(path).FileVersion);
            //if (version == new Version("11.2.2.3"))
            //    return true;

            //return false;
            return true;
        }

        private static bool CheckOnOS64()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Apple Computer, Inc.\iPod\RegisteredApps\4");
            if (key == null)
                return false;

            object value = key.GetValue("path");
            if (value == null)
                return false;

            var path = value.ToString();
            if (!System.IO.File.Exists(path))
                return false;

            var version = new Version(FileVersionInfo.GetVersionInfo(path).FileVersion);
            if (version == new Version("11.2.2.3"))
                return true;

            return false;
        }
    }
}
