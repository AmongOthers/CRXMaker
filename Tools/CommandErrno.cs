using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public class CommandErrno : LxErrno
    {
        public static readonly object FINISHED = "FINISHED";
        public static readonly object TIMEOUT = "TIMEOUT";
        public static readonly object STOPPED = "STOPPED";
        public static readonly object CANCELED = "CANCELED";

        private object _shellCode;
        private string _output;
        private string _error;
        private int _exitCode;
        private object _exCode;
        public object ShellCode
        {
            get { return _shellCode; }
        }
        public int ExitCode
        {
            get { return _exitCode; }
        }
        public string Output
        {
            get { return _output; }
        }
        public string Error
        {
            get { return _error; }
        }
        public object ExCode
        {
            get { return _exCode; }
        }
        public CommandErrno(object shellCode, int exitCode, string output, string error, object exCode = null)
        {
            _shellCode = shellCode;
            _exitCode = exitCode;
            _output = output;
            _error = error;
            _exCode = exCode;
        }
        public CommandErrno(object shellCode, int exitCode, string output = null)
            : this(shellCode, exitCode, output, null)
        {
        }
        public CommandErrno(object shellCode, int exitCode = int.MaxValue)
            : this(shellCode, exitCode, null)
        {
        }
    }
}
