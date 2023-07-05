using System.Collections.Generic;
using Engine.Models;

namespace Engine.Utilities
{
    public class Loader
    {
        public int Errors { get; set; }
        public int Loaded { get; set; }
        public List<_3DObject> LoadedModels { get; set; }

        public Loader()
        {
            Errors = 0;
            Loaded = 0;
            LoadedModels = new List<_3DObject>();
        }

        public void Load(string[] lines)
        {
            int count;
            for (int i = 0; i < lines.Length; i++)
            {
                var words = lines[i].Split(' ');
                switch (words[0])
                {
                    case "point":
                        count = int.Parse(words[1]);
                        for (int j = 0; j < count; j++)
                        {
                            i++;
                            if (!LoadPoint(lines[i]))
                                Errors++;
                            Loaded++;
                        }
                        break;
                    case "curveC0":
                        count = int.Parse(words[1]);
                        for (int j = 0; j < count; j++)
                        {
                            i++;
                            if (!LoadBezierCurve(lines[i]))
                                Errors++;
                            Loaded++;
                        }
                        break;
                    case "curveC2":
                        count = int.Parse(words[1]);
                        for (int j = 0; j < count; j++)
                        {
                            i++;
                            if (!LoadBSplineCurve(lines[i]))
                                Errors++;
                            Loaded++;
                        }
                        break;
                    case "curveInt":
                        count = int.Parse(words[1]);
                        for (int j = 0; j < count; j++)
                        {
                            i++;
                            if (!LoadInterpolationCurve(lines[i]))
                                Errors++;
                            Loaded++;
                        }
                        break;
                    case "surfaceC0":
                        count = int.Parse(words[1]);
                        for (int j = 0; j < count; j++)
                        {
                            i++;
                            if (!LoadBezierPatch(lines[i]))
                                Errors++;
                            Loaded++;
                        }
                        break;
                    case "tubeC0":
                        count = int.Parse(words[1]);
                        for (int j = 0; j < count; j++)
                        {
                            i++;
                            if (!LoadBezierTubePatch(lines[i]))
                                Errors++;
                            Loaded++;
                        }
                        break;
                    case "surfaceC2":
                        count = int.Parse(words[1]);
                        for (int j = 0; j < count; j++)
                        {
                            i++;
                            if (!LoadBSplinePatch(lines[i]))
                                Errors++;
                            Loaded++;
                        }
                        break;
                    case "tubeC2":
                        count = int.Parse(words[1]);
                        for (int j = 0; j < count; j++)
                        {
                            i++;
                            if (!LoadBSplineTubePatch(lines[i]))
                                Errors++;
                            Loaded++;
                        }
                        break;
                    default:
                        break;
                }

            }

        }

