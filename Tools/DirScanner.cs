using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Tools
{
    public class DirScanner
    {
        public delegate bool OnFileFoundAndIsToGoOn(string filePath);
       
        public static void scan(string path, OnFileFoundAndIsToGoOn onFileFound)
        {
            if (Directory.Exists(path))
            {
                scan(new DirectoryInfo(path), onFileFound);
            }
        }

        public static bool scan(DirectoryInfo dir, OnFileFoundAndIsToGoOn onFileFound)
        {
            FileInfo[] files = dir.GetFiles();
            foreach (var item in files)
            {
                if (!onFileFound(item.FullName))
                {
                    return false;
                }
            }
            DirectoryInfo[] subDirs = dir.GetDirectories();
            foreach (var item in subDirs)
            {
                if (!scan(item, onFileFound))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
