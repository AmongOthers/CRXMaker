using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public class TaskArgs : EventArgs
    {
        NewTask mTask;
        public TaskArgs(NewTask task)
        {
            mTask = task;
        }

        public NewTask Task
        {
            get { return mTask; }
        }
    }

    public class TasksArgs : EventArgs
    {
        List<NewTask> mTasks;
        public TasksArgs(List<NewTask> tasks)
        {
            mTasks = tasks;
        }

        public List<NewTask> Tasks
        {
            get { return mTasks; }
        }
    }

    public class MusicTaskArgs : EventArgs
    {
        NewTask mTask;
        int mMoveSucceededCount;
        public MusicTaskArgs(NewTask task, int moveSucceededCount)
        {
            mTask = task;
            mMoveSucceededCount = moveSucceededCount;
        }

        public NewTask Task
        {
            get { return mTask; }
        }

        public int MoveSucceededCount
        {
            get { return mMoveSucceededCount; }
        }
    }

    public class UpdatedOrderResultEventArgs : EventArgs
    {
        public long InstalledSucceededCount { get; private set; }
        public long InstalledFailedCount { get; private set; }

        public UpdatedOrderResultEventArgs(long succeededCount, long failedCount)
        {
            InstalledSucceededCount = succeededCount;
            InstalledFailedCount = failedCount;
        }
    }
}
