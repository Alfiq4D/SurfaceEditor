using System.Collections.Generic;
using Engine.Interfaces;
using Engine.Utilities;
using ObjParser;

namespace Engine.Models
{
    public class StaticObjModel : ObjModel, IObjectable
    {
        public Obj ObjOriginalObject { get; set; }

        public StaticObjModel(Vector4 initialPosition, Vector4 rotation, Vector4 scale) : base(initialPosition, rotation, scale)
        {
        }

        public StaticObjModel(Obj obj): base(Vector4.Zero(), Vector4.Zero(), new Vector4(1, 1, 1))
        {
            Name = "Object" + counter++.ToString();
            List<Vector4> points = CreatePointsFromVertices(obj.VertexList);
            List<Edge> edges = CreateEdgesFromFaces(obj.FaceList);
            Edges = edges;
            Points = points;
            ObjOriginalObject = obj;
        }

        public StaticObjModel(List<Edge> edges, List<Vector4> points): base(Vector4.Zero(), Vector4.Zero(), new Vector4(1,1,1))
        {
            Name = "Object" + counter++.ToString();
            Edges = edges;
            Points = points;
        }

        public override void Render(Renderer renderer, Matrix4 projView, int width, int height)
        {
            renderer.RenderObj(this, projView, width, height, Color);
        }

        public override void RenderSterographic(Renderer renderer, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            renderer.RenderSterographicObj(this, projView, leftProjView, rightProjView, width, height);
        }

        public override string Save()
        {
            return "";
        }

        public override _3DObject Clone()
        {
            StaticObjModel objModel = new StaticObjModel(Edges, Points);
            objModel.ModelScale.X = ModelScale.X;
            objModel.ModelScale.Y = ModelScale.Y;
            objModel.ModelScale.Z = ModelScale.Z;
            objModel.Position.X = Position.X;
            objModel.Position.Y = Position.Y;
            objModel.Position.Z = -Position.Z;
            objModel.Rotation.X = Rotation.X;
            objModel.Rotation.Y = Rotation.Y;
            objModel.Rotation.Z = Rotation.Z;
            objModel.ObjOriginalObject = ObjOriginalObject;
            return objModel;
        }

        public override _3DObject CloneMirrored()
        {
            List<Vector4> points = new List<Vector4>();
            for (int i = 0; i < Points.Count; i++)
            {
                points.Add(new Vector4(Points[i].X, Points[i].Y, -Points[i].Z));
            }
            StaticObjModel objModel = new StaticObjModel(Edges, points);
            objModel.ModelScale.X = ModelScale.X;
            objModel.ModelScale.Y = ModelScale.Y;
            objModel.ModelScale.Z = ModelScale.Z;
            objModel.Position.X = Position.X;
            objModel.Position.Y = Position.Y;
            objModel.Position.Z = -Position.Z;
            objModel.Rotation.X = Rotation.X;
            objModel.Rotation.Y = Rotation.Y;
            objModel.Rotation.Z = Rotation.Z;
            objModel.ObjOriginalObject = ObjOriginalObject;
            return objModel;
        }

        public void SaveToObj(string fileName)
        {
            for (int i = 0; i < ObjOriginalObject.VertexList.Count; ++i)
            {
                var p = ModelMatrix * Points[i];
                ObjOriginalObject.VertexList[i].X = p.X;
                ObjOriginalObject.VertexList[i].Y = p.Y;
                ObjOriginalObject.VertexList[i].Z = p.Z;
            }
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            ObjOriginalObject.WriteObjFile(fileName, null);
        }
    }
}
