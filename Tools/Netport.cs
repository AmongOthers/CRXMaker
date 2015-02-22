using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using System.Net;

namespace Tools
{
    public class Netport
    {
        public enum MyProtocolType
        {
            TCP,
            UDP
        }
        public const MyProtocolType TYPE_TCP = MyProtocolType.TCP;
        public const MyProtocolType TYPE_UDP = MyProtocolType.UDP;
        private const string TYPE_TCP_STR = "TCP";
        private const string TYPE_UDP_STR = "UDP";
        private const string TYPE_STUB = "$TYPE";
        private const string PORT_STUB = "$PORT";
        private static string NETSTAT_PATTERN_STR =
            TYPE_STUB + "\\s*[0-9.]*:" + PORT_STUB + ".*\\s([0-9]+)$";
        /*
         * @post:
         *  result > 0 获取成功
         *  result <=0 获取失败，请检查errno
         */
        public const int PROCESS_NOT_FOUND = -1;
        public const int NETSTATEJ_FAILED = -2;
        public static int getProcessIdListeningOnPort(MyProtocolType type, int port)
        {
            string HEAD_STR = null;
            if (type == MyProtocolType.TCP)
            {
                HEAD_STR = TYPE_TCP_STR;
            }
            else
            {
                HEAD_STR = TYPE_UDP_STR;
            }
            LxPredicate retryPredicate = new LxPredicate((o_shellErrno) =>
            {
                CommandErrno lamda_errno = o_shellErrno as CommandErrno;
                return lamda_errno.ShellCode != CommandErrno.FINISHED ||
                    lamda_errno.ExitCode != 0;
            });
            LxProducer producer = new LxProducer(() =>
            {
                CommandErrno lamda_errno =
                    CommandHelper.excute("netstat", "-ano",
                    ExecutorControl.INDEFINITELY);
                return lamda_errno;
            });
            LxRetryMachine retryMachine = new LxRetryMachine(retryPredicate, producer);
            CommandErrno shellErrno = retryMachine.run(5, 100) as CommandErrno;
            if (!retryPredicate(shellErrno))
            {

                NETSTAT_PATTERN_STR = NETSTAT_PATTERN_STR.
                    Replace(TYPE_STUB, HEAD_STR).
                    Replace(PORT_STUB, port.ToString());
                Regex regex = new Regex(NETSTAT_PATTERN_STR);
                StringReader temp = new StringReader(shellErrno.Output);
                string line = null;
                Match match = null;
                while ((line = temp.ReadLine()) != null)
                {
                    match = regex.Match(line);
                    if (match.Success)
                    {
                        int id = int.Parse(match.Groups[1].Value);
                        return id;
                    }
                }
                return -1;
            }
            else
            {
                return -2;
            }
        }

        public static int GetFreePortInsideDynamicPort()
        {
            return GetFreePortBetween(60000, 65535);
        }

        public static int GetFreePortBetween(int begin, int end)
        {
            for(int i = begin; i <= end; i++)
            {
                if (isTcpPortAvailable(i))
                {
                    return i;
                }
            }
            return -1;
        }

        public static int GetFreePortFrom(int port)
        {
            return GetFreePortBetween(port, 65535);
        }

        public static bool isTcpPortAvailable(int port)
        {
            bool result = true;
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpEndPoints = properties.GetActiveTcpListeners();
            foreach (IPEndPoint p in tcpEndPoints)
            {
                if (p.Port == port)
                {
                    result = false;
                    break;
                }
            }
            return result;
        }
    }
}
