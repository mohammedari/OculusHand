using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Livet;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows.Media.Media3D;

namespace OculusHand.Models
{
    public class OculusRift : IDisposable
    {
        [DllImport(@"RiftWrapper.dll")]
        static extern int OVR_Init();

        [DllImport(@"RiftWrapper.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern unsafe int OVR_Peek(float* w, float* x, float* y, float* z);

        [DllImport(@"RiftWrapper.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern unsafe int OVR_QueryHMD(ref OVR_HMDInfo refHmdInfo);

        DispatcherTimer _timer;

        /////////////////////////////////////////////////////

        public event EventHandler<Matrix3D> OnUpdated;

        public OculusDistortionParameter DistortionParameter { get; private set; }

        public OculusRift()
        {
            _timer = new DispatcherTimer(DispatcherPriority.Render);
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            _timer.Tick += _timer_Tick;

            if (OVR_Init() == -1)
                throw new OculusRiftException("Failed to initialize Oculus Rift.");

            OVR_HMDInfo info = new OVR_HMDInfo();
            if (OVR_QueryHMD(ref info) == -1)
                throw new OculusRiftException("Failed to query HMD infomation.");

            DistortionParameter = new OculusDistortionParameter()
                { 
                    DistortionK = info.DistortionK,
                    ScreenWidthDistance = info.HScreenSize,
                    LensSeparationDistance = info.LensSeparationDistance,
                };

            _timer.Start();
        }

        /////////////////////////////////////////////////////

        unsafe void _timer_Tick(object sender, EventArgs e)
        {
            float w, x, y, z;
            if (OVR_Peek(&w, &x, &y, &z) == -1)
                throw new OculusRiftException("Failed to acquire Oculus Rift orientation.");

            var mat = Matrix3D.Identity;
            mat.Rotate(new Quaternion(x, y, -z, -w));
            RaiseOnUpdated(mat);
        }

        void RaiseOnUpdated(Matrix3D data)
        {
            if (OnUpdated != null)
                OnUpdated(this, data);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct OVR_HMDInfo
        {
            public UInt32 HResolution;
            public UInt32 VResolution;
            public Single HScreenSize;
            public Single VScreenSize;
            public Single VScreenCenter;
            public Single EyeToScreenDistance;
            public Single LensSeparationDistance;
            public Single InterpupillaryDistance;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Single[] DistortionK;

            public Int32 DesktopX;
            public Int32 DesktopY;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public Char[] DisplayDeviceName;
        }

        //////////////////////////////////////////////////
        #region Implimentaion of IDisposable
        bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (this)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;

                _timer.Stop();
            }
        }
        #endregion
    }
}
