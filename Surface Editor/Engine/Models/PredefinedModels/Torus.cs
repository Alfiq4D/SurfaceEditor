using Engine.Interfaces;
using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Models.PredefinedModels
{
    public class Torus: _3DModel, IIntersectable
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

        public bool IsClampedU => true;

        public bool IsClampedV => true;

        public float MaxU => (float)( 2 * Math.PI);

        public float MaxV => (float)(2 * Math.PI);

        public Torus(Vector4 initialPosition, Vector4 rotation, Vector4 scale, float _R, float _r, int _v, int _u) : base(initialPosition, rotation, scale)
        {
            Name = "Torus" + counter++.ToString();
            InsideRadius = _r;
            OutsideRadius = _R;
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
                alpha = (float)(2 * i * Math.PI / (VComplexity - 1));
                for (int j = 0; j < UComplexity; j++)
                {
                    beta = (float)(2 * j * Math.PI / (UComplexity - 1));

                    points[i * UComplexity + j] = (new Vector4(
                        (float)((OutsideRadius + InsideRadius * Math.Cos(beta)) * Math.Cos(alpha)),
                        InsideRadius * (float)Math.Sin(beta),
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
            return new Torus(new Vector4(Position.X, Position.Y, Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), OutsideRadius, InsideRadius, VComplexity, UComplexity);
        }

        public override _3DObject CloneMirrored()
        {
            return new Torus(new Vector4(Position.X, Position.Y, -Position.Z), new Vector4(Rotation.X, Rotation.Y, Rotation.Z), new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z), OutsideRadius, InsideRadius, VComplexity, UComplexity);
        }

        public (float u, float v) GetPoint3DUV(Vector4 pos)
        {
            float bestU = 0, bestV = 0;
            Vector4 bestPos = Evaluate(bestU, bestV);
            float minDistance = Vector4.Distance(bestPos, pos);
            const int N = 128; //number of samples
            float step = (float)(2 * Math.PI) / (N - 1);
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {                   
                    Vector4 p = Evaluate(i * step, j * step);
                    float distance = Vector4.Distance(p, pos);
                    if (distance < minDistance)
                    {
                        bestU = i * step;
                        bestV = j * step;
                        minDistance = distance;
                    }
                }
            }
            return (bestU, bestV);
        }

        public Vector4 Evaluate(float u, float v)
        {
            return ModelMatrix * new Vector4(
                         (float)((OutsideRadius + InsideRadius * Math.Cos(u)) * Math.Cos(v)),
                         InsideRadius * (float)Math.Sin(u),
                         (float)((OutsideRadius + InsideRadius * Math.Cos(u)) * Math.Sin(v))
                         );
        }

        public Vector4 EvaluateDU(float u, float v)
        {
            return ModelMatrix * new Vector4(
                         (float)(-Math.Sin(u) * Math.Cos(v) * InsideRadius),
                         InsideRadius * (float)Math.Cos(u),
                         (float)(-InsideRadius * Math.Sin(u) * Math.Sin(v)),
                         0
                         );
        }

        public Vector4 EvaluateDV(float u, float v)
        {
            return ModelMatrix * new Vector4(
                        (float)((-OutsideRadius * Math.Sin(v)) - InsideRadius * Math.Cos(u) * Math.Sin(v)),
                        0,
                        (float)((OutsideRadius * Math.Cos(v)) + InsideRadius * Math.Cos(u) * Math.Cos(v)),
                        0
                        );
        }

        public float ClampU(float u)
        {
            if (u > MaxU)
            {
                if (IsClampedU)
                {
                    return 0 + (u - MaxU);
                }
                else
                    return MaxU;
            }
            else if (u < 0)
            {
                if (IsClampedU)
                {
                    return MaxU - u;
                }
                else
                    return 0;
            }
            else
                return u;

        }

        public float ClampV(float v)
        { 
            if (v > MaxV)
            {
                if (IsClampedV)
                {
                    return 0 + (v - MaxV);
                }
                else
                    return MaxV;
            }
            else if (v < 0)
            {
                if (IsClampedV)
                {
                    return MaxV - v;
                }
                else
                    return 0;
            }
            else
                return v;
        }

        public Vector4 EvaluateNormal(float u, float v)
        {
            var d1 = EvaluateDU(u, v).Normalized();
            var d2 = EvaluateDV(u, v).Normalized();
            return (d1 ^ d2).Normalized();
        }

        public Vector4 EvaluateTrimming(float u, float v)
        {
            return ModelMatrix * new Vector4(
                         (float)((OutsideRadius + InsideRadius * Math.Cos(u)) * Math.Cos(v)),
                         InsideRadius * (float)Math.Sin(u),
                         (float)((OutsideRadius + InsideRadius * Math.Cos(u)) * Math.Sin(v))
                         );
        }
    }
}
