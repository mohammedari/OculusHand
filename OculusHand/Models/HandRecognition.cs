using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenCvSharp.CPlusPlus;

namespace OculusHand.Models
{
    public class HandRecognition
    {
        //////////////////////////////////////////////
        #region Properties

        public double MaxDepth { get; private set; }
        public byte HandBlob { get; private set; }

        public Mesh Mesh { get; private set; }

        #endregion

        //////////////////////////////////////////////
        #region Public Methods

        public HandRecognition()
        {
            //[TODO]パラメータをコンフィグから設定するようにする
            MaxDepth = 0.8;
            HandBlob = 0;
        }

        /// <summary>
        /// 手の領域のメッシュを再構成します。
        /// </summary>
        public void UpdateMesh(GestureCameraData data)
        {
            Mesh = extractHandMesh(data);
        }

        #endregion

        //////////////////////////////////////////////
        #region Private Methods
        Mesh extractHandMesh(GestureCameraData data)
        {
            var mesh = new Mesh();
            mesh.TextureWidth = data.TextureWidth;
            mesh.TextureHeight = data.TextureHeight;
            mesh.Texture = data.Texture;

            //頂点の追加
            var delaunay = new Subdiv2D();
            delaunay.InitDelaunay(new Rect(0, 0, data.Width, data.Height));
            var indexDictionary = new Dictionary<Point2f, int>();
            int index = 0;
            for (int y = 0; y < data.Height; ++y)
                for (int x = 0; x < data.Width; ++x)
                {
                    //手領域以外は無視
                    if (data.Blob[data.Width * y + x] != HandBlob)
                        continue;

                    Point point;
                    if (data.TryGet(x, y, out point))
                    {
                        //遠いところにあるものは無視
                        if (MaxDepth < point.Z)
                            continue;

                        var p = new Point2f(x, y);
                        delaunay.Insert(p);
                        indexDictionary[p] = index;
                        mesh.AddPoint(point);
                        ++index;
                    }
                }

            //ドロネー三角形分割でインデックス列を作成
            var triangles = delaunay.GetTriangleList();
            foreach (var triangle in triangles)
            {
                var points = new Point2f[3];
                bool skip = false;
                for (int i = 0; i < 3; ++i)
                {
                    float x = triangle[i * 2];
                    float y = triangle[i * 2 + 1];
                    if (x < 0 || data.Width <= x || y < 0 || data.Height <= y)
                    {
                        skip = true;
                        break;
                    }

                    points[i] = new Point2f(x, y);
                }

                if (skip)
                    continue;

                //3頂点の深度画像座標の重心を切り捨ててfalseのインデックスになったら飛ばす
                var center = (points[0] + points[1] + points[2]);
                center.X /= 3;
                center.Y /= 3;
                if (data.Blob[data.Width * (int)center.Y + (int)center.X] != HandBlob)
                    continue;

                mesh.AddIndices(points.Select(p => indexDictionary[p]));
            }

            return mesh;
        }
        #endregion
    }
}
