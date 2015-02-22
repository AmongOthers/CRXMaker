using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Tools
{
    public class VersionComparer
    {
        public static int Compare(string left, string right)
        {
            if (left.Equals(right))
            {
                return 0;
            }
            return parseVersionStr(left).CompareTo(parseVersionStr(right));
        }

        static readonly Regex DIGITS_STR_REGEX = new Regex("[0-9.]*");
        static double parseVersionStr(string versionStr)
        {
            var match = DIGITS_STR_REGEX.Match(versionStr);
            var digitsStr = match.Groups[0].Value;
            var digitsParts = digitsStr.Split('.');
			//不合法的版本格式
            if (String.IsNullOrEmpty(digitsParts[0]))
            {
                return -1;
            }
			double partBase = 1.0;
            double partValue = 0.0;
            double result = 0.0;
            foreach (var part in digitsParts)
            {
                partValue = int.Parse(part) * partBase;
                result += partValue;
                partBase *= 0.1;
            }
            return result;
        }
    }
}
