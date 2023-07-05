using Engine.Interfaces;
using Engine.Utilities;
using System.Collections.Generic;

namespace Engine.Models
{
    public class Point3D : _3DObject, ISelectable
    {
        public bool IsSelectedToGroup { get; set; } = false;

        public Point3D(Vector4 position) : base(position, new Vector4(0, 0, 0), new Vector4(1, 1, 1))
        {
            Name = "Point" + counter++.ToString();
            Points = new List<Vector4> { new Vector4(0, 0, 0) };
            Edges = new List<Edge>();
        }

        public override void Render(Renderer renderer, Matrix4 projView, int width, int height)
        {
            renderer.RenderPoint(this, projView, width, height, Color);
        }

        public override void RenderSterographic(Renderer renderer, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            renderer.RenderStereographicPoint(this, projView, leftProjView, rightProjView, width, height);
        }

        public override void UpdateModelMatrix()
        {
            ModelMatrix = Transform(Position.X, Position.Y, Position.Z);
        }

        public override string Save()
        {
            return "point 1\n" + Name + " " + Position.ToString() + "\n";
        }

        public override _3DObject Clone()
        {
            return new Point3D(Position);
        }

        public override _3DObject CloneMirrored()
        {
            return new Point3D(new Vector4(Position.X, Position.Y, -Position.Z));
        }

        public Point3D(string name, Vector4 position) : base(position, new Vector4(0, 0, 0), new Vector4(1, 1, 1))
        {
            Name = name;
            Points = new List<Vector4> { new Vector4(0, 0, 0) };
            Edges = new List<Edge>();
        }
    }
}
