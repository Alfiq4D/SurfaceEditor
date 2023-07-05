using Engine.Interfaces;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;

namespace Engine.Utilities
{
    public class Intersector
    {
        private readonly bool offsetEnabled;
        private readonly float offset;
        private float newtonMultiplier = 0.1f; //0.003f;
        private float newtonCorrectionEps = 0.0001f;
        private int newtonMaxIterations = 20;
        private float stepMultiplier = 0.01f;
        private float commonPointEps = 0.004f; // 0.01f
        private float commonPointMultiplier = 0.002f; // 0.001f
        private int commonPointMaxIterations = 10000;
        private int intersectionMaxIterations = 10000;
        private int intersectionMinIteration = 12;
        private float minPointsDistance = 0.0001f;

        public string ErrorString { get; set; }
        public bool Error { get; set; }
        public List<Vector4> Points { get; set; } = new List<Vector4>();
        public List<(float u, float v)> Surface1Parameters { get; set; } = new List<(float u, float v)>();
        public List<(float u, float v)> Surface2Parameters { get; set; } = new List<(float u, float v)>();

        public Intersector(bool offsetEnabled, float offset)
        {
            this.offsetEnabled = offsetEnabled;
            this.offset = offset;
        }

        public void SmoothIntersection(float eps)
        {
            var point = new List<Vector4>(Points);
            Points.Clear();
            for (int i = 1; i < point.Count; i++)
            {
                if (Vector4.Distance(point[i - 1], point[i]) < 2 * eps)
                    Points.Add(point[i - 1]);
            }
        }

        public void Intersect(IIntersectable surface1, IIntersectable surface2, float stepEps, Vector4 startPosition, bool selfIntersection = false, float newton = 0.1f)
        {
            Reset();
            stepMultiplier = stepEps;
            minPointsDistance = stepEps / 100.0f;
            newtonMultiplier = newton;
            (float u, float v) = surface1.GetPoint3DUV(startPosition);
            (float s, float t) = surface2.GetPoint3DUV(startPosition);
            (float u, float v, float s, float t) next = (-1, -1, -1, -1);
            if (!selfIntersection)
                next = GetCommonPoint(surface1, surface2, u, v, s, t);
            else
            {
                next = GetCommonPoint(surface1, surface2, 0, 0, surface1.MaxU, surface1.MaxV, true);
                if (next.u == -1)
                    next = GetCommonPoint(surface1, surface2, 0, 0, 0, surface1.MaxV, true);
                if (next.u == -1)
                    next = GetCommonPoint(surface1, surface2, 0, 0, surface1.MaxU, 0, true);
            }
            if (next.u == -1)
            {
                ErrorString = "Error while searching for intersection";
                Error = true;
                return;
            }

            Vector4 point1 = surface1.Evaluate(next.u, next.v);
            Vector4 point2 = surface2.Evaluate(next.s, next.t);
            AddPoint(point1, next.u, next.v, next.s, next.t, false, surface1);
            Vector4 firstPoint = point1;
            Vector4 oldPoint = point1;
            Vector4 oldOldPoint = new Vector4(100,100,100,0);
            Vector4 oldPoint1 = point2;
            //float pointEps = 0.02f;
            float firstPointEps =  2 * stepEps;
            int counter = 0;
            float distance1 = float.MaxValue;
            float distance2 = float.MaxValue;
            float previousDistance = float.MaxValue;
            float firstPointDistance = float.MaxValue;
            var firstUV = next;
            bool reverse = false;
            while (counter < intersectionMaxIterations && previousDistance > minPointsDistance && (firstPointDistance > firstPointEps || counter < intersectionMinIteration))
            {
                next = GetNextIntersectionPoint(surface1, surface2, next.u, next.v, next.s, next.t, reverse);
                if (next.u == -1)
                    break;
                point1 = surface1.Evaluate(next.u, next.v);
                point2 = surface2.Evaluate(next.s, next.t);
                AddPoint(point1, next.u, next.v, next.s, next.t, reverse, surface1);
                distance1 = Vector4.Distance(oldPoint, point1);
                distance2 = Vector4.Distance(oldPoint1, point2);
                previousDistance = Vector4.Distance(oldOldPoint, point1);
                firstPointDistance = Vector4.Distance(firstPoint, point1);
                oldOldPoint = oldPoint.Clone();
                oldPoint = point1.Clone();
                oldPoint1 = point2.Clone();
                if ((distance1 < minPointsDistance || distance2 < minPointsDistance || previousDistance < minPointsDistance) && !reverse)
                {
                    reverse = true;
                    next = firstUV;
                    distance1 = float.MaxValue;
                    distance2 = float.MaxValue;
                    previousDistance = float.MaxValue;
                    counter = 0;
                }
                counter++;
            }
            if (firstPointDistance < firstPointEps)
            {
                AddPoint(firstPoint, firstUV.u, firstUV.v, firstUV.s, firstUV.t, reverse, surface1);
            }
        }

