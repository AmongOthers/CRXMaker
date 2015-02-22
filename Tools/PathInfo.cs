using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public class PathInfo
    {
        private string path;
        public string Path
        {
            get
            {
                return path;
            }
        }
        private bool isDirectory;
        public bool IsDirectory
        {
            get
            {
                return isDirectory;
            }
        }
        public PathInfo(string path, bool isDirectory)
        {
            this.path = path;
            this.isDirectory = isDirectory;
        }
    }
}
