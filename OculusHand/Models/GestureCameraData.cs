using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OculusHand.Models
{
    public class GestureCameraData
    {
<<<<<<< HEAD
        Dictionary<Index, Point> _dictionary;

        public GestureCameraData(int width, int height, int textureWidth, int textureHeight, byte[] texture)
        {
            _dictionary = new Dictionary<Index, Point>();
=======
        Point[,] _dictionary;
        List<PXCMGesture.Gesture> _gestures;

        public GestureCameraData(int width, int height, int textureWidth, int textureHeight, byte[] texture, byte[] blob, uint blobBackground, List<PXCMGesture.Gesture> gestures)
        {
            _dictionary = new Point[width, height];
>>>>>>> develop
            Width = width;
            Height = height;
            TextureWidth = textureWidth;
            TextureHeight = textureHeight;
            Texture = texture;
<<<<<<< HEAD
=======
            Blob = blob;
            BlobBackground = blobBackground;
            _gestures = gestures;
>>>>>>> develop
        }

        //////////////////////////////////////////////
        #region Properties
<<<<<<< HEAD
        /// <summary>
        /// すべての点群を取得します。
        /// </summary>
        public IEnumerable<Point> Points
        {
            get { return _dictionary.Values; }
        }

        /// <summary>
        /// 点群に含まれる点の数を取得します。
        /// </summary>
        public int Count
        {
            get { return _dictionary.Count; }
        }
=======
>>>>>>> develop

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

<<<<<<< HEAD
        //[TODO]カラーイメージも作る
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
            return _dictionary.TryGetValue(new Index(x, y), out point);
        }

        /// <summary>
        /// 特定の深度画像座標にある点を取得します。対応する座標に点が存在しない場合には例外をすろーします。
        /// </summary>
        /// <param name="x">深度画像X座標</param>
        /// <param name="y">深度画像Y座標</param>
        /// <returns>点データ</returns>
        public Point Get(int x, int y)
        {
            Point point;
            if (!_dictionary.TryGetValue(new Index(x, y), out point))
            {
                throw new GestureCameraException("Failed to get point at (" + x + "," + y + ").");
            }

            return point;
        }

        /// <summary>
        /// 特定の深度画像座標に点データを与えます。
=======
        /// <summary>
        /// ブロブ画像を取得します。
        /// </summary>
        public byte[] Blob { get; private set; }

        /// <summary>
        /// 背景のブロブ画像の値を取得します。
        /// </summary>
        public uint BlobBackground { get; private set; }

        /// <summary>
        /// 左スワイプのジェスチャーを行っているかを取得します。
        /// </summary>
        public bool IsGestureSwipeLeft
        {
            get
            {
                return _gestures.Select(g => g.label).Contains(PXCMGesture.Gesture.Label.LABEL_NAV_SWIPE_LEFT);
            }
        }

        /// <summary>
        /// 右スワイプのジェスチャーを行っているかを取得します。
        /// </summary>
        public bool IsGestureSwipeRight
        {
            get
            {
                return _gestures.Select(g => g.label).Contains(PXCMGesture.Gesture.Label.LABEL_NAV_SWIPE_RIGHT);
            }
        }

        #endregion

        //////////////////////////////////////////////
        #region Public Methods
        /// <summary>
        /// 特定の深度画像座標にある点を取得します。対応する座標に点が存在しない場合にはfalseを返します。
>>>>>>> develop
        /// </summary>
        /// <param name="x">深度画像X座標</param>
        /// <param name="y">深度画像Y座標</param>
        /// <param name="point">点データ</param>
<<<<<<< HEAD
        public void Set(int x, int y, Point point)
        {
            _dictionary[new Index(x, y)] = point;
        }
        #endregion

        //////////////////////////////////////////////
        #region Internal Classes
        /// <summary>
        /// 点群ハッシュテーブルのキーとなるクラスです。
        /// XY座標でユニークかどうかを判定します。
        /// </summary>
        struct Index
        {
            public int X;
            public int Y;

            public Index(int x, int y)
            {
                X = x;
                Y = y;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                {
                    return false;
                }

                var other = (Index)obj;
                return X == other.X && Y == other.Y;
            }

            public override int GetHashCode()
            {
                return X.GetHashCode() ^ Y.GetHashCode();
            }
=======
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
>>>>>>> develop
        }
        #endregion
    }
}
