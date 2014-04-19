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
        public byte BackgroundBlob { get; private set; }
        public int Skip { get; private set; }

        public Mesh Mesh { get; private set; }

        #endregion

        //////////////////////////////////////////////
        #region Public Methods

        public HandRecognition()
        {
            //[TODO]パラメータをコンフィグから設定するようにする
            MaxDepth = 0.5;
            Skip = 3;

            //[TODO]BackgroundBlobはGestureCameraから取得するようにする
            BackgroundBlob = 255;
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
            int index = 0;
            for (int y = 0; y < data.Height; y += Skip)
                for (int x = 0; x < data.Width; x += Skip)
                {
                    //背景領域は無視
                    if (data.Blob[data.Width * y + x] == BackgroundBlob)
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

                        if (indexMap[x - Skip, y] != -1 &&
                            indexMap[x, y - Skip] != -1)
                            mesh.AddIndices(new int[] { indexMap[x - Skip, y],
                                                        indexMap[x, y - Skip], 
                                                        center });
                        if (indexMap[x + Skip, y] != -1 &&
                            indexMap[x, y + Skip] != -1)
                            mesh.AddIndices(new int[] { center, 
                                                        indexMap[x + Skip, y], 
                                                        indexMap[x, y + Skip] });
                    }
                    //頂点が存在しない場合は1マスあけた左下と右上に三角形をつくろうとする
                    else
                    {
                        if (indexMap[x - Skip, y] != -1 &&
                            indexMap[x, y + Skip] != -1 &&
                            indexMap[x - Skip, y + Skip] != -1)
                            mesh.AddIndices(new int[] { indexMap[x - Skip, y], 
                                                        indexMap[x, y + Skip], 
                                                        indexMap[x - Skip, y + Skip]});

                        if (indexMap[x + Skip, y] != -1 &&
                            indexMap[x, y - 1] != -1 &&
                            indexMap[x + Skip, y - Skip] != -1)
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
