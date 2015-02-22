using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Tools
{
    public class CommandExecutor : Executor
    {
        private string _exeName;
        private string _args;
        private Encoding _encoding;
        private Process _cmdProcess;
        private object mLock = new object();

        public CommandExecutor(ExecutorControl control, string exeName, string args, Encoding encoding = null)
            : base(control)
        {
            _exeName = exeName;
            _args = args;
            _encoding = encoding;
        }

        public override bool Start()
        {
            lock (mLock)
            {
                try
                {
					_cmdProcess = new Process();
					_cmdProcess.EnableRaisingEvents = true;
					_cmdProcess.StartInfo.FileName = _exeName;
					_cmdProcess.StartInfo.Arguments = _args;
					_cmdProcess.StartInfo.CreateNoWindow = true;
					_cmdProcess.StartInfo.UseShellExecute = false;
					_cmdProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					_cmdProcess.StartInfo.StandardOutputEncoding = _encoding;
					_cmdProcess.StartInfo.StandardErrorEncoding = _encoding;
					_cmdProcess.StartInfo.RedirectStandardOutput = true;
					_cmdProcess.OutputDataReceived += (sender, data) =>
					{
						_control.onOutput(data.Data + Environment.NewLine);
					};
					_cmdProcess.StartInfo.RedirectStandardError = true;
					_cmdProcess.ErrorDataReceived += (sender, data) =>
					{
						_control.onError(data.Data + Environment.NewLine);
					};
					_cmdProcess.Exited += delegate
					{
						int exitCode = -111;
						try
						{
							exitCode = _cmdProcess.ExitCode;
						}
						catch (InvalidOperationException)
						{
						}
						_control.onExit(exitCode);
					};
					_cmdProcess.Start();
					_cmdProcess.BeginOutputReadLine();
					_cmdProcess.BeginErrorReadLine();
                    return true;

                }
				catch(Exception ex)
                {
                    Logger.Logger.GetLogger(this).Error("CommandExcutor Exception", ex);
                    return false;
				}
            }
        }

        public override void Stop()
        {
            lock (mLock)
            {
                if (_cmdProcess != null)
                {
                    try
                    {
                        _cmdProcess.Kill();
                    }
                    catch (Exception)
                    {
                    }
                }
                _control.onStop();
            }
        }

        public override void Cancel(object code)
        {
            lock (mLock)
            { 
                if (_cmdProcess != null)
                {
                    try
                    {
                        _cmdProcess.Kill();
                    }
                    catch (Exception)
                    {
                    }
                }
                _control.onCancel(code);
            }
        }
    }
}
