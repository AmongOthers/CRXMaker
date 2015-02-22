using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Tools
{
    public class Taskkill
    {
        public static bool killAllByName(string name)
        {
            try
            {
                killWithTaskkill(name);
                return true;
            }
            catch (Exception)
            {
                try
                {
                    string nameWithoutPostfix = Path.GetFileNameWithoutExtension(name);
                    killWithProcessLookup(nameWithoutPostfix);
                    return true;
                }
                catch (Exception)
                {
                }
            }
            return false;
        }

        public static bool killWithProcessLookup(string name)
        {
            var processes = lookupProcess(name);
            foreach (var item in processes)
            {
                item.Kill();
            }
            return waitForProcessesEmpty(name);
        }

        public static bool killWithTaskkill(string name)
        {
            CommandHelper.excute("taskkill",
                "/F /im " + name, ExecutorControl.INDEFINITELY);
            return waitForProcessesEmpty(name);
        }

        private static bool waitForProcessesEmpty(string name)
        {
            int i = 5;
            while (i-- > 0)
            {
                var temp = lookupProcess(name);
                if (temp.Count == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static List<Process> lookupProcess(string name)
        {
            var processes = new List<Process>(
    Process.GetProcessesByName(name));
            return processes;
        }
    }
}
