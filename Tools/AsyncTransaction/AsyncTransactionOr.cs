using System;
using System.Collections.Generic;
using System.Text;

namespace Tools.AsyncTransaction
{
    public class AsyncTransactionOr : AsyncTransaction
    {
        private AsyncTransaction left;
        private AsyncTransaction right;

        public AsyncTransactionOr(AsyncTransaction left, AsyncTransaction right)
        {
            this.left = left;
            this.right = right;
        }

        public override bool IsSuccessful
        {
            get { return this.left.IsSuccessful || this.right.IsSuccessful; }
        }

        public override bool IsFailed
        {
            get { return this.left.IsFailed && this.right.IsFailed; }
        }

        public override void Commit()
        {
            base.Commit();
            this.left.Commit();
            this.right.Commit();
        }
    }
}
