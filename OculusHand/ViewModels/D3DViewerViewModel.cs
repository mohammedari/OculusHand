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

        readonly ColorBGRA _fillColor = SharpDX.Color.White;

        Device _device;
        Effect _effect;

        object _bufferUpdateLock = new object();
        int _vertexCount;
        int _indexCount;
        VertexBuffer _vertexBuffer;
        IndexBuffer _indexBuffer;
        Texture _texture;

        VertexBuffer _surfaceVertexBuffer;
        IndexBuffer _surfaceIndexBuffer;
        Texture _background;
        int _surfaceVertexCount;
        int _surfaceIndexCount;

        Texture _distortion;
        Texture _offset;

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
                config.Parameters.DistortionSurfaceResolutionWidth, 
                config.Parameters.DistortionSurfaceResolutionHeight, 
                config.Parameters.DistortionThetaMappingDepth, 
                config.Parameters.CameraPitchAngle, 
                config.Parameters.CameraOffsetY, 
                config.Parameters.CameraScale, 
                config.Parameters.OffsetU
                );
        }

        ~D3DViewerViewModel()
        {
            Dispose(false);
        }
        #endregion

        ////////////////////////////////////////////////////
        #region Public Methods

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
        /// 背景レンダリングの際に用いる画像の方向を更新します。
        /// </summary>
        public void UpdateOrientation(Matrix3D matrix)
        {
            float[] mat = { (float)matrix.M11,      (float)matrix.M12,      (float)matrix.M13,      (float)matrix.M14, 
                            (float)matrix.M21,      (float)matrix.M22,      (float)matrix.M23,      (float)matrix.M24, 
                            (float)matrix.M31,      (float)matrix.M32,      (float)matrix.M33,      (float)matrix.M34, 
                            (float)matrix.OffsetX,  (float)matrix.OffsetY,  (float)matrix.OffsetZ,  (float)matrix.M44, };
            _effect.SetValue("OculusOrientation", mat);
        }

        /// <summary>
        /// Oculus向けのBarrelDistortionのためのパラメータを設定します。
        /// </summary>
        /// <param name="parameter">Distortionパラメータ</param>
        public void UpdateDistortionParameter(OculusDistortionParameter parameter)
        {
            _effect.SetValue("DistortionParameter", parameter.DistortionK);
            _effect.SetValue("LensHorizontalDistanceRatioFromCenter", parameter.LensSeparationDistance / parameter.ScreenWidthDistance);
        }

        /// <summary>
        /// 背景画像を更新します。
        /// </summary>
        public void UpdateBackground(string filename)
        {
            if (_background != null)
                _background.Dispose();

            _background = Texture.FromFile(_device, filename);
            var handle = _effect.GetParameter(null, "BackgroundImage");
            _effect.SetTexture(handle, _background);
        }

        /// <summary>
        /// レンダリングを行います。
        /// </summary>
        public void Render()
        {
            render(_fillColor);
        }
        #endregion

        ////////////////////////////////////////////////////
        #region 内部処理の実装

        void initializeDirect3D(int backBufferWidth, int backBufferHeight, 
                                int surfaceResolutionWidth, int surfaceResolutionHeight, float thetaMappingDepth,
                                double cameraPitchAngle, double cameraOffsetY, double cameraScale, float offsetU)
        {
            PresentParameters pp = new PresentParameters()
            {
                DeviceWindowHandle = IntPtr.Zero,

                EnableAutoDepthStencil = false,

                MultiSampleType = MultisampleType.None,

                BackBufferCount = 2,
                BackBufferWidth = backBufferWidth,
                BackBufferHeight = backBufferHeight,
                BackBufferFormat = Format.A8R8G8B8,

                PresentationInterval = PresentInterval.One,
                PresentFlags = PresentFlags.None,
                SwapEffect = SwapEffect.Flip,
                Windowed = true,
            };

            //デバイスの作成
            _device = new Device(new Direct3DEx(), 0, DeviceType.Hardware, IntPtr.Zero, CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded, pp);
            _device.VertexFormat = Vertex.Format;

            //エフェクトの作成
            _effect = Effect.FromFile(_device, "Mesh.fx", ShaderFlags.OptimizationLevel3);
            _effect.Technique = "Mesh";
            _effect.SetValue("Transform", Matrix.Identity);

            //背景描画用のメッシュの作成
            _surfaceVertexBuffer = new VertexBuffer(_device, backBufferWidth * backBufferHeight * Marshal.SizeOf(typeof(Vertex)), Usage.WriteOnly, Vertex.Format, Pool.Default);
            _surfaceIndexBuffer = new IndexBuffer(_device, backBufferWidth * backBufferHeight * Marshal.SizeOf(typeof(int)), Usage.WriteOnly, Pool.Default, false);
            makeSurface(surfaceResolutionWidth, surfaceResolutionHeight);
            _effect.SetValue("ThetaMappingDepth", thetaMappingDepth);

            //Barrel Distortion用のテクスチャを作成
            _distortion = new Texture(_device, backBufferWidth, backBufferHeight, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
            _effect.SetTexture(_effect.GetParameter(null, "Distortion"), _distortion);

            //オフセット表示用のテクスチャを作成
            _offset = new Texture(_device, backBufferWidth, backBufferHeight, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
            _effect.SetTexture(_effect.GetParameter(null, "Offset"), _offset);
            _effect.SetValue("OffsetU", offsetU);

            //Handメッシュの座標変換を設定
            var matrix = Matrix3D.Identity;
            matrix.Rotate(new System.Windows.Media.Media3D.Quaternion(new Vector3D(1, 0, 0), cameraPitchAngle));
            matrix.OffsetY = cameraOffsetY;
            matrix.Scale(new Vector3D(cameraScale, cameraScale, cameraScale));

            float[] mat = { (float)matrix.M11,      (float)matrix.M12,      (float)matrix.M13,      (float)matrix.M14, 
                            (float)matrix.M21,      (float)matrix.M22,      (float)matrix.M23,      (float)matrix.M24, 
                            (float)matrix.M31,      (float)matrix.M32,      (float)matrix.M33,      (float)matrix.M34, 
                            (float)matrix.OffsetX,  (float)matrix.OffsetY,  (float)matrix.OffsetZ,  (float)matrix.M44, };
            _effect.SetValue("Transform", mat);
        }

        void releaseDirect3D()
        {
            if (_vertexBuffer != null)
                _vertexBuffer.Dispose();
            if (_indexBuffer != null)
                _indexBuffer.Dispose();
            if (_texture != null)
                _texture.Dispose();

            if (_surfaceVertexBuffer != null)
                _surfaceVertexBuffer.Dispose();
            if (_surfaceIndexBuffer != null)
                _surfaceIndexBuffer.Dispose();
            if (_background != null)
                _background.Dispose();

            if (_effect != null)
                _effect.Dispose();
            if (_device != null)
                _device.Dispose();
        }

        void makeSurface(int width, int height)
        {
            var vertices = new List<Vertex>();
            for (int y = 0; y <= height; ++y)
                for (int x = 0; x <= width; ++x)
                    vertices.Add(new Vertex(2 * (x - width / 2.0f) / width,
                                            2 * (y - height / 2.0f) / height, 
                                            0, 
                                            (float)x / width, 
                                            (float)y / height));

            var indices = new List<int>();
            for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                {
                    indices.Add(y * (width + 1) + x);
                    indices.Add((y + 1) * (width + 1) + x + 1);
                    indices.Add(y * (width + 1) + x + 1);

                    indices.Add(y * (width + 1) + x);
                    indices.Add((y + 1) * (width + 1) + x);
                    indices.Add((y + 1) * (width + 1) + x + 1);
                }

            var vst = _surfaceVertexBuffer.Lock(0, vertices.Count * Marshal.SizeOf(typeof(Vertex)), LockFlags.None);
            vst.WriteRange(vertices.ToArray());
            _surfaceVertexBuffer.Unlock();

            var ist = _surfaceIndexBuffer.Lock(0, indices.Count * Marshal.SizeOf(typeof(int)), LockFlags.None);
            ist.WriteRange(indices.ToArray());
            _surfaceIndexBuffer.Unlock();

            _surfaceVertexCount = vertices.Count;
            _surfaceIndexCount = indices.Count;
        }

        void setIndices(int[] arr)
        {
            //インデックスバッファの作成
            var size = Marshal.SizeOf(typeof(int));
            if (_indexBuffer == null || _indexCount < arr.Length)
            {
                _indexBuffer = new IndexBuffer(_device, size * arr.Length, Usage.WriteOnly | Usage.Dynamic, Pool.Default, false);
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
            }

            var st = _vertexBuffer.Lock(0, size * arr.Length, LockFlags.None);
            st.WriteRange<Vertex>(arr);
            _vertexBuffer.Unlock();
        }

        void setTexture(byte[] color, int width, int height)
        {
            if (_texture == null || 
                _texture.GetLevelDescription(0).Width != width || 
                _texture.GetLevelDescription(0).Height != height)
            {
                if (_texture != null)
                    _texture.Dispose();

                _texture = new Texture(_device, width, height, 1, Usage.Dynamic, Format.A8R8G8B8, Pool.Default);

                //テクスチャのセット
                var handle = _effect.GetParameter(null, "HandTexture");
                _effect.SetTexture(handle, _texture);
            }
            
            //テクスチャの書き込み
            var data = _texture.LockRectangle(0, LockFlags.None);
            unsafe
            {
                var ptr = (byte*)data.DataPointer.ToPointer();
                for (int y = 0; y < height; ++y)
                    for (int x = 0; x < width; ++x)
                    {
                        ptr[(y * width + x) * 4 + 0] = color[(y * width + x) * 3 + 0];  //B
                        ptr[(y * width + x) * 4 + 1] = color[(y * width + x) * 3 + 1];  //G
                        ptr[(y * width + x) * 4 + 2] = color[(y * width + x) * 3 + 2];  //R
                        ptr[(y * width + x) * 4 + 3] = 255;                             //A
                    }
            }
            _texture.UnlockRectangle(0);
        }

        void render(ColorBGRA background)
        {
            ImageSource.Lock();
            ImageSource.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _device.GetBackBuffer(0, 0).NativePointer);

            //通常のイメージを描画Distortion用のテクスチャに描画
            var renderTarget = _device.GetRenderTarget(0);
            _device.SetRenderTarget(0, _distortion.GetSurfaceLevel(0));

            _device.Clear(ClearFlags.Target, background, 0, 0);
            _device.BeginScene();
            _effect.Begin();

            drawBackground();
            drawHand();

            _effect.End();
            _device.EndScene();

            //Offset用のテクスチャに最終画面を描画
            _device.SetRenderTarget(0, _offset.GetSurfaceLevel(0));

            _device.Clear(ClearFlags.Target, background, 0, 0);
            _device.BeginScene();
            _effect.Begin();

            drawBarrelDistortion();

            _effect.End();
            _device.EndScene();

            //最終画面をオフセットして描画
            _device.SetRenderTarget(0, renderTarget);

            _device.Clear(ClearFlags.Target, background, 0, 0);
            _device.BeginScene();
            _effect.Begin();

            drawOffset();

            _effect.End();
            _device.EndScene();

            ImageSource.AddDirtyRect(new Int32Rect(0, 0, ImageSource.PixelWidth, ImageSource.PixelHeight));
            ImageSource.Unlock();
        }

        void drawBackground()
        {
            _device.SetStreamSource(0, _surfaceVertexBuffer, 0, Marshal.SizeOf(typeof(Vertex)));
            _device.Indices = _surfaceIndexBuffer;

            _device.SetRenderState(RenderState.FillMode, FillMode.Solid);
            _effect.BeginPass(2);
            _device.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, _surfaceVertexCount, 0, _surfaceIndexCount / 3);
            _effect.EndPass();
        }

        void drawHand()
        {
            if (_vertexBuffer == null || _indexBuffer == null)
                return;

            _device.SetStreamSource(0, _vertexBuffer, 0, Marshal.SizeOf(typeof(Vertex)));
            _device.Indices = _indexBuffer;

            //Mesh 
            _device.SetRenderState(RenderState.FillMode, FillMode.Solid);
            _effect.BeginPass(0);
            if (0 < _vertexCount && 0 < _indexCount)
                lock (_bufferUpdateLock)
                {
                    _device.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, _vertexCount, 0, _indexCount / 3);
                }
            _effect.EndPass();

            //Wire frame
            //_device.SetRenderState(RenderState.FillMode, FillMode.Wireframe);
            //_effect.BeginPass(1);
            //if (0 < _vertexCount && 0 < _indexCount)
            //    lock (_bufferUpdateLock)
            //    {
            //        _device.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, _vertexCount, 0, _indexCount / 3);
            //    }
            //_effect.EndPass();
        }

        void drawBarrelDistortion()
        {
            _device.SetStreamSource(0, _surfaceVertexBuffer, 0, Marshal.SizeOf(typeof(Vertex)));
            _device.Indices = _surfaceIndexBuffer;

            _device.SetRenderState(RenderState.FillMode, FillMode.Solid);
            _effect.BeginPass(4);
            _device.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, _surfaceVertexCount, 0, _surfaceIndexCount / 3);
            _effect.EndPass();
        }

        void drawOffset()
        {
            _device.SetStreamSource(0, _surfaceVertexBuffer, 0, Marshal.SizeOf(typeof(Vertex)));
            _device.Indices = _surfaceIndexBuffer;

            _device.SetRenderState(RenderState.FillMode, FillMode.Solid);
            _effect.BeginPass(5);
            _device.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, _surfaceVertexCount, 0, _surfaceIndexCount / 3);
            _effect.EndPass();
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
