using Engine.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;

namespace Engine.Utilities
{
    public class PathGenerator
    {
        float[,] heightMap;
        int divisionsX, divisionsY;
        readonly Vector3 blockSize;
        readonly float baseHeight;
        int samplingSize;
        readonly float zOffset;
        readonly float safeHeight;
        readonly string filePath;
        readonly string mapsPath;
        readonly string trimmingsPath;
        readonly float[] roughingLayersHeight;
        readonly float xMin, xMax, yMin, yMax;
        int trimmingPrecision;
        readonly float uMultiplier;
        readonly float roughingZOffset;
        int offset;

        public PathGenerator()
        {
            divisionsX = 100;
            divisionsY = 100;
            samplingSize = 150;
            trimmingPrecision = 600;
            uMultiplier = 1.3f;
            blockSize = new Vector3(15, 15, 5);
            baseHeight = 2;
            zOffset = 2.2f;
            roughingZOffset = 0.05f;
            safeHeight = 60;
            roughingLayersHeight = new float[3];
            roughingLayersHeight[0] = 3.6f;
            roughingLayersHeight[1] = 2.2f;
            roughingLayersHeight[2] = 2.0f;
            offset = (int)(0.08f * divisionsX);
            filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\..\\..\\Paths\\";
            mapsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\..\\..\\Maps\\";
            trimmingsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\..\\..\\Trimmings\\";
            xMin = -7.5f;
            xMax = 7.5f;
            yMin = 0;
            yMax = 15;
        }

        public void CreateRoughingPath(ParametricModel model)
        {
            divisionsX = 100;
            divisionsY = 100;
            samplingSize = 200;
            offset = (int)(0.08f * divisionsX);
            if (!LoadHeightMap())
                CreateHeightMap(model, roughingZOffset);
            var positions = GenerateRoughingPath();
            CreatePathFile(positions, 3, filePath + "1.k16");
        }

        public _3DObject CreateBaseMachiningPath(ParametricModel model)
        {
            float r = 0.5f;
            var positions = CreateEnvelope(model, r);
            CreateEnvelopePath(positions);
            positions = CreateEnvelope(model, 0.75f);
            var pos = GenerateFlattingPath(positions);
            CreateFlattingPath(pos);;
            IntersectionCurve intersectionCurve = new IntersectionCurve(pos);
            return intersectionCurve;
        }

        public _3DObject CreateFinishMachiningPath(ParametricModel model)
        {
            var intersections = CalculateIntersections(model, 0.4f);

            var path = CreateFinishSurfacePath(model, intersections);
            List<Vector3> positions = new List<Vector3>();
            positions.AddRange(path.Select(v => { return TransformToWorld(v); }));
            positions.Insert(0, new Vector3(0, 0, safeHeight));
            positions.Add(new Vector3(0, 0, safeHeight));

            var path1 = CreateFinishIntersectionPaths(model, intersections);
            var positions1 = new List<Vector3>();
            positions1.AddRange(path1.Select(v => { return TransformToWorld(v); }));

            var path2 = CreateInsideFinishPaths(model, 0.38f);
            var positions2 = new List<Vector3>();
            positions2.AddRange(path2.Select(v => { return TransformToWorld(v); }));
            positions2.Insert(0, new Vector3(0, 0, safeHeight));
            positions2.Add(new Vector3(0, 0, safeHeight));

            List<Vector3> fullPath = new List<Vector3>();
            fullPath.AddRange(positions);
            fullPath.AddRange(positions1);
            fullPath.AddRange(positions2);
            CreatePathFile(fullPath, 3, filePath + "4.k08");

            IntersectionCurve intersectionCurve = new IntersectionCurve(new List<Vector4>(path1));
            return intersectionCurve;
        }

        public void CreateHeightMap(ParametricModel model, float zOffset = 0.05f)
        {
            heightMap = new float[divisionsX, divisionsY];
            for (int i = 0; i < divisionsX; i++)
            {
                for (int j = 0; j < divisionsY; j++)
                {
                    heightMap[i, j] = baseHeight + zOffset;
                }
            }
            foreach (var surface in model.Parts)
            {
                foreach (var singlePatch in surface.SinglePatches)
                {
                    for (int i = 0; i < samplingSize; i++)
                    {
                        float u = 1f * i / (samplingSize - 1);
                        for (int j = 0; j < samplingSize; j++)
                        {
                            float v = 1f * j / (samplingSize - 1);
                            Vector4 point = singlePatch.GetPatchValue(u, v);
                            var pos = TransformToGrid(point);
                            heightMap[(int)pos.X, (int)pos.Y] = Math.Max(heightMap[(int)pos.X, (int)pos.Y], pos.Z + zOffset);
                        }
                    }
                }
            }
            SaveHeightMap();
        }

        private List<Vector3> GenerateRoughingPath()
        {
            float r = 0.8f;
            int R = (int)(r * (divisionsY - 1) / blockSize.Y);
            List<Vector3> positions = new List<Vector3>
            {
                new Vector3(0, 0, safeHeight)
            };
            for (int i = 0; i < 2; i++)
            {
                var pos = TransformToWorld(-offset, -offset, roughingLayersHeight[i]);
                pos.Z = safeHeight;
                positions.Add(pos);
                int y = -offset;
                for (; y < divisionsY + offset; y += R)
                {
                    bool skipped = false;
                    int x = -offset;
                    float height = GetHeightValue(x, y, i);
                    positions.Add(TransformToWorld(x, y, height));
                    float z = height;
                    for (; x < divisionsX - 1 + offset; x++)
                    {
                        float maxZ = GetMaxHeightRadius(x, y, i, R, r);
                        if (maxZ == z)
                        {
                            skipped = true;
                            continue;
                        }
                        else
                        {
                            if (skipped)
                            {
                                positions.Add(TransformToWorld(x - 1, y, z));
                                positions.Add(TransformToWorld(x, y, maxZ));
                            }
                            else
                            {
                                positions.Add(TransformToWorld(x, y, maxZ));
                            }
                            skipped = false;
                            z = maxZ;
                        }
                    }
                    height = GetHeightValue(x, y, i);
                    positions.Add(TransformToWorld(x, y, height));
                    y += R;
                    height = GetHeightValue(x, y, i);
                    z = height;
                    positions.Add(TransformToWorld(x, y, height));
                    skipped = false;
                    for (; x > -offset; x--)
                    {
                        float maxZ = GetMaxHeightRadius(x, y, i, R, r);
                        if (maxZ == z)
                        {
                            skipped = true;
                            continue;
                        }
                        else
                        {
                            if (skipped)
                            {
                                positions.Add(TransformToWorld(x + 1, y, z));
                                positions.Add(TransformToWorld(x, y, maxZ));
                            }
                            else
                            {
                                positions.Add(TransformToWorld(x, y, maxZ));
                            }
                            skipped = false;
                            z = maxZ;
                        }
                    }
                    height = GetHeightValue(x, y, i); // TODO: change to get height radius
                    positions.Add(TransformToWorld(x, y, height));
                }
                pos = TransformToWorld(-offset, y - R, roughingLayersHeight[i]);
                pos.Z = safeHeight;
                positions.Add(pos);
            }
            positions.Add(new Vector3(0, 0, safeHeight));
            return positions;
        }

        private float GetHeightValue(int x, int y, int layer)
        {
            float height;
            if (!(x < 0 || x > divisionsX - 1 || y < 0 || y > divisionsY - 1))
                height = Math.Max(heightMap[x, y], roughingLayersHeight[layer]);
            else
                height = roughingLayersHeight[layer];
            return height;
        }

        private float GetMaxHeightRadius(int x, int y, int layer, int R, float r)
        {
            float height;
            float maxZ = roughingLayersHeight[layer];
            float gridStepY = 1f * divisionsY / divisionsX * blockSize.X / blockSize.Y;
            int i1 = x - R;
            int i2 = x + R;
            float radius2 = R * R;
            for (int i = i1; i <= i2; i++)
            {
                float localRadius = x - i;
                localRadius *= localRadius;
                localRadius = (float)Math.Sqrt(radius2 - localRadius);

                localRadius *= gridStepY;

                int j1 = (int)Math.Ceiling(y - localRadius);
                int j2 = (int)Math.Floor(y + localRadius);

                for (int j = j1; j <= j2; j++)
                {
                    float h = 0;
                    if (i >= 0 && i < divisionsX && j >= 0 && j < divisionsY)
                    {
                        var center = TransformToWorld(x, y);
                        var point = TransformToWorld(i, j);
                        var dist = Vector2.Distance(center, point);
                        h = r - (float)Math.Sqrt(r * r - dist * dist);
                    }
                    height = GetHeightValue(i, j, layer) - h;
                    if (height > maxZ)
                        maxZ = height;
                }
            }
            return maxZ;
        }

