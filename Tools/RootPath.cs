using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public class RootPath
    {
        private readonly string _path;
        public RootPath(string path)
        {
            this._path = path;
        }
        public string getFileName(string fileName)
        {
            return System.IO.Path.Combine(_path, fileName);
        }
        public string Path 
        {
            get { return _path; }
        }
    }
}
