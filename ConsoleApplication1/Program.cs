using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            var machineId = GetMachineId();
            Console.WriteLine(machineId);
            Console.Read();
        }

        private static string GetMachineId()
        {
            var machineId = String.Empty;
            var cpuId = String.Empty;
            var boardId = String.Empty;
            //CPU
            try
            {
                System.Management.ManagementClass mc = new ManagementClass("win32_processor");
                ManagementObjectCollection moc = mc.GetInstances();
                if (moc.Count > 0)
                {
                    foreach (var mo in moc)
                    {

                        cpuId = mo["processorid"].ToString();
                        break;
                    }
                }
            }
            catch (Exception)
            {

            }
            machineId += cpuId;
            //主板
            try
            {
                System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");
                foreach (var mo in searcher.Get())
                {
                    boardId = mo["SerialNumber"].ToString().Trim();
                }
            }
            catch (Exception)
            {

            }
            machineId += boardId;
            return machineId;
        }

    }
}