        private void SaveHeightMap()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            string text = "";
            for (int i = 0; i < divisionsX; i++)
            {
                for (int j = 0; j < divisionsY; j++)
                {
                    text += heightMap[i, j] + "\n";
                }
            }
            File.WriteAllText(mapsPath + divisionsX + "_" + divisionsY + ".txt", text);
        }

        private bool LoadHeightMap()
        {
            string filename = mapsPath + divisionsX + "_" + divisionsY + ".txt";
            if (!File.Exists(filename))
                return false;
            try
            {
                var lines = File.ReadAllLines(filename);
                heightMap = new float[divisionsX, divisionsY];
                int i = 0;
                int j = 0;
                foreach (var line in lines)
                {
                    heightMap[i, j] = float.Parse(line);
                    j++;
                    if (j > divisionsY - 1)
                    {
                        j = 0;
                        i++;
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void CreatePathFile(List<Vector3> positions, int noFirstOperation, string filename = null)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            string text = "";
            int n = noFirstOperation;
            foreach (Vector3 position in positions)
            {
                text += "N" + n++ + "G01X" + position.X.ToString("0.000") + "Y" + position.Y.ToString("0.000") + "Z" + position.Z.ToString("0.000") + "\n";
            }
            if (filename == null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                if (saveFileDialog.ShowDialog() == true)
                {

                    File.WriteAllText(saveFileDialog.FileName, text);
                }
            }
            else
            {
                File.WriteAllText(filename, text);
            }
        }

        private void CreateFirstPathFile(List<Vector3> positions, int noFirstOperation, string filename = null)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            string text = "";
            int n = noFirstOperation;
            Vector3 p = new Vector3(0, 0, safeHeight);
            text += "N" + n++ + "G01Z" + p.Z.ToString("0.000") + "\n";
            foreach (Vector3 position in positions)
            {
                text += "N" + n++ + "G01X" + position.X.ToString("0.000") + "Y" + position.Y.ToString("0.000") + "Z" + position.Z.ToString("0.000") + "\n";
            }
            if (filename == null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                if (saveFileDialog.ShowDialog() == true)
                {

                    File.WriteAllText(saveFileDialog.FileName, text);
                }
            }
            else
            {
                File.WriteAllText(filename, text);
            }
        }

        private Vector3 TransformToWorld(int x, int y, float z) // xyz - grid positions
        {
            float X = x * blockSize.X / (divisionsX - 1) - blockSize.X / 2;
            float Y = y * blockSize.Y / (divisionsY - 1) - blockSize.Y / 2;
            float Z = z;
            return new Vector3(X * 10, Y * 10, Z * 10);
        }

        private Vector4 TransformToScene(int x, int y, float z) // xyz - grid positions
        {
            float X = x * (xMax - xMin) / (divisionsX - 1) + xMin;
            float Y = y * (yMax - yMin) / (divisionsY - 1) + yMin;
            float Z = z;
            return new Vector4(X, Y, Z, 1);
        }

        private Vector2 TransformToWorld(int x, int y) // xy - grid positions
        {
            float X = x * blockSize.X / (divisionsX - 1) - blockSize.X / 2;
            float Y = y * blockSize.Y / (divisionsY - 1) - blockSize.Y / 2;
            return new Vector2(X, Y);
        }

        private Vector3 TransformToWorld(Vector4 position) // vec - scene position
        {
            float X = (position.X - xMin) / (xMax - xMin) * blockSize.X - blockSize.X / 2;
            float Y = (position.Y - yMin) / (yMax - yMin) * blockSize.Y - blockSize.Y / 2;
            return new Vector3(X * 10, Y * 10, (position.Z) * 10);
        }

        private Vector3 TransformToGrid(Vector4 position) // vec - scene position
        {
            Vector3 result = new Vector3();
            result.X = (position.X - xMin) / (xMax - xMin) * (divisionsX - 1);
            result.Y = (position.Y - yMin) / (yMax - yMin) * (divisionsY - 1);
            result.Z = position.Z + zOffset;
            return result;
        }

        private List<Vector4> CreateEnvelope(ParametricModel model, float r)
        {
            EnvelopeConnector connector = new EnvelopeConnector();
            RectangularBezierPatch rectangularBezier = new RectangularBezierPatch(new Vector4(0, 0, 0, 0), 2, 2, 20, 20);
            rectangularBezier.Position.X = 0;

            Intersector tailIntersector = new Intersector(true, r);
            tailIntersector.Intersect(model["Tail"], rectangularBezier, 0.01f, ElephantPathHelper.Tail.IntersectionPoints.Top);
            connector.ConnectIntersetcion(tailIntersector.Points);

            Intersector torsoIntersector = new Intersector(true, r);
            torsoIntersector.Intersect(model["Torso"], rectangularBezier, 0.01f, ElephantPathHelper.Torso.IntersectionPoints.Top);
            connector.ConnectIntersetcion(torsoIntersector.Points);

            Intersector headIntersector = new Intersector(true, r);
            headIntersector.Intersect(model["Head"], rectangularBezier, 0.01f, ElephantPathHelper.Head.IntersectionPoints.Top);
            connector.ConnectIntersetcion(headIntersector.Points);

            Intersector trumpetIntersetor = new Intersector(true, r);
            trumpetIntersetor.Intersect(model["Trumpet"], rectangularBezier, 0.01f, ElephantPathHelper.Trumpet.IntersectionPoints.Front);
            connector.ConnectIntersetcion(trumpetIntersetor.Points);

            Intersector frontLegsIntersector = new Intersector(true, r);
            frontLegsIntersector.Intersect(model["FrontLegs"], rectangularBezier, 0.01f, ElephantPathHelper.FrontLegs.IntersectionPoints.Front);
            frontLegsIntersector.Points.Add(r * (new Vector4(0, -1, 0, 0)) + frontLegsIntersector.Points.Last());
            connector.ConnectIntersetcion(frontLegsIntersector.Points);

            connector.MoveDirBefeoreNext(r, new Vector4(0, 1, 0, 0));

            Intersector frontLegsIntersector1 = new Intersector(true, r);
            frontLegsIntersector1.Intersect(model["FrontLegs"], rectangularBezier, 0.01f, ElephantPathHelper.FrontLegs.IntersectionPoints.Back);
            connector.AddIntersection(frontLegsIntersector1.Points);

            Intersector torsoIntersector1 = new Intersector(true, r);
            torsoIntersector1.Intersect(model["Torso"], rectangularBezier, 0.01f, ElephantPathHelper.Torso.IntersectionPoints.Bottom);
            connector.ConnectIntersetcion(torsoIntersector1.Points);

            Intersector backLegsIntersector = new Intersector(true, -r);
            backLegsIntersector.Intersect(model["BackLegs"], rectangularBezier, 0.01f, ElephantPathHelper.BackLegs.IntersectionPoints.Front);
            connector.ConnectIntersetcion(backLegsIntersector.Points, 1, true);

            connector.MoveDir(r, new Vector4(0, -1, 0, 0));
            connector.MoveDirBefeoreNext(r, new Vector4(0, 1, 0, 0));

            Intersector backLegsIntersector1 = new Intersector(true, -r);
            backLegsIntersector1.Intersect(model["BackLegs"], rectangularBezier, 0.01f, ElephantPathHelper.BackLegs.IntersectionPoints.Back);
            connector.AddIntersection(backLegsIntersector1.Points);

            Intersector torsoIntersector2 = new Intersector(true, r);
            torsoIntersector2.Intersect(model["Torso"], rectangularBezier, 0.01f, ElephantPathHelper.Torso.IntersectionPoints.Bottom);
            connector.ConnectIntersetcion(torsoIntersector2.Points);

            Intersector tailIntersector1 = new Intersector(true, r);
            tailIntersector1.Intersect(model["Tail"], rectangularBezier, 0.01f, ElephantPathHelper.Tail.IntersectionPoints.Bottom);
            connector.ConnectIntersetcion(tailIntersector1.Points);

            connector.ConnectEnvelope(false);

            return connector.Positions;
        }

        private void CreateEnvelopePath(List<Vector4> points)
        {
            List<Vector3> positions = new List<Vector3>();
            MoveFlatToStart(positions);
            int idx = 0;
            var pos = TransformToWorld(-offset, -offset, baseHeight);
            float sqDist = Vector4.SquareDistance(points[idx], new Vector4(pos.X, pos.Y, pos.Z, 1));
            for (int i = 1; i < points.Count; ++i)
            {
                if (Vector4.SquareDistance(points[i], new Vector4(pos.X, pos.Y, pos.Z, 1)) < sqDist)
                {
                    sqDist = Vector4.SquareDistance(points[i], new Vector4(pos.X, pos.Y, pos.Z, 1));
                    idx = i;
                }
            }
            positions.AddRange(points.Skip(idx).Select(v => { v.Z = baseHeight; return TransformToWorld(v); }));
            positions.AddRange(points.Take(idx + 1).Select(v => { v.Z = baseHeight; return TransformToWorld(v); }));
            MoveToSafe(positions);
            CreatePathFile(positions, 3, filePath + "3.f10");
        }

        private List<Vector4> GenerateFlattingPath(List<Vector4> positions)
        {
            divisionsX = 100;
            divisionsY = 100;
            offset = (int)(0.08f * divisionsX);
            List<Vector4> path = new List<Vector4>();
            float smallR = 0.6f;
            //float envelopeR = 0.2f;
            float envelopeR = 0.0f;
            float eps = 0.1f;
            float r = smallR * 2 - eps;
            int R = (int)(r * (divisionsY - 1) / blockSize.Y);
            float xIncrement = 1f * R * (yMax - yMin) / (divisionsY - 1);

            Vector4 offsetVector = new Vector4(0, envelopeR, 0, 0);
            List<Vector4> lowerEnvelope = new List<Vector4>(positions);
            lowerEnvelope = lowerEnvelope.Select(v => v - offsetVector).ToList();
            List<Vector4> upperEnvelope = new List<Vector4>(positions);
            upperEnvelope = upperEnvelope.Select(v => v + offsetVector).ToList();

            var start = TransformToScene(50, -offset, 0);
            var middle = TransformToScene(50, divisionsY / 2, 0);
            var end = TransformToScene(50, divisionsY + offset, 0);
            List<Vector4> line = new List<Vector4> { start, middle };
            EnvelopeConnector connector = new EnvelopeConnector();

            bool lower = false;
            int startY = -offset;
            int endY = divisionsY + offset;
            int middleY = divisionsY / 3;
            int x = -offset + 5;
            int firstX = -100;
            start = TransformToScene(x, startY, 0);
            middle = TransformToScene(x, divisionsY / 2, 0);
            middle.Y += 1;
            line = new List<Vector4> { start, middle };
            for (; x < divisionsX + offset + 4;)
            {
                connector.Clear();
                connector.ConnectIntersetcion(line);
                bool found = false;
                if (x < 10)
                    found = connector.AddDirectedConnectionX(lowerEnvelope, new Vector4(R, 0, 0, 0), start.X + xIncrement, 2);
                else
                    found = connector.AddDirectedConnectionX(lowerEnvelope, new Vector4(R, 0, 0, 0), start.X + xIncrement);
                if (!found)
                {
                    path.Add(start);
                    path.Add(TransformToScene(x, endY, 0));
                    int tmp = startY;
                    startY = endY;
                    endY = tmp;
                    lower = !lower;
                }
                else
                {
                    if (firstX == -100)
                        firstX = x;
                    path.AddRange(connector.Positions);
                    x += R;
                    path.Add(TransformToScene(x, startY, 0));

                }
                x += R;
                start = TransformToScene(x, startY, 0);
                middle = TransformToScene(x, divisionsY / 2, 0);
                middle.Y += 1;
                line = new List<Vector4> { start, middle };
            }
            x = firstX;
            endY = -offset;
            startY = divisionsY + offset;
            middleY = 0;
            start = TransformToScene(x, startY, 0);
            middle = TransformToScene(x, middleY, 0);
            line = new List<Vector4> { start, middle };
            positions.AddRange(MoveCutterTo(positions.Last(), start));
            for (; x < divisionsX + offset;)
            {
                connector.Clear();
                connector.ConnectIntersetcion(line);

                bool found = false;
                if (x < 29)
                    found = connector.AddDirectedConnectionX(upperEnvelope, new Vector4(R, 0, 0, 0), start.X + xIncrement, 1);
                else
                    found = connector.AddDirectedConnectionX(upperEnvelope, new Vector4(R, 0, 0, 0), start.X + xIncrement);
                if (!found)
                {
                    break;
                }
                else
                {
                    path.AddRange(connector.Positions);
                    x += R;
                    path.Add(TransformToScene(x, startY, 0));
                }
                x += R;
                start = TransformToScene(x, startY, 0);
                middle = TransformToScene(x, middleY, 0);
                line = new List<Vector4> { start, middle };
            }

            return path;
        }

        private void CreateFlattingPath(List<Vector4> points)
        {
            List<Vector3> positions = new List<Vector3>();
            MoveFlatToStart(positions);
            positions.AddRange(points.Select(v => { if (v.Z != safeHeight) v.Z = baseHeight; return TransformToWorld(v); }));
            MoveToSafe(positions);
            CreatePathFile(positions, 3, filePath + "2.f12");
        }

        private List<Vector4> MoveCutterTo(Vector4 start, Vector4 end)
        {
            List<Vector4> points = new List<Vector4>
            {
                new Vector4(start.X, start.Y, safeHeight / 10),
                new Vector4(end.X, end.Y, safeHeight / 10)
            };

            return points;
        }

        private void MoveFlatToStart(List<Vector3> positions)
        {
            positions.Add(new Vector3(0, 0, safeHeight));
            var pos1 = TransformToWorld(-offset, -offset, safeHeight / 10);
            var pos2 = TransformToWorld(-offset, -offset, baseHeight);
            positions.Add(pos1);
            positions.Add(pos2);
        }

        private void MoveToSafe(List<Vector3> positions)
        {
            var pos = positions.Last();
            pos.Z = safeHeight;
            positions.Add(pos);
            positions.Add(new Vector3(0, 0, safeHeight));
        }

        private List<Vector4> CreateFinishSurfacePath(ParametricModel model, Dictionary<string, Intersector> intersections)
        {
            List<Vector4> path = new List<Vector4>();
            float r = 0.4f;
            int R = (int)(r * (divisionsY - 1) / blockSize.Y);

            path.AddRange(CreateFrontLegsFinish(model, intersections, r));

            path.AddRange(CreateBackLegsFinish(model, intersections, r));

            path.AddRange(CreateTailFinish(model, intersections, r));

            path.AddRange(CreateTrumpetFinish(model, intersections, r));

            path.AddRange(CreateTorsoFinish(model, intersections, r));

            path.AddRange(CreateEarFinish(model, intersections, r));

            path.AddRange(CreateHeadFinish(model, intersections, r));

            return path;
        }

        private List<Vector4> CreateFrontLegsFinish(ParametricModel model, Dictionary<string, Intersector> intersections, float r)
        {
            List<Vector4> path = new List<Vector4>();
            var floorIntersection = new List<(float u, float v)>(intersections["FrontLegs_Floor_Left"].Surface1Parameters);
            floorIntersection.AddRange(intersections["FrontLegs_Floor_Right"].Surface1Parameters);

            var surface = model["FrontLegs"];
            var trimming1 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.5f, 0.5f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            floorIntersection = intersections["FrontLegs_Torso"].Surface1Parameters;
            var trimming2 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.1f, 0.8f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            floorIntersection = intersections["FrontLegs_Trumpet"].Surface1Parameters;
            var trimming3 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.8f, 0.1f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            var trimming = MergeTrimmingTables(trimming1, trimming2);
            trimming = MergeTrimmingTables(trimming, trimming3);

            var frontLegsFloor1s = JsonConvert.SerializeObject(trimming1);
            File.WriteAllText(trimmingsPath + nameof(frontLegsFloor1s) + ".txt", frontLegsFloor1s);
            var frontLegsTorso2s = JsonConvert.SerializeObject(trimming2);
            File.WriteAllText(trimmingsPath + "frontLegsTorso2s.txt", frontLegsTorso2s);
            var frontLegs = JsonConvert.SerializeObject(trimming);
            File.WriteAllText(trimmingsPath + "frontLegs.txt", frontLegs);
            var frontLegsTrumpet = JsonConvert.SerializeObject(trimming3);
            File.WriteAllText(trimmingsPath + nameof(frontLegsTrumpet) + ".txt", frontLegsTrumpet);

            bool uFound = false;
            bool uSafe = false;
            bool vFound = false;
            bool vSafe = false;
            var shiftedSurface = new ShiftedSurface(surface, r);
            int N = ElephantPathHelper.GetPart(surface.Name).Samples;
            float stepU = (int)surface.MaxU * 1.0f / (uMultiplier * N - 1);
            float stepV = (int)surface.MaxV * 1.0f / (N - 1);
            bool trimmingFunc(float u, float v, bool[,] trim) => trim[(int)Math.Min(u * trimmingPrecision / surface.MaxU, trimmingPrecision - 1), (int)Math.Min(v * trimmingPrecision / surface.MaxV, trimmingPrecision - 1)];
            for (int i = 0; i < N; i++)
            {
                uFound = false;
                float v = i * stepV;
                for (int j = 0; j < (int)(uMultiplier * N); j++)
                {
                    float u = j * stepU;
                    if (trimmingFunc(u, v, trimming))
                    {
                        if(vFound)
                            vSafe = true;
                        continue;
                    }
                    var pos = shiftedSurface.Evaluate(u, v);
                    pos.Z -= r;
                    pos.Z += zOffset;
                    if ((vSafe && vFound) || (path.Count > 0 && Vector4.Distance(pos, path.Last()) > 1))
                    {
                        vSafe = false;
                        if (path.Count > 0)
                            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                    }
                    else if (uSafe)
                    {
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                        uSafe = false;
                    }
                    path.Add(pos);
                    uFound = true;
                    vFound = true;
                }
                vSafe = false;
                vFound = false;

                if (!uFound && path.Count > 0)
                {
                    uSafe = true;
                    path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                }
                uFound = false;

                i++;
                if (i == N)
                    break;
                v = i * stepV;

                for (int j = (int)(uMultiplier * N - 1); j >= 0; j--)
                {
                    float u = j * stepU;
                    if (trimmingFunc(u, v, trimming))
                    {
                        if (vFound)
                            vSafe = true;
                        continue;
                    }
                    var pos = shiftedSurface.Evaluate(u, v);
                    pos.Z -= r;
                    pos.Z += zOffset;
                    if ((vSafe && vFound) || (path.Count > 0 && Vector4.Distance(pos, path.Last()) > 1))
                    {
                        vSafe = false;
                        if (path.Count > 0)
                            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                    }
                    else if (uSafe)
                    {
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                        uSafe = false;
                    }

                    path.Add(pos);
                    uFound = true;
                    vFound = true;
                }
                vSafe = false;
                vFound = false;

                if (!uFound && path.Count > 0)
                {
                    uSafe = true;
                    path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                }
                uFound = false;
            }
            path.Insert(0, new Vector4(path.First().X, path.First().Y, safeHeight / 10));
            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
            return path;
        }

        private List<Vector4> CreateBackLegsFinish(ParametricModel model, Dictionary<string, Intersector> intersections, float r)
        {
            List<Vector4> path = new List<Vector4>();

            var floorIntersection = new List<(float u, float v)>(intersections["BackLegs_Floor_Left"].Surface1Parameters);
            floorIntersection.AddRange(intersections["BackLegs_Floor_Right"].Surface1Parameters);

            var surface = model["BackLegs"];
            var trimming1 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.5f, 0.9f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            floorIntersection = intersections["BackLegs_Torso"].Surface1Parameters;
            var trimming2 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.1f, 0.1f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            var trimming = MergeTrimmingTables(trimming1, trimming2);

            bool uFound = false;
            bool uSafe = false;
            var shiftedSurface = new ShiftedSurface(surface, -r);
            int N = ElephantPathHelper.GetPart(surface.Name).Samples;
            float stepU = (int)surface.MaxU * 1.0f / (uMultiplier * N - 1);
            float stepV = (int)surface.MaxV * 1.0f / (N - 1);
            bool trimmingFunc(float u, float v, bool[,] trim) => trim[(int)Math.Min(u * trimmingPrecision / surface.MaxU, trimmingPrecision - 1), (int)Math.Min(v * trimmingPrecision / surface.MaxV, trimmingPrecision - 1)];
            for (int i = 0; i < N; i++)
            {
                uFound = false;
                float v = i * stepV;
                for (int j = 0; j < (int)(uMultiplier * N); j++)
                {
                    float u = j * stepU;
                    if (trimmingFunc(u, v, trimming))
                        continue;
                    var pos = shiftedSurface.Evaluate(u, v);
                    if (uSafe)
                    {
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                        uSafe = false;
                    }
                    pos.Z -= r;
                    pos.Z += zOffset;
                    path.Add(pos);
                    uFound = true;
                }

                if (!uFound && path.Count > 0)
                {
                    uSafe = true;
                    path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                }
                uFound = false;

                i++;
                if (i == N)
                    break;
                v = i * stepV;

                for (int j = (int)(uMultiplier * N - 1); j >= 0; j--)
                {
                    float u = j * stepU;
                    if (trimmingFunc(u, v, trimming))
                        continue;
                    var pos = shiftedSurface.Evaluate(u, v);
                    if (uSafe)
                    {
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                        uSafe = false;
                    }
                    pos.Z -= r;
                    pos.Z += zOffset;
                    path.Add(pos);
                    uFound = true;
                }

                if (!uFound && path.Count > 0)
                {
                    uSafe = true;
                    path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                }
                uFound = false;
            }
            path.Insert(0, new Vector4(path.First().X, path.First().Y, safeHeight / 10));
            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
            return path;
        }

        private List<Vector4> CreateTailFinish(ParametricModel model, Dictionary<string, Intersector> intersections, float r)
        {
            List<Vector4> path = new List<Vector4>();

            var floorIntersection = new List<(float u, float v)>(intersections["Tail_Floor_Down"].Surface1Parameters);

            var surface = model["Tail"];
            var trimming1 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.5f, 0.9f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            floorIntersection = intersections["Tail_Torso"].Surface1Parameters;
            var trimming2 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.9f, 0.9f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            var trimming = MergeTrimmingTables(trimming1, trimming2);

            bool uFound = false;
            bool uSafe = false;
            var shiftedSurface = new ShiftedSurface(surface, r);
            int N = ElephantPathHelper.GetPart(surface.Name).Samples;
            float stepU = (int)surface.MaxU * 1.0f / (uMultiplier * N - 1);
            float stepV = (int)surface.MaxV * 1.0f / (N - 1);
            bool trimmingFunc(float u, float v, bool[,] trim) => trim[(int)Math.Min(u * trimmingPrecision / surface.MaxU, trimmingPrecision - 1), (int)Math.Min(v * trimmingPrecision / surface.MaxV, trimmingPrecision - 1)];
            for (int i = 0; i < N; i++)
            {
                uFound = false;
                float v = i * stepV;
                for (int j = 0; j < (int)(uMultiplier * N); j++)
                {
                    float u = j * stepU;
                    if (trimmingFunc(u, v, trimming))
                        continue;
                    var pos = shiftedSurface.Evaluate(u, v);
                    pos.Z -= r;
                    pos.Z += zOffset;
                    if (path.Count > 0 && Vector4.Distance(pos, path.Last()) > 0.2)
                    {
                        if (path.Count > 0)
                            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                    }
                    if (uSafe)
                    {
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                        uSafe = false;
                    }
                    path.Add(pos);
                    uFound = true;
                }

                if (!uFound && path.Count > 0)
                {
                    uSafe = true;
                    path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                }
                uFound = false;

                i++;
                if (i == N)
                    break;
                v = i * stepV;

                for (int j = (int)(uMultiplier * N - 1); j >= 0; j--)
                {
                    float u = j * stepU;
                    if (trimmingFunc(u, v, trimming))
                        continue;
                    var pos = shiftedSurface.Evaluate(u, v);
                    pos.Z -= r;
                    pos.Z += zOffset;
                    if (path.Count > 0 && Vector4.Distance(pos, path.Last()) > 0.2)
                    {
                        if (path.Count > 0)
                            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                    }
                    if (uSafe)
                    {
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                        uSafe = false;
                    }
                    path.Add(pos);
                    uFound = true;
                }

                if (!uFound && path.Count > 0)
                {
                    uSafe = true;
                    path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                }
                uFound = false;
            }
            path.Insert(0, new Vector4(path.First().X, path.First().Y, safeHeight / 10));
            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
            return path;
        }

        private List<Vector4> CreateTrumpetFinish(ParametricModel model, Dictionary<string, Intersector> intersections, float r)
        {
            List<Vector4> path = new List<Vector4>();

            var floorIntersection = new List<(float u, float v)>(intersections["Trumpet_Floor_Left"].Surface1Parameters);
            floorIntersection.AddRange(intersections["Trumpet_Floor_Right"].Surface1Parameters);

            var surface = model["Trumpet"];
            var trimming1 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.5f, 0.9f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            floorIntersection = intersections["FrontLegs_Trumpet"].Surface2Parameters;
            var trimming2 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.9f, 0.9f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            floorIntersection = intersections["Trumpet_Head"].Surface1Parameters;
            var trimming3 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.0f, 0.1f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            var trimming = MergeTrimmingTables(trimming1, trimming2);
            trimming = MergeTrimmingTables(trimming, trimming3);

            var trumpetFloor1s = JsonConvert.SerializeObject(trimming1);
            var trumpetFrontLegs2s = JsonConvert.SerializeObject(trimming2);
            var trumpetHead2s = JsonConvert.SerializeObject(trimming3);
            File.WriteAllText(trimmingsPath + "trumpetFloor1s.txt", trumpetFloor1s);
            File.WriteAllText(trimmingsPath + "trumpetFrontLegs2s.txt", trumpetFrontLegs2s);
            File.WriteAllText(trimmingsPath + "trumpetHead2s.txt", trumpetHead2s);
            var trumpet = JsonConvert.SerializeObject(trimming);
            File.WriteAllText(trimmingsPath + "trumpet.txt", trumpet);

            bool uFound = false;
            bool uSafe = false;
            var shiftedSurface = new ShiftedSurface(surface, r);
            int N = ElephantPathHelper.GetPart(surface.Name).Samples;
            float stepU = (int)surface.MaxU * 1.0f / (uMultiplier * N - 1);
            float stepV = (int)surface.MaxV * 1.0f / (N - 1);
            bool trimmingFunc(float u, float v, bool[,] trim) => trim[(int)Math.Min(u * trimmingPrecision / surface.MaxU, trimmingPrecision - 1), (int)Math.Min(v * trimmingPrecision / surface.MaxV, trimmingPrecision - 1)];
            for (int i = 0; i < N; i++)
            {
                uFound = false;
                float v = i * stepV;
                for (int j = 0; j < (int)(uMultiplier * N); j++)
                {
                    float u = j * stepU;
                    if (trimmingFunc(u, v, trimming))
                        continue;
                    var pos = shiftedSurface.Evaluate(u, v);
                    pos.Z -= r;
                    pos.Z += zOffset;
                    if (path.Count > 0 && Vector4.Distance(pos, path.Last()) > 1)
                    {
                        if (path.Count > 0)
                            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                    }
                    else if (uSafe)
                    {
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                        uSafe = false;
                    }
                    path.Add(pos);
                    uFound = true;
                }

                if (!uFound && path.Count > 0)
                {
                    uSafe = true;
                    path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                }
                uFound = false;

                i++;
                if (i == N)
                    break;
                v = i * stepV;

                for (int j = (int)(uMultiplier * N - 1); j >= 0; j--)
                {
                    float u = j * stepU;
                    if (trimmingFunc(u, v, trimming))
                        continue;
                    var pos = shiftedSurface.Evaluate(u, v);
                    pos.Z -= r;
                    pos.Z += zOffset;
                    if (path.Count > 0 && Vector4.Distance(pos, path.Last()) > 1)
                    {
                        if (path.Count > 0)
                            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                    }
                    if (uSafe)
                    {
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                        uSafe = false;
                    }
                    path.Add(pos);
                    uFound = true;
                }

                if (!uFound && path.Count > 0)
                {
                    uSafe = true;
                    path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                }
                uFound = false;
            }
            path.Insert(0, new Vector4(path.First().X, path.First().Y, safeHeight / 10));
            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
            return path;
        }

        private List<Vector4> CreateTorsoFinish(ParametricModel model, Dictionary<string, Intersector> intersections, float r)
        {
            List<Vector4> path = new List<Vector4>();
            var floorIntersection = new List<(float u, float v)>(intersections["Torso_Floor_Down"].Surface1Parameters);
            floorIntersection.AddRange(intersections["Torso_Floor_Up"].Surface1Parameters);

            var surface = model["Torso"];
            var trimming1 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.3f, 0.5f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            floorIntersection = intersections["BackLegs_Torso"].Surface2Parameters;
            var trimming2 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.3f, 0.7f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            floorIntersection = intersections["FrontLegs_Torso"].Surface2Parameters;
            var trimming3 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.7f, 0.7f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            floorIntersection = intersections["Tail_Torso"].Surface2Parameters;
            var trimming4 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.0f, 0.5f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            floorIntersection = intersections["Head_Torso"].Surface2Parameters;
            var trimming5 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => ((p.u) / surface.MaxU, p.v / surface.MaxV)).ToList(), 1.0f, 0.5f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            trimming5 = DilatateTrimmingTable(trimming5);
            trimming5 = DilatateTrimmingTable(trimming5);
            trimming5 = DilatateTrimmingTable(trimming5);
            floorIntersection = intersections["Ear_Torso"].Surface2Parameters;
            var trimming6 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.78f, 0.0f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            trimming6 = DilatateTrimmingTableVPositive(trimming6);
            var trimming = MergeTrimmingTables(trimming1, trimming2);
            trimming = MergeTrimmingTables(trimming, trimming3);
            trimming = MergeTrimmingTables(trimming, trimming4);
            trimming = MergeTrimmingTables(trimming, trimming5);
            trimming = MergeTrimmingTables(trimming, trimming6);

            var torsoFloor1s = JsonConvert.SerializeObject(trimming1);
            File.WriteAllText(trimmingsPath + nameof(torsoFloor1s) + ".txt", torsoFloor1s);
            var torsoBackLegs2s = JsonConvert.SerializeObject(trimming2);
            File.WriteAllText(trimmingsPath + "torsoBackLegs2s.txt", torsoBackLegs2s);
            var torsoFrontLegs = JsonConvert.SerializeObject(trimming3);
            File.WriteAllText(trimmingsPath + "torsoFrontLegs.txt", torsoFrontLegs);
            var torsoTail = JsonConvert.SerializeObject(trimming4);
            File.WriteAllText(trimmingsPath + nameof(torsoTail) + ".txt", torsoTail);
            var torsoHead = JsonConvert.SerializeObject(trimming5);
            File.WriteAllText(trimmingsPath + nameof(torsoHead) + ".txt", torsoHead);
            var torsoEar = JsonConvert.SerializeObject(trimming6);
            File.WriteAllText(trimmingsPath + nameof(torsoEar) + ".txt", torsoEar);
            var torso = JsonConvert.SerializeObject(trimming);
            File.WriteAllText(trimmingsPath + nameof(torso) + ".txt", torso);

            bool uFound = false;
            bool uSafe = false;
            bool vFound = false;
            bool vSafe = false;
            var shiftedSurface = new ShiftedSurface(surface, r);
            int N = ElephantPathHelper.GetPart(surface.Name).Samples;
            float stepU = (int)surface.MaxU * 1.0f / (uMultiplier * N - 1);
            float stepV = (int)surface.MaxV * 1.0f / (N - 1);
            bool trimmingFunc(float u, float v, bool[,] trim) => trim[(int)Math.Min(u * (trimmingPrecision - 1) / surface.MaxU, trimmingPrecision - 1), (int)Math.Min(v * (trimmingPrecision - 1) / surface.MaxV, trimmingPrecision - 1)];
            for (int i = 0; i < N; i++)
            {
                uFound = false;
                float v = i * stepV;
                for (int j = 0; j < (int)(uMultiplier * N); j++)
                {
                    float u = j * stepU;
                    if (trimmingFunc(u, v, trimming))
                    {
                        if (vFound)
                            vSafe = true;
                        continue;
                    }
                    var pos = shiftedSurface.Evaluate(u, v);
                    pos.Z -= r;
                    pos.Z += zOffset;
                    if (path.Count > 0 && Vector4.Distance(pos, path.Last()) > 0.3)
                    {
                        if (path.Count > 0)
                            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                    }
                    if ((vSafe && vFound) || (path.Count > 0 && Vector4.Distance(pos, path.Last()) > 1))
                    {
                        vSafe = false;
                        if (path.Count > 0)
                            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                    }
                    else if (uSafe)
                    {
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                        uSafe = false;
                    }
                    path.Add(pos);
                    uFound = true;
                    vFound = true;
                }
                vSafe = false;
                vFound = false;

                if (!uFound && path.Count > 0)
                {
                    uSafe = true;
                    path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                }
                uFound = false;

                i++;
                if (i == N)
                    break;
                v = i * stepV;

                for (int j = (int)(uMultiplier * N - 1); j >= 0; j--)
                {
                    float u = j * stepU;
                    if (trimmingFunc(u, v, trimming))
                    {
                        if (vFound)
                            vSafe = true;
                        continue;
                    }
                    var pos = shiftedSurface.Evaluate(u, v);
                    pos.Z -= r;
                    pos.Z += zOffset;
                    if (path.Count > 0 && Vector4.Distance(pos, path.Last()) > 0.3)
                    {
                        if (path.Count > 0)
                            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                    }
                    if ((vSafe && vFound) || (path.Count > 0 && Vector4.Distance(pos, path.Last()) > 1))
                    {
                        vSafe = false;
                        if (path.Count > 0)
                            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                    }
                    else if (uSafe)
                    {
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                        uSafe = false;
                    }
                    path.Add(pos);
                    uFound = true;
                    vFound = true;
                }
                vSafe = false;
                vFound = false;

                if (!uFound && path.Count > 0)
                {
                    uSafe = true;
                    path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                }
                uFound = false;
            }
            path.Insert(0, new Vector4(path.First().X, path.First().Y, safeHeight / 10));
            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));

