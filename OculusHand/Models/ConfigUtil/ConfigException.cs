using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConfigUtil
{
    [Serializable]
    public class ConfigException : ApplicationException
    {
        public ConfigException() { }
        public ConfigException(string message) : base(message) { }
        public ConfigException(string message, Exception inner) : base(message, inner) { }
        protected ConfigException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
