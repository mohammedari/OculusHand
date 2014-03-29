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

using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using OculusHand.Models;
using OpenCvSharp.CPlusPlus;

namespace OculusHand.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        GestureCamera _camera = null;
        HandRecognition _hand;

        public void Initialize()
        {
            initializeCamera();
                
            var config = Util.GetConfigManager();

            //[TODO]パラメータをセット
            _hand = new HandRecognition();
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

        bool initializeCamera()
        {
            disposeCamera();
            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                var config = Util.GetConfigManager();
                _camera = new GestureCamera(config.Parameters.DeviceName);
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
            }
        }
        #endregion
    }
}
