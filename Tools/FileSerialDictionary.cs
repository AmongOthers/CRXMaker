using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Tools
{
    public class FileSerialDictionary
    {
        static Dictionary<string, FileSerialDictionary> sDicDic = new Dictionary<string, FileSerialDictionary>();
        static Object sGetLock = new Object();

        string mFileName = null;
        Dictionary<string, string> mDic = null;
        BinaryFormatter formatter = new BinaryFormatter();
        bool mIsClosed = true;
        Object mLock = new Object();

        public static FileSerialDictionary getDictionary(string fileName)
        {
            lock (sGetLock)
            {
                FileSerialDictionary dic = null;
                if (sDicDic.ContainsKey(fileName))
                {
                    dic = sDicDic[fileName];
                }
                else
                {
                    dic = new FileSerialDictionary(fileName);
                    sDicDic[fileName] = dic;
                }
                return dic;
            }
        }

        public static void closeDictionary(string fileName)
        {
        }

        internal FileSerialDictionary(string fileName)
        {
            bool isLoadOk = false ;
            mFileName = fileName;
            Stream loadStream = null;
            if (File.Exists(fileName))
            {
                try
                {
                    loadStream = File.Open(fileName, FileMode.Open);
                    mDic = (Dictionary<string, string>)formatter.Deserialize(loadStream);
                    isLoadOk = true;
                    loadStream.Close();
                }
                catch
                {
                    if (loadStream != null)
                    {
                        loadStream.Close();
                    }
                }
            }
            if (!isLoadOk)
            {
                mDic = new Dictionary<string, string>();
            }
            sDicDic[fileName] = this;
        }

        public bool updateRecord(string key, string value)
        {
            bool result = false;
            lock (mLock)
            {
                if (mDic.ContainsKey(key))
                {
                    mDic[key] = value;
                }
                else
                {
                    mDic.Add(key, value);
                }
                try
                {
                    Stream updateStream = File.Open(mFileName, FileMode.Create);
                    if (updateStream != null)
                    {
                        formatter.Serialize(updateStream, mDic);
                        updateStream.Close();
                        result = true;
                    }
                }
                catch
                {

                }
            }
            return result;
        }

        public string getValue(string key)
        {
            lock (mLock)
            {
                if (mDic.ContainsKey(key))
                {
                    return mDic[key];
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public void Dispose()
        {
            close();
        }

        internal void close()
        {
            if (!mIsClosed)
            {
                mIsClosed = true;
                sDicDic.Remove(mFileName);
            }
        }
    }
}
