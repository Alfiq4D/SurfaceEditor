using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Models.PredefinedModels
{
    public class Cornucopia: _3DModel
    {
        private float a = 1f / 4;
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

        private float b = 1f/4;
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

        public Cornucopia(Vector4 initialPosition, Vector4 rotation, Vector4 scale, float _a, float _b, int _v, int _u) : base(initialPosition, rotation, scale)
        {
            Name = "Cornucopia" + counter++.ToString();
            A = _a;
            B = _b;
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
                alpha = (float)(3 * i * (Math.PI / (VComplexity - 1)));
                for (int j = 0; j < UComplexity; j++)
                {
                    beta = (float)(2 * j * (Math.PI / (UComplexity - 1)));
                    points[i * UComplexity + j] = (new Vector4(
                        (float)(Math.Exp(B * alpha) * Math.Cos(alpha) + Math.Exp(A * alpha) * Math.Cos(beta) * Math.Cos(alpha)),
                        (float)(Math.Exp(A * alpha) * Math.Sin(beta)),
                        (float)(Math.Exp(B * alpha) * Math.Sin(alpha) + Math.Exp(A * alpha) * Math.Cos(beta) * Math.Sin(alpha))
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
            return new Cornucopia(new Vector4(Position.X, Position.Y, Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), A, B, VComplexity, UComplexity);
        }

        public override _3DObject CloneMirrored()
        {
            return new Cornucopia(new Vector4(Position.X, Position.Y, -Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), A, B, VComplexity, UComplexity);
        }
    }
}
