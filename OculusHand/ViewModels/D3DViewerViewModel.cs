using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using OculuSLAM.Models;
using SharpDX;
using SharpDX.Direct3D9;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using System.Drawing;
using OpenCvSharp.CPlusPlus;

namespace OculuSLAM.ViewModels
{
    /// <summary>
    /// D3DViewerのビューモデルです。
    /// </summary>
    public class D3DViewerViewModel : ViewModel
    {
        ////////////////////////////////////////////////////
        #region 内部変数

        readonly ColorBGRA _background = SharpDX.Color.White;

        Device _device;
        Effect _effect;

        object _vertexBufferLock = new object();
        object _cameraVertexBufferLock = new object();
        int _count;

        VertexBuffer _vertexBuffer;
        VertexBuffer _cameraVertexBuffer;

        #endregion

        ////////////////////////////////////////////////////
        #region ImageSource変更通知プロパティ
        private D3DImage _ImageSource;
        /// <summary>
        /// レンダリング結果の画像イメージです。ViewのImageSourceにバインドしてください。
        /// </summary>
        public D3DImage ImageSource
        {
            get
            { return _ImageSource; }
            set
            { 
                if (_ImageSource == value)
                    return;
                _ImageSource = value;
                RaisePropertyChanged("ImageSource");
            }
        }
        #endregion

        ////////////////////////////////////////////////////
        #region コンストラクタ、デストラクタ
        /// <summary>
        /// 初期化を行います。
        /// </summary>
        public D3DViewerViewModel()
        {
            ImageSource = new D3DImage();

            var config = Util.GetConfigManager();
            initializeDirect3D(
                config.Parameters.BackBufferWidth, 
                config.Parameters.BackBufferHeight, 
                config.Parameters.PointSize);
        }

        ~D3DViewerViewModel()
        {
            Dispose(false);
        }
        #endregion

        ////////////////////////////////////////////////////
        #region Public Methods
        /// <summary>
        /// 初期化を行います。
        /// </summary>
        public void Initialize()
        {
        }

        /// <summary>
        /// 現在の点群を破棄して、与えられた点群を表示します。
        /// </summary>
        /// <param name="points">点群データ</param>
        public void UpdatePoints(PointCloud cloud)
        {
            var vl = from p in cloud.Points select new Vertex(p.X, p.Y, p.Z, p.R, p.G, p.B);
            setVertices(vl.ToArray());
        }

        /// <summary>
        /// レンダリングの際に用いる行列を更新します。
        /// </summary>
        /// <param name="matrix">平行回転行列の値</param>
        public void UpdateMatrix(Matrix3D matrix)
        {
            setMatrix(matrix);
        }

        /// <summary>
        /// 位置姿勢の変換行列を更新します。
        /// </summary>
        /// <param name="transform">位置姿勢変換行列</param>
        public void UpdateTransform(Mat transform)
        {
            //[TODO]このメソッドが1回も呼び出されなかった場合に正しく描画されない問題を解決する
            float scale = 0.1f;
            Mat camera = Mat.Zeros(7, 4, (MatType)MatType.CV_32F);
            getMat(-scale, -scale,      0).CopyTo(camera[new Range(0, 1), new Range(0, 4)]);
            getMat(     0,      0, -scale).CopyTo(camera[new Range(1, 2), new Range(0, 4)]);
            getMat(-scale,  scale,      0).CopyTo(camera[new Range(2, 3), new Range(0, 4)]);
            getMat( scale,  scale,      0).CopyTo(camera[new Range(3, 4), new Range(0, 4)]);
            getMat(     0,      0, -scale).CopyTo(camera[new Range(4, 5), new Range(0, 4)]);
            getMat( scale, -scale,      0).CopyTo(camera[new Range(5, 6), new Range(0, 4)]);
            getMat(-scale, -scale,      0).CopyTo(camera[new Range(6, 7), new Range(0, 4)]);

            var arr = matToVertex(camera * transform).ToArray();

            lock (_cameraVertexBufferLock)
            {
                if (_cameraVertexBuffer != null)
                    _cameraVertexBuffer.Dispose();

                //頂点バッファの作成
                var size = Marshal.SizeOf(typeof(Vertex));
                _cameraVertexBuffer = new VertexBuffer(_device, size * 7, Usage.None, Vertex.Format, Pool.Default);
                var st = _cameraVertexBuffer.Lock(0, size * 7, LockFlags.None);
                st.WriteRange<Vertex>(arr);
                _cameraVertexBuffer.Unlock();
            }
        }

        IEnumerable<Vertex> matToVertex(Mat mat)
        {
            var indexer = mat.GetGenericIndexer<float>();
            for (int i = 0; i < mat.Rows; ++i)
            {
                yield return new Vertex(indexer[i, 0], indexer[i, 1], indexer[i, 2], 0, 0, 0);
            }
        }

        Mat getMat(float x, float y, float z)
        {
            var mat = new Mat(1, 4, MatType.CV_32F);
            var indexer = mat.GetGenericIndexer<float>();

            indexer[0, 0] = x;
            indexer[0, 1] = y;
            indexer[0, 2] = z;
            indexer[0, 3] = 1;

            return mat;
        }

