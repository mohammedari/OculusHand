using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Input;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using System.IO;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using OculusHand.Models;

namespace OculusHand.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        GestureCamera _camera = null;
        OculusRift _oculus;
        HandRecognition _hand;
        string _path;
        uint _backgroundImageIndex;

        public void Initialize()
        {
            var config = Util.GetConfigManager();
            initializeCamera(config.Parameters.DeviceName, config.Parameters.GestureModuleName);

            _oculus = new OculusRift();
            DistortionParameter = _oculus.DistortionParameter;
            _oculus.OnUpdated += new EventHandler<Matrix3D>((o, e) => { Orientation = e; });

            _hand = new HandRecognition(
                config.Parameters.HandRecognitionMaxDepth, 
                config.Parameters.HandRecognitionMaxDepthGap, 
                config.Parameters.HandRecognitionPixelSkip);

            _path = config.Parameters.BackgroundImagePath;
            var files = new List<string>();
            foreach (var pattern in new string[] { "*.jpg", "*.jpeg" })
            {
                try
                {
                    files.AddRange(Directory.GetFiles(_path, pattern));
                }
                catch (DirectoryNotFoundException)
                {
                }
                catch (FileNotFoundException)
                {
                }
            }

            if (files.Count == 0)
            {
                ErrorMessage = "Failed to find background image file.";
                return;
            }

            var rand = new Random();
            _backgroundImageIndex = (uint)rand.Next(files.Count);

            BackgroundImagePath = files[(int)_backgroundImageIndex];
        }

        ~MainWindowViewModel()
        {
            Dispose(false);
        }

        //////////////////////////////////////////////////
        #region Mesh変更通知プロパティ
        private Mesh _mesh = new Mesh();

        public Mesh Mesh
        {
            get
            { return _mesh; }
            set
            {
                if (_mesh == value)
                {
                    return;
                }

                _mesh = value;
                RaisePropertyChanged("Mesh");
            }
        }
        #endregion

        #region Orientation変更通知プロパティ
        private Matrix3D _Orientation;

        public Matrix3D Orientation
        {
            get
            { return _Orientation; }
            set
            { 
                if (_Orientation == value)
                    return;
                _Orientation = value;
                RaisePropertyChanged("Orientation");
            }
        }
        #endregion

        #region DistortionParameter変更通知プロパティ
        private OculusDistortionParameter _DistortionParameter;

        public OculusDistortionParameter DistortionParameter
        {
            get
            { return _DistortionParameter; }
            set
            { 
                if (_DistortionParameter == value)
                    return;
                _DistortionParameter = value;
                RaisePropertyChanged("DistortionParameter");
            }
        }
        #endregion

        #region BackgroundImagePath変更通知プロパティ
        private string _BackgroundImagePath;

        public string BackgroundImagePath
        {
            get
            { return _BackgroundImagePath; }
            set
            { 
                if (_BackgroundImagePath == value)
                    return;
                _BackgroundImagePath = value;
                RaisePropertyChanged("BackgroundImagePath");
            }
        }
        #endregion

        #region ErrorMessage変更通知プロパティ
        private string _ErrorMessage;

        public string ErrorMessage
        {
            get
            { return _ErrorMessage; }
            set
            {
                if (_ErrorMessage == value)
                {
                    return;
                }

                _ErrorMessage = value;
                RaisePropertyChanged("ErrorMessage");
            }
        }
        #endregion

        //////////////////////////////////////////////////
        #region Public Methods

        public void Capture()
        {
        }

        #endregion

        //////////////////////////////////////////////////
        #region Private Methods
        void disposeCamera()
        {
            if (_camera != null)
            {
                if (_camera.IsLooping)
                {
                    _camera.StopLoop();
                }

                _camera.Dispose();
            }
        }

        bool initializeCamera(string device, string module)
        {
            disposeCamera();
            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                _camera = new GestureCamera(device, module);
            }
            catch (GestureCameraException e)
            {
                ErrorMessage = e.Message;
                disposeCamera();
                return false;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

            _camera.OnUpdated += onCameraUpdated;
            _camera.StartLoop();
            return true;
        }

        void onCameraUpdated(object s, GestureCamera.OnUpdatedEventArgs e)
        {
            _hand.UpdateMesh(e.Data);
            Mesh = _hand.Mesh;

            if (e.Data.IsGestureSwipeRight)
                updateBackgroundImage(1);
            else if (e.Data.IsGestureSwipeLeft)
                updateBackgroundImage(-1);
        }

        void updateBackgroundImage(int i)
        {
            var files = Directory.GetFiles(_path, "*.jpg");
            if (files.Length == 0)
                throw new InvalidOperationException("Failed to find background image file.");

            _backgroundImageIndex += (uint)i;
            BackgroundImagePath = files[_backgroundImageIndex % files.Length];
        }
        #endregion

        //////////////////////////////////////////////////
        #region Implimentaion of IDisposable
        bool _disposed = false;

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            lock (this)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;

                base.Dispose();
                disposeCamera();
                _oculus.Dispose();
            }
        }
        #endregion
    }
}
