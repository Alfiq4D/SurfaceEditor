using Engine.Interfaces;
using Engine.Utilities;
using ObjParser;
using System.Collections.Generic;

namespace Engine.Models
{
    public class MeshObjModel : ObjModel, IObjectable
    {
        private Vector4 oldPosition = Vector4.Zero();
        private Vector4 oldRotation = Vector4.Zero();
        private Vector4 oldScale = new Vector4(1, 1, 1);

        public List<ArtificialPoint3D> ControlPoints;

        public override IEnumerable<_3DObject> DisplayPoints => ControlPoints;

        public Obj ObjOriginalObject { get; set; }

        public MeshObjModel(Vector4 initialPosition, Vector4 rotation, Vector4 scale) : base(initialPosition, rotation, scale)
        {
        }

        public MeshObjModel(Obj obj) : base(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1))
        {
            Name = "Object" + counter++.ToString();
            List<Vector4> points = CreatePointsFromVertices(obj.VertexList);
            List<Edge> edges = CreateEdgesFromFaces(obj.FaceList);
            Edges = edges;
            Points = points;
            ControlPoints = CreateMesh(points);
            ObjOriginalObject = obj;
        }

        public MeshObjModel(List<Edge> edges, List<Vector4> points) : base(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1))
        {
            Name = "Object" + counter++.ToString();
            Edges = edges;
            Points = points;
            ControlPoints = CreateMesh(points);
        }

        public override void Render(Renderer renderer, Matrix4 projView, int width, int height)
        {
            renderer.RenderPointMesh(this, projView, width, height, Color);
        }

        public override void RenderSterographic(Renderer renderer, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            renderer.RenderSterographicPointMesh(this, projView, leftProjView, rightProjView, width, height);
        }

        public override string Save()
        {
            return "";
        }

        public override void UpdateModelMatrix()
        {
            var scaleRatio = ModelScale / oldScale;
            ModelMatrix = Transform(Position.X, Position.Y, Position.Z) * RotateX(Rotation.X) * RotateY(Rotation.Y) * RotateZ(Rotation.Z) * Scale(scaleRatio.X, scaleRatio.Y, scaleRatio.Z) * RotateX(-oldRotation.X) * RotateY(-oldRotation.Y) * RotateZ(-oldRotation.Z)  * Transform(-oldPosition.X, -oldPosition.Y, -oldPosition.Z);
            oldPosition = new Vector4(Position.X, Position.Y, Position.Z);
            oldRotation = new Vector4(Rotation.X, Rotation.Y, Rotation.Z);
            oldScale = new Vector4(ModelScale.X, ModelScale.Y, ModelScale.Z);
            if (ControlPoints != null)
                for (int i = 0; i < ControlPoints.Count; i++)
                {
                        ControlPoints[i].UpdatePosition(ModelMatrix);
                }
        }

        public override _3DObject Clone()
        {
            List<Vector4> points = new List<Vector4>();
            for (int i = 0; i < Points.Count; i++)
            {
                points.Add(new Vector4(Points[i].X, Points[i].Y, Points[i].Z));
            }
            MeshObjModel mesh = new MeshObjModel(Edges, points);
            mesh.ModelScale.X = ModelScale.X;
            mesh.ModelScale.Y = ModelScale.Y;
            mesh.ModelScale.Z = ModelScale.Z;
            mesh.Position.X = Position.X;
            mesh.Position.Y = Position.Y;
            mesh.Position.Z = Position.Z;
            mesh.Rotation.X = Rotation.X;
            mesh.Rotation.Y = Rotation.Y;
            mesh.Rotation.Z = Rotation.Z;
            mesh.ObjOriginalObject = ObjOriginalObject;
            return mesh;
        }

        public override _3DObject CloneMirrored()
        {
            List<Vector4> points = new List<Vector4>();
            for (int i = 0; i < Points.Count; i++)
            {
                points.Add(new Vector4(Points[i].X, Points[i].Y, -Points[i].Z));
            }
            MeshObjModel mesh = new MeshObjModel(Edges, points);
            mesh.ModelScale.X = ModelScale.X;
            mesh.ModelScale.Y = ModelScale.Y;
            mesh.ModelScale.Z = ModelScale.Z;
            mesh.Position.X = Position.X;
            mesh.Position.Y = Position.Y;
            mesh.Position.Z = -Position.Z;
            mesh.Rotation.X = Rotation.X;
            mesh.Rotation.Y = Rotation.Y;
            mesh.Rotation.Z = Rotation.Z;
            mesh.ObjOriginalObject = ObjOriginalObject;
            return mesh;
        }

        private List<ArtificialPoint3D> CreateMesh(List<Vector4> points)
        {
            List<ArtificialPoint3D> point3Ds = new List<ArtificialPoint3D>();
            foreach (var v in points)
            {
                ArtificialPoint3D fp = new ArtificialPoint3D(v);
                point3Ds.Add(fp);
            }
            return point3Ds;
        }

        public void SaveToObj(string fileName)
        {
            for (int i = 0; i < ObjOriginalObject.VertexList.Count; ++i)
            {
                ObjOriginalObject.VertexList[i].X = ControlPoints[i].Position.X;
                ObjOriginalObject.VertexList[i].Y = ControlPoints[i].Position.Y;
                ObjOriginalObject.VertexList[i].Z = ControlPoints[i].Position.Z;
            }
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            ObjOriginalObject.WriteObjFile(fileName, null);
        }
    }
}
