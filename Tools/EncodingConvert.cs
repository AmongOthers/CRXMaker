using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public class EncodingConvert
    {
        public static string convert(Encoding srcEncoding, Encoding dstEncoding, string src)
        {
            byte[] srcBytes = srcEncoding.GetBytes(src);
            byte[] dstBytes = Encoding.Convert(srcEncoding, dstEncoding, srcBytes);
            return dstEncoding.GetString(dstBytes);
        }
    }
}
