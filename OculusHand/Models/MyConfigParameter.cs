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

        public float HandRecognitionMaxDepth { get; set; }
        public float HandRecognitionMaxDepthGap { get; set; }
        public int HandRecognitionPixelSkip { get; set; }

        public int DistortionSurfaceResolutionWidth { get; set; }
        public int DistortionSurfaceResolutionHeight { get; set; }
        public float DistortionThetaMappingDepth { get; set; }
        public string BackgroundImagePath { get; set; }

        public double CameraPitchAngle { get; set; }
        public double CameraOffsetY { get; set; }
        public double CameraScale { get; set; }

        public float OffsetU { get; set; }
        public double BackgroundImageAutoUpdateInterval { get; set; }
        public float LensHorizontalDistanceRatioFromCenter { get; set; }

        public override void Initialize()
        {
            DeviceName = "";
            GestureModuleName = "";

            BackBufferWidth = 960;
            BackBufferHeight = 540;

            HandRecognitionMaxDepth = 0.5f;
            HandRecognitionMaxDepthGap = 0.01f;
            HandRecognitionPixelSkip = 1;

            DistortionSurfaceResolutionWidth = 80;
            DistortionSurfaceResolutionHeight = 45;
            DistortionThetaMappingDepth = 1f;
            BackgroundImagePath = @"Images\";

            CameraPitchAngle = -5;
            CameraOffsetY = 0.15;
            CameraScale = 2;

            OffsetU = 0.0f;
            BackgroundImageAutoUpdateInterval = 30;
            LensHorizontalDistanceRatioFromCenter = 0.3f;
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}
