using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Models.PredefinedModels
{
    public class KlainBottle : _3DModel
    {
        private float a = 3;
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

        private float b = 4;
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

        private float c = 2;
        public float C
        {
            get { return c; }
            set
            {
                if (c == value) return;
                if (value < 0)
                    c = 1;
                else
                    c = value;
                Create();
                NotifyPropertyChanged();
            }
        }

        public KlainBottle(Vector4 initialPosition, Vector4 rotation, Vector4 scale, float _a, float _b, float _c, int _v, int _u) : base(initialPosition, rotation, scale)
        {
            Name = "KlainBottle" + counter++.ToString();
            A = _a;
            B = _b;
            C = _c;
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
                    float r(float u) => (float)(C * (1 - Math.Cos(u) / 2));
                    if (beta <= Math.PI)
                    {
                        points[i * UComplexity + j] = new Vector4(
                            (float)((A * (1 + Math.Sin(beta)) + r(beta) * Math.Cos(alpha)) * Math.Cos(beta)),
                            (float)((B + r(beta) * Math.Cos(alpha)) * Math.Sin(beta)),
                            (float)(r(beta) * Math.Sin(alpha))
                            );
                    }
                    else
                    {
                        points[i * UComplexity + j] = (new Vector4(
                            (float)(A * (1 + Math.Sin(beta)) * Math.Cos(beta) - r(beta) * Math.Cos(alpha)),
                            (float)(B * Math.Sin(beta)),
                            (float)(r(beta) * Math.Sin(alpha))
                            ));
                    }

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
            return new KlainBottle(new Vector4(Position.X, Position.Y, Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), A, B, C, VComplexity, UComplexity);
        }

        public override _3DObject CloneMirrored()
        {
            return new KlainBottle(new Vector4(Position.X, Position.Y, -Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), A, B, C, VComplexity, UComplexity);
        }
    }
}
