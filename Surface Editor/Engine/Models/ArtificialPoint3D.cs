using Engine.Interfaces;
using Engine.Utilities;
using System;
using System.Collections.Generic;

namespace Engine.Models
{
    // Only as artificial bezier points in B-Spline and in Surfaces
    public class ArtificialPoint3D: _3DObject, ISelectable
    {
        private static int counterF = 0;
        public bool isFused = false;

        public ArtificialPoint3D(Vector4 position) : base(position, new Vector4(0, 0, 0), new Vector4(1, 1, 1))
        {
            Points = new List<Vector4> { new Vector4(0, 0, 0) };
            Edges = new List<Edge>();
            Name = "PatchPoint" + counterF++.ToString();
        }

        public ArtificialPoint3D(string name, Vector4 position) : base(position, new Vector4(0, 0, 0), new Vector4(1, 1, 1))
        {
            Points = new List<Vector4> { new Vector4(0, 0, 0) };
            Edges = new List<Edge>();
            Name = name;
        }

        public bool IsSelectedToGroup { get; set; } = false;
        public _3DObject SymmetricalPoint { get; set; }

        public override _3DObject Clone()
        {
            return new ArtificialPoint3D(Position);
        }

        public override _3DObject CloneMirrored()
        {
            return new ArtificialPoint3D(new Vector4(Position.X, Position.Y, -Position.Z));
        }

        public override void Render(Renderer renderer, Matrix4 projView, int width, int height)
        {
            throw new NotImplementedException();
        }

        public override void RenderSterographic(Renderer renderer, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            throw new NotImplementedException();
        }

        public override string Save()
        {
            return "";
        }

        public override void UpdateModelMatrix()
        {
            ModelMatrix = Transform(Position.X, Position.Y, Position.Z);
        }

        public void UpdatePosition(Matrix4 matrix)
        {
            Position = matrix * Position;
        }
    }
}
