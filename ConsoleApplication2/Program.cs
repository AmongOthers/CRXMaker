using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApplication2
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < 100; i++ )
            {
                var id = getKeyCode();
                Console.WriteLine(id);
            }
            Console.Read();
        }

        private static string getKeyCode()
        {
            var timestamp = Tools.EpochHelper.GetCurrentTimeStamp().ToString();
            timestamp = timestamp.Substring(timestamp.Length - 2, 2);
            var guid = Guid.NewGuid().ToString();
            guid = guid.Substring(0, 6);
            var id = timestamp + guid;
            return id;
        }
    }
}
