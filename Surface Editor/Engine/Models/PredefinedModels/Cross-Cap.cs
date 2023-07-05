using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Models.PredefinedModels
{
    public class Cross_Cap: _3DModel
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

        public Cross_Cap(Vector4 initialPosition, Vector4 rotation, Vector4 scale, float _a, int _v, int _u) : base(initialPosition, rotation, scale)
        {
            Name = "Cross-Cap" + counter++.ToString();
            A = _a;
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
                    points[i * UComplexity + j] = (new Vector4(
                        (float)(A * Math.Sin(2 * beta) * Math.Cos(alpha)),
                        (float)(-A * (Math.Sin(beta) * Math.Sin(beta) - Math.Cos(beta) * Math.Cos(beta) * Math.Cos(alpha) * Math.Cos(alpha))),
                        (float)(A * Math.Sin(2 * beta) * Math.Sin(alpha))
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
            return new Cross_Cap(new Vector4(Position.X, Position.Y, Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), A, VComplexity, UComplexity);
        }

        public override _3DObject CloneMirrored()
        {
            return new Cross_Cap(new Vector4(Position.X, Position.Y, -Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), A, VComplexity, UComplexity);
        }
    }
}
