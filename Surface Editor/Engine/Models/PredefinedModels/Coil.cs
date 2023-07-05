using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Models.PredefinedModels
{
    public class Coil : _3DModel
    {
        private float a = 4;
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

        private float b = 3;
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

        private float h = 2;
        public float H
        {
            get { return h; }
            set
            {
                if (h == value) return;
                if (value < 0)
                    h = 1;
                else
                    h = value;
                Create();
                NotifyPropertyChanged();
            }
        }

        public Coil(Vector4 initialPosition, Vector4 rotation, Vector4 scale, float _a, float _b, float _h, int _v, int _u) : base(initialPosition, rotation, scale)
        {
            Name = "Coil" + counter++.ToString();
            A = _a;
            B = _b;
            H = _h;
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
                    beta = (float)(4 * j * (Math.PI / (UComplexity - 1)));
                    points[i * UComplexity + j] = (new Vector4(
                        (float)((A + B * Math.Cos(alpha)) * Math.Cos(beta) + B * H / Math.Sqrt(A * A + H * H) * Math.Sin(beta) * Math.Sin(alpha)),
                        (float)(H * beta + B * A / Math.Sqrt(A * A + H * H) * Math.Sin(alpha)),
                        (float)((A + B * Math.Cos(alpha)) * Math.Sin(beta) - B * H / Math.Sqrt(A * A + H * H) * Math.Cos(beta) * Math.Sin(alpha))
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
            return new Coil(new Vector4(Position.X, Position.Y, Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), A, B, H, VComplexity, UComplexity);
        }

        public override _3DObject CloneMirrored()
        {
            return new Coil(new Vector4(Position.X, Position.Y, -Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), A, B, H, VComplexity, UComplexity);
        }
    }
}
