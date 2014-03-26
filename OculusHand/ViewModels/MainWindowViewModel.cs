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
using OculuSLAM.Models;
using OpenCvSharp.CPlusPlus;

namespace OculuSLAM.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        GestureCamera _camera = null;
        VoxelizedMap _map;
        IterativeClosestPoint _icp;
        int _icpMinPoints;

        public void Initialize()
        {
            //initializeCamera();
                
            var config = Util.GetConfigManager();
            _map = new VoxelizedMap(config.Parameters.VoxelSize);
            _icp = new IterativeClosestPoint(
                config.Parameters.ICPSamplingCount,
                config.Parameters.ICPErrorConvergence,
                config.Parameters.ICPMaxIteration);
            _icpMinPoints = config.Parameters.ICPPointCountMin;

            test();
        }

        ~MainWindowViewModel()
        {
            Dispose(false);
        }

        //////////////////////////////////////////////////
        #region Points変更通知プロパティ
        private PointCloud _cloud = new PointCloud();

        public PointCloud Points
        {
            get
            { return _cloud; }
            set
            {
                if (_cloud == value)
                {
                    return;
                }

                _cloud = value;
                RaisePropertyChanged("Points");
            }
        }
        #endregion


        #region Transform変更通知プロパティ
        private Mat _Transform;

        public Mat Transform
        {
            get
            { return _Transform; }
            set
            { 
                if (_Transform == value)
                    return;
                _Transform = value;
                RaisePropertyChanged("Transform");
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
            //PointCloud cloud;
            //lock (Points)
            //{
            //    cloud = (PointCloud)Points.Clone();
            //}

            //cloud.Save("capture_" + DateTime.Now.ToString("yyyyMMddHHmmssff") + ".pcd");

            icp();
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
            if (_map.Count < _icpMinPoints)
            {
                _map.Points = e.Cloud.Points;
                lock (Points)
                {
                    Points = new PointCloud(_map.Points);
                }
                return;
            }

            if (e.Cloud.Count < _icpMinPoints)
            {
                Console.WriteLine("Points not enough.");
                return;
            }

            if (!_icp.Match(_map, e.Cloud))
            {
                Console.WriteLine("ICP not converged.");
                return;
            }
            _icp.TransformPointCloud(e.Cloud);

            _map.Merge(e.Cloud);
            lock (Points)
            {
                Points = new PointCloud(_map.Points);
            }

            Transform = _icp.Transform.Clone();
        }

        //[TODO] ICPのテストのための初期化
        void test()
        {
            lock (Points)
            {
                Points = new PointCloud("capture_2014031100051137.pcd");
            }
        }

        //[TODO] ICPのテストを行う
        int _i = 0;
        void icp()
        {
            if (_i == 0)
            {
                var config = Util.GetConfigManager();
                //_map = new VoxelizedMap("capture_2014031100051370.pcd", config.Parameters.VoxelSize);
                //_map = new VoxelizedMap("capture_2014031100051137.pcd", config.Parameters.VoxelSize);
                _map = new VoxelizedMap("capture_2014031100051137_transformed.pcd", config.Parameters.VoxelSize);

                if (Points.Count < config.Parameters.ICPPointCountMin)
                {
                    Console.WriteLine("points not enough.");
                    return;
                }

                if (!_icp.Match(_map, Points))
                {
                    Console.WriteLine("ICP not converged.");
                    return;
                }

                Points = new PointCloud(_map.Points);
                //_icp.TransformPointCloud(Points);
                //RaisePropertyChanged("Points"); //[TODO] RaisePropertyChangedで更新されるようにする

                Transform = _icp.Transform.Clone();
            }
            else if (_i % 3 == 1) //1
            {
                Points = new PointCloud("capture_2014031100051137.pcd");
            }
            else if (_i % 3 == 2) //2
            {
                var points = new PointCloud("capture_2014031100051137.pcd");
                _icp.TransformPointCloud(points);
                Points = points;

                //points.Save("capture_2014031100051137_transformed.pcd");
            }
            else //3
            {
                Points = new PointCloud("capture_2014031100051137_transformed.pcd");
            }
            ++_i;

            //_map.Merge(Points);
            //lock (Points)
            //{
            //    Points = new PointCloud(_map.Points);
            //}
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
