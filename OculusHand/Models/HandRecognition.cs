using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OculusHand.Models
{
    public class HandRecognition
    {
        //////////////////////////////////////////////
        #region Properties

        public float MaxDepth { get; private set; }
        public float MaxDepthGap { get; private set; }
        public int Skip { get; private set; }

        public Mesh Mesh { get; private set; }

        #endregion

        //////////////////////////////////////////////
        #region Public Methods

        public HandRecognition(float maxDepth, float maxDepthGap, int skip)
        {
            MaxDepth = maxDepth;
            MaxDepthGap = maxDepthGap;
            Skip = skip;
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
        }
        #endregion
    }
}
