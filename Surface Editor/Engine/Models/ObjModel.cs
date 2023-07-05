using Engine.Utilities;
using System.Collections.Generic;

namespace Engine.Models
{
    public abstract class ObjModel: _3DObject
    {
        public ObjModel(Vector4 initialPosition, Vector4 rotation, Vector4 scale) : base(initialPosition,rotation, scale) { }

        public List<Vector4> CreatePointsFromVertices(List<ObjParser.Types.Vertex> vertices)
        {
            List<Vector4> points = new List<Vector4>();
            foreach (ObjParser.Types.Vertex vertex in vertices)
            {
                points.Add(new Vector4((float)vertex.X, (float)vertex.Y, (float)vertex.Z));
            }
            return points;
        }

        public List<Edge> CreateEdgesFromFaces(List<ObjParser.Types.Face> faces)
        {
            List<Edge> edges = new List<Edge>();
            foreach (ObjParser.Types.Face face in faces)
            {
                for (int i = 0; i < face.VertexIndexList.Length - 1; i++)
                {
                    Edge e = new Edge(face.VertexIndexList[i] - 1, face.VertexIndexList[i + 1] - 1);
                    if (!CheckIfEdgeAlreadyExist(e, edges))
                        edges.Add(e);
                }
            }
            return edges;
        }

        public bool CheckIfEdgeAlreadyExist(Edge e, List<Edge> edges)
        {
            foreach (Edge edge in edges)
            {
                if ((edge.Index1 == e.Index1 && edge.Index2 == e.Index2)
                    || (edge.Index1 == e.Index2 && edge.Index2 == e.Index1))
                    return true;
            }
            return false;
        }
    }
}
