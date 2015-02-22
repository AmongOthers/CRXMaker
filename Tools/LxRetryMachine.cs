using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Tools
{
    public class LxRetryMachine
    {
        private LxPredicate mIsGoOnRetrying;
        private LxProducer mProducer;
        private bool _isPassed;
        public LxRetryMachine(LxPredicate isGoOnRetrying, LxProducer producer)
        {
            mIsGoOnRetrying = isGoOnRetrying;
            mProducer = producer;
        }
        public LxRetryMachine(LxJudge isGoOnRetrying, LxJob job)
        {
            mIsGoOnRetrying = new LxPredicate((nullObject) =>
            {
                return isGoOnRetrying();
            });
            mProducer = new LxProducer(() =>
            {
                job();
                return null;
            });
        }
        public bool IsPassed
        {
            get { return _isPassed; }
        }
        public object run(int tryTimes, int sleep)
        {
            _isPassed = false;
            object result = null;
            int temp = tryTimes;
            while (temp-- > 0)
            {
                if (!mIsGoOnRetrying(result = mProducer()))
                {
                    _isPassed = true;
                    break;
                }
                Thread.Sleep(sleep);
            }
            return result;
        }
    }
}
