using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    /*
     * 将异常包装成错误信息，某些方法内部发生异常，但是该方法不适合抛出异常，
     * 但是上层需要知道具体的问题，例如，用于日志，可以使用这个类进行封装
     */
    public class ExceptionErrno : LxErrno
    {
        private Exception mOpException;
        public Exception OpException
        {
            get
            {
                return mOpException;
            }
        }
        public ExceptionErrno(Exception e)
        {
            mOpException = e;
        }
    }
}