        /// <summary>
        /// レンダリングを行います。
        /// </summary>
        public void Render()
        {
            render(_background);
        }
        #endregion

        ////////////////////////////////////////////////////
        #region 内部処理の実装

        void initializeDirect3D(int backBufferWidth, int backBufferHeight, float pointSize)
        {
            PresentParameters pp = new PresentParameters()
            {
                DeviceWindowHandle = IntPtr.Zero,

                EnableAutoDepthStencil = false,

                MultiSampleType = MultisampleType.None,

                BackBufferCount = 2,
                BackBufferWidth = backBufferWidth,
                BackBufferHeight = backBufferHeight,
                BackBufferFormat = Format.X8R8G8B8,

                PresentationInterval = PresentInterval.One,
                PresentFlags = PresentFlags.None,
                SwapEffect = SwapEffect.Flip,
                Windowed = true,
            };

            //デバイスの作成
            _device = new Device(new Direct3DEx(), 0, DeviceType.Hardware, IntPtr.Zero, CreateFlags.HardwareVertexProcessing, pp);
            _effect = Effect.FromFile(_device, "PointSprite.fx", ShaderFlags.OptimizationLevel3);
            _effect.Technique = "PointSprite";

            //パラメータのセット
            _effect.SetValue("Transform", Matrix.Identity);
            _effect.SetValue("PointSize", pointSize);
        }

        void releaseDirect3D()
        {
            if (_device != null)
                _device.Dispose();
            if (_effect != null)
                _effect.Dispose();
            if (_vertexBuffer != null)
                _vertexBuffer.Dispose();
        }

        void setVertices(Vertex[] arr)
        {
            var count = arr.Length;

            if (0 == count)
            {
                _count = count;
                return;
            }

            //頂点バッファの更新
            lock (_vertexBufferLock)
            {
                if (_vertexBuffer != null)
                    _vertexBuffer.Dispose();

                //頂点バッファの作成
                var size = Marshal.SizeOf(typeof(Vertex));
                _vertexBuffer = new VertexBuffer(_device, size * count, Usage.None, Vertex.Format, Pool.Default);
                var st = _vertexBuffer.Lock(0, size * count, LockFlags.None);
                st.WriteRange<Vertex>(arr);
                _vertexBuffer.Unlock();

                _count = count;
            }
        }

        void render(ColorBGRA background)
        {
            ImageSource.Lock();
            ImageSource.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _device.GetBackBuffer(0, 0).NativePointer);

            _device.Clear(ClearFlags.Target, background, 0, 0);
            _device.BeginScene();
            _effect.Begin();

            _device.SetRenderState(RenderState.FillMode, FillMode.Point);
            _effect.BeginPass(0);
            if (0 < _count)
                lock (_vertexBufferLock)
                {
                    _device.SetStreamSource(0, _vertexBuffer, 0, Marshal.SizeOf(typeof(Vertex)));
                    _device.VertexFormat = Vertex.Format;
                    _device.DrawPrimitives(PrimitiveType.PointList, 0, _count);
                }
            _effect.EndPass();

            _device.SetRenderState(RenderState.FillMode, FillMode.Wireframe);
            _effect.BeginPass(0);
            if (0 < _count)
                lock (_vertexBufferLock)
                {
                    _device.SetStreamSource(0, _cameraVertexBuffer, 0, Marshal.SizeOf(typeof(Vertex)));
                    _device.VertexFormat = Vertex.Format;
                    _device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 7);
                }
            _effect.EndPass();

            _effect.End();
            _device.EndScene();

            ImageSource.AddDirtyRect(new Int32Rect(0, 0, ImageSource.PixelWidth, ImageSource.PixelHeight));
            ImageSource.Unlock();
        }

        void setMatrix(Matrix3D matrix)
        {
            if (!matrix.HasInverse)
                return;

            matrix.Invert();    //なんか反転しなくちゃダメだった？

            float[] mat = { (float)matrix.M11,      (float)matrix.M12,      (float)matrix.M13,      (float)matrix.M14, 
                            (float)matrix.M21,      (float)matrix.M22,      (float)matrix.M23,      (float)matrix.M24, 
                            (float)matrix.M31,      (float)matrix.M32,      (float)matrix.M33,      (float)matrix.M34, 
                            (float)matrix.OffsetX,  (float)matrix.OffsetY,  (float)matrix.OffsetZ,  (float)matrix.M44, };
            _effect.SetValue("Transform", mat);
        }

        /// <summary>
        /// DirectXに渡される頂点
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        struct Vertex
        {
            public const VertexFormat Format = VertexFormat.Position | VertexFormat.Normal;

            public Vector3 Position;
            public Vector3 Color;

            public Vertex(float x, float y, float z, float r, float g, float b)
            {
                Position.X = x;
                Position.Y = y;
                Position.Z = -z;    //左手系に変換

                Color.X = r;
                Color.Y = g;
                Color.Z = b;
            }
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
                releaseDirect3D();
            }
        }
        #endregion
    }
}
