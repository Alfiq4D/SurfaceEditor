using System.Collections.Generic;
using Engine.Utilities;
using Engine.Interfaces;

namespace Engine.Models
{
    public class RectangularBezierPatch : SurfacePatch, IIntersectable
    {
        public RectangularBezierPatch(Vector4 initialPosition, ArtificialPoint3D[,] points) : base(initialPosition, points)
        {
            Name = "Patch" + counter++.ToString();
            CreateSinglePatches();
            CreateEdges();
        }

        public RectangularBezierPatch(string name, Vector4 initialPosition, ArtificialPoint3D[,] points) : base(initialPosition, points)
        {
            Name = name;
            CreateSinglePatches();
            CreateEdges();
        }

        public RectangularBezierPatch(Vector4 initialPosition, int u, int v, int width, int height) : base(initialPosition)
        {
            Name = "Patch" + counter++.ToString();
            ArtificialPoint3D[,] points = CreatePoints(u, v, width, height);
            InitPatch(points);
            CreateSinglePatches();
            CreateEdges();
            Vector4 mid = new Vector4(width / 2.0f, height / 2.0f, 0, 1);
            Position.X = mid.X;
            Position.Y = mid.Y;
        }

        public bool IsClampedU => false;

        public bool IsClampedV => false;

        public float MaxU => ControlPoints.GetLength(0) / 3;

        public float MaxV => ControlPoints.GetLength(1) / 3;

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
            return new RectangularBezierPatch(Position, points);
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
            return new RectangularBezierPatch(new Vector4(Position.X, Position.Y, -Position.Z), points);
        }

        public override float GetStepU()
        {
            return 1f / (VerticalPrecision - 1);
        }

        public override float GetStepV()
        {
            return 1f / (HorizontalPrecision - 1);
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
            string val = "surfaceC0 1\n" + Name + " " + ControlPoints.GetLength(0) / 3 + " " +  ControlPoints.GetLength(1) / 3;
            for (int i = 0; i < ControlPoints.GetLength(1); i++)
            {
                for (int j = 0; j < ControlPoints.GetLength(0); j++)
                {
                    val += " " + ControlPoints[j,i].Position.ToString() + ";" + ControlPoints[j,i].Name;
                }
            }
            val += "\n";
            return val;
        }

        protected override void CreateSinglePatches()
        {
            for (int i = 0; i < ControlPoints.GetLength(0) - 1; i+=3)
            {
                for (int j = 0; j < ControlPoints.GetLength(1) - 1; j+=3)
                {
                    ArtificialPoint3D[,] points = new ArtificialPoint3D[4, 4];
                    for (int m = 0; m < 4; m++)
                    {
                        for (int n = 0; n < 4; n++)
                        {
                            points[m, n] = ControlPoints[i + m, j + n];
                        }
                    }
                    SinglePatch singlePatch = new SinglePatchC0(points, GetTrimmingFunction(i / 3, j / 3));
                    SinglePatches.Add(singlePatch);
                }
            }
        }

