using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Models
{
    public class BezierCurve : Curve
    {

        public List<Segment> Segments = new List<Segment>();

        public override IEnumerable<_3DObject> DisplayPoints
        {
            get
            {
                return ControlPoints.Cast<_3DObject>().ToList();
            }
        }

        public BezierCurve(Vector4 initialPosition, Vector4 rotation, Vector4 scale) : base(initialPosition, rotation, scale)
        {
            Name = "Bezier" + counter++.ToString();
            ControlPoints.CollectionChanged += ControlPoints_CollectionChanged;
        }

        public BezierCurve(string name, Vector4 initialPosition, Vector4 rotation, Vector4 scale) : base(initialPosition, rotation, scale)
        {
            Name = name;
            ControlPoints.CollectionChanged += ControlPoints_CollectionChanged;
        }

        public override _3DObject Clone()
        {
            BezierCurve curve = new BezierCurve(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1));
            for (int i = 0; i < ControlPoints.Count; i++)
            {
                Point3D point = (Point3D)ControlPoints[i].Clone();
                curve.AddPoint(point);
            }
            return curve;
        }

        public override _3DObject CloneMirrored()
        {
            BezierCurve curve = new BezierCurve(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1));
            for (int i = 0; i < ControlPoints.Count; i++)
            {
                Point3D point = (Point3D)ControlPoints[i].CloneMirrored();
                curve.AddPoint(point);
            }
            return curve;
        }

        private void ControlPoints_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Edges = new List<Edge>();
            Points.Clear();
            foreach (Point3D p in ControlPoints)
            {
                Points.Add(p.Position);
            }
            for (int i = 1; i < Points.Count; i++)
            {
                Edges.Add(new Edge(i, i - 1));
            }
            CreateSegments();
            NotifyPropertyChanged();
        }

        public override void AddPoint(Point3D p)
        {
            ControlPoints.Add(p);
        }

        public override void DeletePoint(Point3D p)
        {
            ControlPoints.Remove(p);             
        }

        public override void Render(Renderer renderer, Matrix4 projView, int width, int height)
        {
            renderer.RenderBezier(this, projView, width, height, Color);
        }

        public override void RenderSterographic(Renderer renderer, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            renderer.RenderStereographicBezier(this, projView, leftProjView, rightProjView, width, height);
        }

        private void CreateSegments()
        {
            var p = ControlPoints.ToList();
            Segments = new List<Segment>();
            for (int i = 0; i < Points.Count; i+=3)
            {
                Segments.Add(new Segment(p.GetRange(i, Math.Min(4, Points.Count - i))));
            }
        }

        public override string Save()
        {
            string val = "curveC0 1\n" + Name;
            foreach(var point in ControlPoints)
            {
                val += " " + point.Position.ToString() + ";" + point.Name;
            }
            val += "\n";
            return val;
        }

        public class Segment
        {
            List<Point3D> points;

            public Segment(List<Point3D> points)
            {
                this.points = points;
            }

            public Vector4 GetBezierValue(float t)
            {
                switch (points.Count)
                { 
                    case 4:
					    return (1 - t) * (1 - t) * (1 - t) * points[0].Position
                        + 3 * (1 - t) * (1 - t) * (t) * points[1].Position
                        + 3 * (1 - t) * (t) * (t) * points[2].Position
                        + (t) * (t) * (t) * points[3].Position;
				    case 3:
					    return (1 - t) * (1 - t) * points[0].Position
                            + 2 * (1 - t) * (t) * points[1].Position
                            + (t) * (t) * points[2].Position;
				    case 2:
					    return (1 - t) * points[0].Position
                            + (t) * points[1].Position;
                    case 1:
                        return points[0].Position;
                    default:
                        return Vector4.Zero();
                }
            }

            public Vector4 GetBezierValue2D(float t)
            {
                switch (points.Count)
                {
                    case 4:
                        return (1 - t) * (1 - t) * (1 - t) * points[0].ScreenPosition
                        + 3 * (1 - t) * (1 - t) * (t) * points[1].ScreenPosition
                        + 3 * (1 - t) * (t) * (t) * points[2].ScreenPosition
                        + (t) * (t) * (t) * points[3].ScreenPosition;
                    case 3:
                        return (1 - t) * (1 - t) * points[0].ScreenPosition
                            + 2 * (1 - t) * (t) * points[1].ScreenPosition
                            + (t) * (t) * points[2].ScreenPosition;
                    case 2:
                        return (1 - t) * points[0].ScreenPosition
                            + (t) * points[1].ScreenPosition;
                    case 1:
                        return points[0].ScreenPosition;
                    default:
                        return Vector4.Zero();
                }
            }

            public int GetBezierPolygonCircuit()
            {
                float sum = 0;
                if (points.Count > 0)
                {
                    for (int i = 1; i < points.Count; i++)
                    {
                        sum += Vector4.Distance(points[i - 1].ScreenPosition, points[i].ScreenPosition);
                    }
                }
                return (int)sum;
            }

            public float GetMaxX()
            {
                float maxX = float.MinValue;
                foreach(var p in points)
                {
                    if (p.ScreenPosition.X < 0 && p.Position.X > maxX)
                        maxX = p.Position.X;
                }
                return maxX;
            }
        }
    }
}
