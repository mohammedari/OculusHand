using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusHand.Models
{
    /// <summary>
    /// GestureCameraから取得される点群を表現するクラス
    /// </summary>
    public class Point
    {
        public float U;
        public float V;
        public float X;
        public float Y;
        public float Z;

<<<<<<< HEAD:OculusHand/Models/Point.cs
        public Point(float u, float v, float x, float y, float z, float r, float g, float b)
=======
        public Point(float u, float v, float x, float y, float z)
>>>>>>> develop:OculusHand/Models/Point.cs
        {
            U = u;
            V = v;
            X = x;
            Y = y;
            Z = z;
        }
        //////////////////////////////////////////////
        #region 比較演算子の実装

        public static bool operator ==(Point lhs, Point rhs)
        {
            if (object.ReferenceEquals(lhs, rhs))
                return true;

            if ((object)lhs == null || (object)rhs == null)
                return false;

            return lhs.Equals(rhs);
        }

        public static bool operator !=(Point lhs, Point rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (Point)obj;
            return U == other.U && V == other.V && 
<<<<<<< HEAD:OculusHand/Models/Point.cs
                   X == other.X && Y == other.Y && Z == other.Z && 
                   R == other.R && G == other.G && B == other.B;
=======
                   X == other.X && Y == other.Y && Z == other.Z;
>>>>>>> develop:OculusHand/Models/Point.cs
        }

        public override int GetHashCode()
        {
            return U.GetHashCode() ^ V.GetHashCode() ^ 
<<<<<<< HEAD:OculusHand/Models/Point.cs
                   X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^
                   R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode(); ;
=======
                   X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
>>>>>>> develop:OculusHand/Models/Point.cs
        }

        #endregion
    }
}
