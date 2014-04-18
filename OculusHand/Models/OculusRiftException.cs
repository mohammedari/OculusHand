using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusHand.Models
{
    [Serializable]
    public class OculusRiftException : Exception
    {
        public OculusRiftException() { }
        public OculusRiftException(string message) : base(message) { }
        public OculusRiftException(string message, Exception inner) : base(message, inner) { }
        protected OculusRiftException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
