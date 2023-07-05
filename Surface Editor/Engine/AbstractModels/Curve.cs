using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.Utilities;

namespace Engine.Models
{
    public abstract class Curve : _3DObject
    {
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

        public ObservableCollection<Point3D> ControlPoints { get; set; } = new ObservableCollection<Point3D>();

        public Curve(Vector4 initialPosition, Vector4 rotation, Vector4 scale) : base(initialPosition, rotation, scale)
        {
            Points = new List<Vector4>();
            Edges = new List<Edge>();
        }

        public abstract void AddPoint(Point3D p);

        public abstract void DeletePoint(Point3D p);

        public void FindAndSubstitutePointsInPatch(_3DObject point1, _3DObject point2, Point3D point)
        {
            for (int i = 0; i < ControlPoints.Count; i++)
            {
                if (ControlPoints[i] == point1 || ControlPoints[i] == point2)
                    ControlPoints[i] = point;
            }
        }
    }
}
