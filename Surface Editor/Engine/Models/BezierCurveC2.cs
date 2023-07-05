using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Engine.Models
{
    public class BezierCurveC2 : Curve
    {
        public enum Representation
        {
            Bezier,
            BSpline
        }

        public override IEnumerable<_3DObject> DisplayPoints
        {
            get
            {
                if (representation == Representation.Bezier)
                    return bezierPoints.Cast<_3DObject>().ToList();
                else if (representation == Representation.BSpline)
                    return ControlPoints.Cast<_3DObject>().ToList();
                else return new List<_3DObject>();
            }
        }

        public Representation representation = Representation.BSpline;

        public List<SegmentC2> BezierSegments = new List<SegmentC2>();
        public List<SegmentBSpline> BSplineSegments = new List<SegmentBSpline>();

        public List<Vector4> deBoorPoints = new List<Vector4>();

        public List<Vector4> bernstainControlPoints = new List<Vector4>();

        public List<ArtificialPoint3D> bezierPoints = new List<ArtificialPoint3D>();

        public BezierCurveC2(Vector4 initialPosition, Vector4 rotation, Vector4 scale) : base(initialPosition, rotation, scale)
        {
            Name = "BezierC2_" + counter++.ToString();
            ControlPoints.CollectionChanged += ControlPoints_CollectionChanged;
        }

        public BezierCurveC2(string name, Vector4 initialPosition, Vector4 rotation, Vector4 scale) : base(initialPosition, rotation, scale)
        {
            Name = name;
            ControlPoints.CollectionChanged += ControlPoints_CollectionChanged;
        }

        public override _3DObject Clone()
        {
            BezierCurveC2 curve = new BezierCurveC2(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1));
            for (int i = 0; i < ControlPoints.Count; i++)
            {
                Point3D point = (Point3D)ControlPoints[i].Clone();
                curve.AddPoint(point);
            }
            return curve;
        }

        public override _3DObject CloneMirrored()
        {
            BezierCurveC2 curve = new BezierCurveC2(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1));
            for (int i = 0; i < ControlPoints.Count; i++)
            {
                Point3D point = (Point3D)ControlPoints[i].CloneMirrored();
                curve.AddPoint(point);
            }
            return curve;
        }

        private void ControlPoints_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(e.NewItems != null)
                foreach(Point3D p in e.NewItems)
                    p.PropertyChanged += P_PropertyChanged;
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
            deBoorPoints.Clear();
            foreach (Point3D v in ControlPoints)
                deBoorPoints.Add(new Vector4(v.Position.X, v.Position.Y, v.Position.Z));
            int capacity = 3 * deBoorPoints.Count;// + 4;
            bezierPoints = new List<ArtificialPoint3D>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                bezierPoints.Add(new ArtificialPoint3D(Vector4.Zero()));
                bezierPoints[i].Position.PropertyChanged += BezierCurveC2_PropertyChanged;
            }
            UpdateCurve();
            NotifyPropertyChanged();
        }

        public override void AddPoint(Point3D p)
        {
            ControlPoints.Add(p);          
        }

        private void P_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateCurve();
        }

        public override void DeletePoint(Point3D p)
        {
            ControlPoints.Remove(p);
        }

        private void CorrectDeBoorPoints(Vector4 point)
        {
            int index = bernstainControlPoints.IndexOf(point);
            if (index < 3)
            {
                ControlPoints[0].Position.Y = point.Y;
                ControlPoints[0].Position.X = point.X;
                ControlPoints[0].Position.Z = point.Z;
            }
            else if (index > bernstainControlPoints.Count - 3)
            {
                ControlPoints[ControlPoints.Count - 1].Position.X = point.X;
                ControlPoints[ControlPoints.Count - 1].Position.Y = point.Y;
                ControlPoints[ControlPoints.Count - 1].Position.Z = point.Z;
            }
            else
            {
                int divIndex = index / 3;
                int modIndex = index % 3;
                switch (modIndex)
                {
                    case 0:
                        if (divIndex < ControlPoints.Count)
                        {
                            Vector4 ge = point - bernstainControlPoints[index - 1];
                            Vector4 f = point + ge;
                            Vector4 pf = f - deBoorPoints[divIndex - 1];
                            Vector4 dest = f + 2 * pf;
                            ControlPoints[divIndex].Position.X = dest.X;
                            ControlPoints[divIndex].Position.Y = dest.Y;
                            ControlPoints[divIndex].Position.Z = dest.Z;
                        }
                        else if (divIndex == ControlPoints.Count)
                        {
                            Vector4 ge = point - deBoorPoints[divIndex - 2];
                            Vector4 dest = point + ge * 1/5f;
                            ControlPoints[divIndex - 1].Position.X = dest.X;
                            ControlPoints[divIndex - 1].Position.Y = dest.Y;
                            ControlPoints[divIndex - 1].Position.Z = dest.Z;
                        }
                        break;
                    case 1:
                        if (divIndex == ControlPoints.Count)
                        {
                            Vector4 dest1 = new Vector4(point.X, point.Y, point.Z);
                            ControlPoints[divIndex - 1].Position.X = dest1.X;
                            ControlPoints[divIndex -1 ].Position.Y = dest1.Y;
                            ControlPoints[divIndex -1 ].Position.Z = dest1.Z;
                        }
                        else
                        {
                            Vector4 df = point - deBoorPoints[divIndex - 1];
                            Vector4 dest1 = point + 2 * df;
                            ControlPoints[divIndex].Position.X = dest1.X;
                            ControlPoints[divIndex].Position.Y = dest1.Y;
                            ControlPoints[divIndex].Position.Z = dest1.Z;
                        }
                        break;
                    case 2:
                        Vector4 dg = point - deBoorPoints[divIndex - 1];
                        Vector4 dest2 = point + dg * 0.5f;
                        ControlPoints[divIndex].Position.X = dest2.X;
                        ControlPoints[divIndex].Position.Y = dest2.Y;
                        ControlPoints[divIndex].Position.Z = dest2.Z;
                        break;
                    default:
                        break;
                }
            }
            deBoorPoints.Clear();
            foreach (Point3D v in ControlPoints)
                deBoorPoints.Add(new Vector4(v.Position.X, v.Position.Y, v.Position.Z));
        }

        private void UpdateCurve()
        {
            if (deBoorPoints.Count > 0)
            {
                if (representation == Representation.Bezier)
                {
                    CreateBernstainControlPoints();
                    for (int i = 2; i < bernstainControlPoints.Count - 2; i++)
                    {
                        bezierPoints[i - 2].Position = bernstainControlPoints[i];
                        bezierPoints[i - 2].UpdateModelMatrix();
                        bezierPoints[i - 2].Position.PropertyChanged += BezierCurveC2_PropertyChanged;
                    }
                    CreateBezierSegments();
                }
                else if (representation == Representation.BSpline)
                {
                    deBoorPoints.Clear();
                    foreach (Point3D v in ControlPoints)
                        deBoorPoints.Add(new Vector4(v.Position.X, v.Position.Y, v.Position.Z));
                    CreateBSplineSegments();
                }
            }
            else
            {
                BezierSegments.Clear();
                BSplineSegments.Clear();
            }
        }

        private void BezierCurveC2_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CorrectDeBoorPoints((Vector4)sender);
            UpdateCurve();
        }

        public void ChangeRepresentationToBSpline()
        {
            representation = Representation.BSpline;
            UpdateCurve();
            NotifyPropertyChanged();
        }

        public void ChangeRepresentationToBernstain()
        {
            representation = Representation.Bezier;
            UpdateCurve();
            NotifyPropertyChanged();
        }

        public override void Render(Renderer renderer, Matrix4 projView, int width, int height)
        {
            renderer.RenderBezierC2(this, projView, width, height, Color);
        }

        public override void RenderSterographic(Renderer renderer, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            renderer.RenderStereographicBezierC2(this, projView, leftProjView, rightProjView, width, height);
        }

        private void CreateBezierSegments()
        {
            var p = bernstainControlPoints.ToList();
            BezierSegments = new List<SegmentC2>();
            for (int i = 0; i < p.Count; i += 3)
            {
                BezierSegments.Add(new SegmentC2(p.GetRange(i, Math.Min(4, p.Count - i))));
            }
        }

        private void CreateBSplineSegments()
        {
            var p = deBoorPoints.ToList();
            p.Insert(0, deBoorPoints[0]);
            p.Insert(0, deBoorPoints[0]);
            p.Add(deBoorPoints[deBoorPoints.Count -1]);
            p.Add(deBoorPoints[deBoorPoints.Count -1 ]);
            BSplineSegments = new List<SegmentBSpline>();
            for (int i = 0; i < p.Count - 3; i += 1)
            {
                BSplineSegments.Add(new SegmentBSpline(p.GetRange(i, Math.Min(4, p.Count - i))));
            }
        }

        private void CreateBernstainControlPoints()
        {
            var p = deBoorPoints.ToList();
            p.Insert(0, deBoorPoints[0]);
            p.Insert(0, deBoorPoints[0]);
            p.Add(deBoorPoints[deBoorPoints.Count -1 ]);
            p.Add(deBoorPoints[deBoorPoints.Count -1 ]);
            int count = p.Count;
            int m = p.Count - 3;
            List<Vector4> f = new List<Vector4>();
            List<Vector4> g = new List<Vector4>();
            List<Vector4> e = new List<Vector4>();
            if (count > 1)
                f.Add(p[1]);
            if (count > 4)
                g.Add(p[1] / 2f + p[2] / 2f);
            for (int i = 1; i < m - 1; i++)
            {
                f.Add(p[i + 1] * (2f / 3) + p[i + 2] * (1f / 3));
                g.Add(p[i + 1] * (1f/ 3) + p[i + 2] * (2f / 3));
            }
            if (m > 0)
                f.Add(p[m] / 2f + p[m + 1] / 2f);
            if (m + 1 > 0)
                g.Add(p[m + 1]);
            if (count > 0)
                e.Add(p[0]);
            for (int i = 1; i < m; i++)
            {
                e.Add(g[i - 1] / 2f + f[i] / 2f);
            }
            if (count> 0)
                e.Add(p[m + 2]);
            bernstainControlPoints.Clear();
            for (int i = 0; i < e.Count - 1; i++)
            {
                if(e.Count > i)
                    bernstainControlPoints.Add(e[i]);
                if (f.Count > i)
                    bernstainControlPoints.Add(f[i]);
                if (g.Count > i)
                    bernstainControlPoints.Add(g[i]);
            }
            if (e.Count >  0)
                bernstainControlPoints.Add(e[e.Count -1]);
        }

        public override string Save()
        {
            string val = "curveC2 1\n" + Name;
            foreach (var point in ControlPoints)
            {
                val += " " + point.Position.ToString() + ";" + point.Name;
            }
            val += "\n";
            return val;
        }

        public class SegmentC2: ISegment
        {
            public List<Vector4> Points { get; set; }
            public List<Vector4> ScreenPoints { get; set; }

            public SegmentC2(List<Vector4> points)
            {
                Points= points;
                ScreenPoints = new List<Vector4>(points);
            }

            public Vector4 GetValue(float t)
            {
                switch (Points.Count)
                {
                    case 4:
                        return (1 - t) * (1 - t) * (1 - t) * Points[0]
                        + 3 * (1 - t) * (1 - t) * (t) * Points[1]
                        + 3 * (1 - t) * (t) * (t) * Points[2]
                        + (t) * (t) * (t) * Points[3];
                    case 3:
                        return (1 - t) * (1 - t) * Points[0]
                            + 2 * (1 - t) * (t) * Points[1]
                            + (t) * (t) * Points[2];
                    case 2:
                        return (1 - t) * Points[0]
                            + (t) * Points[1];
                    case 1:
                        return Points[0];
                    default:
                        return Vector4.Zero();
                }
            }

            public Vector4 GetValue2D(float t)
            {
                switch (Points.Count)
                {
                    case 4:
                        return (1 - t) * (1 - t) * (1 - t) * ScreenPoints[0]
                        + 3 * (1 - t) * (1 - t) * (t) * ScreenPoints[1]
                        + 3 * (1 - t) * (t) * (t) * ScreenPoints[2]
                        + (t) * (t) * (t) * ScreenPoints[3];
                    case 3:
                        return (1 - t) * (1 - t) * ScreenPoints[0]
                            + 2 * (1 - t) * (t) * ScreenPoints[1]
                            + (t) * (t) * ScreenPoints[2];
                    case 2:
                        return (1 - t) * ScreenPoints[0]
                            + (t) * ScreenPoints[1];
                    case 1:
                        return ScreenPoints[0];
                    default:
                        return Vector4.Zero();
                }
            }

            public int GetPolygonCircuit()
            {
                float sum = 0;
                if (Points.Count > 0)
                {
                    for (int i = 1; i < Points.Count; i++)
                    {
                        sum += Vector4.Distance(ScreenPoints[i - 1], ScreenPoints[i]);
                    }
                }
                return (int)sum;
            }      
        }

        public class SegmentBSpline: ISegment
        {

            public List<Vector4> Points { get; set; }
            public List<Vector4> ScreenPoints { get; set; }

            public SegmentBSpline(List<Vector4> points)
            {
                Points = points;
                ScreenPoints = new List<Vector4>(points);
            }

            public Vector4 GetValue(float t)
            {
                return (1 - t) * (1 - t) * (1 - t) / 6 * Points[0] +
                    ((3 * t - 6) * t * t + 4) / 6 * Points[1] +
                    (((-3 * t + 3) * t + 3) * t + 1) / 6 * Points[2] +
                    (t * t * t) / 6 * Points[3];
            }

            public Vector4 GetValue2D(float t)
            {
                return (1 - t) * (1 - t) * (1 - t) / 6 * ScreenPoints[0] +
                    ((3 * t - 6) * t * t + 4) / 6 * ScreenPoints[1] +
                    (((-3 * t + 3) * t + 3) * t + 1) / 6 * ScreenPoints[2] +
                    (t * t * t) / 6 * ScreenPoints[3];
            }

            public int GetPolygonCircuit()
            {
                float sum = 0;
                if (Points.Count > 0)
                {
                    for (int i = 1; i < Points.Count; i++)
                    {
                        sum += Vector4.Distance(ScreenPoints[i - 1], ScreenPoints[i]);
                    }
                }
                return (int)sum;
            }
        }

        public interface ISegment
        {
            List<Vector4> Points { get; set; }
            List<Vector4> ScreenPoints { get; set; }

            Vector4 GetValue(float t);
            Vector4 GetValue2D(float t);
            int GetPolygonCircuit();
        }
    }
}
