using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Engine.Utilities
{
    public class Projection: INotifyPropertyChanged
    {
        public float N { get; set; }
        public float F { get; set; }
        public float Fov
        {
            get { return fov; }
            set
            {
                if (fov == value) return;
                fov = value;
                NotifyPropertyChanged();
            }
        }

        public float A { get; set; }

        public float D
        {
            get { return d; }
            set
            {
                if (d == value) return;
                d = value;
                NotifyPropertyChanged();
            }
        }

        private float d;
        private float fov;
        private float r;
        private float left, right, bottom, top;
        private float a, b, c;

        public event PropertyChangedEventHandler PropertyChanged;

        public Projection(float _n, float _f, float _FOV, float _a)
        {
            N = _n;
            F = _f;
            Fov = _FOV;
            A = _a;
            D = 6.4f;
            //r = (F - N) / 5 ;
            r = 20;

            top = N * (float)Math.Tan((Math.PI / 180) * (Fov / 2));
            bottom = -top;
            right = top * A;
            left = -top * A;
            a = A * (float)Math.Tan((Math.PI / 180) * (Fov / 2)) * r;
            b = a - D / 2 / 10;
            c = a + D / 2 / 10;
        }

        public void ModifyProjectionValues(float _n, float _f, float _FOV, float _a)
        {
            N = _n;
            F = _f;
            Fov = _FOV;
            A = _a;
           // D = 6.4f;
           // r = (F - N) / 5;
            
            top = N * (float)Math.Tan((Math.PI / 180) * (Fov / 2));
            bottom = -top;
            right = top * A;
            left = -top * A;
            a = A * (float)Math.Tan((Math.PI / 180) * (Fov / 2)) * r;
            b = a - D / 2 / 10;
            c = a + D / 2 / 10;
        }

        public Matrix4 PerspectiveProjectionMatrix()
        {
            float e = 1 / (float)(Math.Tan((Math.PI / 180) * (Fov / 2)));
            return new Matrix4(e / A, 0, 0, 0,
                                0, e , 0, 0,
                                0, 0, -(F + N) / (F - N), -2 * (F * N) / (F - N),
                                0, 0, -1, 0);
        }

        public Matrix4 RightStereoMatrix()
        {
            float e = 1 / (float)(Math.Tan((Math.PI / 180) * (Fov / 2)));
            return new Matrix4(e / A, 0, -(D / 20) / (2 * r), 0,
                                0, e, 0, 0,
                                0, 0, -(F + N) / (F - N), -2 * (F * N) / (F - N),
                                0, 0, -1, 0);
        }

        public Matrix4 LeftStereoMatrix()
        {
            float e = 1 / (float)(Math.Tan((Math.PI / 180) * (Fov / 2)));
            return new Matrix4(e / A, 0, (D / 20) / (2 * r), 0,
                                0, e, 0, 0,
                                0, 0, -(F + N) / (F - N), -2 * (F * N) / (F - N),
                                0, 0, -1, 0);
        }

        public Matrix4 SteroLeftFrustum()
        {
            left = -b * (N / r);
            right = c * (N / r);
            return new Matrix4((2 * N) / (right - left), 0, (right + left) / (right - left), 0,
                                0, (2 * N) / (top - bottom), (top + bottom) / (top - bottom), 0,
                                0, 0, -(F + N) / (F - N), -2 * (F * N) / (F - N),
                                0, 0, -1, 0);
        }

        public Matrix4 SteroRightFrustum()
        {
            left = -c * (N / r);
            right = b * (N / r);
            return new Matrix4((2 * N) / (right - left), 0, (right + left) / (right - left), 0,
                                0, (2 * N) / (top - bottom), (top + bottom) / (top - bottom), 0,
                                0, 0, -(F + N) / (F - N), -2 * (F * N) / (F - N),
                                0, 0, -1, 0);
        }

        public Matrix4 OrtographicProjectionMatrix()
        {
            float e = 1 / (float)(Math.Tan((Math.PI / 180) * (Fov / 2)) * 20 * N);
            return new Matrix4(e / A, 0, 0, 0,
                    0, e, 0, 0,
                    0, 0, -2 / (F - N), -(F + N) / (F - N),
                    0, 0, 0, 1);
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