        private void AddPoint(Vector4 p, float u, float v, float s, float t, bool reverse, IIntersectable obj1)
        {
            Vector4 point = new Vector4(p.X, p.Y, p.Z, p.W);
            if(offsetEnabled)
            {
                Vector4 n = obj1.EvaluateDU(u, v) ^ obj1.EvaluateDV(u, v);
                n.W = 0;
                n = n.Normalized();
                point += n * offset;
            }
            if (reverse)
            {
                Points.Insert(0, point);
                Surface1Parameters.Insert(0, (u, v));
                Surface2Parameters.Insert(0, (s, t));
            }
            else
            {
                Points.Add(point);
                Surface1Parameters.Add((u, v));
                Surface2Parameters.Add((s, t));
            }
        }

        private void Reset()
        {
            ErrorString = "";
            Error = false;
            Points = new List<Vector4>();
            Surface1Parameters = new List<(float u, float v)>();
            Surface2Parameters = new List<(float u, float v)>();
        }

        private Vector4 GetGradient(IIntersectable obj1, IIntersectable obj2, float u, float v, float s, float t)
        {
            Vector4 p1 = obj1.Evaluate(u, v);
            Vector4 p2 = obj2.Evaluate(s, t);
            Vector4 pd = p2 - p1;
            Vector4 du1 = obj1.EvaluateDU(u, v).Normalized();
            Vector4 dv1 = obj1.EvaluateDV(u, v).Normalized();
            Vector4 du2 = obj2.EvaluateDU(s, t).Normalized();
            Vector4 dv2 = obj2.EvaluateDV(s, t).Normalized();
            Vector4 res = new Vector4(Vector4.DotProduct(pd, du1), Vector4.DotProduct(pd, dv1), Vector4.DotProduct(-pd, du2), Vector4.DotProduct(-pd, dv2));

            return 2 * res;
        }

        private (float u, float v, float s, float t) GetCommonPoint(IIntersectable obj1, IIntersectable obj2, float u, float v, float s, float t, bool selfIntersection = false)
        {
            float startU = u, startV = v, startS = s, startT = t;
            Vector4 p1 = obj1.Evaluate(u, v);
            Vector4 p2 = obj2.Evaluate(s, t);
            float dist = Vector4.Distance(p1, p2);
            int counter = 0;
            if (!selfIntersection)
            {
                while (dist > commonPointEps && counter < commonPointMaxIterations)
                {
                    Vector4 g = GetGradient(obj1, obj2, u, v, s, t);
                    g *= commonPointMultiplier;
                    u = obj1.ClampU(u + g.X);
                    v = obj1.ClampV(v + g.Y);
                    s = obj2.ClampU(s + g.Z);
                    t = obj2.ClampV(t + g.W);
                    Vector4 p = CoorectIntersectionPoint(obj1, obj2, u, v, s, t);
                    p1 = obj1.Evaluate(p.X, p.Y);
                    p2 = obj2.Evaluate(p.Z, p.W);
                    dist = Vector4.Distance(p1, p2);
                    counter++;
                }
            }
            else
            {
                bool doAgain = false;
                int repetition = 0;
                while (doAgain || ((dist > commonPointEps || (Math.Abs(u - s) < commonPointEps && Math.Abs(v - t) < commonPointEps)) && counter < commonPointMaxIterations))
                {
                    doAgain = false;
                    Vector4 g = GetGradient(obj1, obj2, u, v, s, t);
                    g *= commonPointMultiplier;
                    u = obj1.ClampU(u + g.X);
                    v = obj1.ClampV(v + g.Y);
                    s = obj2.ClampU(s + g.Z);
                    t = obj2.ClampV(t + g.W);
                    p1 = obj1.Evaluate(u, v);
                    p2 = obj2.Evaluate(s, t);
                    dist = Vector4.Distance(p1, p2);
                    counter++;
                    if (repetition == 0 && selfIntersection && counter == commonPointMaxIterations)
                    {
                        repetition++;
                        counter = 0;
                        commonPointMultiplier /= 10.0f;
                        u = startU;
                        v = startV;
                        s = startS;
                        t = startT;
                        doAgain = true;
                    }
                }
            }
            if (counter == commonPointMaxIterations)
            {
                ErrorString = "Error while searching for intersection";
                Error = true;
                return (-1, -1, -1, -1);
            }
            return (u, v, s, t);
        }

