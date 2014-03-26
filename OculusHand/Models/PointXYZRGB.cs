using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculuSLAM.Models
{
    public struct PointXYZRGB
    {
        public float X;
        public float Y;
        public float Z;
        public float R;
        public float G;
        public float B;

        public PointXYZRGB(float x, float y, float z, float r, float g, float b)
        {
            X = x;
            Y = y;
            Z = z;
            R = r;
            G = g;
            B = b;
        }
        //////////////////////////////////////////////
        #region 比較演算子の実装

        public static bool operator ==(PointXYZRGB lhs, PointXYZRGB rhs)
        {
            if (object.ReferenceEquals(lhs, rhs))
                return true;

            if ((object)lhs == null || (object)rhs == null)
                return false;

            return lhs.Equals(rhs);
        }

        public static bool operator !=(PointXYZRGB lhs, PointXYZRGB rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (PointXYZRGB)obj;
            return X == other.X && Y == other.Y && Z == other.Z && 
                   R == other.R && G == other.G && B == other.B;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^
                   R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode(); ;
        }

        #endregion
    }
}
