using Engine.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Models
{
    public abstract class _3DModel: _3DObject
    {            
        private int uComplexity;
        public int UComplexity
        {
            get { return uComplexity; }
            set
            {
                if (uComplexity == value) return;
                if (value < 1)
                    uComplexity = 1;
                else
                    uComplexity = value;
                Create();
                NotifyPropertyChanged();
            }
        }
        private int vComplexity;

        public int VComplexity
        {
            get { return vComplexity; }
            set
            {
                if (vComplexity == value) return;
                if (value < 1)
                    vComplexity = 1;
                else
                    vComplexity = value;
                Create();
                NotifyPropertyChanged();
            }
        }

        public List<ArtificialPoint3D> ControlPoints { get; set; } = new List<ArtificialPoint3D>();

        public override IEnumerable<_3DObject> DisplayPoints
        {
            get
            {
                return ControlPoints.Cast<_3DObject>().ToList();
            }
        }

        protected _3DModel(Vector4 initialPosition, Vector4 rotation, Vector4 scale): base(initialPosition, rotation, scale)
        {
        }      

        public _3DModel(Vector4 initialPosition, Vector4 rotation, Vector4 scale, List<Vector4> points, List<Edge> edges) : this(initialPosition, rotation, scale)
        {
            if (points != null)
                Points = points;
            else
                Points = new List<Vector4>();
            if (edges != null)
                Edges = edges;
            else
                Edges = new List<Edge>();
        }

        public override void Render(Renderer renderer, Matrix4 projView, int width, int height)
        {
            renderer.Render3DModel(this, projView, width, height, Color);
        }

        public override void RenderSterographic(Renderer renderer,Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            renderer.RenderSteroegraphic3DModel(this, projView, leftProjView, rightProjView, width, height);
        }

        public abstract _3DModel Create();
    }
}
