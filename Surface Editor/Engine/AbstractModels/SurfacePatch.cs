using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace Engine.Models
{
    public abstract class SurfacePatch : _3DObject
    {
        private Vector4 oldPosition = Vector4.Zero();
        private Vector4 oldRotation = Vector4.Zero();

        //public bool[,] TrimmingTable;
        public List<bool[,]> TrimmingTables = new List<bool[,]>();

        public override IEnumerable<_3DObject> DisplayPoints
        {
            get
            {
                foreach (var p in ScreenPoints)
                    yield return p;
            }
        }

        private bool drawPoly;
        public bool DrawPoly
        {
            get { return drawPoly; }
            set
            {
                if (drawPoly == value) return;
                else
                    drawPoly = value;
                NotifyPropertyChanged();
            }
        }

        private int horizontalPrecision;
        public int HorizontalPrecision
        {
            get { return horizontalPrecision; }
            set
            {
                if (horizontalPrecision == value) return;
                else
                    horizontalPrecision = value;
                NotifyPropertyChanged();
            }
        }

        private int verticalPrecision;
        public int VerticalPrecision
        {
            get { return verticalPrecision; }
            set
            {
                if (verticalPrecision == value) return;
                else
                    verticalPrecision = value;
                NotifyPropertyChanged();
            }
        }

        public List<SinglePatch> SinglePatches = new List<SinglePatch>();

        protected SurfacePatch(Vector4 initialPosition, ArtificialPoint3D[,] points) : base(initialPosition, new Vector4(0, 0, 0, 1), new Vector4(1, 1, 1, 1))
        {
            InitPatch(points);
            verticalPrecision = 4;
            horizontalPrecision = 4;
        }

        protected SurfacePatch(Vector4 initialPosition) : base(initialPosition, new Vector4(0, 0, 0, 1), new Vector4(1, 1, 1, 1))
        {
            verticalPrecision = 4;
            horizontalPrecision = 4;
        }

        public void InitPatch(ArtificialPoint3D[,] points)
        {
            ControlPoints = points;
            for (int i = 0; i < points.GetLength(0); i++)
            {
                for (int j = 0; j < points.GetLength(1); j++)
                {
                    ScreenPoints.Add(points[i, j]);
                    points[i, j].PropertyChanged += SurfacePatch_PropertyChanged;
                }
            }
        }

        private void SurfacePatch_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged();
        }

        protected SurfacePatch() : base(Vector4.Zero(), new Vector4(0, 0, 0, 1), new Vector4(1, 1, 1, 1))
        {
            verticalPrecision = 4;
            horizontalPrecision = 4;
        }

        public ArtificialPoint3D[,] ControlPoints { get; set; }

        public ObservableCollection<ArtificialPoint3D> ScreenPoints { get; set; } = new ObservableCollection<ArtificialPoint3D>();

        //public bool IsClampedU => false;

        //public bool IsClampedV
        //{
        //    get
        //    {
        //        if (this is CylindricalBezierPatch || this is CylindricalBezierPatchC2)
        //            return true;
        //        else
        //            return false;
        //    }
        //}

        //public float MaxU => ControlPoints.GetLength(0) / 3;

        //public float MaxV => ControlPoints.GetLength(1) / 3;

        public SurfacePatch(Vector4 initialPosition, Vector4 rotation, Vector4 scale) : base(initialPosition, rotation, scale)
        {
            Points = new List<Vector4>();
            Edges = new List<Edge>();
        }

        public override void UpdateModelMatrix()
        {
            var rotationDelta = Rotation - oldRotation;
            ModelMatrix = Transform(Position.X, Position.Y, Position.Z) * RotateX(rotationDelta.X) * RotateY(rotationDelta.Y) * RotateZ(rotationDelta.Z) * Transform(-oldPosition.X, -oldPosition.Y, -oldPosition.Z);
            oldPosition = new Vector4(Position.X, Position.Y, Position.Z);
            oldRotation = new Vector4(Rotation.X, Rotation.Y, Rotation.Z);
            if (ControlPoints != null)
                for (int i = 0; i < ControlPoints.GetLength(0); i ++)
                {
                    for (int j = 0; j < ControlPoints.GetLength(1); j ++)
                    {
                         ControlPoints[i, j].UpdatePosition(ModelMatrix);
                    }
                }
        }

        public Vector4 GetMidlle()
        {
            Vector4 sum = Vector4.Zero();
            foreach (var p in ControlPoints)
                sum += p.Position;
            sum /= 16;
            return sum;
        }

        public Vector4 GetPatchValue(ArtificialPoint3D p1, ArtificialPoint3D p2, float val)
        {
            var idx1 = GetPointIndex(p1);
            var idx2 = GetPointIndex(p2);
            if (idx1.i == idx2.i)
            {
                if(idx1.i == 3)
                    return SinglePatches[0].GetPatchValue(0, 1 - val);
                if (idx1.i == 0)
                    return SinglePatches[0].GetPatchValue(1, val);
            }
            if (idx1.j == idx2.j)
            {
                if (idx1.j == 3)
                    return SinglePatches[0].GetPatchValue(1 - val, 0);
                if (idx1.j == 0)
                    return SinglePatches[0].GetPatchValue(val, 1);
            }
            return Vector4.Zero();
        }

        public Vector4 GetDerivativeBorder(ArtificialPoint3D p1, ArtificialPoint3D p2, float val)
        {
            var idx1 = GetPointIndex(p1);
            var idx2 = GetPointIndex(p2);
            if (idx1.i == idx2.i)
            {
                if (idx1.i == 3)
                    if (idx1.j < idx2.j)
                        return -SinglePatches[0].GetPatchDU(0, 1 - val);
                    else
                        return -SinglePatches[0].GetPatchDU(0, val);
                if (idx1.i == 0)
                    if (idx1.j < idx2.j)
                        return SinglePatches[0].GetPatchDU(1, 1 - val);
                    else
                        return SinglePatches[0].GetPatchDU(1, val);
            }
            if (idx1.j == idx2.j)
            {
                if (idx1.j == 3)
                    if (idx1.i < idx2.i)
                        return -SinglePatches[0].GetPatchDV(val, 0);
                    else
                        return -SinglePatches[0].GetPatchDV(1 - val, 0);
                if (idx1.j == 0)
                    if (idx1.i < idx2.i)
                        return SinglePatches[0].GetPatchDV(val, 1);
                    else
                        return SinglePatches[0].GetPatchDV(1 - val, 1);
            }
            return Vector4.Zero();
        }

        //public Vector4 GetTangentBorder(FakePoint3D p1, FakePoint3D p2, float val)
        //{
        //    var idx1 = GetPointIndex(p1);
        //    var idx2 = GetPointIndex(p2);
        //    if (idx1.i == idx2.i)
        //    {
        //        if (idx1.i == 3)
        //            if (idx1.j < idx2.j)
        //                return -SinglePatches[0].GetPatchDV(0, val);
        //            else
        //                return SinglePatches[0].GetPatchDV(0, val);
        //        if (idx1.i == 0)
        //            if (idx1.j < idx2.j)
        //                return -SinglePatches[0].GetPatchDV(1, 1 - val);
        //            else
        //                return SinglePatches[0].GetPatchDV(1, 1 - val);
        //    }
        //    if (idx1.j == idx2.j)
        //    {
        //        if (idx1.j == 3)
        //            if (idx1.i < idx2.i)
        //                return SinglePatches[0].GetPatchDU(1 - val, 0);
        //            else
        //                return -SinglePatches[0].GetPatchDU(1  - val, 0);
        //        if (idx1.j == 0)
        //            if (idx1.i < idx2.i)
        //                return -SinglePatches[0].GetPatchDU( val, 1);
        //            else
        //                return SinglePatches[0].GetPatchDU(val, 1);
        //    }
        //    return Vector4.Zero();
        //}

        public Vector4 GetTangentBorder(ArtificialPoint3D p1, ArtificialPoint3D p2, float val)
        {
            var idx1 = GetPointIndex(p1);
            var idx2 = GetPointIndex(p2);
            if (idx1.i == idx2.i)
            {
                if (idx1.i == 3)
                    if (idx1.j < idx2.j)
                        return -SinglePatches[0].GetPatchDV(0, 1 - val);
                    else
                        return SinglePatches[0].GetPatchDV(0, 1 - val);
                if (idx1.i == 0)
                    if (idx1.j < idx2.j)
                        return -SinglePatches[0].GetPatchDV(1, val);
                    else
                        return SinglePatches[0].GetPatchDV(1, val);
            }
            if (idx1.j == idx2.j)
            {
                if (idx1.j == 3)
                    if (idx1.i < idx2.i)
                        return -SinglePatches[0].GetPatchDU(val, 0);
                    else
                        return SinglePatches[0].GetPatchDU(val, 0);
                if (idx1.j == 0)
                    if (idx1.i < idx2.i)
                        return -SinglePatches[0].GetPatchDU(1 - val, 1);
                    else
                        return SinglePatches[0].GetPatchDU(1 - val, 1);
            }
            return Vector4.Zero();
        }


        //public Vector4 GetTangentBorder(FakePoint3D p1, FakePoint3D p2, float val)
        //{
        //    var idx1 = GetPointIndex(p1);
        //    var idx2 = GetPointIndex(p2);
        //    if (idx1.i == idx2.i)
        //    {
        //        if (idx1.i == 3)
        //            if (idx1.j < idx2.j)
        //                return -SinglePatches[0].GetPatchDV(0, val);
        //            else
        //                return SinglePatches[0].GetPatchDV(0, 1 - val);
        //        if (idx1.i == 0)
        //            if (idx1.j < idx2.j)
        //                return -SinglePatches[0].GetPatchDV(1, val);
        //            else
        //                return SinglePatches[0].GetPatchDV(1, 1-  val);
        //    }
        //    if (idx1.j == idx2.j)
        //    {
        //        if (idx1.j == 3)
        //            if (idx1.i < idx2.i)
        //                return -SinglePatches[0].GetPatchDU(1 - val, 0);
        //            else
        //                return SinglePatches[0].GetPatchDU(val, 0);
        //        if (idx1.j == 0)
        //            if (idx1.i < idx2.i)
        //                return -SinglePatches[0].GetPatchDU(1 - val, 1);
        //            else
        //                return SinglePatches[0].GetPatchDU( val, 1);
        //    }
        //    return Vector4.Zero();
        //}

        public (int i, int j) GetPointIndex(ArtificialPoint3D point)
        {
            for (int i = 0; i < ControlPoints.GetLength(0); i++)
            {
                for (int j = 0; j < ControlPoints.GetLength(1); j++)
                {
                    if (ControlPoints[i, j] == point)
                        return (i, j);
                }
            }
            return (-1,-1);
        }

        public abstract float GetStepU();
        public abstract float GetStepV();

        protected abstract void CreateSinglePatches();

        public abstract int GetUPatchesNumber();
        public abstract int GetVPatchesNumber();

        protected abstract ArtificialPoint3D[,] CreatePoints(int u, int v, int width, int height);

        protected Func<float, float, bool> GetTrimmingFunction(int indexU, int indexV)
        {
            //int numberU = GetUPatchesNumber(), numberV = GetVPatchesNumber();
            //return (u, v) =>
            //{
            //    if (TrimmingTables == null) return true;
            //    var precision = TrimmingTables.GetLength(0);
            //    return TrimmingTables[Math.Min((int)((indexU + (1 -u)) / numberU * precision), precision - 1), Math.Min((int)((indexV + (1 -v)) / numberV * precision), precision - 1)];
            //};
            int numberU = GetUPatchesNumber(), numberV = GetVPatchesNumber();
            return (u, v) =>
            {
                if (TrimmingTables.Count == 0) return true;
                bool ret = true;
                foreach (var TrimmingTable in TrimmingTables)
                {
                    var precision = TrimmingTable.GetLength(0);
                    //if (!(TrimmingTable[Math.Min((int)((indexU + (1 - u)) / numberU * precision), precision - 1), Math.Min((int)((indexV + (1 - v)) / numberV * precision), precision - 1)]))
                    if (!(TrimmingTable[Math.Min((int)(((numberU - 1 -indexU) + (u)) / numberU * precision), precision - 1), Math.Min((int)(((numberV - 1 - indexV) + (v)) / numberV * precision), precision - 1)]))
                        ret = false;
                }
                return ret;
            };
        }

        public List<ArtificialPoint3D> GetJoinedPointsFromSurface()
        {
            List<ArtificialPoint3D> points = new List<ArtificialPoint3D>();
            List<Point> indexes = new List<Point>();
            for (int i = 0; i < ControlPoints.GetLength(0); i++)
            {
                for (int j = 0; j < ControlPoints.GetLength(1); j++)
                {
                    if (ControlPoints[i, j].isFused == true && (i == 0 || i == ControlPoints.GetLength(0) - 1) && (j == 0 || j == ControlPoints.GetLength(1) - 1))
                    {
                        points.Add(ControlPoints[i, j]);
                        indexes.Add(new Point(i, j));
                    }
                }
            }
            List<ArtificialPoint3D> toDel = new List<ArtificialPoint3D>();
            bool foundColinear = false;
            for (int i = 0; i < indexes.Count; i++)
            {
                for (int j = 0; j < indexes.Count; j++)
                {
                    if (i != j && (indexes[i].X == indexes[j].X || indexes[i].Y == indexes[j].Y))
                    {
                        foundColinear = true;
                    }
                }
                if (!foundColinear)
                    toDel.Add(points[i]);
            }
            foreach (var p in toDel)
                points.Remove(p);
            return points;
        }

        public void FindAndSubstitutePointsInPatch(_3DObject fp1, _3DObject fp2, ArtificialPoint3D fp)
        {
            for (int i = 0; i < ControlPoints.GetLength(0); i++)
            {
                for (int j = 0; j < ControlPoints.GetLength(1); j++)
                {
                    if (ControlPoints[i, j] == fp1 || ControlPoints[i, j] == fp2)
                        ControlPoints[i, j] = fp;
                }
            }
            foreach (var sp in SinglePatches)
            {
                for (int i = 0; i < sp.points.GetLength(0); i++)
                {
                    for (int j = 0; j < sp.points.GetLength(1); j++)
                    {
                        if (sp.points[i, j] == fp1 || sp.points[i, j] == fp2)
                            sp.points[i, j] = fp;
                    }
                }
            }
            if (ScreenPoints.Contains((ArtificialPoint3D)fp1))
            {
                int ind = ScreenPoints.IndexOf((ArtificialPoint3D)fp1);
                ScreenPoints[ind] = fp;
            }
            if (ScreenPoints.Contains((ArtificialPoint3D)fp2))
            {
                int ind = ScreenPoints.IndexOf((ArtificialPoint3D)fp2);
                ScreenPoints[ind] = fp;
            }
        }

        //public (float u, float v) GetPoint3DUV(Vector4 pos)
        //{
        //    float bestU = 0, bestV = 0;
        //    Vector4 bestPos = Evaluate(bestU, bestV);
        //    float minDistance = Vector4.Distance(bestPos, pos);
        //    const int N = 128; //number of samples
        //    float stepU = ControlPoints.GetLength(0) / 3 * 1.0f / (N - 1);
        //    float stepV = ControlPoints.GetLength(1) / 3 * 1.0f / (N - 1);
        //    for (int i = 0; i < N; i++)
        //    {
        //        for (int j = 0; j < N; j++)
        //        {
        //            Vector4 p = Evaluate(i * stepU, j * stepV);
        //            float distance = Vector4.Distance(p, pos);
        //            if (distance < minDistance)
        //            {
        //                bestU = i * stepU;
        //                bestV = j * stepV;
        //                minDistance = distance;
        //            }
        //        }
        //    }
        //    return (bestU, bestV);
        //}

        //public Vector4 Evaluate(float u, float v)
        //{
        //    int idx1 = ControlPoints.GetLength(0) / 3;
        //    int idx2 = ControlPoints.GetLength(1) / 3;
        //    int modU = (int)u;
        //    int modV = (int)v;
        //    if (modV == idx2)
        //        modV -= 1;
        //    if (modU == idx1)
        //        modU -= 1;
        //    return SinglePatches[(idx1 - 1 - modU) * idx1 + (idx2 - 1- modV)].GetPatchValue(u - modU, v - modV);
        //    //return SinglePatches[0].GetPatchValue(u, v);
        //}

        //public Vector4 EvaluateDU(float u, float v)
        //{
        //    int idx1 = ControlPoints.GetLength(0) / 3;
        //    int idx2 = ControlPoints.GetLength(1) / 3;
        //    int modU = (int)u;
        //    int modV = (int)v;
        //    if (modV == idx2)
        //        modV -= 1;
        //    if (modU == idx1)
        //        modU -= 1;
        //    //return SinglePatches[modU * idx1 + modV].GetPatchDU(u- modU, v - modV);
        //    return SinglePatches[(idx1 - 1 - modU) * idx1 + (idx2 - 1 - modV)].GetPatchDU(u- modU, v - modV);
        //    //return SinglePatches[0].GetPatchDU(u, v);
        //}

        //public Vector4 EvaluateDV(float u, float v)
        //{
        //    int idx1 = ControlPoints.GetLength(0) / 3;
        //    int idx2 = ControlPoints.GetLength(1) / 3;
        //    int modU = (int)u;
        //    int modV = (int)v;
        //    if (modV == idx2)
        //        modV -= 1;
        //    if (modU == idx1)
        //        modU -= 1;
        //    //return SinglePatches[modU * idx1 + modV].GetPatchDV(u - modU, v - modV);
        //    return SinglePatches[(idx1 - 1 - modU) * idx1 + (idx2 - 1 - modV)].GetPatchDV(u - modU, v - modV);
        //    //return SinglePatches[0].GetPatchDV(u, v);
        //}

        //public float ClampU(float u)
        //{
        //    if (u > MaxU)
        //    {
        //        if (IsClampedU)
        //        {
        //            return 0 + (u - MaxU);
        //        }
        //        else
        //            return MaxU;
        //    }
        //    else if (u < 0)
        //    {
        //        if (IsClampedU)
        //        {
        //            return MaxU - u;
        //        }
        //        else
        //            return 0;
        //    }
        //    else
        //        return u;

        //}

        //public float ClampV(float v)
        //{
        //    if (v > MaxV)
        //    {
        //        if (IsClampedV)
        //        {
        //            return 0 + (v - MaxV);
        //        }
        //        else
        //            return MaxV;
        //    }
        //    else if (v < 0)
        //    {
        //        if (IsClampedV)
        //        {
        //            return MaxV - v;
        //        }
        //        else
        //            return 0;
        //    }
        //    else
        //        return v;
        //}

        public abstract class SinglePatch
        {
            public ArtificialPoint3D[,] points;
            public abstract Vector4 GetPatchValue(float u, float v);
            public abstract Vector4 GetPatchTrimmingValue(float u, float v);
            public abstract Vector4 GetPatchDU(float u, float v);
            public abstract Vector4 GetPatchDV(float u, float v);
            public Func<float, float, bool> GetTrimingValue;
        }

        public class SinglePatchGregory : SinglePatch
        {
            public new Vector4[,] points;
            public Vector4[] netPoints;

            public SinglePatchGregory(Vector4[,] points, Vector4[] netPoints)
            {
                this.points = points;
                this.netPoints = netPoints;
            }

            private float GetHermite(int i, float t)
            {
                switch (i)
                {
                    case 0:
                        return (1 - t) * (1 - t) * (1 + 2 * t);
                    case 2:
                        return t * (1 - t) * (1 - t);
                    case 1:
                        return t * t * (3 - 2 * t);
                    case 3:
                        return t* t * (t -1);
                    default:
                        return 0;
                }
            }

            public override Vector4 GetPatchDU(float u, float v)
            {
                throw new NotImplementedException();
            }

            public override Vector4 GetPatchDV(float u, float v)
            {
                throw new NotImplementedException();
            }

            public override Vector4 GetPatchValue(float u, float v)
            {
                if (u + v == 0)
                    points[2, 2] = Vector4.Zero();
                else
                    points[2, 2] = (u * netPoints[0] + v * netPoints[1]) / (u + v);
                if (u + 1 - v == 0)
                    points[2, 3] = Vector4.Zero();
                else
                    points[2, 3] = (u * netPoints[2] + (1 - v) * netPoints[3]) / (u + 1 - v);
                if (1 - u + v == 0)
                    points[3, 2] = Vector4.Zero();
                else
                    points[3, 2] = ((1 - u) * netPoints[4] + v * netPoints[5]) / (1 - u + v);
                if (1 - u + 1 - v == 0)
                    points[3, 3] = Vector4.Zero();
                else
                    points[3, 3] = ((1 - u) * netPoints[6] + (1 - v) * netPoints[7]) / (1 - u + 1 - v);
                Vector4 val = Vector4.Zero();
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        val += GetHermite(i, u) * GetHermite(j, v) * points[i, j];
                    }
                }
                val.W = 1;
                return val;
            }

            public override Vector4 GetPatchTrimmingValue(float u, float v)
            {
                return GetPatchValue(u, v);
            }
        }

        public class SinglePatchC0: SinglePatch
        {         
            private float GetBezier(int i, float t)
            {
                switch (i)
                {
                    case 3:
                        return (1 - t) * (1 - t) * (1 - t);
                    case 2:
                        return 3 * (1 - t) * (1 - t) * (t);
                    case 1:
                        return 3 * (1 - t) * (t) * (t);
                    case 0:
                        return (t) * (t) * (t);
                     default:
                        return 0;
                }
            }

            private float GetBezierDerivative(int i, float t)
            {
                switch (i)
                {
                    case 3:
                        return -3 * (1 - t) * (1 - t);
                    case 2:
                        return 3 * (1 - t) * (1 - t) - 6 * t * (1 - t);
                    case 1:
                        return 6 * t * (1 - t) - 3 * t * t;
                    case 0:
                        return 3 * t * t;
                    default:
                        return 0;
                }
            }

            public override Vector4 GetPatchDU(float u, float v)
            {
                Vector4[] vCurve = new Vector4[4];
                for (int i = 0; i < 4; i++)
                {
                    vCurve[i] = Vector4.Zero();
                    for (int j = 0; j < 4; j++)
                    {
                        vCurve[i] += GetBezier(j, v) * points[i, j].Position;
                    }
                }
                Vector4 val = Vector4.Zero();
                val += GetBezierDerivative(0, u) * vCurve[0] + GetBezierDerivative(1, u) * vCurve[1] + GetBezierDerivative(2, u) * vCurve[2] + GetBezierDerivative(3, u) * vCurve[3];
                val.W = 0;
                return val;
            }

            public override Vector4 GetPatchDV(float u, float v)
            {
                Vector4[] uCurve = new Vector4[4];
                for (int i = 0; i < 4; i++)
                {
                    uCurve[i] = Vector4.Zero();
                    for (int j = 0; j < 4; j++)
                    {
                        uCurve[i] += GetBezier(j, u) * points[j, i].Position;
                    }
                }
                Vector4 val = Vector4.Zero();
                val += GetBezierDerivative(0, v) * uCurve[0] + GetBezierDerivative(1, v) * uCurve[1] + GetBezierDerivative(2, v) * uCurve[2] + GetBezierDerivative(3, v) * uCurve[3];
                val.W = 0;
                return val;
            }

            public SinglePatchC0(ArtificialPoint3D[,] points, Func<float, float, bool> f)
            {
                this.points = points;
                GetTrimingValue = f;
            }

            public override Vector4 GetPatchValue(float u, float v)
            {
                Vector4 val = Vector4.Zero();
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        val +=  GetBezier(i, u) * GetBezier(j, v) * points[i, j].Position;
                    }
                }
                val.W = 1;
                return val;
            }

            public override Vector4 GetPatchTrimmingValue(float u, float v)
            {
                if (GetTrimingValue(u, v) == false) return null;
                Vector4 val = Vector4.Zero();
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        val += GetBezier(i, u) * GetBezier(j, v) * points[i, j].Position;
                    }
                }
                val.W = 1;
                return val;
            }
        }

        public class SinglePatchC2 : SinglePatch
        {
            List<float> knotsU = new List<float>();
            List<float> knotsV = new List<float>();

            private float GetBSpline(int i, float t)
            {
                switch (i)
                {
                    case 3:
                        return (1 - t) * (1 - t) * (1 - t) / 6;
                    case 2:
                        return ((3 * t - 6) * t * t + 4) / 6;
                    case 1:
                        return (((-3 * t + 3) * t + 3) * t + 1) / 6;
                    case 0:
                        return (t * t * t) / 6;
                    default:
                        return 0;
                }
            }

            private float GetBSplineDerivative(int i, float t)
            {
                switch (i)
                {
                    case 3:
                        return -(1 - t) * (1 - t) / 2;
                    case 2:
                        return t * (3 * t - 4) / 2;
                    case 1:
                        return -3 * t * t / 2 + t + 0.5f;
                    case 0:
                        return t * t / 2;
                    default:
                        return 0;
                }
            }

            public SinglePatchC2(ArtificialPoint3D[,] points, Func<float, float, bool> f)
            {
                this.points = points;
                GetTrimingValue = f;
            }

            public override Vector4 GetPatchValue(float u, float v)
            {
                Vector4 val = Vector4.Zero();
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        val += GetBSpline(i, u) * GetBSpline(j, v) * points[i, j].Position;
                    }
                }
                val.W = 1;
                return val;
            }

            public override Vector4 GetPatchDU(float u, float v)
            {
                Vector4 val = Vector4.Zero();
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        val += GetBSplineDerivative(i, u) * GetBSpline(j, v) * points[i, j].Position;
                    }
                }
                val.W = 0;
                return val;
            }

            public override Vector4 GetPatchDV(float u, float v)
            {
                Vector4 val = Vector4.Zero();
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        val += GetBSpline(i, u) * GetBSplineDerivative(j, v) * points[i, j].Position;
                    }
                }
                val.W = 0;
                return val;
            }

            public override Vector4 GetPatchTrimmingValue(float u, float v)
            {
                if (GetTrimingValue(u, v) == false) return null;
                Vector4 val = Vector4.Zero();
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        val += GetBSpline(i, u) * GetBSpline(j, v) * points[i, j].Position;
                    }
                }
                val.W = 1;
                return val;
            }
        }
    }
}
