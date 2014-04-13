using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace OculusHand.Models
{
    /// <summary>
    /// GestureCameraから取得された点群で構成されるメッシュを表現するクラス
    /// </summary>
    public class Mesh
    {
        List<Point> _points = new List<Point>();
        List<int> _indices = new List<int>();

        /// <summary>
        /// メッシュを構成する点群を取得します。
        /// </summary>
        public IEnumerable<Point> Points
        {
            get { return _points; }
            set { _points = value.ToList(); }
        }

        /// <summary>
        /// メッシュを構成する平面のインデックス列を取得します。
        /// </summary>
        public IEnumerable<int> Indices
        {
            get { return _indices; }
            set { _indices = value.ToList(); }
        }

        /// <summary>
        /// テクスチャ画像の幅を取得します。
        /// </summary>
        public int TextureWidth { get; set; }

        /// <summary>
        /// テクスチャ画像の高さを取得します。
        /// </summary>
        public int TextureHeight { get; set; }

        /// <summary>
        /// テクスチャを取得します。
        /// </summary>
        public byte[] Texture { get; set; }

        /// <summary>
        /// ブロブ画像の幅を取得します。
        /// </summary>
        public int BlobWidth { get; set; }

        /// <summary>
        /// ブロブ画像の高さを取得します。
        /// </summary>
        public int BlobHeight { get; set; }

        /// <summary>
        /// ブロブ画像を取得します。
        /// </summary>
        public byte[] Blob { get; set; }

        /// <summary>
        /// ブロブ画像中の背景に対応する値
        /// </summary>
        public byte BackgroundBlob { get; set; }

        /////////////////////////////////////////////
        
        /// <summary>
        /// メッスを構成する頂点を追加します。
        /// </summary>
        /// <param name="point">頂点</param>
        public void AddPoint(Point point)
        {
            _points.Add(point);
        }

        /// <summary>
        /// メッシュを構成する平面のインデックス列を追加します。
        /// </summary>
        /// <param name="indices">平面のインデックス列</param>
        public void AddIndices(IEnumerable<int> indices)
        {
            _indices.AddRange(indices);
        }
    }
}
