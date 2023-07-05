using System;
using System.Collections.Generic;
using Engine.Utilities;

namespace Engine.Models
{
    public class Voxel: _3DObject
    {
        public bool isActive = true;

        private float width;
        private float height;
        private float depth;

        public Voxel(Vector4 position) : base(position, new Vector4(0, 0, 0), new Vector4(1, 1, 1))
        {
            Name = "Voxel";// + counter++.ToString();
            Points = new List<Vector4> { new Vector4(0, 0, 0) };
            Edges = new List<Edge>();
        }

        public Voxel(Vector4 position, float size) : base(position, new Vector4(0, 0, 0), new Vector4(1, 1, 1))
        {
            Name = "Voxel";// + counter++.ToString();
            Points = new List<Vector4> { new Vector4(position.X - size / 2.0f, position.Y - size / 2.0f, position.Z - size / 2.0f),
                new Vector4(position.X + size / 2.0f, position.Y - size / 2.0f, position.Z - size / 2.0f),
                new Vector4(position.X + size / 2.0f, position.Y - size / 2.0f, position.Z + size / 2.0f),
                new Vector4(position.X - size / 2.0f, position.Y - size / 2.0f, position.Z + size / 2.0f),
                new Vector4(position.X - size / 2.0f, position.Y + size / 2.0f, position.Z - size / 2.0f),
                new Vector4(position.X + size / 2.0f, position.Y + size / 2.0f, position.Z - size / 2.0f),
                new Vector4(position.X + size / 2.0f, position.Y + size / 2.0f, position.Z + size / 2.0f),
                new Vector4(position.X - size / 2.0f, position.Y + size / 2.0f, position.Z + size / 2.0f) };
            Edges = new List<Edge> { new Edge(0,1), new Edge(1,2), new Edge(2,3), new Edge(3,0),
                new Edge(0,4), new Edge(1,5), new Edge(2,6), new Edge(3,7),
                new Edge(4,5), new Edge(5,6), new Edge(6,7), new Edge(7,4)};
            this.width = position.X;
            this.height = position.Y;
            this.depth = position.Z;
        }

        public Voxel(float width, float height, float depth, Vector4 position, float size): this(position, size)
        {
            this.width = width;
            this.height = height;
            this.depth = depth;
        }

        public System.Windows.Media.Media3D.Point3D Point { get => new System.Windows.Media.Media3D.Point3D(width, height, depth); }

        public (float w, float h, float d) GetCoords(float cos, float sin)
        {
            return (cos * width - sin * depth, height, cos * depth + sin * width);
        }

        public override _3DObject Clone()
        {
            throw new NotImplementedException();
        }

        public override _3DObject CloneMirrored()
        {
            throw new NotImplementedException();
        }

        public override void Render(Renderer renderer, Matrix4 projView, int width, int height)
        {
            renderer.RenderVoxel(this, projView, width, height, Color);
        }

        public override void RenderSterographic(Renderer renderer, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            renderer.RenderStereographicVoxel(this, projView, leftProjView, rightProjView, width, height);
        }

        public override string Save()
        {
            return "";
        }
    }
}
