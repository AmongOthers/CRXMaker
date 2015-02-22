using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Tools
{
    public class IPCUtil
    {
        public const int WM_COPYDATA = 0x004A;

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(
        int hWnd, // handle to destination window
        int Msg, // message
        int wParam, // first message parameter
        ref COPYDATASTRUCT lParam // second message parameter
        );

        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public static extern int FindWindow(string lpClassName, string
        lpWindowName);

        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpData;
        }

        public static string get_string_from_message(Message m)
        {
            if (m.Msg == WM_COPYDATA)
            {
                COPYDATASTRUCT str = new COPYDATASTRUCT();
                Type type = str.GetType();
                str = (COPYDATASTRUCT)m.GetLParam(type);
                return str.lpData;
            }
            return null;
        }

        public static bool send_string_message_to(string window_name, string msg)
        {
            int WINDDOW_HANDLER = IPCUtil.FindWindow(null, window_name);
            if (WINDDOW_HANDLER != 0)
            {
                byte[] sarr = System.Text.Encoding.Default.GetBytes(msg);
                int len = sarr.Length;
                IPCUtil.COPYDATASTRUCT cds;
                cds.dwData = (IntPtr)100;
                cds.lpData = msg;
                cds.cbData = len + 1;
                IPCUtil.SendMessage(WINDDOW_HANDLER, IPCUtil.WM_COPYDATA, 0, ref cds);
                //不知道SendMessage的返回值含义，暂时先return true
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
