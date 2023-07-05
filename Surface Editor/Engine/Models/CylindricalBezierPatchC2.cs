using Engine.Interfaces;
using Engine.Utilities;
using System;
using System.Collections.Generic;

namespace Engine.Models
{
    public class CylindricalBezierPatchC2 : SurfacePatch, IIntersectable
    {
        public CylindricalBezierPatchC2(Vector4 initialPosition, ArtificialPoint3D[,] points) : base(initialPosition, points)
        {
            Name = "Patch" + counter++.ToString();
            CreateSinglePatches();
            LinkSymmetricalPoints();
            CreateEdges();
        }

        public CylindricalBezierPatchC2(string name, Vector4 initialPosition, ArtificialPoint3D[,] points) : base(initialPosition, points)
        {
            Name = name;
            CreateSinglePatches();
            LinkSymmetricalPoints();
            CreateEdges();
        }

        public CylindricalBezierPatchC2(Vector4 initialPosition, int u, int v, int radius, int height) : base(initialPosition)
        {
            Name = "Patch" + counter++.ToString();
            ArtificialPoint3D[,] points = CreatePoints(u, v, radius, height);
            InitPatch(points);
            CreateSinglePatches();
            LinkSymmetricalPoints();
            CreateEdges();
            Vector4 mid = new Vector4(0, height / 2.0f, 0, 1);
            Position.Y = mid.Y;
        }

        public bool IsClampedU => false;

        public bool IsClampedV => true;

        public float MaxU => ControlPoints.GetLength(0) - 3;

        public float MaxV => ControlPoints.GetLength(1) - 3 + 3;

        public override _3DObject Clone()
        {
            ArtificialPoint3D[,] points = new ArtificialPoint3D[ControlPoints.GetLength(0), ControlPoints.GetLength(1)];
            for (int i = 0; i < ControlPoints.GetLength(0); i++)
            {
                for (int j = 0; j < ControlPoints.GetLength(1); j++)
                {
                    points[i, j] = (ArtificialPoint3D)ControlPoints[i, j].Clone();
                }
            }
            return new CylindricalBezierPatchC2(Position, points);
        }

        public override _3DObject CloneMirrored()
        {
            ArtificialPoint3D[,] points = new ArtificialPoint3D[ControlPoints.GetLength(0), ControlPoints.GetLength(1)];
            for (int i = 0; i < ControlPoints.GetLength(0); i++)
            {
                for (int j = 0; j < ControlPoints.GetLength(1); j++)
                {
                    points[i, j] = (ArtificialPoint3D)ControlPoints[i, j].CloneMirrored();
                }
            }
            return new CylindricalBezierPatchC2(new Vector4(Position.X, Position.Y, -Position.Z), points);
        }

        public override float GetStepU()
        {
            return 1f / (VerticalPrecision - 3);
        }

        public override float GetStepV()
        {
            return 1f / (HorizontalPrecision - 3);
        }

        public override void Render(Renderer renderer, Matrix4 projView, int width, int height)
        {
            renderer.RenderBezierPatch(this, projView, width, height, Color);
        }

        public override void RenderSterographic(Renderer renderer, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            renderer.RenderBezierPatchStereographic(this, projView, leftProjView, rightProjView, width, height);
        }

        public override string Save()
        {
            string val = "tubeC2 1\n" + Name + " " + (ControlPoints.GetLength(1) / 3) + " " + (ControlPoints.GetLength(0) / 3);
            for (int i = 0; i < ControlPoints.GetLength(0); i++)
            {
                for (int j = 0; j < ControlPoints.GetLength(1); j++)
                {
                    val += " " + ControlPoints[i, j].Position.ToString() + ";" + ControlPoints[i, j].Name;
                }
            }
            val += "\n";
            return val;
        }

