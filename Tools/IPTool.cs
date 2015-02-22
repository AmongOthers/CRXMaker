using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace Tools
{
    public class IPTool
    {
        static string sIpAddress = null;
        public static String FindLocalIp()
        {
            if (sIpAddress != null)
            {
                return sIpAddress;
            }
            foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (!address.IsIPv6LinkLocal && check_ip_valid(address.ToString()))
                {
                    sIpAddress = address.ToString();
                    break;
                }
            }
            return sIpAddress;
        }

        /// <summary>
        /// 是否有一个合法的IPv4地址
        /// </summary>
        /// <returns></returns>
        public static bool IsHasLegalIPv4Address()
        {
            bool isHasLegalIPv4Address = false;
            try
            {
                foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (!address.IsIPv6LinkLocal && check_ip_valid(address.ToString()))
                    {
                        isHasLegalIPv4Address = true;
                        break;
                    }
                }
            }
            catch (Exception)
            {
            }
            return isHasLegalIPv4Address;
        }

        static readonly Func<String>[] GET_MAC_ADDRESS_FUNCS = new Func<String>[] {
			GetMacAddressByWMI, 
			GetMacAddressByNetBios,
			GetMacAddressBySendARP,
			GetMacAddressByAdapter,
			GetMacAddressByDos
        };
        static string sMacAddress = null;
        public static String FindMacAddr()
        {
            if (sMacAddress != null)
            {
				return sMacAddress;
            }

            //String result = null;
            //ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            //ManagementObjectCollection moc2 = mc.GetInstances();
            //foreach (ManagementObject mo in moc2)
            //{
            //    if (result == null)
            //    {
            //        if ((bool)mo["IPEnabled"] == true)
            //        {
            //            result = mo["MacAddress"].ToString();
            //        }
            //    }
            //}
            //return result;

            foreach (var func in GET_MAC_ADDRESS_FUNCS)
            {
                if (!String.IsNullOrEmpty(sMacAddress = func()))
                {
                    break;
                }
            }

            return sMacAddress;
        }

        private static bool check_ip_valid(string ip_str)
        {
            if (ip_str.Contains("127.0.0.1"))
            {
                return false;
            }
            string[] strArray = ip_str.Split(new char[] { '.' });
            int num = 0;
            foreach (string str in strArray)
            {
                int num2;
                num++;
                string s = str.Trim();
                try
                {
                    num2 = int.Parse(s);
                }
                catch (Exception)
                {
                    return false;
                }
                if ((num2 < 0) || (num2 >= 256))
                {
                    return false;
                }
            }
            if (num != 4)
            {
                return false;
            }
            return true;
        }



        [DllImport("NETAPI32.DLL")]
        public static extern char Netbios(ref   NCB ncb);

        [DllImport("Iphlpapi.dll")]
        static extern int SendARP(Int32 DestIP, Int32 SrcIP, ref Int64 MacAddr, ref Int32 PhyAddrLen);

        [DllImport("Iphlpapi.dll")]
        public static extern uint GetAdaptersAddresses(uint Family, uint flags, IntPtr Reserved, IntPtr PAdaptersAddresses, ref uint pOutBufLen);


        /// <summary>
        /// 通过WMI获得电脑的mac地址
        /// </summary>
        /// <returns></returns>
        public static string GetMacAddressByWMI()
        {
            string mac = string.Empty;
            try
            {
                ManagementObjectSearcher query = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = 'TRUE'");
                ManagementObjectCollection queryCollection = query.Get();

                foreach (ManagementObject mo in queryCollection)
                {
                    if (mo["IPEnabled"].ToString() == "True")
                    {
                        mac = mo["MacAddress"].ToString();
                        break;
                    }
                }
            }
            catch
            {
            }
            return mac;
        }

        /// <summary>
        /// 通过NetBios获取MAC地址
        /// </summary>
        /// <returns></returns>
        public static string GetMacAddressByNetBios()
        {
            string macAddress = string.Empty;
            try
            {
                string addr = string.Empty;
                int cb;
                ASTAT adapter;
                NCB Ncb = new NCB();
                char uRetCode;
                LANA_ENUM lenum;

                Ncb.ncb_command = (byte)NCBCONST.NCBENUM;
                cb = Marshal.SizeOf(typeof(LANA_ENUM));
                Ncb.ncb_buffer = Marshal.AllocHGlobal(cb);
                Ncb.ncb_length = (ushort)cb;
                uRetCode = Netbios(ref   Ncb);
                lenum = (LANA_ENUM)Marshal.PtrToStructure(Ncb.ncb_buffer, typeof(LANA_ENUM));
                Marshal.FreeHGlobal(Ncb.ncb_buffer);
                if (uRetCode != (short)NCBCONST.NRC_GOODRET)
                    return "";

                for (int i = 0; i < lenum.length; i++)
                {
                    Ncb.ncb_command = (byte)NCBCONST.NCBRESET;
                    Ncb.ncb_lana_num = lenum.lana[i];
                    uRetCode = Netbios(ref   Ncb);
                    if (uRetCode != (short)NCBCONST.NRC_GOODRET)
                        return "";

                    Ncb.ncb_command = (byte)NCBCONST.NCBASTAT;
                    Ncb.ncb_lana_num = lenum.lana[i];
                    Ncb.ncb_callname[0] = (byte)'*';
                    cb = Marshal.SizeOf(typeof(ADAPTER_STATUS)) + Marshal.SizeOf(typeof(NAME_BUFFER)) * (int)NCBCONST.NUM_NAMEBUF;
                    Ncb.ncb_buffer = Marshal.AllocHGlobal(cb);
                    Ncb.ncb_length = (ushort)cb;
                    uRetCode = Netbios(ref   Ncb);
                    adapter.adapt = (ADAPTER_STATUS)Marshal.PtrToStructure(Ncb.ncb_buffer, typeof(ADAPTER_STATUS));
                    Marshal.FreeHGlobal(Ncb.ncb_buffer);

                    if (uRetCode == (short)NCBCONST.NRC_GOODRET)
                    {
                        if (i > 0)
                            addr += ":";
                        addr = string.Format("{0,2:X}:{1,2:X}:{2,2:X}:{3,2:X}:{4,2:X}:{5,2:X}",
                              adapter.adapt.adapter_address[0],
                              adapter.adapt.adapter_address[1],
                              adapter.adapt.adapter_address[2],
                              adapter.adapt.adapter_address[3],
                              adapter.adapt.adapter_address[4],
                              adapter.adapt.adapter_address[5]);
                    }
                }
                macAddress = addr.Replace(' ', '0');

            }
            catch
            {
            }

            return macAddress;
        }

        /// <summary>
        /// SendArp获取MAC地址
        /// </summary>
        /// <returns></returns>
        public static string GetMacAddressBySendARP()
        {
            StringBuilder strReturn = new StringBuilder();
            try
            {
                System.Net.IPHostEntry Tempaddr = (System.Net.IPHostEntry)Dns.GetHostByName(Dns.GetHostName());
                System.Net.IPAddress[] TempAd = Tempaddr.AddressList;
                Int32 remote = (int)TempAd[0].Address;
                Int64 macinfo = new Int64();
                Int32 length = 6;
                SendARP(remote, 0, ref macinfo, ref length);

                string temp = System.Convert.ToString(macinfo, 16).PadLeft(12, '0').ToUpper();

                int x = 12;
                for (int i = 0; i < 6; i++)
                {
                    if (i == 5) { strReturn.Append(temp.Substring(x - 2, 2)); }
                    else { strReturn.Append(temp.Substring(x - 2, 2) + ":"); }
                    x -= 2;
                }

                return strReturn.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 通过适配器信息获取MAC地址
        /// </summary>
        /// <returns></returns>
        public static string GetMacAddressByAdapter()
        {
            string macAddress = string.Empty;
            try
            {
                IntPtr PAdaptersAddresses = new IntPtr();

                uint pOutLen = 100;
                PAdaptersAddresses = Marshal.AllocHGlobal(100);
                uint ret = GetAdaptersAddresses(0, 0, (IntPtr)0, PAdaptersAddresses, ref pOutLen);

                if (ret == 111)
                {
                    Marshal.FreeHGlobal(PAdaptersAddresses);
                    PAdaptersAddresses = Marshal.AllocHGlobal((int)pOutLen);
                    ret = GetAdaptersAddresses(0, 0, (IntPtr)0, PAdaptersAddresses, ref pOutLen);
                }

                IP_Adapter_Addresses adds = new IP_Adapter_Addresses();

                IntPtr pTemp = PAdaptersAddresses;

                while (pTemp != (IntPtr)0)
                {
                    Marshal.PtrToStructure(pTemp, adds);
                    string adapterName = Marshal.PtrToStringAnsi(adds.AdapterName);
                    string FriendlyName = Marshal.PtrToStringAuto(adds.FriendlyName);
                    string tmpString = string.Empty;

                    for (int i = 0; i < 6; i++)
                    {
                        tmpString += string.Format("{0:X2}", adds.PhysicalAddress[i]);
                        if (i < 5)
                        {
                            tmpString += ":";
                        }
                    }

                    RegistryKey theLocalMachine = Registry.LocalMachine;
                    RegistryKey theSystem = theLocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces");
                    RegistryKey theInterfaceKey = theSystem.OpenSubKey(adapterName);

                    if (theInterfaceKey != null)
                    {
                        macAddress = tmpString;
                        break;
                    }

                    pTemp = adds.Next;
                }
            }
            catch
            {
            }
            return macAddress;
        }

        /// <summary>
        /// 通过DOS命令获得MAC地址
        /// </summary>
        /// <returns></returns>
        public static string GetMacAddressByDos()
        {
            string macAddress = string.Empty;
            Process p = null;
            StreamReader reader = null;
            try
            {
                ProcessStartInfo start = new ProcessStartInfo("cmd.exe");

                start.FileName = "ipconfig";
                start.Arguments = "/all";
                start.CreateNoWindow = true;
                start.RedirectStandardOutput = true;
                start.RedirectStandardInput = true;
                start.UseShellExecute = false;

                p = Process.Start(start);
                reader = p.StandardOutput;

                string line = reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    if (line.ToLower().IndexOf("physical address") > 0 || line.ToLower().IndexOf("物理地址") > 0)
                    {
                        int index = line.IndexOf(":");
                        index += 2;
                        macAddress = line.Substring(index);
                        macAddress = macAddress.Replace('-', ':');
                        break;
                    }
                    line = reader.ReadLine();
                }
            }
            catch
            {
            }
            finally
            {
                if (p != null)
                {
                    p.WaitForExit();
                    p.Close();
                }
                if (reader != null)
                {
                    reader.Close();
                }
            }
            return macAddress;
        }
    }




    [StructLayout(LayoutKind.Sequential)]
    public struct ADAPTER_STATUS
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] adapter_address;
        public byte rev_major;
        public byte reserved0;
        public byte adapter_type;
        public byte rev_minor;
        public ushort duration;
        public ushort frmr_recv;
        public ushort frmr_xmit;
        public ushort iframe_recv_err;
        public ushort xmit_aborts;
        public uint xmit_success;
        public uint recv_success;
        public ushort iframe_xmit_err;
        public ushort recv_buff_unavail;
        public ushort t1_timeouts;
        public ushort ti_timeouts;
        public uint reserved1;
        public ushort free_ncbs;
        public ushort max_cfg_ncbs;
        public ushort max_ncbs;
        public ushort xmit_buf_unavail;
        public ushort max_dgram_size;
        public ushort pending_sess;
        public ushort max_cfg_sess;
        public ushort max_sess;
        public ushort max_sess_pkt_size;
        public ushort name_count;
    }

    public enum NCBCONST
    {
        NCBNAMSZ = 16,
        MAX_LANA = 254,
        NCBENUM = 0x37,
        NRC_GOODRET = 0x00,
        NCBRESET = 0x32,
        NCBASTAT = 0x33,
        NUM_NAMEBUF = 30,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NAME_BUFFER
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)NCBCONST.NCBNAMSZ)]
        public byte[] name;
        public byte name_num;
        public byte name_flags;
    }


    [StructLayout(LayoutKind.Auto)]
    public struct ASTAT
    {
        public ADAPTER_STATUS adapt;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)NCBCONST.NUM_NAMEBUF)]
        public NAME_BUFFER[] NameBuff;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NCB
    {
        public byte ncb_command;
        public byte ncb_retcode;
        public byte ncb_lsn;
        public byte ncb_num;
        public IntPtr ncb_buffer;
        public ushort ncb_length;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)NCBCONST.NCBNAMSZ)]
        public byte[] ncb_callname;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)NCBCONST.NCBNAMSZ)]
        public byte[] ncb_name;
        public byte ncb_rto;
        public byte ncb_sto;
        public IntPtr ncb_post;
        public byte ncb_lana_num;
        public byte ncb_cmd_cplt;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] ncb_reserve;
        public IntPtr ncb_event;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LANA_ENUM
    {
        public byte length;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)NCBCONST.MAX_LANA)]
        public byte[] lana;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class IP_Adapter_Addresses
    {
        public uint Length;
        public uint IfIndex;
        public IntPtr Next;

        public IntPtr AdapterName;
        public IntPtr FirstUnicastAddress;
        public IntPtr FirstAnycastAddress;
        public IntPtr FirstMulticastAddress;
        public IntPtr FirstDnsServerAddress;

        public IntPtr DnsSuffix;
        public IntPtr Description;

        public IntPtr FriendlyName;

        [MarshalAs(UnmanagedType.ByValArray,
             SizeConst = 8)]
        public Byte[] PhysicalAddress;

        public uint PhysicalAddressLength;
        public uint flags;
        public uint Mtu;
        public uint IfType;

        public uint OperStatus;

        public uint Ipv6IfIndex;
        public uint ZoneIndices;

        public IntPtr FirstPrefix;
    }
}
