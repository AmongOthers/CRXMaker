using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public class Timestamp
    {
        public static string GetCurrentTime() 
        {
            string nowAsFileName = DateTime.Now.ToString("yyMMddHHmmss");
            return nowAsFileName;
        }

        public static string GetOrderCurrentTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