        private (float u, float v, float s, float t) GetNextIntersectionPoint(IIntersectable obj1, IIntersectable obj2, float u, float v, float s, float t, bool reverse)
        {
            Vector4 du1 = obj1.EvaluateDU(u, v).Normalized();
            Vector4 dv1 = obj1.EvaluateDV(u, v).Normalized();
            Vector4 n1 = du1 ^ dv1;
            Vector4 du2 = obj2.EvaluateDU(s, t).Normalized();
            Vector4 dv2 = obj2.EvaluateDV(s, t).Normalized();
            Vector4 n2 = du2 ^ dv2;
            Vector4 dir = n1 ^ n2;
            if (Vector4.Distance(dir, Vector4.Zero()) == 0)
            {
                ErrorString = "Error while searching for intersection";
                Error = true;
                return (u, v, s, t);
            }
            dir = dir.Normalized();
            Vector4 res = new Vector4(Vector4.DotProduct(dir, du1), Vector4.DotProduct(dir, dv1), Vector4.DotProduct(dir, du2), Vector4.DotProduct(dir, dv2));
            res *= (reverse ? -stepMultiplier : stepMultiplier);
            u = obj1.ClampU(u + res.X);
            v = obj1.ClampV(v + res.Y);
            s = obj2.ClampU(s + res.Z);
            t = obj2.ClampV(t + res.W);
            Vector4 p = CoorectIntersectionPoint(obj1, obj2, u, v, s, t);
            return (p.X, p.Y, p.Z, p.W);
        }

        private (float u, float v, float s, float t) GetNextIntersectionPointAlt(IIntersectable obj1, IIntersectable obj2, float u, float v, float s, float t, bool reverse, float eps)
        {
            Vector4 du1 = obj1.EvaluateDU(u, v).Normalized();
            Vector4 dv1 = obj1.EvaluateDV(u, v).Normalized();
            Vector4 n1 = du1 ^ dv1;
            Vector4 du2 = obj2.EvaluateDU(s, t).Normalized();
            Vector4 dv2 = obj2.EvaluateDV(s, t).Normalized();
            Vector4 n2 = du2 ^ dv2;
            Vector4 dir = n1 ^ n2;

            var worldSpacePoint = obj1.Evaluate(u, v);

            float direction = reverse ? -1 : 1;
            float div = 0.1f;
            var surfacePoint = worldSpacePoint + direction * div * dir;

            Func<float, float, Vector4> f3 = (fu, fv) => surfacePoint + fu * n1 + fv * n2;

            var system = new EquationSystem(obj1, obj2, f3, false);
            var startPoints = new float[] {u, v, s, t, 0, 0};

            float[] solutions = new float[] { -1, -1, -1, -1 };

            NewtonRaphsonSolver.Solve(system, startPoints, out solutions, 10, div * div, out var _);

            return (obj1.ClampU(solutions[0]), obj1.ClampV(solutions[1]), obj2.ClampU(solutions[2]), obj2.ClampV(solutions[3]));
        }

        private Vector4 CoorectIntersectionPoint(IIntersectable surface1, IIntersectable surface2, float u, float v, float s, float t)
        {
            Vector4 basePoint = new Vector4(u, v, s, t);
            float correction = float.MaxValue;
            int i = 0;
            while (correction > newtonCorrectionEps && i < newtonMaxIterations)
            {
                Vector4 du1 = surface1.EvaluateDU(basePoint.X, basePoint.Y);
                Vector4 dv1 = surface1.EvaluateDV(basePoint.X, basePoint.Y);
                Vector4 du2 = surface2.EvaluateDU(basePoint.Z, basePoint.W);
                Vector4 dv2 = surface2.EvaluateDV(basePoint.Z, basePoint.W);
                Vector4 d = -(surface1.Evaluate(basePoint.X, basePoint.Y) - surface2.Evaluate(basePoint.Z, basePoint.W));
                Vector<float> F = Vector<float>.Build.DenseOfArray(new float[] { d.X, d.Y, d.Z });
                var r = GetInversedJacobiMatrix(du1, dv1, du2, dv2) * F;
                r *= newtonMultiplier;
                basePoint.X = surface1.ClampU(basePoint.X + r[0]);
                basePoint.Y = surface1.ClampV(basePoint.Y + r[1]);
                basePoint.Z = surface2.ClampU(basePoint.Z + r[2]);
                basePoint.W = surface2.ClampV(basePoint.W + r[3]);
                ++i;
                correction = (float)r.L2Norm();
            }
            return basePoint;
        }

        private Matrix<float> GetInversedJacobiMatrix(Vector4 du1, Vector4 dv1, Vector4 du2, Vector4 dv2)
        {
            float[][] rows = new float[3][]
           {
                new float[4],
                new float[4],
                new float[4]
           };
            Vector4[] cols = new Vector4[]
           {
               du1, dv1, -du2, -dv2
           };
            for (int i = 0; i < 4; ++i)
            {
                rows[0][i] = cols[i].X;
                rows[1][i] = cols[i].Y;
                rows[2][i] = cols[i].Z;
            }
            var m = Matrix<float>.Build.DenseOfRowArrays(rows);
            var j = m.PseudoInverse();
            return j;
        }
    }
}
