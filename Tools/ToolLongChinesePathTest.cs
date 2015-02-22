using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Tools
{
    public class ToolLongChinesePathTest
    {
        //[^x00-xff]匹配双字节字符(包括中文)
        private static Regex TOO_LONG_CHINESE_PATH_REGEX = new Regex(
            "[^\x00-\xff].*[^\x00-\xff].*[^\x00-\xff].*[^\x00-\xff].*[^\x00-\xff].*[^\x00-\xff]");
        public static bool IsTooLongChinesePath(String path)
        {
            return TOO_LONG_CHINESE_PATH_REGEX.Match(path).Success;
        }
    }
}
