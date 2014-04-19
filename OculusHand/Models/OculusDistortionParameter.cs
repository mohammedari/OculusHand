using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Livet;

namespace OculusHand.Models
{
    public class OculusDistortionParameter
    {
        public float[] DistortionK { get; set; }
        public float ScreenWidthDistance { get; set; }
        public float LensSeparationDistance { get; set; }
    }
}
