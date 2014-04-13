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

        public Mesh Mesh { get; private set; }

        #endregion

        //////////////////////////////////////////////
        #region Public Methods

        public HandRecognition()
        {
            //[TODO]パラメータをコンフィグから設定するようにする
            MaxDepth = 0.5;

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
            var indexMap = new Nullable<int>[data.Width, data.Height];
            int index = 0;
            for (int y = 0; y < data.Height; ++y)
                for (int x = 0; x < data.Width; ++x)
                {
                    //背景領域は無視
                    if (data.Blob[data.Width * y + x] == BackgroundBlob)
                        continue;

                    Point point;
                    if (data.TryGet(x, y, out point))
                    {
                        //遠いところにあるものは無視
                        if (MaxDepth < point.Z)
                            continue;

                        indexMap[x, y] = index;
                        mesh.AddPoint(point);
                        ++index;
                    }
                }

            //メッシュインデックスの作成
            //メッシュを構成する頂点が密であることが前提
            for (int y = 1; y < data.Height - 1; ++y)
                for (int x = 1; x < data.Width - 1; ++x)
                {
                    //頂点が存在する場合は左上と右下に三角形をつくろうとする
                    if (indexMap[x, y].HasValue)
                    {
                        int center = indexMap[x, y].Value;

                        if (indexMap[x - 1, y].HasValue &&
                            indexMap[x, y - 1].HasValue)
                            mesh.AddIndices(new int[] { indexMap[x - 1, y].Value,
                                                        indexMap[x, y - 1].Value, 
                                                        center });
                        if (indexMap[x + 1, y].HasValue &&
                            indexMap[x, y + 1].HasValue)
                            mesh.AddIndices(new int[] { center, 
                                                        indexMap[x + 1, y].Value, 
                                                        indexMap[x, y + 1].Value });
                    }
                    //頂点が存在しない場合は1マスあけた左下と右上に三角形をつくろうとする
                    else
                    {
                        if (indexMap[x - 1, y].HasValue &&
                            indexMap[x, y + 1].HasValue &&
                            indexMap[x - 1, y + 1].HasValue)
                            mesh.AddIndices(new int[] { indexMap[x - 1, y].Value, 
                                                        indexMap[x, y + 1].Value, 
                                                        indexMap[x - 1, y + 1].Value});

                        if (indexMap[x + 1, y].HasValue &&
                            indexMap[x, y - 1].HasValue && 
                            indexMap[x + 1, y - 1].HasValue)
                            mesh.AddIndices(new int[] { indexMap[x + 1, y].Value, 
                                                        indexMap[x, y - 1].Value, 
                                                        indexMap[x + 1, y - 1].Value});
                    }
                }

            return mesh;
        }
        #endregion
    }
}
