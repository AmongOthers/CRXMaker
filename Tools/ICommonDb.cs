using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public interface ICommonDb
    {
        bool Put(string section,string key, string value);
        string Get(string section, string key);
        bool Clear(string section);
    }
}
