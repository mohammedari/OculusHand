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

using OculusHand.Models;
using SharpDX;
using SharpDX.Direct3D9;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using System.Drawing;

namespace OculusHand.ViewModels
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

        object _bufferUpdateLock = new object();
        int _vertexCount;
        int _indexCount;
        VertexBuffer _vertexBuffer;
        IndexBuffer _indexBuffer;
        Texture _texture;

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
        public void UpdateMesh(OculusHand.Models.Mesh mesh)
        {
            //左手系に変換
            var vertices = mesh.Points.Select(p => new Vertex(p.X, p.Y, -p.Z, p.U, p.V)).ToArray();
            var indices = mesh.Indices.ToArray();

            if (indices.Length == 0)
            {
                _indexCount = 0;
                return;
            }

            lock (_bufferUpdateLock)
            {
                setVertices(vertices);
                setIndices(indices);
                setTexture(mesh.Texture, mesh.TextureWidth, mesh.TextureHeight);
                _vertexCount = vertices.Length;
                _indexCount = indices.Length;
            }
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
            _device = new Device(new Direct3DEx(), 0, DeviceType.Hardware, IntPtr.Zero, CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded, pp);
            _device.VertexFormat = Vertex.Format;
            _device.SetRenderState(RenderState.FillMode, FillMode.Solid);

            //エフェクトの作成
            _effect = Effect.FromFile(_device, "Mesh.fx", ShaderFlags.OptimizationLevel3);
            _effect.Technique = "Mesh";
            _effect.SetValue("Transform", Matrix.Identity);
        }

        void releaseDirect3D()
        {
            if (_device != null)
                _device.Dispose();
            if (_effect != null)
                _effect.Dispose();
            if (_vertexBuffer != null)
                _vertexBuffer.Dispose();
            if (_indexBuffer != null)
                _indexBuffer.Dispose();
            if (_texture != null)
                _texture.Dispose();
        }

        void setIndices(int[] arr)
        {
            //インデックスバッファの作成
            var size = Marshal.SizeOf(typeof(int));
            if (_indexBuffer == null || _indexCount < arr.Length)
            {
                _indexBuffer = new IndexBuffer(_device, size * arr.Length, Usage.WriteOnly | Usage.Dynamic, Pool.Default, false);
                _device.Indices = _indexBuffer;
            }

            var st = _indexBuffer.Lock(0, size * arr.Length, LockFlags.None);
            st.WriteRange<int>(arr);
            _indexBuffer.Unlock();
        }

        void setVertices(Vertex[] arr)
        {
            //頂点バッファの作成
            var size = Marshal.SizeOf(typeof(Vertex));
            if (_vertexBuffer == null || _vertexCount < arr.Length)
            {
                _vertexBuffer = new VertexBuffer(_device, size * arr.Length, Usage.WriteOnly | Usage.Dynamic, Vertex.Format, Pool.Default);
                _device.SetStreamSource(0, _vertexBuffer, 0, Marshal.SizeOf(typeof(Vertex)));
            }

            var st = _vertexBuffer.Lock(0, size * arr.Length, LockFlags.None);
            st.WriteRange<Vertex>(arr);
            _vertexBuffer.Unlock();
        }

        void setTexture(byte[] arr, int width, int height)
        {
            if (_texture == null || 
                _texture.GetLevelDescription(0).Width != width || 
                _texture.GetLevelDescription(0).Height != height)
            {
                if (_texture != null)
                    _texture.Dispose();

                _texture = new Texture(_device, width, height, 1, Usage.Dynamic, Format.A8R8G8B8, Pool.Default); //エラーが出る

                //テクスチャのセット
                var handle = _effect.GetParameter(null, "HandTexture");
                _effect.SetTexture(handle, _texture);
            }

            //テクスチャの書き込み
            var data = _texture.LockRectangle(0, LockFlags.None);
            using (var ds = new DataStream(data.DataPointer, width * height * 4, false, true))
            {
                for (int i = 0; i < width * height; ++i)
                {
                    ds.Write(arr[i * 3 + 0]); //B
                    ds.Write(arr[i * 3 + 1]); //G
                    ds.Write(arr[i * 3 + 2]); //R
                    ds.WriteByte(255);        //A
                }
            }
            _texture.UnlockRectangle(0);
        }

        void render(ColorBGRA background)
        {
            ImageSource.Lock();
            ImageSource.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _device.GetBackBuffer(0, 0).NativePointer);

            _device.Clear(ClearFlags.Target, background, 0, 0);
            _device.BeginScene();
            _effect.Begin();

            _device.SetRenderState(RenderState.FillMode, FillMode.Solid);
            _effect.BeginPass(0);
            if (0 < _vertexCount && 0 < _indexCount)
                lock (_bufferUpdateLock)
                {
                    _device.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, _vertexCount, 0, _indexCount / 3);
                }
            _effect.EndPass();

            _device.SetRenderState(RenderState.FillMode, FillMode.Wireframe);
            _effect.BeginPass(1);
            if (0 < _vertexCount && 0 < _indexCount)
                lock (_bufferUpdateLock)
                {
                    _device.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, _vertexCount, 0, _indexCount / 3);
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
            //[TODO]VertexFormat.Texture0だとシェーダに値が渡らない問題を解決する
            public static readonly VertexFormat Format = VertexFormat.Position | VertexFormat.Normal;

            public Vector3 Position;
            public Vector3 Texture;

            public Vertex(float x, float y, float z, float u, float v)
            {
                Position.X = x;
                Position.Y = y;
                Position.Z = z;

                Texture.X = u;
                Texture.Y = v;
                Texture.Z = 0;
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
