using System;
using System.Collections.Generic;
using Engine.Utilities;

namespace Engine.Models
{
    public class VoxelGrid : _3DObject
    {
        public enum RenderingMode
        {
            squares,
            qubes
        }

        public RenderingMode renderingMode;
        public Voxel[,,] Voxels;
        public int Precision
        {
            get { return precision; }
            set
            {
                if (precision == value) return;
                precision = value;
                CreateVoxels();
                NotifyPropertyChanged();
            }
        }
        private int precision;

        public VoxelGrid(int precision = 30) : base(new Vector4(0,0,0), new Vector4(0, 0, 0), new Vector4(1, 1, 1))
        {
            Name = "VoxelGrid" + counter++.ToString();
            Precision = precision;
            renderingMode = RenderingMode.squares;
            CreateVoxels();
        }

        private void CreateVoxels()
        {
            Voxels = new Voxel[Precision, Precision, Precision];
            if (Precision == 1)
            {
                Voxels[0, 0, 0] = new Voxel(new Vector4(0, 0, 0), 2);
            }
            else
            {
                float step = 1.0f / (Precision - 1);
                for (int i = 0; i < Precision; i++)
                {
                    for (int j = 0; j < Precision; j++)
                    {
                        for (int k = 0; k < Precision; k++)
                        {
                            Voxels[i, j, k] = new Voxel(new Vector4(-Precision / 2 + i * step * Precision, -Precision / 2 + j * step * Precision, -Precision / 2 + k * step * Precision), step * Precision);
                        }
                    }
                }
                //float step = 1.0f / (Precision - 1);
                //for (int i = 0; i < Precision; i++)
                //{
                //    for (int j = 0; j < Precision; j++)
                //    {
                //        for (int k = 0; k < Precision; k++)
                //        {
                //            Voxels[i, j, k] = new Voxel(new Vector4(i * step, j * step, k * step));
                //        }
                //    }
                //}
            }
        }

        public void UpdateWithKinectData(short[] depthData, int width, int height, float angle, int maxDepth, int minDepth)
        {
            var sin = (float)Math.Sin(angle);
            var cos = (float)Math.Cos(angle);
            int counter = 0;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var depth = depthData[y * width + x];
                    if (depth <= 0) continue;
                    int yVox = (height - 1 - y) * Precision / height;
                    //int xVox = (width - 1 - x) * precision / width;
                    for (int xVox = 0; xVox < Precision; xVox++)
                    {
                        for (int zVox = 0; zVox < Precision; zVox++)
                        {
                            var vox = Voxels[xVox, yVox, zVox];
                            if (vox.isActive == false) continue;
                            var coords = vox.GetCoords(cos, sin);
                            coords = TransformCoords(coords, width, height, maxDepth, minDepth);
                            if (Math.Abs(x - coords.w) > 2) continue;
                            if (depth > coords.d)
                            {
                                vox.isActive = false;
                                counter++;
                            }
                        }
                    }
                }
            }
            //angle += (float)Math.PI / 4;
        }

        private (float w, float h, float d) TransformCoords((float w, float h, float d) coords, int width, int height, int maxDepth, int minDepth)
        {
            return (coords.w / Precision * 640 + 320, coords.h / Precision * 480 + 240, coords.d / Precision * (maxDepth - minDepth) + (minDepth + maxDepth) / 2);
        }

        public override _3DObject Clone()
        {
            throw new NotImplementedException();
        }

        public override _3DObject CloneMirrored()
        {
            throw new NotImplementedException();
        }

        public override void Render(Renderer renderer, Matrix4 projView, int width, int height)
        {
            renderer.RenderVoxelGrid(this, projView, width, height, Color);
        }

        public override void RenderSterographic(Renderer renderer, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            renderer.RenderStereographicVoxelGrid(this, projView, leftProjView, rightProjView, width, height);
        }

        public override string Save()
        {
            return "";
        }

        public void StartAnimation()
        {
            Random random = new Random();
            for (int i = 0; i < Precision / 2; i++)
            {
                for (int j = 0; j < Precision / 2; j++)
                {
                    for (int k = 0; k < Precision / 2; k++)
                    {
                        Voxels[random.Next(i, Precision), random.Next(j, Precision), random.Next(k, Precision)].isActive = false;
                    }
                }
            }
        }

        public StaticObjModel ConvertToObj()
        {
            var triangles = MarchingCubes.GetTriangles(this);
            var res = CreateEdgesAndPointsFromTraingles(triangles);
            StaticObjModel model = new StaticObjModel(res.Item1, res.Item2);
            return model;
        }

        private (List<Edge>, List<Vector4>) CreateEdgesAndPointsFromTraingles(Triangle[] triangles)
        {
            List<Vector4> points = new List<Vector4>();
            List<Edge> edges = new List<Edge>();
            int index1, index2, index3;
            foreach (var triangle in triangles)
            {
                Vector4 p1 = new Vector4((float)triangle.p1.X, (float)triangle.p1.Y, (float)triangle.p1.Z);
                Vector4 p2 = new Vector4((float)triangle.p2.X, (float)triangle.p2.Y, (float)triangle.p2.Z);
                Vector4 p3 = new Vector4((float)triangle.p3.X, (float)triangle.p3.Y, (float)triangle.p3.Z);
                index1 = CheckIfPointExist(p1, points);
                index2 = CheckIfPointExist(p2, points);
                index3 = CheckIfPointExist(p3, points);
                if (index1 == -1)
                    points.Add(p1);
                if (index2 == -1)
                    points.Add(p2);
                if (index3 == -1)
                    points.Add(p3);
                //Edge e = new Edge(points.Count - 3, points.Count - 2);
                //edges.Add(e);
                //e = new Edge(points.Count - 2, points.Count - 1);
                //edges.Add(e);
                //e = new Edge(points.Count - 1, points.Count - 3);
                //edges.Add(e);
                Edge e = new Edge(index1 != -1 ? index1 : points.IndexOf(p1), index2 != -1 ? index2 : points.IndexOf(p2));
                edges.Add(e);
                e = new Edge(index2 != -1 ? index2 : points.IndexOf(p2), index3 != -1 ? index3 : points.IndexOf(p3));
                edges.Add(e);
                e = new Edge(index3 != -1 ? index3 : points.IndexOf(p3), index1 != -1 ? index1 : points.IndexOf(p1));
                edges.Add(e);
            }
            return (edges, points);
        }

        private int CheckIfPointExist(Vector4 point, List<Vector4> points)
        {
            float eps = 0.00005f;
            foreach (var p in points)
            {
                if (Math.Abs(point.X - p.X) < eps && Math.Abs(point.Y - p.Y) < eps && Math.Abs(point.Z - p.Z) < eps)
                {
                    return points.IndexOf(p);
                }
            }
            return -1;
        }
    }
}
