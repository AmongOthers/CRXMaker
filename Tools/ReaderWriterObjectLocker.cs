using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Threading;

namespace Tools
{
    [Serializable]
    public class ReaderWriterObjectLocker : ISerializable
    {
        [NonSerialized]
        private ReaderWriterLock mReaderWriterLocker;
        [NonSerialized]
        private IDisposable mWriterReleaser;
        [NonSerialized]
        private IDisposable mReaderReleaser;

        public ReaderWriterObjectLocker()
        {
            mReaderWriterLocker = new ReaderWriterLock();
            mWriterReleaser = new WriterReleaser(mReaderWriterLocker);
            mReaderReleaser = new ReaderReleaser(mReaderWriterLocker);
        }

        protected ReaderWriterObjectLocker(SerializationInfo info, StreamingContext context)
            : this()
        {

        }

        [SecurityPermissionAttribute(SecurityAction.Assert, Flags = SecurityPermissionFlag.AllFlags)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {

        }

        public IDisposable ReadLock()
        {
            mReaderWriterLocker.AcquireReaderLock(-1);
            return mReaderReleaser;
        }

        public IDisposable WriteLock()
        {
            mReaderWriterLocker.AcquireWriterLock(-1);
            return mReaderReleaser;
        }

        class ReaderReleaser : IDisposable
        {
            private ReaderWriterLock mReaderWriterLock;
            public ReaderReleaser(ReaderWriterLock locker)
            {
                mReaderWriterLock = locker;
            }

            public void Dispose()
            {
                try
                {
                    mReaderWriterLock.ReleaseReaderLock();
                }
                catch (ApplicationException ex)
                {
                    Logger.Logger.GetLogger(this).Error("释放读操作锁导常", ex);
                }
            }
        }

        class WriterReleaser : IDisposable
        {
            private ReaderWriterLock mReaderWriterLock;
            public WriterReleaser(ReaderWriterLock locker)
            {
                mReaderWriterLock = locker;
            }

            public void Dispose()
            {
                try
                {
                    mReaderWriterLock.ReleaseWriterLock();
                }
                catch (ApplicationException ex)
                {
                    Logger.Logger.GetLogger(this).Error("释放写操作锁导常", ex);
                }
            }
        }
    }
}
