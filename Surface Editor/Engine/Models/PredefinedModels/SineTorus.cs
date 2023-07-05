using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Models.PredefinedModels
{
    public class SineTorus: _3DModel
    {
        private float insideRadius;
        public float InsideRadius
        {
            get { return insideRadius; }
            set
            {
                if (insideRadius == value) return;
                if (value < 0)
                    insideRadius = 1;
                if (value > outsideRadius)
                    insideRadius = outsideRadius - 1;
                else
                    insideRadius = value;
                Create();
                NotifyPropertyChanged();
            }
        }

        private float outsideRadius = float.MaxValue;
        public float OutsideRadius
        {
            get { return outsideRadius; }
            set
            {
                if (outsideRadius == value) return;
                if (value < 0)
                    outsideRadius = 1;
                if (value < insideRadius)
                    outsideRadius = insideRadius + 1;
                else
                    outsideRadius = value;
                Create();
                NotifyPropertyChanged();
            }
        }

        private float k = 4;
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

        public SineTorus(Vector4 initialPosition, Vector4 rotation, Vector4 scale, float _R, float _r, float _k, int _v, int _u) : base(initialPosition, rotation, scale)
        {
            Name = "SineTorus" + counter++.ToString();
            InsideRadius = _r;
            OutsideRadius = _R;
            K = k;
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
                        (float)((OutsideRadius + InsideRadius * Math.Cos(beta)) * Math.Cos(alpha)),
                        InsideRadius * (float)Math.Sin(beta) * (float)Math.Cos(K * alpha),
                        (float)((OutsideRadius + InsideRadius * Math.Cos(beta)) * Math.Sin(alpha))
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
            return new SineTorus(new Vector4(Position.X, Position.Y, -Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), OutsideRadius, InsideRadius, K, VComplexity, UComplexity);
        }

        public override _3DObject CloneMirrored()
        {
            return new SineTorus(new Vector4(Position.X, Position.Y, -Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), OutsideRadius, InsideRadius, K, VComplexity, UComplexity);
        }
    }
}
