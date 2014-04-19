using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace OculusHand.Models
{
    public class GestureCamera : IDisposable
    {
        //////////////////////////////////////////////////
        #region Private Fields
        UtilMPipeline _pipeline;
        float _unit_m;
        float _depthLowConf;
        float _depthSaturation;

        bool _looping = false;
        Thread _t;
        int _joinTimeout = 1000;
        #endregion

        //////////////////////////////////////////////////
        #region Properties
        /// <summary>
        /// Event which is raised when a new frame arrived.
        /// </summary>
        public event EventHandler<OnUpdatedEventArgs> OnUpdated;

        /// <summary>
        /// True if image aquisition loop is active.
        /// </summary>
        public bool IsLooping { get { return _looping; } }
        #endregion

        //////////////////////////////////////////////////
        #region Public Methods
        public void StartLoop()
        {
            if (_looping)
                throw new InvalidOperationException("Loop is already started.");

            _looping = true;
            _t = new Thread(new ThreadStart(() =>
            {
                while (_looping)
                    update();
            }));
            _t.Start();
        }

        public void StopLoop()
        {
            if (!_looping)
                throw new InvalidOperationException("Loop is not started.");

            _looping = false;
            if (!_t.Join(_joinTimeout))
                _t.Abort();
        }

        public void Update()
        {
            update();
        }
        #endregion

        //////////////////////////////////////////////////
        #region Constructer and Destructor
        /// <summary>
        /// Create instance with device name and module name.
        /// </summary>
        /// <param name="deviceName">The name of using device</param>
        public GestureCamera(string deviceName, string gestureModuleName)
        {
            buildPipeline(deviceName, gestureModuleName);
        }

        ~GestureCamera()
        {
            Dispose(false);
        }
        #endregion 

        //////////////////////////////////////////////////
        #region Private Methods
        void buildPipeline(string deviceName, string gestureModuleName)
        {
            _pipeline = new UtilMPipeline();
            _pipeline.QueryCapture().SetFilter(deviceName);
            _pipeline.EnableImage(PXCMImage.ColorFormat.COLOR_FORMAT_RGB24);
            _pipeline.EnableImage(PXCMImage.ColorFormat.COLOR_FORMAT_VERTICES);
            _pipeline.EnableGesture(gestureModuleName);
            if (!_pipeline.Init())
                throw new GestureCameraException("Failed to initialize pipeline.");

            float unit;
            GestureCameraUtil.Assert(
                _pipeline.QueryCapture().device.QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_UNIT, out unit),
                "Failed to query sensor depth unit.");
            _unit_m = unit / 1000000;

            GestureCameraUtil.Assert(
                _pipeline.QueryCapture().device.QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_LOW_CONFIDENCE_VALUE, out _depthLowConf),
                "Failed to query sensor depth low confidence value.");
            GestureCameraUtil.Assert(
                _pipeline.QueryCapture().device.QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_SATURATION_VALUE, out _depthSaturation),
                "Failed to query sensor depth saturation value.");
        }

        void update()
        {
            if (!_pipeline.AcquireFrame(true))
                throw new GestureCameraException("Failed to aquire frame.");
            if (_pipeline.IsDisconnected())
                return;

            var color = _pipeline.QueryImage(PXCMImage.ImageType.IMAGE_TYPE_COLOR);
            var depth = _pipeline.QueryImage(PXCMImage.ImageType.IMAGE_TYPE_DEPTH);
            var gesture = _pipeline.QueryGesture();

            PXCMGesture.Blob blobInfo;
            GestureCameraUtil.Assert(gesture.QueryBlobData(PXCMGesture.Blob.Label.LABEL_SCENE, 0, out blobInfo), "Failed to query blob data.");
            PXCMImage blob;
            GestureCameraUtil.Assert(gesture.QueryBlobImage(PXCMGesture.Blob.Label.LABEL_SCENE, 0, out blob), "Failed to query blob image.");

            PXCMImage.ImageData colorData;
            GestureCameraUtil.Assert(
                color.AcquireAccess(PXCMImage.Access.ACCESS_READ, out colorData),
                "Failed to acquire access on color image.");
            PXCMImage.ImageData depthData;
            GestureCameraUtil.Assert(
                depth.AcquireAccess(PXCMImage.Access.ACCESS_READ, out depthData),
                "Failed to acquire access on depth image.");
            PXCMImage.ImageData blobData;
            GestureCameraUtil.Assert(
                blob.AcquireAccess(PXCMImage.Access.ACCESS_READ, out blobData),
                "Failed to acquire access on blob image.");

            int colorWidth = (int)color.info.width;
            int colorHeight = (int)color.info.height;
            var colorImage = colorData.ToByteArray(0, 3 * colorWidth * colorHeight);

            int depthWidth = (int)depth.info.width;
            int depthHeight = (int)depth.info.height;
            var verticies = depthData.ToShortArray(0, 3 * depthWidth * depthHeight);
            var uv = depthData.ToFloatArray(2, 2 * depthWidth * depthHeight);

            int blobWidth = (int)blob.info.width;
            int blobHeight = (int)blob.info.height;
            var blobImage = blobData.ToByteArray(0, blobWidth * blobHeight);

            var data = new GestureCameraData(depthWidth, depthHeight, colorWidth, colorHeight, colorImage, blobImage);
            for (int j = 0; j < depthHeight; ++j)
                for (int i = 0; i < depthWidth; ++i)
                {
                    float u = uv[2 * (j * depthWidth + i) + 0];
                    float v = uv[2 * (j * depthWidth + i) + 1];
                    int colorX = (int)(u * (colorWidth - 1));
                    int colorY = (int)(v * (colorHeight - 1));
                    if (0 > colorX || colorX > colorWidth || 0 > colorY || colorY > colorHeight)
                        continue;

                    float x = verticies[3 * (j * depthWidth + i) + 0];
                    float y = verticies[3 * (j * depthWidth + i) + 1];
                    float z = verticies[3 * (j * depthWidth + i) + 2];
                    if (z == _depthLowConf || z == _depthSaturation)
                        continue;

                    x *= _unit_m;
                    y *= _unit_m;
                    z *= _unit_m;

                    data.Set(i, j, new Point(u, v, x, y, z));
                }

            GestureCameraUtil.Assert(
                color.ReleaseAccess(ref colorData),
                "Failed to release access on color image.");
            GestureCameraUtil.Assert(
                depth.ReleaseAccess(ref depthData),
                "Failed to release access on depth image.");
            _pipeline.ReleaseFrame();

            RaiseOnUpdated(data);
        }

        void RaiseOnUpdated(GestureCameraData data)
        {
            if (OnUpdated != null)
                OnUpdated(this, new OnUpdatedEventArgs(data));
        }
        #endregion

        //////////////////////////////////////////////////
        #region Static Methods
        /// <summary>
        /// Get device names.
        /// Devices' group is restricted to sensor type.
        /// Devices' subgroup is restricted to video capture type.
        /// </summary>
        /// <returns>Device names</returns>
        public static string[] GetDeviceNames()
        {
            var session = GestureCameraUtil.GetSession();

            var descOriginal = new PXCMSession.ImplDesc();
            descOriginal.group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR;
            descOriginal.subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE;

            var list = new List<string>();
            for (uint i = 0; ; i++)
            {
                PXCMSession.ImplDesc desc;
                if (GestureCameraUtil.HasError(session.QueryImpl(ref descOriginal, i, out desc)))
                    break;

                PXCMCapture capture;
                if (GestureCameraUtil.HasError(session.CreateImpl<PXCMCapture>(ref desc, PXCMCapture.CUID, out capture)))
                    continue;

                for (uint j = 0; ; j++)
                {
                    PXCMCapture.DeviceInfo info;
                    if (GestureCameraUtil.HasError(capture.QueryDevice(j, out info)))
                        break;
                    list.Add(info.name.get());
                }

                capture.Dispose();
            }

            return list.ToArray();
        }
        #endregion

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

                if (_pipeline != null)
                {
                    //_pipeline.Close();    //causes NullReference exception?
                    _pipeline.Dispose();
                }
            }
        }
        #endregion

        //////////////////////////////////////////////////
        #region OnUpdatedEventArgs Class
        /// <summary>
        /// Event argument class for on updated event.
        /// Image resources will be automatically disposed.
        /// </summary>
        public class OnUpdatedEventArgs : EventArgs
        {
            public GestureCameraData Data { get; private set; }

            internal OnUpdatedEventArgs(GestureCameraData data)
            {
                Data = data;
            }
        }
        #endregion
    }
}
