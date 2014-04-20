using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

<<<<<<< HEAD
using MIConvexHull;

=======
>>>>>>> develop
namespace OculusHand.Models
{
    public class HandRecognition
    {
        //////////////////////////////////////////////
        #region Properties

<<<<<<< HEAD
        //[TODO]手の領域抽出のためのパラメータ
=======
        public float MaxDepth { get; private set; }
        public float MaxDepthGap { get; private set; }
        public int Skip { get; private set; }
>>>>>>> develop

        public Mesh Mesh { get; private set; }

        #endregion

        //////////////////////////////////////////////
        #region Public Methods
<<<<<<< HEAD
=======

        public HandRecognition(float maxDepth, float maxDepthGap, int skip)
        {
            MaxDepth = maxDepth;
            MaxDepthGap = maxDepthGap;
            Skip = skip;
        }

>>>>>>> develop
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
<<<<<<< HEAD
=======

>>>>>>> develop
            mesh.TextureWidth = data.TextureWidth;
            mesh.TextureHeight = data.TextureHeight;
            mesh.Texture = data.Texture;

            //頂点の追加
<<<<<<< HEAD
            var vertices = new List<Vertex>();
            int index = 0;
            for (int y = 0; y < data.Height; ++y)
                for (int x = 0; x < data.Width; ++x)
                {
                    //[TODO]微分して手の領域の頂点を抽出する
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
                mesh.AddIndices(cell.Indices);
            }

            return mesh;
        }
        #endregion

        //////////////////////////////////////////////
        #region Internal Classes
        class Vertex : IVertex
        {
            public int X;
            public int Y;
            public int Index;

            public Vertex(int x, int y, int index)
            {
                X = x;
                Y = y;
                Index = index;
            }

            public double[] Position
            {
                get
                {
                    return new double[] { X, Y };
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
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
=======
            var indexMap = new int[data.Width, data.Height];
            var depthMap = new float[data.Width, data.Height];
            int index = 0;
            for (int y = 0; y < data.Height; y += Skip)
                for (int x = 0; x < data.Width; x += Skip)
                {
                    //背景領域は無視
                    if (data.Blob[data.Width * y + x] == data.BlobBackground)
                    {
                        indexMap[x, y] = -1;
                        continue;
                    }

                    Point point;
                    if (data.TryGet(x, y, out point))
                    {
                        //遠いところにあるものは無視
                        if (MaxDepth < point.Z)
                        {
                            indexMap[x, y] = -1;
                            continue;
                        }

                        indexMap[x, y] = index;
                        depthMap[x, y] = point.Z;
                        mesh.AddPoint(point);
                        ++index;
                    }
                    else
                    {
                        indexMap[x, y] = -1;
                    }
                }

            //メッシュインデックスの作成
            //メッシュを構成する頂点が密であることが前提
            for (int y = Skip; y < data.Height - Skip; y += Skip)
                for (int x = Skip; x < data.Width - Skip; x += Skip)
                {
                    //頂点が存在する場合は左上と右下に三角形をつくろうとする
                    if (indexMap[x, y] != -1)
                    {
                        int center = indexMap[x, y];

                        if (indexMap[x - Skip, y] != -1 && Math.Abs(depthMap[x - Skip, y] - depthMap[x, y]) < MaxDepthGap &&
                            indexMap[x, y - Skip] != -1 && Math.Abs(depthMap[x, y - Skip] - depthMap[x, y]) < MaxDepthGap &&
                                                           Math.Abs(depthMap[x - Skip, y] - depthMap[x, y - Skip]) < MaxDepthGap)
                            mesh.AddIndices(new int[] { indexMap[x - Skip, y],
                                                        indexMap[x, y - Skip], 
                                                        center });
                        if (indexMap[x + Skip, y] != -1 && Math.Abs(depthMap[x + Skip, y] - depthMap[x, y]) < MaxDepthGap &&
                            indexMap[x, y + Skip] != -1 && Math.Abs(depthMap[x, y + Skip] - depthMap[x, y]) < MaxDepthGap &&
                                                           Math.Abs(depthMap[x + Skip, y] - depthMap[x, y + Skip]) < MaxDepthGap)
                            mesh.AddIndices(new int[] { center, 
                                                        indexMap[x + Skip, y], 
                                                        indexMap[x, y + Skip] });
                    }
                    //頂点が存在しない場合は1マスあけた左下と右上に三角形をつくろうとする
                    else
                    {
                        if (indexMap[x - Skip, y] != -1 && Math.Abs(depthMap[x - Skip, y] - depthMap[x, y + Skip]) < MaxDepthGap &&
                            indexMap[x, y + Skip] != -1 && Math.Abs(depthMap[x, y + Skip] - depthMap[x - Skip, y + Skip]) < MaxDepthGap &&
                            indexMap[x - Skip, y + Skip] != -1 && Math.Abs(depthMap[x - Skip, y + Skip] - depthMap[x - Skip, y]) < MaxDepthGap)
                            mesh.AddIndices(new int[] { indexMap[x - Skip, y], 
                                                        indexMap[x, y + Skip], 
                                                        indexMap[x - Skip, y + Skip]});

                        if (indexMap[x + Skip, y] != -1 && Math.Abs(depthMap[x + Skip, y] - depthMap[x, y - Skip]) < MaxDepthGap && 
                            indexMap[x, y - Skip] != -1 && Math.Abs(depthMap[x, y - Skip] - depthMap[x + Skip, y - Skip]) < MaxDepthGap &&
                            indexMap[x + Skip, y - Skip] != -1 && Math.Abs(depthMap[x + Skip, y - Skip] - depthMap[x + Skip, y]) < MaxDepthGap)
                            mesh.AddIndices(new int[] { indexMap[x + Skip, y], 
                                                        indexMap[x, y - Skip], 
                                                        indexMap[x + Skip, y - Skip]});
                    }
                }

            return mesh;
>>>>>>> develop
        }
        #endregion
    }
}
