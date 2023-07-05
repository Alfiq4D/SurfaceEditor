using System;
using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Engine.Utilities
{
    public class Vector4: INotifyPropertyChanged
    {
        private float x;

        public float X
        {
            get { return x; }
            set
            {
                if (x == value) return;
                x = value;
                NotifyPropertyChanged();
            }
        }

        private float y;
        public float Y
        {
            get { return y; }
            set
            {
                if (y == value) return;
                y = value;
                NotifyPropertyChanged();
            }
        }

        private float z;
        public float Z
        {
            get { return z; }
            set
            {
                if (z == value) return;
                z = value;
                NotifyPropertyChanged();
            }
        }
        public float W { get; set; }
        public Vector4() { }
        public Vector4(float x, float y, float z, float w = 1)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector4(Vector3 v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
            W = 1;
        }

        public static Vector4 operator *(Vector4 v, float d)
        {
            return new Vector4(v.X * d, v.Y * d, v.Z * d, v.W * d);
        }

        /// <summary>
        /// Dot products of two vectors
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static float operator *(Vector4 v1, Vector4 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z + v1.W * v2.W;
        }

        public static float DotProduct(Vector4 v1, Vector4 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        public static Vector4 operator *(float d, Vector4 v)
        {
            return v * d;
        }

        public static Vector4 operator /(Vector4 v1, Vector4 v2)
        {
            return new Vector4(v1.X / v2.X, v1.Y / v2.Y, v1.Z / v2.Z, 1);
        }
        /// <summary>
        /// Cross product of two vectors
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Vector4 operator ^(Vector4 v1, Vector4 v2)
        {
            return new Vector4(v1.Y * v2.Z - v1.Z * v2.Y, v1.Z * v2.X - v1.X * v2.Z, v1.X * v2.Y - v1.Y * v2.X);
        }

        public static Vector4 operator -(Vector4 v1, Vector4 v2)
        {
            return new Vector4(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z, v1.W - v2.W);
        }
        public static Vector4 operator +(Vector4 v1, Vector4 v2)
        {
            return new Vector4(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z, v1.W + v2.W);
        }

        public static Vector4 operator -(Vector4 v)
        {
            return new Vector4(-v.X, -v.Y, -v.Z, -v.W);
        }

        public Vector4 Normalized()
        {
            float length = (float)Math.Sqrt(X * X + Y * Y + Z * Z);
            if (length == 0) throw new Exception("Vector has 0 length");
            return new Vector4(X / length, Y / length, Z / length, W);
        }

        public static float Distance(Vector4 v1, Vector4 v2)
        {
            return (float)Math.Sqrt((v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y) + (v1.Z - v2.Z) * (v1.Z - v2.Z));
        }

        public static float SquareDistance(Vector4 v1, Vector4 v2)
        {
            return (v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y) + (v1.Z - v2.Z) * (v1.Z - v2.Z);
        }

        public static Vector4 operator /(Vector4 v, float d)
        {
            //if (d == 0) throw new Exception("Trying to divide by 0");
            return new Vector4(v.X / d, v.Y / d, v.Z / d, v.W / d);
        }

        public static bool operator ==(Vector4 v1, Vector4 v2)
        {
            if (object.ReferenceEquals(v1, null) && object.ReferenceEquals(v2, null)) return true;
            if (object.ReferenceEquals(v1, null) || object.ReferenceEquals(v2, null)) return false;
            return v1.X == v2.X && v1.Y == v2.Y && v1.Z == v2.Z && v1.W == v2.W;
        }

        public static bool operator !=(Vector4 v1, Vector4 v2)
        {
            return !(v1 == v2);
        }

        public static Vector4 Zero()
        {
            return new Vector4(0, 0, 0, 1);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Vector4 Clone()
        {
            return new Vector4(X, Y, Z, W);
        }

        public static Vector4 CalculateNormal(Vector4 A, Vector4 B, Vector4 C)
        {
            return ((B - A) ^ (C - A)).Normalized();
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            x.ToString(new CultureInfo("en-US"));
            return x.ToString() + ";" + y.ToString() + ";" + z.ToString();
        }

        public override bool Equals(object obj)
        {
            var vector = obj as Vector4;
            return vector != null &&
                   x == vector.x &&
                   X == vector.X &&
                   y == vector.y &&
                   Y == vector.Y &&
                   z == vector.z &&
                   Z == vector.Z &&
                   W == vector.W;
        }

        public override int GetHashCode()
        {
            var hashCode = 482205958;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            hashCode = hashCode * -1521134295 + Z.GetHashCode();
            hashCode = hashCode * -1521134295 + W.GetHashCode();
            return hashCode;
        }
    }
}
