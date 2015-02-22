using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public class ResouceCleaner : IDisposable
    {
        private LxJob _job;
        private bool _isClosed = true;
        private object _closeLock = new object();

        public bool IsClosed
        {
            get 
            {
                lock (_closeLock)
                {
                    return _isClosed;
                }
            }
        }
        
        public ResouceCleaner(LxJob job)
        {
            _job = job;
            _isClosed = false;
        }

        public void close()
        {
            lock (_closeLock)
            {
                if (!_isClosed)
                {
                    _isClosed = true;
                    _job();
                }
            }
        }

        public void Dispose()
        {
            close();
        }
    }
}
