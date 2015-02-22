using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Tools
{
    public class JsonMessage : Dictionary<string, string>
    {
        public JsonMessage()
        {
        }

        public JsonMessage(JsonMessage other)
        {
            foreach (var item in other.Keys)
            {
                this[item] = other[item];
            }
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static JsonMessage parse(string json)
        {
            return (JsonMessage)(JsonConvert.DeserializeObject(json, typeof(JsonMessage)));
        }

        public void Merge(JsonMessage msg)
        {
            foreach (var key in msg.Keys)
            {
                this[key] = msg[key];
            }
        }

        public bool IsIncluded(JsonMessage other)
        {
            foreach (var key in other.Keys)
            {
                if (!this.ContainsKey(key))
                {
                    return false;
                }
                if (this[key] != other[key])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
