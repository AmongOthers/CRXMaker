using System;
using System.Collections.Generic;
using System.Text;
using System.Management;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Tools
{
    public class CheckWinVersion
    {
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool lpSystemInfo);

        public static string sOsInfo = null;

        public static int getOSArchitecture()
        {
            string pa = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            return ((String.IsNullOrEmpty(pa) || String.Compare(pa, 0, "x86", 0, 3, true) == 0) ? 32 : 64);
        }

        private static string sOSbits;
        public static string Detect3264()
        {
            if (sOSbits != null)
            {
                return sOSbits;
            }
            try
            {
                ConnectionOptions oConn = new ConnectionOptions();
                System.Management.ManagementScope oMs = new System.Management.ManagementScope("\\\\localhost", oConn);
                System.Management.ObjectQuery oQuery = new System.Management.ObjectQuery("select AddressWidth from Win32_Processor");
                ManagementObjectSearcher oSearcher = new ManagementObjectSearcher(oMs, oQuery);
                ManagementObjectCollection oReturnCollection = oSearcher.Get();
                string addressWidth = null;

                foreach (ManagementObject oReturn in oReturnCollection)
                {
                    addressWidth = oReturn["AddressWidth"].ToString();
                }

                sOSbits = addressWidth;
            }
            catch (Exception)
            {
                try
                {
                    bool retVal;
                    IsWow64Process(Process.GetCurrentProcess().Handle, out retVal);
                    if (retVal)
                        sOSbits = "64";
                    else
                        sOSbits = "32";
                }
                catch (Exception)
                {
                    //如果所有方法都获取不了系统的位数，那么我们认为是32位的
                    sOSbits = "32";
                }
            }
            return sOSbits;
        }
        public static string getOSInfo()
        {
            if (sOsInfo != null)
            {
                return sOsInfo;
            }
            //Get Operating system information.  
            OperatingSystem os = Environment.OSVersion;
            //Get version information about the os.  
            Version vs = os.Version;

            //Variable to hold our return value  
            string operatingSystem = "";

            if (os.Platform == PlatformID.Win32Windows)
            {
                //This is a pre-NT version of Windows  
                switch (vs.Minor)
                {
                    case 0:
                        operatingSystem = "95";
                        break;
                    case 10:
                        if (vs.Revision.ToString() == "2222A")
                            operatingSystem = "98SE";
                        else
                            operatingSystem = "98";
                        break;
                    case 90:
                        operatingSystem = "Me";
                        break;
                    default:
                        break;
                }
            }
            else if (os.Platform == PlatformID.Win32NT)
            {
                switch (vs.Major)
                {
                    case 3:
                        operatingSystem = "NT 3.51";
                        break;
                    case 4:
                        operatingSystem = "NT 4.0";
                        break;
                    case 5:
                        if (vs.Minor == 0)
                            operatingSystem = "2000";
                        else
                            operatingSystem = "XP";
                        break;
                    case 6:
                        if (vs.Minor == 0)
                            operatingSystem = "Vista";
                        else if (vs.Minor == 2 || vs.Minor == 3)
                            operatingSystem = "8";
                        else
                            operatingSystem = "7";
                        break;
                    default:
                        break;
                }
            }
            //Make sure we actually got something in our OS check  
            //We don't want to just return " Service Pack 2" or " 32-bit"  
            //That information is useless without the OS version.  
            if (operatingSystem != "")
            {
                //Got something.  Let's prepend "Windows" and get more info.  
                operatingSystem = "Windows_" + operatingSystem;
                //See if there's a service pack installed.  
                //if (os.ServicePack != "")
                //{
                //    //Append it to the OS name.  i.e. "Windows XP Service Pack 3"  
                //    operatingSystem += " " + os.ServicePack;
                //}
                //Append the OS architecture.  i.e. "Windows XP Service Pack 3 32-bit"  
                operatingSystem += "_" + Detect3264() + "-bit";
            }
            //Return the information we've gathered.  
            return operatingSystem.ToUpper();
        }
    } 

}
