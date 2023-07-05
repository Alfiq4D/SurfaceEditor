using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace Engine.Utilities
{
    public class Matrix4
    {
        public Vector4 R1, R2, R3, R4;
        public Matrix4(Vector4 v1, Vector4 v2, Vector4 v3, Vector4 v4)
        {
            R1 = v1;
            R2 = v2;
            R3 = v3;
            R4 = v4;
        }
        public Matrix4() { }
        public Matrix4(float a11, float a12, float a13, float a14, float a21, float a22, float a23, float a24, float a31, float a32, float a33, float a34, float a41, float a42, float a43, float a44)
        {
            R1 = new Vector4(a11, a12, a13, a14);
            R2 = new Vector4(a21, a22, a23, a24);
            R3 = new Vector4(a31, a32, a33, a34);
            R4 = new Vector4(a41, a42, a43, a44);
        }

        public static Vector4 operator *(Matrix4 m, Vector4 v)
        {
            return new Vector4(m.R1 * v, m.R2 * v, m.R3 * v, m.R4 * v);
        }

        public static IEnumerable<Vector4> operator *(Matrix4 m, IEnumerable<Vector4> vectors)
        {
            return vectors.Select(v => m * v);
        }

        public static Matrix4 operator *(Matrix4 m1, Matrix4 m2)
        {
            Vector4 w1 = new Vector4(m2.R1.X, m2.R2.X, m2.R3.X, m2.R4.X),
                w2 = new Vector4(m2.R1.Y, m2.R2.Y, m2.R3.Y, m2.R4.Y),
                w3 = new Vector4(m2.R1.Z, m2.R2.Z, m2.R3.Z, m2.R4.Z),
                w4 = new Vector4(m2.R1.W, m2.R2.W, m2.R3.W, m2.R4.W);
            return new Matrix4(m1.R1 * w1, m1.R1 * w2, m1.R1 * w3, m1.R1 * w4,
                m1.R2 * w1, m1.R2 * w2, m1.R2 * w3, m1.R2 * w4,
                m1.R3 * w1, m1.R3 * w2, m1.R3 * w3, m1.R3 * w4,
                m1.R4 * w1, m1.R4 * w2, m1.R4 * w3, m1.R4 * w4);
        }

        public static Matrix4 ElementalMatrix()
        {
            return new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        }

        public static Matrix4 Invert(Matrix4 matrix)
        {
            Matrix3D matrix3 = new Matrix3D(matrix.R1.X, matrix.R1.Y, matrix.R1.Z, matrix.R1.W,
                matrix.R2.X, matrix.R2.Y, matrix.R2.Z, matrix.R2.W,
                matrix.R3.X, matrix.R3.Y, matrix.R3.Z, matrix.R3.W,
                matrix.R4.X, matrix.R4.Y, matrix.R4.Z, matrix.R4.W);
            matrix3.Invert();
            return new Matrix4((float)matrix3.M11, (float)matrix3.M12, (float)matrix3.M13, (float)matrix3.M14,
                (float)matrix3.M21, (float)matrix3.M22, (float)matrix3.M23, (float)matrix3.M24,
               (float)matrix3.M31, (float)matrix3.M32, (float)matrix3.M33, (float)matrix3.M34,
               (float)matrix3.OffsetX, (float)matrix3.OffsetY, (float)matrix3.OffsetZ, (float)matrix3.M44);
        }

        public static Matrix4 Transpose(Matrix4 matrix)
        {
            return new Matrix4(matrix.R1.X, matrix.R2.X, matrix.R3.X, matrix.R4.X, matrix.R1.Y, matrix.R2.Y, matrix.R3.Y, matrix.R4.Y, matrix.R1.Z, matrix.R2.Z, matrix.R3.Z, matrix.R4.Z, matrix.R1.W, matrix.R2.W, matrix.R3.W, matrix.R4.W);
        }

    }
}
