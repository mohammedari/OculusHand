using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculuSLAM.Models
{
    [Serializable]
    public class GestureCameraException : ApplicationException
    {
        public GestureCameraException() { }
        public GestureCameraException(string message) : base(message) { }
        public GestureCameraException(string message, Exception inner) : base(message, inner) { }
        protected GestureCameraException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}