        protected override void CreateSinglePatches()
        {
            ArtificialPoint3D[,] fakePoints = new ArtificialPoint3D[ControlPoints.GetLength(0), ControlPoints.GetLength(1) + 3];
            for (int i = 0; i < ControlPoints.GetLength(0); i++)
            {
                for (int j = 0; j < ControlPoints.GetLength(1); j++)
                {
                    fakePoints[i, j] = ControlPoints[i, j];
                }
            }
            for (int i = 0; i < ControlPoints.GetLength(0); i++)
            {
                fakePoints[i, ControlPoints.GetLength(1)] = ControlPoints[i, 0];
                fakePoints[i, ControlPoints.GetLength(1) + 1] = ControlPoints[i, 1];
                fakePoints[i, ControlPoints.GetLength(1) + 2] = ControlPoints[i, 2];
            }

            for (int i = 0; i < fakePoints.GetLength(0) - 3; i += 1)
            {
                for (int j = 0; j < fakePoints.GetLength(1) - 3; j += 1)
                {
                    ArtificialPoint3D[,] points = new ArtificialPoint3D[4, 4];
                    for (int m = 0; m < 4; m++)
                    {
                        for (int n = 0; n < 4; n++)
                        {
                            points[m, n] = fakePoints[i + m, (j + n) % fakePoints.GetLength(1)];
                        }
                    }
                    SinglePatch singlePatch = new SinglePatchC2(points, GetTrimmingFunction(i, j));
                    SinglePatches.Add(singlePatch);
                }
            }
        }

