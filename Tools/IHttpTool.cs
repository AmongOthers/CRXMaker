using System;
using System.Collections.Generic;
using System.Text;

namespace Tools
{
    public delegate void EndHttpGetHandler(object error, string getHttpGetResult);
    public delegate void BeginHttpGetHandler(string url, EndHttpGetHandler endHttpGetResult);
}
