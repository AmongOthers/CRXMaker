using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Tools
{
    public class ExecutorControl
    {
        public const int INDEFINITELY = -1;//无限期
        public delegate bool StopFlagDelegate(string str);
        public delegate bool FilterDelegate(string str);

        private string id;
        public ExecutorControl(string id)
        {
            this.id = id;
        }

        private Executor _excutor;
        public void setExcutor(Executor excutor)
        {
            _excutor = excutor;
        }
        private object _outputLock = new object();
        private int _outputCount = 0;
        public void onOutput(string str) 
        {
            lock (_outputLock)
            {
                _outputCount++;
                if (_outputFilter == null || !_outputFilter(str))
                {
                    _output.Append(str);
                }
                if (_outputFlag != null)
                {
                    if (_outputFlag(Output))
                    {
                        _excutor.Stop();
                    }
                }
            }
        }
        private object _errorLock = new object();
        private int _errorCount = 0;
        public void onError(string str) 
        {
            lock (_errorLock)
            {
                _errorCount++;
                if (_errorFilter == null || !_errorFilter(str))
                {
                    _error.Append(str);
                }
                if (_errorFlag != null)
                {
                    if (_errorFlag(Error))
                    {
                        _excutor.Stop();
                    }
                }
            }
        }
        public void onExit(int exitCode) 
        {
            lock (_doneLock)
            {
                _isExited = true;
                _exitCode = exitCode;
                Monitor.PulseAll(_doneLock);
            }
        }
        public void onStop()
        {
            lock (_doneLock)
            {
                _isStopped = true;
                Monitor.PulseAll(_doneLock);
            }
        }
        public void onCancel(object code)
        {
            lock (_doneLock)
            {
                _isCanceled = true;
                _cancelCode = code;
                Monitor.PulseAll(_doneLock);
            }
        }
        private bool isDone()
        {
            return _isExited || _isStopped || _isCanceled;
        }
        private object _doneLock = new object();
        public void waitDone() 
        {
            lock (_doneLock)
            {
                if (!isDone())
                {
                    Monitor.Wait(_doneLock);
                }
            }
        }
        public void waitDone(int ms)
        {
            if (ms == INDEFINITELY)
            {
                waitDone();
                return;
            }
            lock (_doneLock)
            {
                if (isDone())
                {
                    return;
                }
            }
			//ms是多少秒内没有输出就超时
            int outputCount = _outputCount;
            int errorCount = _errorCount;
            int waitLeftMs = ms;
            while (waitLeftMs > 0)
            {
                Thread.Sleep(10);
                lock (_doneLock)
                {
                    if (isDone())
                    {
                        return;
                    }
                }
                lock (_outputLock)
                {
                    if (outputCount != _outputCount)
                    {
                        outputCount = _outputCount;
                        waitLeftMs = ms;
                        continue;
                    }
                }
                lock (_errorLock)
                {
                    if (errorCount != _errorCount)
                    {
                        errorCount = _errorCount;
                        waitLeftMs = ms;
                        continue;
                    }
                }
                waitLeftMs -= 10;
            }
        }

        public void waitUtilOutputFinished()
        {
            if (!_isExited)
            {
                throw new Exception("程序未退出前本调用无效");
            }
            //如果此时输出为空，那么我们等待5秒直到输出缓冲开始读取
            int waitLeftMs = 5000;
            if (_outputCount == 0)
            {
                while (waitLeftMs > 0)
                {
                    Thread.Sleep(10);
                    lock (_outputLock)
                    {
                        if (_outputCount != 0)
                        {
                            break;
                        }
                    }
                    waitLeftMs -= 10;
                }
            }
            //开始读取输出缓冲后，如果1秒内输出无新的变化，那么可以认为输出已完成
            int outputCount = _outputCount;
            waitLeftMs = 1000;
            while (waitLeftMs > 0)
            {
                Thread.Sleep(10);
                lock (_outputLock)
                {
                    if (outputCount != _outputCount)
                    {
                        outputCount = _outputCount;
                        waitLeftMs = 1000;
                        continue;
                    }
                }
                waitLeftMs -= 10;
            }
            Logger.Logger.GetLogger(this).Debug(this.id + " waitUtilOutputFinished: " + Output);
        }

        private StringBuilder _output = new StringBuilder();
        public string Output 
        {
            get
            {
                lock (_outputLock)
                {
					return _output.ToString();
                }
            }
        }
        private StopFlagDelegate _outputFlag;
        public void setOutputStopFlag(StopFlagDelegate flag)
        {
            _outputFlag = flag;
        }
        private StopFlagDelegate _errorFlag;
        public void setErrorStopFlag(StopFlagDelegate flag)
        {
            _errorFlag = flag;
        }
        private FilterDelegate _outputFilter;
        public void setOutputFilter(FilterDelegate filter)
        {
            _outputFilter = filter;
        }
        private FilterDelegate _errorFilter;
        public void setErrorFilter(FilterDelegate filter)
        {
            _errorFilter = filter;
        }
        private StringBuilder _error = new StringBuilder();
        public string Error 
        {
            get
            {
                lock (_errorLock)
                {
					return _error.ToString();
                }
            }
        }
        private bool _isCanceled = false;
        private object _cancelCode = null;
        private bool _isStopped = false;
        private bool _isExited = false;
        public bool IsExited
        {
            get { return _isExited; }
        }
        public bool IsStopped
        {
            get { return _isStopped; }
        }
        public bool IsCanceled
        {
            get { return _isCanceled; }
        }
        private int _exitCode = -666;
        public int ExitCode 
        {
            get { return _exitCode; }
        }
        public CommandErrno ShellErrno
        {
            get
            {
                object shellErrno = null;
                object exCode = null;
                if (IsExited)
                {
                    shellErrno = CommandErrno.FINISHED;
                }
                else if (IsStopped)
                {
                    shellErrno = CommandErrno.STOPPED;
                }
                else if (IsCanceled)
                {
                    shellErrno = CommandErrno.CANCELED;
                    exCode = _cancelCode;
                }
                else
                {
                    shellErrno = CommandErrno.TIMEOUT;
                }
                return new CommandErrno(shellErrno, ExitCode, Output, Error, exCode);
            }
        }

    }
}
