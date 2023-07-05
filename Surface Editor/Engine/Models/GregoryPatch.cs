using System;
using System.Collections.Generic;
using Engine.Utilities;

namespace Engine.Models
{
    public class GregoryPatch : SurfacePatch
    {
        public List<SurfacePatch> SurfacePatches { get; set; }
        private readonly List<List<ArtificialPoint3D>> cornerPoints;
        private readonly List<ArtificialPoint3D> corners;
        public List<Vector4> netPoints;
        private Vector4 centralPoint;// p, p31, p32, p33, p21, p22, p23, p11, p12, p13, q1, q2, q3;
        private Vector4[] p3, p2, p1, q;
        public List<Vector4> vectorsPoints = new List<Vector4>();
        private int N = 3;

        public GregoryPatch(List<List<ArtificialPoint3D>> points, List<SurfacePatch> patches) : base()
        {
            Name = "GregoryPatch" + counter++.ToString();
            SurfacePatches = patches;
            cornerPoints = points;
            N = SurfacePatches.Count;
            netPoints = new List<Vector4>();
            corners = GetCommonPoints(cornerPoints, SurfacePatches);
            CalculateCentralPoint();
            ControlPoints = new ArtificialPoint3D[0, 0];
            foreach (var patch in patches)
                patch.PropertyChanged += Patch_PropertyChanged;
            UpdateGregory();
        }

        private List<ArtificialPoint3D> GetCommonPoints(List<List<ArtificialPoint3D>> cornerPoints, List<SurfacePatch> surfacePatches)
        {
            List<ArtificialPoint3D> corners = new List<ArtificialPoint3D>();
            var idx = surfacePatches[N - 1].GetPointIndex(cornerPoints[0][0]);
            if (idx.i != -1)
                corners.Add(cornerPoints[0][0]);
            else
                corners.Add(cornerPoints[0][1]);
            for (int i = 0; i < surfacePatches.Count - 1; i++)
            {
                idx = surfacePatches[i].GetPointIndex(cornerPoints[(i + 1)%N][0]);
                if (idx.i != -1)
                    corners.Add(cornerPoints[(i + 1) %N][0]);
                else
                    corners.Add(cornerPoints[(i + 1) %N][1]);
            }
            return corners;            
        }

        private void UpdateGregory()
        {
            CalculateCentralPoint();
            netPoints = new List<Vector4>();
            Edges = new List<Edge>();
            SinglePatches.Clear();
            for (int i = 0; i < N; i++)
            {
                FillSubPatch(i);
            }
            CreateEdges();
        }

        private void FillSubPatch(int i)
        {
            int j = (i + N - 1) % N;
            Vector4[,] subPatch = new Vector4[4, 4];
            subPatch[0, 0] = corners[i].Position;
            subPatch[0, 1] = p3[i];
            subPatch[1, 1] = centralPoint;
            subPatch[1, 0] = p3[j];

            Vector4 vv00 = SurfacePatches[i].GetTangentBorder(corners[i], corners[(i + 1) % N], 1) / 2;
            Vector4 vu00 = SurfacePatches[j].GetTangentBorder(corners[i], corners[j], 0) / 2;
            Vector4 vv01 = SurfacePatches[i].GetTangentBorder(corners[i], corners[(i + 1) % N], 0.5f) / 2;
            Vector4 vu01 = SurfacePatches[i].GetDerivativeBorder(corners[i], corners[(i + 1) % N], 0.5f) / 2;
            Vector4 vu10 = SurfacePatches[j].GetTangentBorder(corners[i], corners[j], 0.5f) / 2;
            Vector4 vv10 = SurfacePatches[j].GetDerivativeBorder(corners[i], corners[j], 0.5f) / 2;

            subPatch[0, 2] = vv00;
            subPatch[0, 3] =  vv01;
            subPatch[1, 2] = vv10;
            subPatch[1, 3] = centralPoint - p1[j];

            subPatch[2, 0] =  vu00;
            subPatch[2, 1] = vu01;
            subPatch[3, 0] =  vu10;
            subPatch[3, 1] = centralPoint - p1[i];

            Vector4 vvu00 = SurfacePatches[i].GetDerivativeBorder(corners[i], corners[(i + 1) % N], 1);
            Vector4 vuv00 = SurfacePatches[j].GetDerivativeBorder(corners[i], corners[j], 0);
            Vector4 vuv01 =- vv01;
            Vector4 vvu01 =- vu01;
            Vector4 vuv10 = -vv10;
            Vector4 vvu10 = vu10;
            Vector4 vuv11 = -vv01;
            Vector4 vvu11 = vu10;

            vectorsPoints.Clear();
            vectorsPoints.Add(vuv00);
            vectorsPoints.Add(vvu00);
            vectorsPoints.Add(vuv01);
            vectorsPoints.Add(vvu01);
            vectorsPoints.Add(vuv10);
            vectorsPoints.Add(vvu10);
            vectorsPoints.Add(vuv11);
            vectorsPoints.Add(vvu11);            
            SinglePatches.Add(new SinglePatchGregory(subPatch, vectorsPoints.ToArray()));
        }

        private void Patch_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateGregory();
        }

        public override _3DObject Clone()
        {
            return null;
        }

        public override _3DObject CloneMirrored()
        {
            return null;
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
            renderer.RenderGregoryPatch(this, projView, width, height, Color);
        }

        public override void RenderSterographic(Renderer renderer, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            renderer.RenderStereographicGregoryPatch(this, projView, leftProjView, rightProjView, width, height);
        }

        public override string Save()
        {
            return "";
        }

        protected override void CreateSinglePatches()
        {
            throw new NotImplementedException();
        }

        private void CalculateCentralPoint()
        {
            p3 = new Vector4[N];
            p2 = new Vector4[N];
            p1 = new Vector4[N];
            q = new Vector4[N];
            Vector4 sumQ = Vector4.Zero();

            for (int i = 0; i < SurfacePatches.Count; i++)
            {
                p3[i] = SurfacePatches[i].GetPatchValue(corners[i], corners[(i + 1) % N], 0.5f);
                p2[i] = p3[i] + SurfacePatches[i].GetDerivativeBorder(corners[i], corners[(i + 1) % N], 0.5f) / 3;
                q[i] = (3 * p2[i] - p3[i]) / 2;
                sumQ += q[i];
            }
            centralPoint = sumQ / N;
            for (int i = 0; i < SurfacePatches.Count; i++)
            {
                p1[i] = (2 * q[i] + centralPoint) / 3;
            }
        }

        private void CreateEdges()
        {
            Edges = new List<Edge>();
            netPoints = new List<Vector4>();
            int counter = 0;
            for(int j= 0; j < SurfacePatches.Count; ++j)
            {
                float step = 0.2f;
                for(float i = step; i< 1; i+= step)
                {
                    Vector4 p = SurfacePatches[j].GetPatchValue(cornerPoints[j][0], cornerPoints[j][1], i);
                    netPoints.Add(p);
                    Vector4 pp = p + SurfacePatches[j].GetDerivativeBorder(cornerPoints[j][0], cornerPoints[j][1], i) / 3;
                    pp.W = 1;
                    netPoints.Add(pp);
                    Edges.Add(new Edge(2 * counter, 2 * counter + 1));
                    counter++;
                }
            }
        }

        public override int GetUPatchesNumber()
        {
            throw new NotImplementedException();
        }

        public override int GetVPatchesNumber()
        {
            throw new NotImplementedException();
        }

        protected override ArtificialPoint3D[,] CreatePoints(int u, int v, int width, int height)
        {
            throw new NotImplementedException();
        }
    }
}
