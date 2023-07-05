using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Models.PredefinedModels
{
    public class Sea_Shell : _3DModel
    {
        private float a = 2;
        public float A
        {
            get { return a; }
            set
            {
                if (a == value) return;
                if (value < 0)
                    a = 1;
                else
                    a = value;
                Create();
                NotifyPropertyChanged();
            }
        }

        private float b = 2;
        public float B
        {
            get { return b; }
            set
            {
                if (b == value) return;
                if (value < 0)
                    b = 1;
                else
                    b = value;
                Create();
                NotifyPropertyChanged();
            }
        }

        private float k = 6;
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

        private float m = -1.0f / 20;
        public float M
        {
            get { return m; }
            set
            {
                if (m == value) return;
                m = value;
                Create();
                NotifyPropertyChanged();
            }
        }

        public Sea_Shell(Vector4 initialPosition, Vector4 rotation, Vector4 scale, float _a, float _b, float _k, float _m, int _v, int _u) : base(initialPosition, rotation, scale)
        {
            Name = "Sea-Shell" + counter++.ToString();
            A = _a;
            B = _b;
            K = _k;
            M = _m;
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
                    beta = (float)(8 * j * (Math.PI / (UComplexity - 1)));
                    points[i * UComplexity + j] = (new Vector4(
                        (float)((A + B * Math.Cos(alpha)) * Math.Exp(M * beta) * Math.Cos(beta)),
                        (float)((K * A + B * Math.Sin(alpha)) * Math.Exp(M * beta)),
                        (float)((A + B * Math.Cos(alpha)) * Math.Exp(M * beta) * Math.Sin(beta))
                        ));

                    if (i + 1 < VComplexity)
                        Edges.Add(new Edge(i * UComplexity + j, ((i + 1) % VComplexity) * UComplexity + j));
                    if (j + 1 < UComplexity)
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
            return new Sea_Shell(new Vector4(Position.X, Position.Y, Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), A, B, K, M, VComplexity, UComplexity);
        }

        public override _3DObject CloneMirrored()
        {
            return new Sea_Shell(new Vector4(Position.X, Position.Y, -Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), A, B, K, M, VComplexity, UComplexity);
        }
    }
}