        private bool LoadPoint(string line)
        {
            try
            {
                var values = line.Split(' ');
                var name = values[0];
                var pointData = values[1].Split(';');
                Vector4 position = new Vector4(float.Parse(pointData[0]), float.Parse(pointData[1]), float.Parse(pointData[2]));
                Point3D point3D = new Point3D(name, position);
                LoadedModels.Add(point3D);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool LoadBSplineCurve(string line)
        {
            try
            {
                var values = line.Split(' ');
                var name = values[0];
                BezierCurveC2 curve = new BezierCurveC2(name, Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1));
                LoadedModels.Add(curve);
                for (int k = 1; k < values.Length; k++)
                {
                    var pointData = values[k].Split(';');
                    Vector4 pointPosition = new Vector4(float.Parse(pointData[0]), float.Parse(pointData[1]), float.Parse(pointData[2]));
                    Point3D point3D;
                    if (pointData.Length > 3)
                        point3D = new Point3D(pointData[3], pointPosition);
                    else
                        point3D = new Point3D(pointPosition);
                    curve.AddPoint(point3D);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool LoadBezierCurve(string line)
        {
            try
            {
                var values = line.Split(' ');
                var name = values[0];
                BezierCurve curve = new BezierCurve(name, Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1));
                LoadedModels.Add(curve);
                for (int k = 1; k < values.Length; k++)
                {
                    var pointData = values[k].Split(';');
                    Vector4 pointPosition = new Vector4(float.Parse(pointData[0]), float.Parse(pointData[1]), float.Parse(pointData[2]));
                    Point3D point3D;
                    if (pointData.Length > 3)
                        point3D = new Point3D(pointData[3], pointPosition);
                    else
                        point3D = new Point3D(pointPosition);
                    curve.AddPoint(point3D); ;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool LoadInterpolationCurve(string line)
        {
            try
            {
                var values = line.Split(' ');
                var name = values[0];
                InterpolationCurve curve = new InterpolationCurve(name, Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1));
                LoadedModels.Add(curve);
                for (int k = 1; k < values.Length; k++)
                {
                    var pointData = values[k].Split(';');
                    Vector4 pointPosition = new Vector4(float.Parse(pointData[0]), float.Parse(pointData[1]), float.Parse(pointData[2]));
                    Point3D point3D;
                    if (pointData.Length > 3)
                        point3D = new Point3D(pointData[3], pointPosition);
                    else
                        point3D = new Point3D(pointPosition);
                    curve.AddPoint(point3D);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool LoadBezierPatch(string line)
        {
            try
            {
                var values = line.Split(' ');
                var name = values[0];
                int width = int.Parse(values[1]);
                int height = int.Parse(values[2]);
                ArtificialPoint3D[,] points = new ArtificialPoint3D[width * 3 + 1, height * 3 + 1];
                List<ArtificialPoint3D> pointsList = new List<ArtificialPoint3D>();
                for (int k = 3; k < values.Length; k++)
                {
                    var pointData = values[k].Split(';');
                    Vector4 pointPosition = new Vector4(float.Parse(pointData[0]), float.Parse(pointData[1]), float.Parse(pointData[2]));
                    ArtificialPoint3D point3D;
                    if (pointData.Length > 3)
                        point3D = new ArtificialPoint3D(pointData[3], pointPosition);
                    else
                        point3D = new ArtificialPoint3D(pointPosition);
                    pointsList.Insert(pointsList.Count, point3D);
                }
                if (pointsList.Count != (width * 3 + 1) * (height * 3 + 1)) return false;
                int c = 0;
                for (int m = 0; m < height * 3 + 1; m++)
                {
                    for (int n = 0; n < width * 3 + 1; n++)
                    {
                        points[n, m] = pointsList[c++];
                    }
                }
                RectangularBezierPatch patch = new RectangularBezierPatch(name, new Vector4(0, 0, 0, 1), points);
                LoadedModels.Add(patch);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool LoadBezierTubePatch(string line)
        {
            try
            {
                var values = line.Split(' ');
                var name = values[0];
                int width = int.Parse(values[1]);
                int height = int.Parse(values[2]);
                ArtificialPoint3D[,] points = new ArtificialPoint3D[height * 3 + 1, width * 3];
                List<ArtificialPoint3D> pointsList = new List<ArtificialPoint3D>();
                for (int k = 3; k < values.Length; k++)
                {
                    var pointData = values[k].Split(';');
                    Vector4 pointPosition = new Vector4(float.Parse(pointData[0]), float.Parse(pointData[1]), float.Parse(pointData[2]));
                    ArtificialPoint3D point3D;
                    if (pointData.Length > 3)
                        point3D = new ArtificialPoint3D(pointData[3], pointPosition);
                    else
                        point3D = new ArtificialPoint3D(pointPosition);
                    pointsList.Insert(pointsList.Count, point3D);
                }
                if (pointsList.Count != (width * 3) * (height * 3 + 1)) return false;
                int c = 0;
                for (int m = 0; m < height * 3 + 1; m++)
                {
                    for (int n = 0; n < width * 3; n++)
                    {
                        points[m, n] = pointsList[c++];
                    }
                }
                CylindricalBezierPatch patch = new CylindricalBezierPatch(name, new Vector4(0, 0, 0, 1), points);
                LoadedModels.Add(patch);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool LoadBSplinePatch(string line)
        {
            try
            {
                var values = line.Split(' ');
                var name = values[0];
                int width = int.Parse(values[1]);
                int height = int.Parse(values[2]);
                ArtificialPoint3D[,] points = new ArtificialPoint3D[width * 3 + 1, height * 3 + 1];
                List<ArtificialPoint3D> pointsList = new List<ArtificialPoint3D>();
                for (int k = 3; k < values.Length; k++)
                {
                    var pointData = values[k].Split(';');
                    Vector4 pointPosition = new Vector4(float.Parse(pointData[0]), float.Parse(pointData[1]), float.Parse(pointData[2]));
                    ArtificialPoint3D point3D;
                    if (pointData.Length > 3)
                        point3D = new ArtificialPoint3D(pointData[3], pointPosition);
                    else
                        point3D = new ArtificialPoint3D(pointPosition);
                    pointsList.Insert(pointsList.Count, point3D);
                }
                if (pointsList.Count != (width * 3 + 1) * (height * 3 + 1)) return false;
                int c = 0;
                for (int m = 0; m < height * 3 + 1; m++)
                {
                    for (int n = 0; n < width * 3 + 1; n++)
                    {
                        points[n, m] = pointsList[c++];
                    }
                }
                RectangularBezierPatchC2 patch = new RectangularBezierPatchC2(name, new Vector4(0, 0, 0, 1), points);
                LoadedModels.Add(patch);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool LoadBSplineTubePatch(string line)
        {
            try
            {
                var values = line.Split(' ');
                var name = values[0];
                int width = int.Parse(values[1]);
                int height = int.Parse(values[2]);
                ArtificialPoint3D[,] points = new ArtificialPoint3D[height * 3 + 1, width * 3];
                List<ArtificialPoint3D> pointsList = new List<ArtificialPoint3D>();
                for (int k = 3; k < values.Length; k++)
                {
                    var pointData = values[k].Split(';');
                    Vector4 pointPosition = new Vector4(float.Parse(pointData[0]), float.Parse(pointData[1]), float.Parse(pointData[2]));
                    ArtificialPoint3D point3D;
                    if (pointData.Length > 3)
                        point3D = new ArtificialPoint3D(pointData[3], pointPosition);
                    else
                        point3D = new ArtificialPoint3D(pointPosition);
                    pointsList.Insert(pointsList.Count, point3D);
                }
                if (pointsList.Count != (width * 3) * (height * 3 + 1)) return false;
                int c = 0;
                for (int m = 0; m < height * 3 + 1; m++)
                {
                    for (int n = 0; n < width * 3; n++)
                    {
                        points[m, n] = pointsList[c++];
                    }
                }
                CylindricalBezierPatchC2 patch = new CylindricalBezierPatchC2(name, new Vector4(0, 0, 0, 1), points);
                LoadedModels.Add(patch);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
