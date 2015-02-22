using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Tools
{
    public class CommandExecutorEx : Executor
    {
        private string mExeName;
        private string mArgs;
        private List<string> mInputCmdList;
        private Encoding mEncoding;
        private Process mCmdProcess;
        private object mLock = new object();

        public CommandExecutorEx(string exeName, string args, Encoding encoding = null)
            : base()
        {
            mExeName = exeName;
            mArgs = args;
            mEncoding = encoding;
        }

        public void Init(ExecutorControl control, List<string> inputCmdList = null)
        {
            lock (mLock)
            {
                _control = control;
                _control.setExcutor(this);
                mInputCmdList = inputCmdList;
            }
        }

        public override bool Start()
        {
            lock (mLock)
            {
                try
                {
                    if (mCmdProcess == null)
                    {
                        mCmdProcess = new Process();
                        mCmdProcess.EnableRaisingEvents = true;
                        mCmdProcess.StartInfo.FileName = mExeName;
                        mCmdProcess.StartInfo.Arguments = mArgs;
                        mCmdProcess.StartInfo.CreateNoWindow = true;
                        mCmdProcess.StartInfo.UseShellExecute = false;
                        mCmdProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        mCmdProcess.StartInfo.StandardOutputEncoding = mEncoding;
                        mCmdProcess.StartInfo.StandardErrorEncoding = mEncoding;
                        mCmdProcess.StartInfo.RedirectStandardOutput = true;
                        mCmdProcess.StartInfo.RedirectStandardInput = true;
                        mCmdProcess.OutputDataReceived += (sender, data) =>
                        {
                            _control.onOutput(data.Data + Environment.NewLine);
                        };
                        mCmdProcess.StartInfo.RedirectStandardError = true;
                        mCmdProcess.ErrorDataReceived += (sender, data) =>
                        {
                            _control.onError(data.Data + Environment.NewLine);
                        };
                        mCmdProcess.Exited += delegate
                        {
                            int exitCode = -111;
                            lock (mLock)
                            {

                                try
                                {
                                    exitCode = mCmdProcess.ExitCode;
                                }
                                catch (InvalidOperationException)
                                {
                                }
                                mCmdProcess = null;
                            }
                            _control.onExit(exitCode);
                        };
                        mCmdProcess.Start();
                        mCmdProcess.BeginOutputReadLine();
                        mCmdProcess.BeginErrorReadLine();
                    }

                    if (mInputCmdList != null && mInputCmdList.Count != 0)
                    {
                        StreamWriter input = mCmdProcess.StandardInput;
                        foreach (string s in mInputCmdList)
                        {
                            if (!String.IsNullOrEmpty(s))
                            {
                                input.WriteLine(s);
                            }
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Logger.GetLogger(this).Error("CommandExcutor Exception", ex);
                    mCmdProcess = null;
                    return false;
                }
            }
        }

        public override void Stop()
        {
            lock (mLock)
            {
                //if (mCmdProcess != null)
                //{
                //    try
                //    {
                //        mCmdProcess.Kill();
                //    }
                //    catch (Exception)
                //    {
                //    }
                //}
                _control.onStop();
            }
        }

        public override void Cancel(object code)
        {
            lock (mLock)
            {
                //if (mCmdProcess != null)
                //{
                //    try
                //    {
                //        mCmdProcess.Kill();
                //    }
                //    catch (Exception)
                //    {
                //    }
                //}
                _control.onCancel(code);
            }
        }
    }
}
