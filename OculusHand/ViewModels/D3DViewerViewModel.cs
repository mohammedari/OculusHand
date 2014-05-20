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
using SharpDX.DXGI;
using SharpDX.D3DCompiler;
using Dx9 = SharpDX.Direct3D9;
using SharpDX.Direct3D10;
using Device = SharpDX.Direct3D10.Device;
using Buffer = SharpDX.Direct3D10.Buffer;
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

        readonly Color4 _fillColor = SharpDX.Color.White;

        Device _device;
        Effect _effect;
        EffectTechnique _technique;

        object _bufferUpdateLock = new object();
        int _vertexCount;
        int _indexCount;
        Buffer _vertexBuffer;
        Buffer _indexBuffer;
        Texture2D _texture;

        Buffer _surfaceVertexBuffer;
        Buffer _surfaceIndexBuffer;
        Texture2D _background;
        int _surfaceVertexCount;
        int _surfaceIndexCount;

        Texture2D _distortion;
        Texture2D _offset;
        Texture2D _renderTarget;
        Texture2D _depthStencil;

        RenderTargetView _distortionView;
        RenderTargetView _offsetView;
        RenderTargetView _renderTargetView;
        DepthStencilView _depthStencilView;

        int _width, _height;

        #endregion

        ////////////////////////////////////////////////////
        #region ImageSource変更通知プロパティ
        private DX10ImageSource _imageSource;
        /// <summary>
        /// レンダリング結果の画像イメージです。ViewのImageSourceにバインドしてください。
        /// </summary>
        public DX10ImageSource ImageSource
        {
            get
            { return _imageSource; }
            set
            { 
                if (_imageSource == value)
                    return;
                _imageSource = value;
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
            _imageSource = new DX10ImageSource();

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
            float[] matValues = { (float)matrix.M11,      (float)matrix.M12,      (float)matrix.M13,      (float)matrix.M14, 
                            (float)matrix.M21,      (float)matrix.M22,      (float)matrix.M23,      (float)matrix.M24, 
                            (float)matrix.M31,      (float)matrix.M32,      (float)matrix.M33,      (float)matrix.M34, 
                            (float)matrix.OffsetX,  (float)matrix.OffsetY,  (float)matrix.OffsetZ,  (float)matrix.M44, };
            _effect.GetVariableByName("OculusOrientation").AsMatrix().SetMatrix(new Matrix(matValues));
        }

        /// <summary>
        /// Oculus向けのBarrelDistortionのためのパラメータを設定します。
        /// </summary>
        /// <param name="parameter">Distortionパラメータ</param>
        public void UpdateDistortionParameter(OculusDistortionParameter parameter)
        {
            _effect.GetVariableByName("DistortionParameter").AsVector().Set(new Vector4(parameter.DistortionK));

            //[TODO]正しいパラメータを適用する
            //_effect.GetVariableByName("LensHorizontalDistanceRatioFromCenter").AsScalar().Set(parameter.LensSeparationDistance / parameter.ScreenWidthDistance);
            _effect.GetVariableByName("LensHorizontalDistanceRatioFromCenter").AsScalar().Set(Util.GetConfigManager().Parameters.LensHorizontalDistanceRatioFromCenter);
        }

        /// <summary>
        /// 背景画像を更新します。
        /// </summary>
        public void UpdateBackground(string filename)
        {
            if (_background != null)
                _background.Dispose();

            _background = Texture2D.FromFile<Texture2D>(_device, filename);
            _effect.GetVariableByName("BackgroundImage").AsShaderResource().SetResource(new ShaderResourceView(_device, _background));
        }

        /// <summary>
        /// レンダリングを行います。
        /// </summary>
        public void Render()
        {
            render(_fillColor, _width, _height);
        }
        #endregion

        ////////////////////////////////////////////////////
        #region 内部処理の実装

        void initializeDirect3D(int backBufferWidth, int backBufferHeight, 
                                int surfaceResolutionWidth, int surfaceResolutionHeight, float thetaMappingDepth,
                                double cameraPitchAngle, double cameraOffsetY, double cameraScale, float offsetU)
        {
            _width = backBufferWidth;
            _height = backBufferHeight;

            //バックバッファのフォーマット
            Texture2DDescription colordesc = new Texture2DDescription
            {
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = Format.B8G8R8A8_UNorm,
                Width = backBufferWidth,
                Height = backBufferHeight,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.Shared,
                CpuAccessFlags = CpuAccessFlags.None,
                ArraySize = 1
            };

            //デプスステンシルのフォーマット
            //BackBufferと同じサイズで、出力結合ステージで使用
            Texture2DDescription depthdesc = new Texture2DDescription
            {
                BindFlags = BindFlags.DepthStencil,
                Format = Format.D32_Float_S8X24_UInt,
                Width = backBufferWidth,
                Height = backBufferHeight,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.None,
                CpuAccessFlags = CpuAccessFlags.None,
                ArraySize = 1,
            };

            //デバイスの作成
            //[TODO]Debug
            //_device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.Debug);
            _device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);

            //エフェクトの作成
            var compileResult = ShaderBytecode.CompileFromFile("Mesh.fx", "fx_4_0", ShaderFlags.OptimizationLevel3, EffectFlags.None, null, null);
            if (compileResult.HasErrors)
                throw new InvalidOperationException("Effect file includes compilation errors. " + compileResult.Message); 
            _effect = new Effect(_device, compileResult.Bytecode);
            _effect.GetVariableByName("Transform").AsMatrix().SetMatrix(Matrix.Identity);
            _technique = _effect.GetTechniqueByName("Mesh");

            //頂点レイアウトの設定
            var vertexLayout = new InputLayout(_device, _technique.GetPassByIndex(0).Description.Signature, new[] {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0) 
            });
            _device.InputAssembler.InputLayout = vertexLayout;
            _device.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;

            //背景描画用のメッシュの作成
            makeSurface(surfaceResolutionWidth, surfaceResolutionHeight);
            _effect.GetVariableByName("ThetaMappingDepth").AsScalar().Set(thetaMappingDepth);

            //Barrel Distortion用のテクスチャを作成
            _distortion = new Texture2D(_device, colordesc);
            _distortionView = new RenderTargetView(_device, _distortion);
            _effect.GetVariableByName("Distortion").AsShaderResource().SetResource(new ShaderResourceView(_device, _distortion));

            //オフセット表示用のテクスチャを作成
            _offset = new Texture2D(_device, colordesc);
            _offsetView = new RenderTargetView(_device, _offset);
            _effect.GetVariableByName("Offset").AsShaderResource().SetResource(new ShaderResourceView(_device, _offset));
            _effect.GetVariableByName("OffsetU").AsScalar().Set(offsetU);

            //描画ターゲットテクスチャの作成
            _renderTarget = new Texture2D(_device, colordesc);
            _renderTargetView = new RenderTargetView(_device, _renderTarget);
            _depthStencil = new Texture2D(_device, depthdesc);
            _depthStencilView = new DepthStencilView(_device, _depthStencil);

            //ImageSourceにRenderTargetを設定
            _imageSource.SetRenderTargetDX10(_renderTarget);

            //Handメッシュの座標変換を設定
            var matrix = Matrix3D.Identity;
            matrix.Rotate(new System.Windows.Media.Media3D.Quaternion(new Vector3D(1, 0, 0), cameraPitchAngle));
            matrix.OffsetY = cameraOffsetY;
            matrix.Scale(new Vector3D(cameraScale, cameraScale, cameraScale));

            float[] matValues = { (float)matrix.M11,      (float)matrix.M12,      (float)matrix.M13,      (float)matrix.M14, 
                            (float)matrix.M21,      (float)matrix.M22,      (float)matrix.M23,      (float)matrix.M24, 
                            (float)matrix.M31,      (float)matrix.M32,      (float)matrix.M33,      (float)matrix.M34, 
                            (float)matrix.OffsetX,  (float)matrix.OffsetY,  (float)matrix.OffsetZ,  (float)matrix.M44, };
            _effect.GetVariableByName("Transform").AsMatrix().SetMatrix(new Matrix(matValues));
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
            
            //[TODO]なぜかBuffer作成時にDataStreamを指定するとメモリアクセス違反？
            //現状DynamicなBufferしか作れないのを何とかする

            //頂点バッファを作成して書き込み
            _surfaceVertexBuffer = new Buffer(_device, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = vertices.Count * Marshal.SizeOf(typeof(Vertex)),
                Usage = ResourceUsage.Dynamic
            });
            var vertexStream = _surfaceVertexBuffer.Map(MapMode.WriteDiscard);
            vertexStream.WriteRange(vertices.ToArray());
            _surfaceVertexBuffer.Unmap();


            //インデックスバッファを作成して書き込み
            _surfaceIndexBuffer = new Buffer(_device, new BufferDescription()
            {
                BindFlags = BindFlags.IndexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write, 
                OptionFlags = ResourceOptionFlags.None, 
                SizeInBytes = indices.Count * Marshal.SizeOf(typeof(int)),
                Usage = ResourceUsage.Dynamic
            });
            var indexStream = _surfaceIndexBuffer.Map(MapMode.WriteDiscard);
            indexStream.WriteRange(indices.ToArray());
            _surfaceIndexBuffer.Unmap();

            _surfaceVertexCount = vertices.Count;
            _surfaceIndexCount = indices.Count;
        }

        void setIndices(int[] arr)
        {
            //インデックスバッファの作成
            var size = Marshal.SizeOf(typeof(int));
            if (_indexBuffer == null || _indexCount < arr.Length)
            {
                if (null != _indexBuffer)
                    _indexBuffer.Dispose();

                _indexBuffer = new Buffer(_device, new BufferDescription()
                {
                    BindFlags = BindFlags.IndexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = size * arr.Length,
                    Usage = ResourceUsage.Dynamic
                });
            }

            var ds = _indexBuffer.Map(MapMode.WriteDiscard);
            ds.WriteRange(arr);
            _indexBuffer.Unmap();
        }

        void setVertices(Vertex[] arr)
        {
            //頂点バッファの作成
            var size = Marshal.SizeOf(typeof(Vertex));
            if (_vertexBuffer == null || _vertexCount < arr.Length)
            {
                if (null != _vertexBuffer)
                    _vertexBuffer.Dispose();

                _vertexBuffer = new Buffer(_device, new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = size * arr.Length,
                    Usage = ResourceUsage.Dynamic
                });
            }

            var ds = _vertexBuffer.Map(MapMode.WriteDiscard);
            ds.WriteRange(arr);
            _vertexBuffer.Unmap();
        }

        void setTexture(byte[] color, int width, int height)
        {
            if (_texture == null || 
                _texture.Description.Width != width || 
                _texture.Description.Height != height)
            {
                if (_texture != null)
                    _texture.Dispose();

                Texture2DDescription colordesc = new Texture2DDescription
                {
                    BindFlags = BindFlags.ShaderResource,
                    Format = Format.B8G8R8A8_UNorm,
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Dynamic,
                    OptionFlags = ResourceOptionFlags.None,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    ArraySize = 1
                };

                //テクスチャのセット
                _texture = new Texture2D(_device, colordesc);
                _effect.GetVariableByName("HandTexture").AsShaderResource().SetResource(new ShaderResourceView(_device, _texture));
            }
            
            //テクスチャの書き込み
            var data = _texture.Map(Texture2D.CalculateSubResourceIndex(0, 0, 1), MapMode.WriteDiscard, SharpDX.Direct3D10.MapFlags.None);
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
            _texture.Unmap(Texture2D.CalculateSubResourceIndex(0, 0, 1));
        }

        void render(Color4 background, int width, int height)
        {
            _device.Rasterizer.SetViewports(new Viewport(0, 0, width, height, 0.0f, 1.0f));

            //通常のイメージを描画Distortion用のテクスチャに描画
            _device.OutputMerger.SetTargets(_depthStencilView, _distortionView);
            _device.ClearRenderTargetView(_distortionView, background);
            _device.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            drawBackground();
            _device.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            drawHand();

            //Offset用のテクスチャに最終画面を描画
            _device.OutputMerger.SetTargets(_depthStencilView, _offsetView);
            _device.ClearRenderTargetView(_offsetView, background);
            _device.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            drawBarrelDistortion();

            //最終画面をオフセットして描画
            _device.OutputMerger.SetTargets(_depthStencilView, _renderTargetView);
            _device.ClearRenderTargetView(_renderTargetView, background);
            _device.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            drawOffset();

            //WPFの画像を更新
            _device.Flush();
            ImageSource.InvalidateD3DImage();
        }

        void drawBackground()
        {
            _device.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_surfaceVertexBuffer, Marshal.SizeOf(typeof(Vertex)), 0));
            _device.InputAssembler.SetIndexBuffer(_surfaceIndexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);

            _technique.GetPassByIndex(2).Apply();
            _device.DrawIndexed(_surfaceIndexCount, 0, 0);
        }

        void drawHand()
        {
            if (_vertexBuffer == null || _indexBuffer == null)
                return;

            _device.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, Marshal.SizeOf(typeof(Vertex)), 0));
            _device.InputAssembler.SetIndexBuffer(_indexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);

            _technique.GetPassByIndex(0).Apply();
            _device.DrawIndexed(_indexCount, 0, 0);
        }

        void drawBarrelDistortion()
        {
            _device.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_surfaceVertexBuffer, Marshal.SizeOf(typeof(Vertex)), 0));
            _device.InputAssembler.SetIndexBuffer(_surfaceIndexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);

            _technique.GetPassByIndex(4).Apply();
            _device.DrawIndexed(_surfaceIndexCount, 0, 0);
        }

        void drawOffset()
        {
            _device.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_surfaceVertexBuffer, Marshal.SizeOf(typeof(Vertex)), 0));
            _device.InputAssembler.SetIndexBuffer(_surfaceIndexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);

            _technique.GetPassByIndex(5).Apply();
            _device.DrawIndexed(_surfaceIndexCount, 0, 0);
        }

        /// <summary>
        /// DirectXに渡される頂点
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        struct Vertex
        {
            public Vector3 Position;
            public Vector2 Texture;

            public Vertex(float x, float y, float z, float u, float v)
            {
                Position.X = x;
                Position.Y = y;
                Position.Z = z;

                Texture.X = u;
                Texture.Y = v;
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
