using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace Tools
{
    public class RegistryHelper
    {
        public static object getValue(RegistryKey rootKey, string path, string name)
        {
            object result = null;
            try
            {
                RegistryKey pathKey = rootKey.OpenSubKey(path);
                if (pathKey != null)
                {
                    object value = pathKey.GetValue(name);
                    result = value;
                    pathKey.Close();
                }

            }
            catch
            {

            }
            return result;
        }

        public static bool createValue(RegistryKey rootKey, string path, string name, object value)
        {
            bool result = false;
            if (getValue(rootKey, path, name) == null)
            {
                try
                {
                    RegistryKey pathKey = rootKey.OpenSubKey(path, true);
                    if (pathKey == null)
                    {
                        try
                        {
                            pathKey = rootKey.CreateSubKey(path);
                        }
                        catch
                        {

                        }
                    }
                    if (pathKey != null)
                    {
                        pathKey.SetValue(name, value);
                        pathKey.Close();
                        result = true;
                    }
                }
                catch
                {

                }
            }
            else 
            {
                result = true;
            }
            return result;
        }
    }
}
