using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Tools
{
    public class CommandHelper
    {
        public static CommandErrno excute(string exeName, string args, bool isToWait = true)
        {
            ExecutorControl control = new ExecutorControl(String.Format("{0} {1}", exeName, args));
            CommandExecutor excutor = new CommandExecutor(control, exeName, args);
            if (excutor.Start())
            {
				if (isToWait)
				{
					control.waitDone();
				}
				return control.ShellErrno;
            }
            return new CommandErrno(CommandErrno.STOPPED);
        }

        public static CommandErrno excute(string exeName, string args, out ExecutorControl control)
        {
            control = new ExecutorControl(String.Format("{0} {1}", exeName, args));
            CommandExecutor excutor = new CommandExecutor(control, exeName, args);
            if (excutor.Start())
            {
				control.waitDone();
				return control.ShellErrno;
            }
            return new CommandErrno(CommandErrno.STOPPED);
        }

        public static CommandErrno excute(string exeName, string args, int ms)
        {
            ExecutorControl control = new ExecutorControl(String.Format("{0} {1}", exeName, args));
            CommandExecutor excutor = new CommandExecutor(control, exeName, args);
            if (excutor.Start())
            {
				control.waitDone(ms);
				return control.ShellErrno;
            }
            return new CommandErrno(CommandErrno.STOPPED);
        }

        public static CommandErrno excute(string exeName, string args, int ms, out ExecutorControl control, Encoding encoding = null)
        {
            control = new ExecutorControl(String.Format("{0} {1}", exeName, args));
            CommandExecutor excutor = new CommandExecutor(control, exeName, args, encoding);
            if (excutor.Start())
            {
				control.waitDone(ms);
				return control.ShellErrno;
            }
            return new CommandErrno(CommandErrno.STOPPED);
        }

        public static CommandErrno excuteSync(string exeName, string args, Encoding encoding = null)
        {
            CommandErrno cmdErrno = null;
            try
            {
				using (Process process = new Process())
				{
					process.StartInfo.FileName = exeName;
					process.StartInfo.Arguments = args;
					process.StartInfo.CreateNoWindow = true;
					process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.RedirectStandardError = true;
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.StandardOutputEncoding = encoding;
					process.StartInfo.StandardErrorEncoding = encoding;
					process.Start();
					string outputMsg = process.StandardOutput.ReadToEnd();
					string errorMsg = process.StandardError.ReadToEnd();
					process.WaitForExit();
					cmdErrno = new CommandErrno(CommandErrno.FINISHED, 0, outputMsg, errorMsg);
				}
            }
			catch(Exception ex)
            {
                Logger.Logger.GetLogger("CommandHelper").Error("excuteSync exception", ex);
                cmdErrno = new CommandErrno(CommandErrno.STOPPED);
			}
            return cmdErrno;
        }
    }
}
