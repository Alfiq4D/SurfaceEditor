using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Shapes;
using Engine.Interfaces;
using Engine.Utilities;

namespace Engine.Models
{
    public class IntersectionCurve : _3DObject
    {
        private IIntersectable s1;
        public IIntersectable S1
        {
            get { return s1; }
            set
            {
                if (s1 == value) return;
                else
                    s1 = value;
                NotifyPropertyChanged();
            }
        }
        private IIntersectable s2;
        public IIntersectable S2
        {
            get { return s2; }
            set
            {
                if (s2 == value) return;
                else
                    s2 = value;
                NotifyPropertyChanged();
            }
        }
        private bool[,] obj1TrimmingTable;
        private bool[,] obj2TrimmingTable;
        public List<Line> lines1;
        public List<Line> lines2;
        public List<(float u, float v)> points1;
        public List<(float u, float v)> points2;

        private double width, height;

        private float eps;
        public float Eps
        {
            get { return eps; }
            set
            {
                if (eps == value) return;
                else
                    eps = value;
                NotifyPropertyChanged();
            }
        }

        public override _3DObject Clone()
        {
            throw new NotImplementedException();
        }

        public override _3DObject CloneMirrored()
        {
            throw new NotImplementedException();
        }

        public IntersectionCurve(List<Vector4> points): base(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1))
        {
            Name = "Intersection" + counter++.ToString();
            Points = points;
            Edges = new List<Edge>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                Edges.Add(new Edge(i, i + 1));
            }
            Color = Colors.Red;
            points1 = new List<(float u, float v)>();
            points2 = new List<(float u, float v)>();
            CreateParametersLines(points1, points2);
        }

        public IntersectionCurve(List<Vector4> points, IIntersectable obj1, IIntersectable obj2, List<(float u, float v)> points1, List<(float u, float v)> points2, double parametrizationWindowWidth, double parametrizationWindowHeight) : base(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1))
        {
            Name = "Intersection" + counter++.ToString();
            Points = points;
            Edges = new List<Edge>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                Edges.Add(new Edge(i, i + 1));
            }
            Color = Colors.Red;
            S1 = obj1;
            S2 = obj2;
            this.points1 = points1;
            this.points2 = points2;
            width = parametrizationWindowWidth;
            height = parametrizationWindowHeight;
            CreateParametersLines(points1, points2);
        }

        public override void Render(Renderer renderer, Matrix4 projView, int width, int height)
        {
            renderer.RenderIntersectionCurve(this, projView, width, height, Color);
        }

        public override void RenderSterographic(Renderer renderer, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            renderer.RenderStereographicIntersectionCurve(this, projView, leftProjView, rightProjView, width, height);
        }

        public override string Save()
        {
            return "";
        }

        public void UpdateFirstTrimmingTable(double startX, double startY, int precision)
        {
            if (S1 is SurfacePatch surface)
            {
                if (obj1TrimmingTable != null)
                    surface.TrimmingTables.Remove(obj1TrimmingTable);
                obj1TrimmingTable = FloodFill.Fill(points1.Select(p => (p.u / S1.MaxU, p.v / S1.MaxV)).ToList(), (float)startX, (float)startY, S1.IsClampedU, S1.IsClampedV, precision);
                surface.TrimmingTables.Add(obj1TrimmingTable);
            }
        }

        public void UpdateSecondTrimmingTable(double startX, double startY, int precision)
        {
            if (S2 is SurfacePatch surface)
            {
                if (obj2TrimmingTable != null)
                    surface.TrimmingTables.Remove(obj2TrimmingTable);
                obj2TrimmingTable = FloodFill.Fill(points2.Select(p => (p.u / S2.MaxU, p.v / S2.MaxV)).ToList(), (float)startX, (float)startY, S2.IsClampedU, S2.IsClampedV, precision);
                surface.TrimmingTables.Add(obj2TrimmingTable);
            }
        }

        public void RemoveFirstTrimmingTable()
        {
            if (S1 is SurfacePatch surface)
            {
                if (obj1TrimmingTable != null)
                    surface.TrimmingTables.Remove(obj1TrimmingTable);
                obj1TrimmingTable = null;
            }
        }

        public void RemoveSecondTrimmingTable()
        {
            if (S2 is SurfacePatch surface)
            {
                if (obj2TrimmingTable != null)
                    surface.TrimmingTables.Remove(obj2TrimmingTable);
                obj2TrimmingTable = null;
            }
        }

        public void ReverseFirstTrimming()
        {
            if (obj1TrimmingTable != null)
            {
                FloodFill.ChangeTrimmingTable(obj1TrimmingTable);
            }
        }

        public void ReverseSecondTrimming()
        {
            if (obj2TrimmingTable != null)
            {
                FloodFill.ChangeTrimmingTable(obj2TrimmingTable);
            }
        }

        public InterpolationCurve ConvertToInterpolationCurve()
        {            
            InterpolationCurve curve = new InterpolationCurve(GetHalfOfPointsToInterpolation(), Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1));
            return curve;
        }

        private List<Point3D> GetPointsToInterpolation()
        {
            List<Point3D> points = new List<Point3D>();
            foreach (var v in Points)
            {
                Point3D p = new Point3D(new Vector4(v.X, v.Y, v.Z, v.W));
                points.Add(p);
            }
            return points;
        }

        private List<Point3D> GetHalfOfPointsToInterpolation()
        {
            List<Point3D> points = new List<Point3D>();
            for (int i = 0; i < Points.Count; i+=2)
            {
                Point3D p = new Point3D(new Vector4(Points[i].X, Points[i].Y, Points[i].Z, Points[i].W));
                points.Add(p);
            }
            return points;
        }

        private void CreateParametersLines(List<(float u, float v)> parameters1, List<(float u, float v)> parameters2)
        {
            //Point relativePoint = ParametersIntersection1.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));
            lines1 = new List<Line>();
            float canvasEps = 3.0f;
            for (int i = 0; i < parameters1.Count - 1; i++)
            {
                Line line = new Line
                {
                    Stroke = Brushes.LightSteelBlue,

                    X1 = parameters1[i].u / S1.MaxU * width,
                    Y1 = parameters1[i].v / S1.MaxV * height,
                    X2 = parameters1[i + 1].u / S1.MaxU * width,
                    Y2 = parameters1[i + 1].v / S1.MaxV * height,

                    StrokeThickness = 1
                };
                if (!((line.X1 <= 0 + canvasEps && line.X2 >= width - canvasEps)
                    || (line.Y1 <= 0 + canvasEps && line.Y2 >= height - canvasEps)
                    || (line.X1 >= width - canvasEps && line.X2 <= 0 + canvasEps)
                    || (line.Y1 >= height - canvasEps && line.Y2 <= 0 + canvasEps)))
                    lines1.Add(line);
            }
            //relativePoint = ParametersIntersection2.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));
            lines2 = new List<Line>();
            for (int i = 0; i < parameters2.Count - 1; i++)
            {
                Line line = new Line
                {
                    Stroke = Brushes.LightSteelBlue,

                    X1 = parameters2[i].u / S2.MaxU * width,
                    Y1 = parameters2[i].v / S2.MaxV * height,
                    X2 = parameters2[i + 1].u / S2.MaxU * width,
                    Y2 = parameters2[i + 1].v / S2.MaxV * height,

                    StrokeThickness = 1
                };
                if (!((line.X1 <= 0 + canvasEps && line.X2 >= width - canvasEps)
                   || (line.Y1 <= 0 + canvasEps && line.Y2 >= height - canvasEps)
                   || (line.X1 >= width - canvasEps && line.X2 <= 0 + canvasEps)
                   || (line.Y1 >= height - canvasEps && line.Y2 <= 0 + canvasEps)))
                    lines2.Add(line);
            }
            return;
        }
    }
}
