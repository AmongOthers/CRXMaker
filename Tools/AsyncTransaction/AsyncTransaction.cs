using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Tools.AsyncTransaction
{
    public class AsyncTransaction
    {
        public virtual bool IsFinished
        {
            get
            {
                return IsSuccessful || IsFailed;
            }
        }
        public virtual bool IsSuccessful { get; protected set; }
        public virtual bool IsFailed { get; protected set; }
        public virtual void Commit() { }

		public AsyncTransaction Or(AsyncTransaction t)
        {
            return new AsyncTransactionOr(this, t);
        }

		public AsyncTransaction And(AsyncTransaction t)
        {
            return new AsyncTransactionAnd(this, t);
        }

        public AsyncTransaction Then(AsyncTransaction t)
        {
			return new AsyncTransactionThen(this, t);
        }

        public bool Wait(int timeout)
        {
            return AsyncTransaction.Wait(timeout, this);
        }

        public void Wait()
        {
            AsyncTransaction.Wait(this);
        }

        protected AsyncTransaction()
        {
        }

        public static AsyncTransaction AlwaysSuccessful()
        {
			var atom = new AsyncTransaction();
            atom.IsSuccessful = true;
			return atom;
        }

        public static AsyncTransaction AlwaysFailed()
        {
			var atom = new AsyncTransaction();
            atom.IsFailed = true;
			return atom;
        }

        const int SLEEP_UNIT = 100;
        //返回True表示在timeout中返回，返回False表示超时了
        public static bool Wait(int timeout, AsyncTransaction t)
        {
            bool hasPassed = t.IsFinished;
            if (hasPassed)
            {
                return true;
            }
            Thread.Sleep(SLEEP_UNIT);
            do
            {
                hasPassed = t.IsFinished;
                if (timeout > 0)
                {
                    timeout -= SLEEP_UNIT;
                    if (timeout <= 0)
                    {
                        return false;
                    }
                }
                Thread.Sleep(SLEEP_UNIT);
            } while (!hasPassed);
            return true;
        }

        public static void Wait(AsyncTransaction t)
        {
            Wait(-1, t);
        }

        public static void AsyncWait(int timeout, AsyncTransaction t, Action<bool> onResult)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                var result = Wait(timeout, t);
                onResult(result);
            });
        }

        public static void AsyncWait(AsyncTransaction t, Action onResult)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Wait(t);
                onResult();
            });
        }
    }
}