        private void CreateEdges()
        {
            Edges = new List<Edge>();
            int m = ControlPoints.GetLength(0);
            int n = ControlPoints.GetLength(1);
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j < m - 1; j++)
                {
                    Edges.Add(new Edge(j * n + i, (j + 1) * n + i));
                    Edges.Add(new Edge(j * n + i, j * n + (i + 1)));
                }
                Edges.Add(new Edge((m - 1) * n + i, (m - 1) * n + (i + 1)));
            }
            for (int j = 0; j < m - 1; j++)
            {
                Edges.Add(new Edge(j * n + n - 1, (j + 1) * n + n - 1));
            }
        }

        public (float u, float v) GetPoint3DUV(Vector4 pos)
        {
            float bestU = 0, bestV = 0;
            Vector4 bestPos = Evaluate(bestU, bestV);
            float minDistance = Vector4.Distance(bestPos, pos);
            const int N = 128; //number of samples
            float stepU = ControlPoints.GetLength(0) / 3 * 1.0f / (N - 1);
            float stepV = ControlPoints.GetLength(1) / 3 * 1.0f / (N - 1);
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
            int idx1 = ControlPoints.GetLength(0) / 3;
            int idx2 = ControlPoints.GetLength(1) / 3;
            int modU = (int)u;
            int modV = (int)v;
            if (modV == idx2)
                modV -= 1;
            if (modU == idx1)
                modU -= 1;
            return SinglePatches[(idx1 - 1 - modU) * idx2 + (idx2 - 1 - modV)].GetPatchValue(u - modU, v - modV);
            //return SinglePatches[(modU) * idx2 + (modV)].GetPatchValue(u - modU, v - modV);
            //return SinglePatches[(idx2 - 1 - modV) * idx2 + (idx1 - 1 - modU)].GetPatchValue(u - modU, v - modV);
            //return SinglePatches[0].GetPatchValue(u, v);
        }

        public Vector4 EvaluateDU(float u, float v)
        {
            int idx1 = ControlPoints.GetLength(0) / 3;
            int idx2 = ControlPoints.GetLength(1) / 3;
            int modU = (int)u;
            int modV = (int)v;
            if (modV == idx2)
                modV -= 1;
            if (modU == idx1)
                modU -= 1;
            //return SinglePatches[modU * idx1 + modV].GetPatchDU(u- modU, v - modV);
            return SinglePatches[(idx1 - 1 - modU) * idx2 + (idx2 - 1 - modV)].GetPatchDU(u - modU, v - modV);
            //return SinglePatches[(modU) * idx2 + (modV)].GetPatchDU(u - modU, v - modV);
            //return SinglePatches[(idx2 - 1 - modV) * idx2 + (idx1 - 1 - modU)].GetPatchDU(u - modU, v - modV);
            //return SinglePatches[0].GetPatchDU(u, v);
        }

        public Vector4 EvaluateDV(float u, float v)
        {
            int idx1 = ControlPoints.GetLength(0) / 3;
            int idx2 = ControlPoints.GetLength(1) / 3;
            int modU = (int)u;
            int modV = (int)v;
            if (modV == idx2)
                modV -= 1;
            if (modU == idx1)
                modU -= 1;
            //return SinglePatches[modU * idx1 + modV].GetPatchDV(u - modU, v - modV);
            return SinglePatches[(idx1 - 1 - modU) * idx2 + (idx2 - 1 - modV)].GetPatchDV(u - modU, v - modV);
            //return SinglePatches[(modU) * idx2 + (modV)].GetPatchDV(u - modU, v - modV);
            //return SinglePatches[(idx2 - 1 - modV) * idx2 + (idx1 - 1 - modU)].GetPatchDV(u - modU, v - modV);
            //return SinglePatches[0].GetPatchDV(u, v);
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

        public override int GetUPatchesNumber()
        {
            return ControlPoints.GetLength(0) / 3;
        }

        public override int GetVPatchesNumber()
        {
            return ControlPoints.GetLength(1) / 3;
        }

        protected override ArtificialPoint3D[,] CreatePoints(int u, int v, int width, int height)
        {
            ArtificialPoint3D[,] points = new ArtificialPoint3D[u * 3 + 1, v * 3 + 1];
            float wDelta = 1f * width / (u * 3);
            float hDelta = 1f * height / (v * 3);
            Vector4 mid = new Vector4(width / 2.0f, height / 2.0f, 0, 1);
            for (int i = 0; i < u * 3 + 1; i++)
            {
                for (int j = 0; j < v * 3 + 1; j++)
                {
                    ArtificialPoint3D p = new ArtificialPoint3D(new Vector4(i * wDelta - mid.X, j * hDelta - mid.Y, 0));
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
            int idx1 = ControlPoints.GetLength(0) / 3;
            int idx2 = ControlPoints.GetLength(1) / 3;
            int modU = (int)u;
            int modV = (int)v;
            if (modV == idx2)
                modV -= 1;
            if (modU == idx1)
                modU -= 1;
            return SinglePatches[(idx1 - 1 - modU) * idx2 + (idx2 - 1 - modV)].GetPatchValue(u - modU, v - modV);
        }
    }
}
