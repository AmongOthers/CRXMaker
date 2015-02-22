using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;

namespace Tools
{
    public abstract class LxErrno
    {
        protected LxErrno _innerError;
        protected string _source;
        public LxErrno()
            : this(null)
        {
        }
        public LxErrno(LxErrno innerError)
        {
            _innerError = innerError;
        }
        public string Source { get { return _source; } }
        public LxErrno getInnerErrno()
        {
            return _innerError;
        }
        public String InnerErrnoStr { get { return string.Format("{0}", _innerError); } }
        public override string ToString()
        {
            return string.Format("{0}:{1}", this.GetType(), JsonConvert.SerializeObject(this));
        }
    }
}
