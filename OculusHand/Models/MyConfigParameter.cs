using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ConfigUtil;

namespace OculusHand.Models
{
    public class MyConfigParameter : ConfigParameter
    {
        public string DeviceName { get; set; }
        public string GestureModuleName { get; set; }

        public int BackBufferWidth { get; set; }
        public int BackBufferHeight { get; set; }
        public int PointSize { get; set; }

        public float VoxelSize { get; set; }
        public int ICPPointCountMin { get; set; }
        public int ICPMaxIteration { get; set; }
        public double ICPErrorConvergence { get; set; }
        public int ICPSamplingCount { get; set; }

        public override void Initialize()
        {
            DeviceName = "";
            GestureModuleName = "";

            BackBufferWidth = 960;
            BackBufferHeight = 540;
            PointSize = 5;

            VoxelSize = 0.01f;
            ICPPointCountMin = 2000;
            ICPErrorConvergence = 2e-3;
            ICPMaxIteration = 10;
            ICPSamplingCount = 1000;
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}