        private void CreateEdges()
        {
            Edges = new List<Edge>();
            int m = ControlPoints.GetLength(0);
            int n = ControlPoints.GetLength(1);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m - 1; j++)
                {
                    Edges.Add(new Edge(j * n + i, (j + 1) * n + i));
                    Edges.Add(new Edge(j * n + i, j * n + (i + 1) % n));
                }
                Edges.Add(new Edge((m - 1) * n + i, (m - 1) * n + (i + 1) % n));
            }
        }

        private void LinkSymmetricalPoints()
        {
            for (int i = 0; i < ControlPoints.GetLength(0); i++)
            {
                for (int j = 1; j < ControlPoints.GetLength(1) / 2 + 1; j++)
                {
                    ControlPoints[i, j].SymmetricalPoint = ControlPoints[i, ControlPoints.GetLength(1) - j];
                    ControlPoints[i, ControlPoints.GetLength(1) - j].SymmetricalPoint = ControlPoints[i, j];
                }
                ControlPoints[i, 0].SymmetricalPoint = ControlPoints[i, 0];
            }
        }

        public void CorrectCymmetricalPoints()
        {
            for (int i = 0; i < ControlPoints.GetLength(0); i++)
            {
                for (int j = 0; j < ControlPoints.GetLength(1) / 2 + 1; j++)
                {
                    if (ControlPoints[i, j].SymmetricalPoint != null)
                    {
                        ControlPoints[i, j].SymmetricalPoint.Position.X = ControlPoints[i, j].Position.X;
                        ControlPoints[i, j].SymmetricalPoint.Position.Y = ControlPoints[i, j].Position.Y;
                        if (!(ControlPoints[i, j].SymmetricalPoint == ControlPoints[i, j]))
                            ControlPoints[i, j].SymmetricalPoint.Position.Z = -ControlPoints[i, j].Position.Z;
                        else
                            ControlPoints[i, j].Position.Z = 0;
                    }
                }
            }
        }

        public (float u, float v) GetPoint3DUV(Vector4 pos)
        {
            float bestU = 0, bestV = 0;
            Vector4 bestPos = Evaluate(bestU, bestV);
            float minDistance = Vector4.Distance(bestPos, pos);
            const int N = 128; //number of samples
            float stepU = (int)MaxU * 1.0f / (N - 1);
            float stepV = (int)MaxV * 1.0f / (N - 1);
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    Vector4 p = Evaluate(i * stepU, j * stepV);
                    float distance = Vector4.Distance(p, pos);
                    if (distance < minDistance)
                    {
                        bestU = i * stepU;
                        bestV = j * stepV;
                        minDistance = distance;
                    }
                }
            }
            return (bestU, bestV);
        }

        public Vector4 Evaluate(float u, float v)
        {
            u = ClampU(u);
            v = ClampV(v);
            int idx1 = (int)MaxU;
            int idx2 = (int)MaxV;
            int modU = (int)u;
            int modV = (int)v;
            if (modV == idx2)
                modV -= 1;
            if (modU == idx1)
                modU -= 1;
            return SinglePatches[(idx1 - 1 - modU) * idx2 + (idx2 - 1 - modV)].GetPatchValue(u - modU, v - modV);
            //return SinglePatches[0].GetPatchValue(u, v);
        }

        public Vector4 EvaluateDU(float u, float v)
        {
            u = ClampU(u);
            v = ClampV(v);
            int idx1 = (int)MaxU;
            int idx2 = (int)MaxV;
            int modU = (int)u;
            int modV = (int)v;
            if (modV == idx2)
                modV -= 1;
            if (modU == idx1)
                modU -= 1;
            //return SinglePatches[modU * idx1 + modV].GetPatchDU(u- modU, v - modV);
            return SinglePatches[(idx1 - 1 - modU) * idx2 + (idx2 - 1 - modV)].GetPatchDU(u - modU, v - modV);
            //return SinglePatches[0].GetPatchDU(u, v);
        }

        public Vector4 EvaluateDV(float u, float v)
        {
            u = ClampU(u);
            v = ClampV(v);
            int idx1 = (int)MaxU;
            int idx2 = (int)MaxV;
            int modU = (int)u;
            int modV = (int)v;
            if (modV == idx2)
                modV -= 1;
            if (modU == idx1)
                modU -= 1;
            //return SinglePatches[modU * idx1 + modV].GetPatchDV(u - modU, v - modV);
            return SinglePatches[(idx1 - 1 - modU) * idx2 + (idx2 - 1 - modV)].GetPatchDV(u - modU, v - modV);
            //return SinglePatches[0].GetPatchDV(u, v);
        }

        public float ClampU(float u)
        {
            if (u > MaxU)
            {
                if (IsClampedU)
                {
                    while (u > MaxU)
                        u -= MaxU;
                    return u;
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
                    while (v > MaxV)
                        v -= MaxV;
                    return v;
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

        public override int GetUPatchesNumber()
        {
            return ControlPoints.GetLength(0) - 3;
        }

        public override int GetVPatchesNumber()
        {
            return ControlPoints.GetLength(1) - 3 + 3;
        }

        protected override ArtificialPoint3D[,] CreatePoints(int u, int v, int radius, int height)
        {
            ArtificialPoint3D[,] points = new ArtificialPoint3D[u * 3 + 1, v * 3];
            float rDelta = (1f * 2 * (float)Math.PI) / (points.GetLength(1));
            float hDelta = 1f * height / (points.GetLength(0) - 1);
            Vector4 mid = new Vector4(0, height / 2.0f, 0, 1);
            for (int i = 0; i < points.GetLength(0); i++)
            {
                for (int j = 0; j < points.GetLength(1); j++)
                {
                    ArtificialPoint3D p = new ArtificialPoint3D(new Vector4(radius * (float)Math.Cos(j * rDelta), i * hDelta - mid.Y, radius * (float)Math.Sin(j * rDelta)));
                    points[i, j] = p;
                }
            }
            return points;
        }

        public Vector4 EvaluateNormal(float u, float v)
        {
            var d1 = EvaluateDU(u, v).Normalized();
            var d2 = EvaluateDV(u, v).Normalized();
            return (d1 ^ d2).Normalized();
        }

        public Vector4 EvaluateTrimming(float u, float v)
        {
            u = ClampU(u);
            v = ClampV(v);
            int idx1 = (int)MaxU;
            int idx2 = (int)MaxV;
            int modU = (int)u;
            int modV = (int)v;
            if (modV == idx2)
                modV -= 1;
            if (modU == idx1)
                modU -= 1;
            return SinglePatches[(idx1 - 1 - modU) * idx2 + (idx2 - 1 - modV)].GetPatchTrimmingValue(u - modU, v - modV);
        }
    }
}