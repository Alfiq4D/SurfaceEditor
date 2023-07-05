using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Models.PredefinedModels
{
    public class Sphere: _3DModel
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

        public Sphere(Vector4 initialPosition, Vector4 rotation, Vector4 scale, float _r, int _v, int _u) : base(initialPosition, rotation, scale)
        {
            Name = "Sphere" + counter++.ToString();
            Radius = _r;
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
                        (float)(Radius * Math.Cos(alpha) * Math.Cos(beta)),
                        (float)(Radius * Math.Cos(alpha) * Math.Sin(beta)),
                        (float)(Radius * Math.Sin(alpha))
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
            return new Sphere(new Vector4(Position.X, Position.Y, Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), Radius, VComplexity, UComplexity);
        }

        public override _3DObject CloneMirrored()
        {
            return new Sphere(new Vector4(Position.X, Position.Y, -Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), Radius, VComplexity, UComplexity);
        }
    }
}
