using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MIConvexHull;

namespace OculusHand.Models
{
    public class HandRecognition
    {
        //////////////////////////////////////////////
        #region Properties

        //[TODO]手の領域抽出のためのパラメータ

        public Mesh Mesh { get; private set; }

        #endregion

        //////////////////////////////////////////////
        #region Public Methods
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
            //[TODO]Debug
            data = new GestureCameraData(data.Width, data.Height, data.TextureWidth, data.TextureHeight, data.Texture);
            data.Set(0, 0, new Point(0, 0, -0.8f, -0.8f, 0, 1, 1, 1));
            data.Set(2, 1, new Point(1, 0, 0.5f, -0.5f, 0, 1, 1, 1));
            data.Set(0, 3, new Point(0, 1, -0.5f, 0.5f, 0, 1, 1, 1));
            data.Set(4, 5, new Point(1, 1, 0.5f, 0.5f, 0, 1, 1, 1));

            var mesh = new Mesh();
            mesh.TextureWidth = data.TextureWidth;
            mesh.TextureHeight = data.TextureHeight;
            mesh.Texture = data.Texture;

            //頂点の追加
            var vertices = new List<Vertex>();
            int index = 0;
            for (int y = 0; y < data.Height; ++y)
                for (int x = 0; x < data.Width; ++x)
                {
                    //[TODO]微分して手の領域の頂点を抽出する
                    //領域内の頂点をとってきて、Enumマップに書き込む
                    //DFSで探してForegroundかBlankならtrueを返す、Backgroundならfalseを返す
                    //点の距離でForegroundかBackgroundを書き込む、抜けてる点はBlankを書き込んで周りの戻り値がtrueならtrue

                    Point point;
                    if (data.TryGet(x, y, out point))
                    {
                        vertices.Add(new Vertex(x, y, index));
                        mesh.AddPoint(point);
                        ++index;
                    }
                }

            //ドロネー三角形分割でインデックス列を作成
            var voronoi = VoronoiMesh.Create<Vertex, Cell>(vertices);
            foreach (var cell in voronoi.Vertices)
            {
                //[TODO]3頂点の深度画像座標の重心を切り捨てるとtrueのインデックスになるものだけ平面にする！
                mesh.AddIndices(cell.Indices);
            }

            return mesh;
        }
        #endregion

        //////////////////////////////////////////////
        #region Internal Classes
        class Vertex : IVertex
        {
            public int Index;

            public Vertex(int x, int y, int index)
            {
                Position = new double[2];
                Position[0] = x;
                Position[1] = y;
                Index = index;
            }

            public double[] Position { get; set; }
        }

        class Cell : TriangulationCell<Vertex, Cell>
        {
            public IEnumerable<int> Indices
            {
                get
                {
                    return Vertices.Select(v => v.Index);
                }
            }
        }
        #endregion
    }
}
