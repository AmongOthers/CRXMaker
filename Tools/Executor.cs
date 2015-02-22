using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public abstract class Executor
    {
        protected ExecutorControl _control;

        public Executor()
        {
        }
        public Executor(ExecutorControl control)
        {
            _control = control;
            _control.setExcutor(this);
        }
        public abstract bool Start();
        public abstract void Stop();
        public abstract void Cancel(object code);
    }
}
