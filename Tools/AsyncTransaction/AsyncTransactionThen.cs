using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Tools.AsyncTransaction
{
    public class AsyncTransactionThen: AsyncTransaction
    {
        private AsyncTransaction first;
        private AsyncTransaction second;

        public AsyncTransactionThen(AsyncTransaction first, AsyncTransaction second)
        {
            this.first = first;
            this.second = second;
        }

        public override bool IsSuccessful
        {
            get { return this.first.IsSuccessful && this.second.IsSuccessful; }
        }

        public override bool IsFailed
        {
            get { return this.first.IsFailed || this.second.IsFailed; }
        }

        public override void Commit()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                this.first.Commit();
                this.first.Wait();
                if (this.first.IsSuccessful)
                {
                    this.second.Commit();
                }
            });
        }
    }
}