            return path;
        }

        private List<Vector4> CreateEarFinish(ParametricModel model, Dictionary<string, Intersector> intersections, float r)
        {
            List<Vector4> path = new List<Vector4>();

            var floorIntersection = new List<(float u, float v)>(intersections["Ear_Torso"].Surface1Parameters);

            var surface = model["Ear2"];
            var trimming = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.9f, 0.9f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);

            var earTorso1s = JsonConvert.SerializeObject(trimming);
            File.WriteAllText(trimmingsPath + "earTorso1s.txt", earTorso1s);

            bool uFound = false;
            bool uSafe = false;
            var shiftedSurface = new ShiftedSurface(surface, r);
            int N = ElephantPathHelper.GetPart(surface.Name).Samples;
            float stepU = (int)surface.MaxU * 1.0f / (uMultiplier * N - 1);
            float stepV = (int)surface.MaxV * 1.0f / (N - 1);
            bool trimmingFunc(float u, float v, bool[,] trim) => trim[(int)Math.Min(u * trimmingPrecision / surface.MaxU, trimmingPrecision - 1), (int)Math.Min(v * trimmingPrecision / surface.MaxV, trimmingPrecision - 1)];
            for (int i = 0; i < N; i++)
            {
                uFound = false;
                float v = i * stepV;
                for (int j = 0; j < (int)(uMultiplier * N); j++)
                {
                    float u = j * stepU;
                    if (trimmingFunc(u, v, trimming))
                        continue;
                    var pos = shiftedSurface.Evaluate(u, v);
                    pos.Z -= r;
                    pos.Z += zOffset;
                    if (path.Count > 0 && Vector4.Distance(pos, path.Last()) > 0.3)
                    {
                        if (path.Count > 0)
                            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                    }
                    if (uSafe)
                    {
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                        uSafe = false;
                    }
                    path.Add(pos);
                    uFound = true;
                }

                if (!uFound && path.Count > 0)
                {
                    uSafe = true;
                    path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                }
                uFound = false;

                i++;
                if (i == N)
                    break;
                v = i * stepV;

                for (int j = (int)(uMultiplier * N - 1); j >= 0; j--)
                {
                    float u = j * stepU;
                    if (trimmingFunc(u, v, trimming))
                        continue;
                    var pos = shiftedSurface.Evaluate(u, v);
                    pos.Z -= r;
                    pos.Z += zOffset;
                    if (path.Count > 0 && Vector4.Distance(pos, path.Last()) > 0.3)
                    {
                        if (path.Count > 0)
                            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                    }
                    if (uSafe)
                    {
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                        uSafe = false;
                    }
                    path.Add(pos);
                    uFound = true;
                }

                if (!uFound && path.Count > 0)
                {
                    uSafe = true;
                    path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                }
                uFound = false;
            }
            path.Insert(0, new Vector4(path.First().X, path.First().Y, safeHeight / 10));
            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
            return path;
        }

        private List<Vector4> CreateHeadFinish(ParametricModel model, Dictionary<string, Intersector> intersections, float r)
        {
            List<Vector4> path = new List<Vector4>();

            var floorIntersection = new List<(float u, float v)>(intersections["Head_Floor_Up"].Surface1Parameters);
            floorIntersection.AddRange(intersections["Head_Floor_Down"].Surface1Parameters);

            var surface = model["Head"];
            var trimming1 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.9f, 0.5f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            floorIntersection = intersections["Head_Torso"].Surface1Parameters;
            var trimming2 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.1f, 0.9f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            floorIntersection = intersections["Trumpet_Head"].Surface2Parameters;
            var trimming3 = ImprovedFloodFill.DoFloodFill(floorIntersection.Select(p => (p.u / surface.MaxU, p.v / surface.MaxV)).ToList(), 0.9f, 0.9f, surface.IsClampedU, surface.IsClampedV, trimmingPrecision);
            var trimming = MergeTrimmingTables(trimming1, trimming2);
            trimming = MergeTrimmingTables(trimming, trimming3);

            var headFloor1s = JsonConvert.SerializeObject(trimming1);
            File.WriteAllText(trimmingsPath + "headFloor1s.txt", headFloor1s);
            var headTorso1s = JsonConvert.SerializeObject(trimming2);
            File.WriteAllText(trimmingsPath + "headTorso1s.txt", headTorso1s);
            var headTrumpet1s = JsonConvert.SerializeObject(trimming2);
            File.WriteAllText(trimmingsPath + "headTrumpet1s.txt", headTrumpet1s);

            bool uFound = false;
            bool uSafe = false;
            var shiftedSurface = new ShiftedSurface(surface, r);
            int N = ElephantPathHelper.GetPart(surface.Name).Samples;
            float stepU = (int)surface.MaxU * 1.0f / (uMultiplier * N - 1);
            float stepV = (int)surface.MaxV * 1.0f / (N - 1);
            bool trimmingFunc(float u, float v, bool[,] trim) => trim[(int)Math.Min(u * trimmingPrecision / surface.MaxU, trimmingPrecision - 1), (int)Math.Min(v * trimmingPrecision / surface.MaxV, trimmingPrecision - 1)];
            for (int i = 0; i < N; i++)
            {
                uFound = false;
                float v = i * stepV;
                for (int j = 0; j < (int)(uMultiplier * N); j++)
                {
                    float u = j * stepU;
                    if (trimmingFunc(u, v, trimming))
                        continue;
                    var pos = shiftedSurface.Evaluate(u, v);
                    pos.Z -= r;
                    pos.Z += zOffset;
                    if (path.Count > 0 && Vector4.Distance(pos, path.Last()) > 0.3)
                    {
                        if (path.Count > 0)
                            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                    }
                    if (uSafe)
                    {
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                        uSafe = false;
                    }
                    path.Add(pos);
                    uFound = true;
                }

                if (!uFound && path.Count > 0)
                {
                    uSafe = true;
                    path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                }
                uFound = false;

                i++;
                if (i == N)
                    break;
                v = i * stepV;

                for (int j = (int)(uMultiplier * N - 1); j >= 0; j--)
                {
                    float u = j * stepU;
                    if (trimmingFunc(u, v, trimming))
                        continue;
                    var pos = shiftedSurface.Evaluate(u, v);
                    pos.Z -= r;
                    pos.Z += zOffset;
                    if (path.Count > 0 && Vector4.Distance(pos, path.Last()) > 0.3)
                    {
                        if (path.Count > 0)
                            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                    }
                    if (uSafe)
                    {
                        path.Add(new Vector4(pos.X, pos.Y, safeHeight / 10));
                        uSafe = false;
                    }
                    path.Add(pos);
                    uFound = true;
                }

                if (!uFound && path.Count > 0)
                {
                    uSafe = true;
                    path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
                }
                uFound = false;
            }
            path.Insert(0, new Vector4(path.First().X, path.First().Y, safeHeight / 10));
            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));
            return path;
        }

        private bool[,] MergeTrimmingTables(bool[,] trimming1, bool[,] trimming2)
        {
            bool[,] trimming = new bool[trimming1.GetLength(0), trimming1.GetLength(1)];
            for (int i = 0; i < trimming.GetLength(0); i++)
            {
                for (int j = 0; j < trimming.GetLength(1); j++)
                {
                    trimming[i, j] = trimming1[i, j] || trimming2[i, j];
                }
            }
            return trimming;
        }

        private bool[,] DilatateTrimmingTable(bool[,] trimming)
        {
            bool[,] result = new bool[trimming.GetLength(0), trimming.GetLength(1)];
            for (int i = 0; i < result.GetLength(0); i++)
            {
                for (int j = 0; j < result.GetLength(1); j++)
                {
                    if (trimming[i, j])
                        result[i, j] = true;
                    else if(i > 0 && trimming[i - 1, j])
                        result[i, j] = true;
                    else if (i < result.GetLength(0) - 1 && trimming[i + 1, j])
                        result[i, j] = true;
                    else
                        result[i, j] = false;
                }
            }
            return result;
        }

        private bool[,] DilatateTrimmingTableVPositive(bool[,] trimming)
        {
            bool[,] result = new bool[trimming.GetLength(0), trimming.GetLength(1)];
            for (int i = 0; i < result.GetLength(0); i++)
            {
                for (int j = 0; j < result.GetLength(1); j++)
                {
                    if (trimming[i, j])
                        result[i, j] = true;
                    else if (i < result.GetLength(0) - 1 && trimming[i + 1, j])
                        result[i, j] = true;
                    else
                        result[i, j] = false;
                }
            }
            return result;
        }

        private List<Vector4> CreateFinishIntersectionPaths(ParametricModel model, Dictionary<string, Intersector> intersections)
        {
            float r = 0.4f;
            //float offset = 0.001f;
            float offset = 0.000f;
            List<Vector4> path = new List<Vector4>();
            List<Vector4> points;

            points = new List<Vector4>(intersections["BackLegs_Torso"].Points);
            points = TransformPoints(points, r, offset);
            path.Add(new Vector4(points[0].X, points[0].Y, safeHeight / 10));
            path.AddRange(CutOffRoundPath(points));
            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));

            points = new List<Vector4>(intersections["FrontLegs_Torso"].Points);
            points = TransformPoints(points, r, offset);
            List<Vector4> intersectedPath = CutOffRoundPath(points);
            path.Add(new Vector4(intersectedPath[0].X, intersectedPath[0].Y, safeHeight / 10));
            path.AddRange(intersectedPath);
            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));

            points = new List<Vector4>(intersections["Tail_Torso"].Points);
            points = TransformPoints(points, r, offset);
            intersectedPath = CutOffRoundPath(points);
            path.Add(new Vector4(intersectedPath[0].X, intersectedPath[0].Y, safeHeight / 10));
            path.AddRange(intersectedPath);
            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));

            points = new List<Vector4>(intersections["Head_Torso"].Points);
            points = TransformPoints(points, r, offset);
            foreach (var p in points)
                p.Z -= 0.001f;
            intersectedPath = CutOffRoundPath(points);
            path.Add(new Vector4(intersectedPath[0].X, intersectedPath[0].Y, safeHeight / 10));
            path.AddRange(intersectedPath);
            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));

            points = new List<Vector4>(intersections["FrontLegs_Trumpet"].Points);
            points = TransformPoints(points, r, offset);
            intersectedPath = CutOffRoundPath(points);
            path.Add(new Vector4(intersectedPath[0].X, intersectedPath[0].Y, safeHeight / 10));
            path.AddRange(intersectedPath);
            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));

            points = new List<Vector4>(intersections["Ear_Torso"].Points);
            points = TransformPoints(points, r, offset);
            intersectedPath = UnloopPath(points);
            path.Add(new Vector4(intersectedPath[0].X, intersectedPath[0].Y, safeHeight / 10));
            path.AddRange(intersectedPath);
            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));

            points = new List<Vector4>(intersections["Trumpet_Head"].Points);
            points = TransformPoints(points, r, offset);
            intersectedPath = CutOffRoundPath(points);
            path.Add(new Vector4(intersectedPath[0].X, intersectedPath[0].Y, safeHeight / 10));
            path.AddRange(intersectedPath);
            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));

            return path;
        }

        private List<Vector4> TransformPoints(List<Vector4> points, float r, float offset)
        {
            foreach (var p in points)
            {
                p.Z -= r + offset;
                p.Z += zOffset;
            }
            return points;
        }

        private Dictionary<string, Intersector> CalculateIntersections(ParametricModel model, float r)
        {
            Dictionary<string, Intersector> intersections = new Dictionary<string, Intersector>();

            var shiftedBackLegs = new ShiftedSurface(model["BackLegs"], -r);
            var shiftedFrontLegs = new ShiftedSurface(model["FrontLegs"], r);
            var shiftedTail = new ShiftedSurface(model["Tail"], r);
            var shiftedHead = new ShiftedSurface(model["Head"], r);
            var shiftedEar = new ShiftedSurface(model["Ear2"], r);
            var shiftedTorso = new ShiftedSurface(model["Torso"], r);
            var shiftedTrumpet = new ShiftedSurface(model["Trumpet"], r);
            var bezierPatch = new RectangularBezierPatch(new Vector4(0, 0, 0, 0), 2, 2, 20, 20);
            bezierPatch.Position.X = 0;
            var floor = new ShiftedSurface(bezierPatch, r);

            var intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedBackLegs, shiftedTorso, 0.01f, new Vector4(0, 5, -4));
            intersections["BackLegs_Torso"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedFrontLegs, shiftedTorso, 0.01f, new Vector4(1, 6.5f, -5.5f));
            intersections["FrontLegs_Torso"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedTail, shiftedTorso, 0.01f, new Vector4(-4.5f, 6f, -5f, 0));
            intersections["Tail_Torso"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedHead, shiftedTorso, 0.005f, new Vector4(4, 5, 0, 0), false, 0.003f);
            intersections["Head_Torso"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedFrontLegs, shiftedTrumpet, 0.01f, new Vector4(3.5f, 5.5f, 0, 0));
            intersections["FrontLegs_Trumpet"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedEar, shiftedTorso, 0.005f, new Vector4(4.5f, 10.5f, -5, 0));
            intersections["Ear_Torso"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedFrontLegs, floor, 0.01f, new Vector4(1, 6.5f, -5.5f));
            intersections["FrontLegs_Floor_Left"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedFrontLegs, floor, 0.01f, new Vector4(5, 6.5f, -5.5f));
            intersections["FrontLegs_Floor_Right"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedBackLegs, floor, 0.01f, new Vector4(-4, 6.5f, -5.5f));
            intersections["BackLegs_Floor_Left"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedBackLegs, floor, 0.01f, new Vector4(1, 6.5f, -5.5f));
            intersections["BackLegs_Floor_Right"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedTail, floor, 0.01f, new Vector4(-6, 10.5f, -5.5f));
            intersections["Tail_Floor_Up"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedTail, floor, 0.01f, new Vector4(-6, 4.5f, -5.5f));
            intersections["Tail_Floor_Down"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedTrumpet, floor, 0.01f, new Vector4(5, 6, -5.5f));
            intersections["Trumpet_Floor_Left"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedTrumpet, floor, 0.01f, new Vector4(9, 6.5f, -5.5f));
            intersections["Trumpet_Floor_Right"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedTorso, floor, 0.01f, new Vector4(0, 13, 0.5f));
            intersections["Torso_Floor_Up"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedTorso, floor, 0.01f, new Vector4(3.8f, 6.0f, 0.5f), false, 0.01f);
            intersections["Torso_Floor_Down"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedHead, floor, 0.01f, new Vector4(3, 13, 0.5f));
            intersections["Head_Floor_Up"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedHead, floor, 0.01f, new Vector4(3, 4.5f, 0.5f));
            intersections["Head_Floor_Down"] = intersector;

            intersector = new Intersector(false, 0);
            intersector.Intersect(shiftedTrumpet, shiftedHead, 0.01f, new Vector4(5.21f, 6.34f, 0.0f), false, 0.05f);
            intersections["Trumpet_Head"] = intersector;

            return intersections;
        }

        private List<Vector4> UnloopPath(List<Vector4> path)
        {
            var first = path[0];
            int i;
            for (i = 10; i < path.Count; i++)
            {
                if (Vector4.Distance(path[i], first) < 0.03)
                    break;
            }
            path = path.Take(i).ToList();
            path.Add(first);
            return path;
        }

        private List<Vector4> CutOffRoundPath(List<Vector4> path)
        {
            if (path.Count < 1)
                return new List<Vector4>();
            //float height = baseHeight + 0.1f;
            float height = zOffset;
            List<Vector4> correctedPath = new List<Vector4>();
            List<Vector4> correctedPath2 = new List<Vector4>();
            bool found = false;
            bool foundSecond = false;
            if (path[0].Z < height)
                foreach (var p in path)
                {
                    if (p.Z > height)
                    {
                        correctedPath.Add(p);
                        found = true;
                    }
                    else if (found)
                        break;
                }
            else
            {
                foreach (var p in path)
                {
                    if (p.Z > height)
                    {
                        if (found)
                        {
                            correctedPath2.Add(p);
                            foundSecond = true;
                        }
                        else
                            correctedPath.Add(p);
                    }
                    else
                    {
                        found = true;
                        if (foundSecond)
                        {
                            break;
                        }
                    }
                }
                correctedPath.InsertRange(0, correctedPath2);
            }
            return correctedPath;
        }

        private List<Vector4> CreateInsideFinishPaths(ParametricModel model, float r)
        {
            EnvelopeConnector connector = new EnvelopeConnector();
            RectangularBezierPatch rectangularBezier = new RectangularBezierPatch(new Vector4(0, 10, 0, 0), 2, 2, 20, 20);
            rectangularBezier.Position.X = 0;

            Intersector torsoIntersector = new Intersector(true, r);
            torsoIntersector.Intersect(model["Torso"], rectangularBezier, 0.02f, ElephantPathHelper.Torso.IntersectionPoints.Front);
            torsoIntersector.SmoothIntersection(0.1f);
            connector.ConnectIntersetcion(torsoIntersector.Points, 1, true);

            Intersector headIntersector = new Intersector(true, r);
            headIntersector.Intersect(model["Head"], rectangularBezier, 0.02f, ElephantPathHelper.Head.IntersectionPoints.Bottom);
            connector.ConnectIntersetcion(headIntersector.Points, 1, true);

            Intersector trumpetIntersetor = new Intersector(true, r);
            trumpetIntersetor.Intersect(model["Trumpet"], rectangularBezier, 0.02f, ElephantPathHelper.Trumpet.IntersectionPoints.Back);
            connector.ConnectIntersetcion(trumpetIntersetor.Points, 1, true);

            Intersector frontLegsIntersetor = new Intersector(true, r);
            frontLegsIntersetor.Intersect(model["FrontLegs"], rectangularBezier, 0.02f, ElephantPathHelper.FrontLegs.IntersectionPoints.Front);
            connector.ConnectIntersetcion(frontLegsIntersetor.Points, 1, true);

            connector.ConnectEnvelope(false);
            List<Vector4> paths = new List<Vector4>();

            paths.AddRange(CreateLineInsideEnvelope(connector.Positions));
            paths.Add(new Vector4(paths.Last().X, paths.Last().Y, safeHeight / 10));
            var point = connector.Positions[0];
            paths.Add(new Vector4(point.X, point.Y, safeHeight / 10));

            r -= 0.01f;
            connector = new EnvelopeConnector();
            rectangularBezier = new RectangularBezierPatch(new Vector4(0, 10, 0, 0), 2, 2, 20, 20);
            rectangularBezier.Position.X = 0;

            torsoIntersector = new Intersector(true, r);
            torsoIntersector.Intersect(model["Torso"], rectangularBezier, 0.01f, ElephantPathHelper.Torso.IntersectionPoints.Front);
            torsoIntersector.SmoothIntersection(0.1f);
            connector.ConnectIntersetcion(torsoIntersector.Points, 1, true);

            headIntersector = new Intersector(true, r);
            headIntersector.Intersect(model["Head"], rectangularBezier, 0.01f, ElephantPathHelper.Head.IntersectionPoints.Bottom);
            connector.ConnectIntersetcion(headIntersector.Points, 1, true);

            trumpetIntersetor = new Intersector(true, r);
            trumpetIntersetor.Intersect(model["Trumpet"], rectangularBezier, 0.01f, ElephantPathHelper.Trumpet.IntersectionPoints.Back);
            connector.ConnectIntersetcion(trumpetIntersetor.Points, 1, true);

            frontLegsIntersetor = new Intersector(true, r);
            frontLegsIntersetor.Intersect(model["FrontLegs"], rectangularBezier, 0.01f, ElephantPathHelper.FrontLegs.IntersectionPoints.Front);
            connector.ConnectIntersetcion(frontLegsIntersetor.Points, 1, true);

            connector.ConnectEnvelope(false);
            paths.AddRange(connector.Positions);

            foreach (var p in paths)
            {
                if (p.Z != safeHeight / 10)
                    p.Z = baseHeight;
            }
            paths.Insert(0, new Vector4(paths[0].X, paths[0].Y, safeHeight / 10));
            paths.Add(new Vector4(paths.Last().X, paths.Last().Y, safeHeight / 10));
            return paths;
        }

        private List<Vector4> CreateLineInsideEnvelope(List<Vector4> envelope)
        {
            int i = 1;
            for (; i < envelope.Count; i++)
            {
                if (envelope[i].Y < envelope[i - 1].Y)
                    break;
            }
            i--;
            float yIncrement = 0.1f;
            List<Vector4> path = new List<Vector4>();
            Vector4 direction = new Vector4(10, 0, 0, 0);
            EnvelopeConnector connector = new EnvelopeConnector();
            connector.ConnectIntersetcion(new List<Vector4> { envelope[i] - new Vector4(0, 1, 0, 0), envelope[i] + new Vector4(0, 1, 0, 0) });
            connector.AddDirectedConnectionY(envelope, new Vector4(0, 1, 0, 0), envelope[i].Y - yIncrement);
            path.AddRange(connector.Positions);
            bool found;
            while (true)
            {
                connector.Clear();
                List<Vector4> line = new List<Vector4> { path.Last() + direction * 0.01f, path.Last() + direction };
                connector.ConnectIntersetcion(line);
                found = connector.AddDirectedConnectionY(envelope, new Vector4(0, -1, 0, 0), path.Last().Y - yIncrement);
                if (!found)
                    break;
                path.AddRange(connector.Positions);
                direction = -direction;
            }
            path.RemoveAt(path.Count - 1);
            path.RemoveAt(0);
            path.Add(new Vector4(path.Last().X, path.Last().Y, safeHeight / 10));

            i = 1;
            for (; i < envelope.Count; i++)
            {
                if (envelope[i].X > envelope[i - 1].X)
                    break;
            }
            i--;
            float xIncrement = 0.11f;
            direction = new Vector4(0, -10, 0, 0);
            connector = new EnvelopeConnector();
            connector.ConnectIntersetcion(new List<Vector4> { envelope[i] - new Vector4(1, 0, 0, 0), envelope[i] + new Vector4(1, 0, 0, 0) });
            connector.AddDirectedConnectionX(envelope, new Vector4(1, 0, 0, 0), envelope[i].X + xIncrement);
            var point = connector.Positions[1];
            path.Add(new Vector4(point.X, point.Y, safeHeight / 10));
            path.AddRange(connector.Positions.Skip(1));
            while (true)
            {
                connector.Clear();
                List<Vector4> line = new List<Vector4> { path.Last() + direction * 0.01f, path.Last() + direction };
                connector.ConnectIntersetcion(line);
                found = connector.AddDirectedConnectionX(envelope, new Vector4(1, 0, 0, 0), path.Last().X + xIncrement);
                if (!found)
                    break;
                path.AddRange(connector.Positions);
                direction = -direction;
            }
            path.RemoveAt(path.Count - 1);

            return path;
        }
    }
}
