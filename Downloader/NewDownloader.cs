using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace HttpDownloader
{
    public class NewDownloader
    {
        private AddTask _task;
        public event EventHandler ProgressChanged;
        public event EventHandler DownloadFinished;
        private bool _is_cancel = false;
        private Object _lock = new Object();
        private Thread mThread;

        public NewDownloader(string name, string path, string md5, string url)
        {
            _task = new AddTask(name, path, md5, url);
            _task.ProgressChanged += new EventHandler(task_ProgressChanged);
        }

        void task_ProgressChanged(object sender, EventArgs e)
        {
            if (ProgressChanged != null)
            {
                Console.WriteLine("task_ProgressChanged invoke");
                ProgressChanged(this, e);
            }
        }

        public void download()
        {
            if (mThread == null)
            {
                mThread = new Thread(run);
            }
            mThread.Start();
        }

        public void cancel()
        {
            lock (_lock)
            {
                _is_cancel = true;
            }
            mThread.Join();
        }

        public void resume()
        {
            if (mThread == null)
            {
                mThread = new Thread(run);
            }
            if (!mThread.IsAlive)
            {
                mThread.Start();
            }
        }

        private void run()
        {
            while (true)
            {
                lock (_lock)
                {
                    if (_is_cancel)
                    {
                        _task.abandon();
                        break;
                    }
                }

                if (_task.wait())
                {
                    if (_task.IsFailed)
                    {
                        finish(false);
                    }
                    else
                    {
                        finish(true);
                    }
                    break;
                }
                Thread.Sleep(200);
            }
            Console.WriteLine("download run finished");
        }

        private void finish(bool is_success)
        {
            if (DownloadFinished != null)
            {
                DownloadFinishedEventArgs e = new DownloadFinishedEventArgs();
                e.IsSuccess = is_success;
                DownloadFinished(this, e);
            }
        }

        public class DownloadFinishedEventArgs : EventArgs
        {
            public bool IsSuccess { get; set; }
        }
    }
}
