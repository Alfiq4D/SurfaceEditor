using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Engine.Utilities;

namespace Engine.Models
{
    public class InterpolationCurve : Curve
    {
        List<float> knots = new List<float>();
        List<_3DObject> BasicPoints { get; set; } = new List<_3DObject>();
        List<Func<float, float>> basicFunctions = new List<Func<float, float>>();
        List<Vector4> coords = new List<Vector4>();
        public List<float> tau = new List<float>();
        int size = 0;

        private bool useChordParam = true;
        public bool UseChordParam
        {
            get { return useChordParam; }
            set
            {
                if (useChordParam == value) return;
                else
                    useChordParam = value;
                UpdateCurve();
            }
        }

        public override IEnumerable<_3DObject> DisplayPoints
        {
            get
            {
                return ControlPoints.Cast<_3DObject>().ToList();
            }
        }

        public InterpolationCurve(Vector4 initialPosition, Vector4 rotation, Vector4 scale) : base(initialPosition, rotation, scale)
        {
            Name = "Interpolation" + counter++.ToString();
            ControlPoints.CollectionChanged += ControlPoints_CollectionChanged;
        }

        public InterpolationCurve(List<Point3D> points, Vector4 initialPosition, Vector4 rotation, Vector4 scale) : base(initialPosition, rotation, scale)
        {
            Name = "Interpolation" + counter++.ToString();
            foreach (var p in points)
            {
                ControlPoints.Add(p);
                p.PropertyChanged += P_PropertyChanged;
            }
            ControlPoints.CollectionChanged += ControlPoints_CollectionChanged;
            ControlPoints_CollectionChanged(null, null);
        }

        public InterpolationCurve(string name, Vector4 initialPosition, Vector4 rotation, Vector4 scale) : base(initialPosition, rotation, scale)
        {
            Name = name;
            ControlPoints.CollectionChanged += ControlPoints_CollectionChanged;
        }

        public override _3DObject Clone()
        {
            InterpolationCurve curve = new InterpolationCurve(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1));
            for (int i = 0; i < ControlPoints.Count; i++)
            {
                Point3D point = (Point3D)ControlPoints[i].Clone();
                curve.AddPoint(point);
            }
            return curve;
        }

