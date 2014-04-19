using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OculusHand.Models
{
    public class GestureCameraData
    {
        Point[,] _dictionary;

        public GestureCameraData(int width, int height, int textureWidth, int textureHeight, byte[] texture, byte[] blob)
        {
            _dictionary = new Point[width, height];
            Width = width;
            Height = height;
            TextureWidth = textureWidth;
            TextureHeight = textureHeight;
            Texture = texture;
            Blob = blob;
        }

        //////////////////////////////////////////////
        #region Properties

        /// <summary>
        /// 深度画像の幅を取得します。
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// 深度画像の高さを取得します。
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// テクスチャ画像の幅を取得します。
        /// </summary>
        public int TextureWidth { get; private set; }

        /// <summary>
        /// テクスチャ画像の高さを取得します。
        /// </summary>
        public int TextureHeight { get; private set; }

        /// <summary>
        /// テクスチャ画像を取得します。
        /// </summary>
        public byte[] Texture { get; private set; }

        /// <summary>
        /// ブロブ画像を取得します。
        /// </summary>
        public byte[] Blob { get; private set; }

        #endregion

        //////////////////////////////////////////////
        #region Public Methods
        /// <summary>
        /// 特定の深度画像座標にある点を取得します。対応する座標に点が存在しない場合にはfalseを返します。
        /// </summary>
        /// <param name="x">深度画像X座標</param>
        /// <param name="y">深度画像Y座標</param>
        /// <param name="point">点データ</param>
        /// <returns>対応する座標に点が存在したかどうか</returns>
        public bool TryGet(int x, int y, out Point point)
        {
            point = _dictionary[x, y];
            return point == null ? false : true;
        }

        /// <summary>
        /// 特定の深度画像座標に点データを与えます。
        /// </summary>
        /// <param name="x">深度画像X座標</param>
        /// <param name="y">深度画像Y座標</param>
        /// <param name="point">点データ</param>
        public void Set(int x, int y, Point point)
        {
            _dictionary[x, y] = point;
        }
        #endregion
    }
}
