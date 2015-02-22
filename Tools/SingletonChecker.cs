using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace Tools
{
    public class SingletonChecker
    {
        public static void Check()
        {
            var pre_existence = FindPreExistence();
            if (pre_existence != null && pre_existence.Count > 0)
            {
                //DialogResult result = MessageBox.Show(
                //    "对不起, 应用程序已经启动，是否要强制关闭程序？", "提示",
                //    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);

                //if (result == DialogResult.OK)
                //{
                //    kill_process(pre_existence);
                //}
                //else
                //{
                //    Environment.Exit(0);
                //    //Application.Exit();
                //}

                kill_process(pre_existence);
            }
        }

        public static bool HasPreExistence(String processName = null)
        {
            var result = FindPreExistence(processName);
            return result.Count != 0;
        }

        public static List<Process> FindPreExistence(String processName = null)
        {
            processName = processName == null ? Process.GetCurrentProcess().ProcessName : processName;
            if (processName.Contains("vshost"))
            {
                return null;
            }
            var current = Process.GetCurrentProcess();
            var process_list = new List<Process>(
                Process.GetProcessesByName(processName));
            return process_list.FindAll((process) =>
            {
                return process.Id != current.Id;
            });
        }

        private static void kill_process(List<Process> processes)
        {
            foreach (var process in processes)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit();
                    process.Close();
                }
            }
        }
    }
}
