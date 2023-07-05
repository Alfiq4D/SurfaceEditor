using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Models.PredefinedModels
{
    public class Figure8: _3DModel
    {
        private float radius;
        public float Radius
        {
            get { return radius; }
            set
            {
                if (radius == value) return;
                if (value < 0)
                    radius = 1;
                else
                    radius = value;
                Create();
                NotifyPropertyChanged();
            }
        }

        private float b = 1;
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

        public Figure8(Vector4 initialPosition, Vector4 rotation, Vector4 scale, float _r, float _b, int _v, int _u) : base(initialPosition, rotation, scale)
        {
            Name = "Figure-8" + counter++.ToString();
            Radius = _r;
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
                alpha = (float)(2 * i * (Math.PI / (VComplexity - 1)));
                for (int j = 0; j < UComplexity; j++)
                {
                    beta = (float)(2 * j * (Math.PI / (UComplexity - 1)));
                    points[i * UComplexity + j] = (new Vector4(
                        (float)((Radius + B * Math.Cos(beta / 2) * Math.Sin(alpha) - B * Math.Sin(beta / 2) * Math.Sin(2 * alpha)) * Math.Cos(beta)),
                        (float)(2 * Math.Sin(beta / 2) * Math.Sin(alpha) + 2 * Math.Cos(beta / 2) * Math.Sin(2 * alpha)),
                        (float)((Radius + B * Math.Cos(beta / 2) * Math.Sin(alpha) - B * Math.Sin(beta / 2) * Math.Sin(2 * alpha)) * Math.Sin(beta))
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
            return new Figure8(new Vector4(Position.X, Position.Y, Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), Radius, B, VComplexity, UComplexity);
        }

        public override _3DObject CloneMirrored()
        {
            return new Figure8(new Vector4(Position.X, Position.Y, -Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), Radius, B, VComplexity, UComplexity);
        }
    }
}
