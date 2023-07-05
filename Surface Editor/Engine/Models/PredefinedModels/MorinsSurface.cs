using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Models.PredefinedModels
{
    public class MorinsSurface : _3DModel
    {
        private float k = 1;
        public float K
        {
            get { return k; }
            set
            {
                if (k == value) return;
                if (value < 0)
                    k = 1;
                else
                    k = value;
                Create();
                NotifyPropertyChanged();
            }
        }

        private float n = 6;
        public float N
        {
            get { return n; }
            set
            {
                if (n == value) return;
                if (value < 0)
                    n = 1;
                else
                    n = value;
                Create();
                NotifyPropertyChanged();
            }
        }

        public MorinsSurface(Vector4 initialPosition, Vector4 rotation, Vector4 scale, float _k, float _n, int _v, int _u) : base(initialPosition, rotation, scale)
        {
            Name = "MorinsSurface" + counter++.ToString();
            K = _k;
            N = _n;
            UComplexity = _u;
            VComplexity = _v;
        }

        override public _3DModel Create()
        {
            float alpha = 0;
            float beta = 0;
            Edges = new List<Edge>();
            var points = new Vector4[UComplexity * VComplexity];
            for (int i = 0; i < VComplexity; i++)
            {
                alpha = (float)(2 * i * (Math.PI / (VComplexity - 1)));
                for (int j = 0; j < UComplexity; j++)
                {
                    beta = (float)(2 * j * (Math.PI / (UComplexity - 1)));
                    float Ka(float u, float v) => 2 * (float)(Math.Cos(u) / (Math.Sqrt(2) - K * Math.Sin(2 * u) * Math.Sin(n * v)));
                    points[i * UComplexity + j] = (new Vector4(
                        (float)(Ka(beta, alpha) * (2 / (N - 1) * Math.Cos(beta) * Math.Cos((N - 1) * alpha) + Math.Sqrt(2) * Math.Sin(beta) * Math.Cos(alpha))),
                        (float)(Ka(beta, alpha) * (2 / (N - 1) * Math.Cos(beta) * Math.Sin((N - 1) * alpha) - Math.Sqrt(2) * Math.Sin(beta) * Math.Sin(alpha))),
                        (float)(Ka(beta, alpha) * Math.Cos(beta))
                        ));

                    Edges.Add(new Edge(i * UComplexity + j, ((i + 1) % VComplexity) * UComplexity + j));
                    Edges.Add(new Edge(i * UComplexity + j, i * UComplexity + ((j + 1) % UComplexity)));
                }
            }
            Points = points.ToList();
            //ControlPoints.Clear();
            //foreach(var p in Points)
            //{
            //    ControlPoints.Add(new FakePoint3D(p));
            //}
            return this;
        }

        public override string Save()
        {
            return "";
        }

        public override _3DObject Clone()
        {
            return new MorinsSurface(new Vector4(Position.X, Position.Y, Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), K, N, VComplexity, UComplexity);
        }

        public override _3DObject CloneMirrored()
        {
            return new MorinsSurface(new Vector4(Position.X, Position.Y, -Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), K, N, VComplexity, UComplexity);
        }
    }
}
