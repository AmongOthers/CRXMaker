using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Tools.AsyncTransaction
{
    public class AsyncTransactionAtom : AsyncTransaction
    {
        Action<AsyncTransactionAtom> action;

        public AsyncTransactionAtom()
        {
        }
        public AsyncTransactionAtom(Action<AsyncTransactionAtom> action)
        {
            this.action = action;
        }

        public void Succeed()
        {
            this.IsSuccessful = true;
        }

        public void Fail()
        {
            this.IsFailed = true;
        }

        public override void Commit()
        {
            base.Commit();
            ThreadPool.QueueUserWorkItem(delegate
            {
                if (action != null)
                {
                    action.Invoke(this);
                }
            });
        }
    }
}