        public override _3DObject CloneMirrored()
        {
            InterpolationCurve curve = new InterpolationCurve(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1));
            for (int i = 0; i < ControlPoints.Count; i++)
            {
                Point3D point = (Point3D)ControlPoints[i].CloneMirrored();
                curve.AddPoint(point);
            }
            return curve;
        }

        private void ControlPoints_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e != null && e.NewItems != null)
                foreach (Point3D p in e.NewItems)
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
            UpdateCurve();
            NotifyPropertyChanged();
        }

        private void P_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateCurve();
        }

        private void UpdateCurve()
        {
            BasicPoints.Clear();
            BasicPoints.Add(ControlPoints[0]);
            float eps = 0.0001f;
            for (int i = 1; i < ControlPoints.Count; i++)
            {
                if (Math.Abs(ControlPoints[i].Position.X - ControlPoints[i - 1].Position.X) > eps || Math.Abs(ControlPoints[i].Position.Y - ControlPoints[i - 1].Position.Y) > eps || Math.Abs(ControlPoints[i].Position.Z - ControlPoints[i - 1].Position.Z) > eps)
                    BasicPoints.Add(ControlPoints[i]);
            }
            //BasicPoints.Insert(0, new Point3D(BasicPoints[0].Position - new Vector4(1f, 1f, 1f, 0)));
            //BasicPoints.Add(new Point3D(BasicPoints[BasicPoints.Count - 1].Position - new Vector4(1f, 1f, 1f, 0)));
            if (BasicPoints.Count > 1)
            {
                BasicPoints.Insert(0, new ArtificialPoint3D(BasicPoints[0].Position + (BasicPoints[0].Position - BasicPoints[1].Position)));
                BasicPoints.Add(new ArtificialPoint3D(BasicPoints[BasicPoints.Count - 1].Position + (BasicPoints[BasicPoints.Count - 1].Position - BasicPoints[BasicPoints.Count - 2].Position)));
            }
            if (useChordParam)
                CalculateChordLengthKnots();
            else
                CalculateEquidistantKnots();
            CalculateBasicFunctions(3);
            size = BasicPoints.Count;
            GetCoords();
        }

        private void GetCoords()
        {
            int n = BasicPoints.Count;
            if (n < 4)
            {
                coords.Clear();
                for (int i = 0; i < n; i++)
                {
                    coords.Add(new Vector4());
                }
                return;
            }
            float[] matrix4 = CreateMatrix1();
            float[] matrix41 = (float[])matrix4.Clone();
            float[] matrix42 = (float[])matrix4.Clone();
            float[] matrix43 = (float[])matrix4.Clone();
            coords.Clear();
            for (int i = 0; i < n; i++)
            {
                coords.Add(new Vector4());
            }
            float[] vector1 = new float[n], vector2 = new float[n], vector3 = new float[n];
            for (int i = 0; i < n; i++)
            {
                vector1[i] = BasicPoints[i].Position.X;
                vector2[i] = BasicPoints[i].Position.Y;
                vector3[i] = BasicPoints[i].Position.Z;
            }
            SolveMatrix4(matrix41, vector1);
            SolveMatrix4(matrix42, vector2);
            SolveMatrix4(matrix43, vector3);
            for (int i = 0; i < n; i++)
            {
                coords[i].X = vector1[i];
                coords[i].Y = vector2[i];
                coords[i].Z = vector3[i];
            }
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
            renderer.RenderInterpolationCurve(this, projView, width, height, Color);
        }

        public override void RenderSterographic(Renderer renderer, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            renderer.RenderStereographicInterpolationCurve(this, projView, leftProjView, rightProjView, width, height);
        }

        private void CalculateChordLengthKnots()
        {
            knots.Clear();
            List<float> distances = new List<float>();
            float distance = 0;
            knots.Add(0);
            knots.Add(0);
            knots.Add(0);
            for (int i = 1; i < BasicPoints.Count; i++)
            {
                distance += Vector4.Distance(BasicPoints[i].Position, BasicPoints[i - 1].Position);
                distances.Add(distance);
            }
            for (int i = 0; i < BasicPoints.Count - 2; i++)
            {
                knots.Add(distances[i] / distances[distances.Count - 1]);
            }
            knots.Add(1);
            knots.Add(1);
            knots.Add(1);
            tau.Clear();
            for (int i = 0; i < BasicPoints.Count; i++)
            {
                tau.Add((knots[i + 2]));
            }
            knots[3] = 0;
            knots[knots.Count - 4] = 1;
        }

        private void CalculateEquidistantKnots()
        {
            knots.Clear();
            knots.Add(0);
            knots.Add(0);
            knots.Add(0);
            for (int i = 1; i < BasicPoints.Count -1 ; i++)
            {
                knots.Add(i  *1f / (BasicPoints.Count - 1));
            }
            knots.Add(1);
            knots.Add(1);
            knots.Add(1);
            tau.Clear();
            for (int i = 0; i < BasicPoints.Count; i++)
            {
                tau.Add((knots[i + 2]));
            }
            knots[3] = 0;
            knots[knots.Count - 4] = 1;
        }

        private void CalculateBasicFunctions(int n)
        {
            basicFunctions.Clear();
            for (int i = 0; i < BasicPoints.Count; i++)
            {
                basicFunctions.Add(GetBasicFunction(n, i));
            }
        }

        private Func<float, float> GetBasicFunction(int n, int i)
        {
            if (knots[i] == knots[i + n + 1]) return t => 0;
            if (n == 0 && knots[i + 1] == 1)
                return t => t >= knots[i] ? 1 : 0;
            if (n == 0)
                return t => t >= knots[i] && t < knots[i + 1] ? 1 : 0;
            return t =>
                 (knots[i + n] - knots[i] <= 1e10f * float.Epsilon ? 0 : (t - knots[i]) / (knots[i + n] - knots[i]) * GetBasicFunction(n - 1, i)(t)) +
                  (knots[i + n + 1] - knots[i + 1] <= 1e10f * float.Epsilon ? 0 : (knots[i + n + 1] - t) / (knots[i + n + 1] - knots[i + 1]) * GetBasicFunction(n - 1, i + 1)(t));
        }

        private float[] CreateMatrix1()
        {
            int n = BasicPoints.Count;
            int size = n * 3 - 2;
            float[] matrix4 = new float[size];
            matrix4[0] = basicFunctions[0](tau[0]);
            for (int i = 1; i <= 4; ++i)
            {
                matrix4[i] = basicFunctions[i - 1](tau[1]);
            }
            for (int i = 2; i < n -2 ; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    matrix4[5 + (i - 2) * 3 + j] = basicFunctions[i - 1 + j](tau[i]);
                }
            }
            for (int i = 1; i <= 4; ++i)
            {
                matrix4[size - 6 + i] = basicFunctions[n - 5 + i](tau[n - 2]);
            }
            matrix4[size - 1] = basicFunctions[n-1](tau[n-1]);
            return matrix4;
        }

        public Vector4 GetCurveValue(float t)
        {
            Vector4 vector = Vector4.Zero();
            for (int i = 0; i < size; i++)
            {
                vector += coords[i] * basicFunctions[i](t);
            }
            return vector;
        }

        public List<Vector4> GetScreenCoords()
        {
            return coords;
        }

        public Vector4 GetValue2D(float t, List<Vector4> screenCoords)
        {
            Vector4 vector = Vector4.Zero();
            for (int i = 0; i < size; i++)
            {
                vector += screenCoords[i] * basicFunctions[i](t);
            }
            return vector;
        }

        public int GetPolygonCircuit()
        {
            float sum = 0;
            for (int i = 1; i < BasicPoints.Count; i++)
            {
                sum += Vector4.Distance(BasicPoints[i].ScreenPosition, BasicPoints[i - 1].ScreenPosition);
            }
            return (int)sum;
        }

        private void SolveMatrix3(float[] matrix, float[] value)
        {
            float div, first = matrix[1];
            int idx;
            matrix[1] = 0.0f;
            value[1] -= value[0] * first;
            div = matrix[2];
            matrix[2] = 1.0f;
            matrix[3] /= div;
            value[1] /= div;
            for (int i = 2; i < BasicPoints.Count - 1; ++i)
            {
                idx = 1 + (i - 1) * 3;
                first = matrix[idx];
                matrix[idx] = 0.0f;
                matrix[idx + 1] -= matrix[idx - 1] * first;
                value[i] -= value[i - 1] * first;

                div = matrix[idx + 1];
                matrix[idx + 1] = 1.0f;
                matrix[idx + 2] /= div;
                value[i] /= div;
            }
            for (int i = BasicPoints.Count - 2; i > 0; --i)
            {
                idx = 1 + (i - 1) * 3;
                value[i] -= value[i + 1] * matrix[idx + 2];
            }
        }

        private void SolveMatrix4(float[] matrix, float[] value)
        {
            int idx;

            if (BasicPoints.Count == 4)
            {
                //second and third row 0xxx
                value[1] -= value[0] * matrix[1];
                matrix[1] = 0.0f;
                value[2] -= value[0] * matrix[5];
                matrix[5] = 0.0f;
             
                //second row 01xxx
                matrix[3] /= matrix[2];
                matrix[4] /= matrix[2];
                value[1] /= matrix[2];
                matrix[2] = 1.0f;

                //third row 00xxx
                matrix[7] -= matrix[3] * matrix[6];
                matrix[8] -= matrix[4] * matrix[6];
                value[2] -= value[1] * matrix[6];
                matrix[6] = 0.0f;
    
                //third row 0001xx 
                matrix[8] /= matrix[7];
                value[2] /= matrix[7];
                matrix[7] = 1.0f;

                value[2] -= value[3] * matrix[8];
                value[1] -= value[3] * matrix[4];
                value[1] -= value[2] * matrix[3];
            }
            else if (BasicPoints.Count > 4)
            {
                //second row 0xxx
                value[1] -= value[0] * matrix[1];
                matrix[1] = 0.0f;

                //second row 01xxx
                matrix[3] /= matrix[2];
                matrix[4] /= matrix[2];
                value[1] /= matrix[2];
                matrix[2] = 1.0f;

                //third row 00xxx
                matrix[6] -= matrix[3] * matrix[5];
                matrix[7] -= matrix[4] * matrix[5];
                value[2] -= value[1] * matrix[5];
                matrix[5] = 0.0f;

                //third row 0001xx 
                matrix[7] /= matrix[6];
                value[2] /= matrix[6];
                matrix[6] = 1.0f;

                for (int i = 3; i < BasicPoints.Count - 2; ++i)
                {
                    idx = 5 + (i - 2) * 3;
                    //0xxx
                    matrix[idx + 1] -= matrix[idx - 1] * matrix[idx];
                    value[i] -= value[i - 1] * matrix[idx];
                    matrix[idx] = 0.0f;
                    //01xxx
                    matrix[idx + 2] /= matrix[idx + 1];
                    value[i] /= matrix[idx + 1];
                    matrix[idx + 1] = 1.0f;
                }

                //last 0xxx
                if (BasicPoints.Count > 5)
                {
                    matrix[matrix.Length - 4] -= matrix[matrix.Length - 9] * matrix[matrix.Length - 5];
                }
                else if (BasicPoints.Count == 5)
                {
                    matrix[matrix.Length - 4] -= matrix[matrix.Length - 10] * matrix[matrix.Length - 5];
                    matrix[matrix.Length - 3] -= matrix[matrix.Length - 9] * matrix[matrix.Length - 5];
                }
                value[BasicPoints.Count - 2] -= value[BasicPoints.Count - 4] * matrix[matrix.Length - 5];
                matrix[matrix.Length - 5] = 0.0f;

                //last 00xxx
                matrix[matrix.Length - 3] -= matrix[matrix.Length - 6] * matrix[matrix.Length - 4];
                value[BasicPoints.Count - 2] -= value[BasicPoints.Count - 3] * matrix[matrix.Length - 4];
                matrix[matrix.Length - 4] = 0.0f;
         
                //last 001xxx
                matrix[matrix.Length - 2] /= matrix[matrix.Length - 3];
                value[BasicPoints.Count - 2] /= matrix[matrix.Length - 3];
                matrix[matrix.Length - 3] = 1.0f;

                value[BasicPoints.Count - 2] -= value[BasicPoints.Count - 1] * matrix[matrix.Length - 2];
                for (int i = BasicPoints.Count - 3; i > 1; --i)
                {
                    idx = 5 + (i - 2) * 3;
                    value[i] -= value[i + 1] * matrix[idx + 2];
                }
                value[1] -= value[2] * matrix[3];
                value[1] -= value[3] * matrix[4];
            }
        }

        public override string Save()
        {
            string val = "curveInt 1\n" + Name;
            foreach (var point in ControlPoints)
            {
                val += " " + point.Position.ToString() + ";" + point.Name;
            }
            val += "\n";
            return val;
        }
    }
}