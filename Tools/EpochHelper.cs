using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public class EpochHelper
    {
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1);

		/// <summary>
        /// Converts a DateTime to Unix Epoch
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns></returns>
        public static long ToUnixEpoch(DateTime date)
        {
            return (long)(date.ToUniversalTime() - Epoch).TotalMilliseconds;
        }

        /// <summary>
        /// Creates a DateTime from the seconds since Epoch
        /// </summary>
        /// <param name="seconds">The seconds.</param>
        /// <returns></returns>
        public static DateTime ToDateTimeFromUnixEpoch(long ms)
        {
			var startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));	
            var time = startTime.AddMilliseconds(ms);
			return time;
        }

        public static long GetCurrentTimeStamp()
        {
            return ToUnixEpoch(DateTime.Now);
        }
    }
}
