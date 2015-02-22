using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Tools
{
    public class LivingShell
    {
        public delegate void OnMsg(string msg);
        public delegate void OnExit();

        public event OnMsg Output;
        public event OnMsg Error;
        public event OnExit Exit;
        private Process _cmdProcess;

        public LivingShell(string exeName, string args, Encoding encoding)
        {
            _cmdProcess = new Process();
            _cmdProcess.StartInfo.FileName = exeName;
            _cmdProcess.StartInfo.Arguments = args;
            _cmdProcess.StartInfo.CreateNoWindow = true;
            _cmdProcess.StartInfo.UseShellExecute = false;
            _cmdProcess.StartInfo.StandardOutputEncoding = encoding;
            _cmdProcess.StartInfo.StandardErrorEncoding = encoding;
            _cmdProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            _cmdProcess.StartInfo.RedirectStandardOutput = true;
            _cmdProcess.OutputDataReceived += (object sender, DataReceivedEventArgs data) =>
            {
                if (Output != null)
                {
                    Output(data.Data);
                }
            };

            _cmdProcess.StartInfo.RedirectStandardError = true;
            _cmdProcess.ErrorDataReceived += (object sender, DataReceivedEventArgs data) =>
            {
                if (Error != null)
                {
                    Error(data.Data);
                }
            };

            _cmdProcess.EnableRaisingEvents = true;
            _cmdProcess.Exited += (object sender, EventArgs data) =>
            {
                if (Exit != null)
                {
                    Exit();
                }
            };
        }

        public bool start()
        {
            try
            {
                _cmdProcess.Start();
				_cmdProcess.BeginOutputReadLine();
				_cmdProcess.BeginErrorReadLine();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Logger.GetLogger(this).Error("LivingShell exception", ex);
                return false;
            }
        }

        public void stop()
        {
            try
            {
                _cmdProcess.Kill();
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }
        }
    }
}
