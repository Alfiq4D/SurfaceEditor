using Engine.Interfaces;
using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Engine.Models
{
    public class Cursor3D : _3DObject, INotifyPropertyChanged
    {
        public enum Mode
        {
            normal,
            addVoxels,
            removeVoxels
        }

        public _3DObject selectedObject;

        public Mode mode;

        public Cursor3D(Vector4 position) : base(position, new Vector4(0, 0, 0), new Vector4(1, 1, 1))
        {
            selectedObject = null; ;
            List<Vector4> p = new List<Vector4>
            {
                new Vector4(-0.5f,0,0),
                new Vector4(0.5f,0,0),
                new Vector4(0, -0.5f, 0),
                new Vector4(0, 0.5f, 0),
                new Vector4(0, 0, -0.5f),
                new Vector4(0, 0, 0.5f),
            };
            Points = p;
            Edges = new List<Edge>
            {
                new Edge(0, 1),
                new Edge(2, 3),
                new Edge(4, 5)
            };
            mode = Mode.normal;
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
            renderer.RenderCursor(this, projView, width, height);
        }

        public override void RenderSterographic(Renderer renderer, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            renderer.RenderStereographicCursor(this, projView, leftProjView, rightProjView, width, height);
        }

        public override string Save()
        {
            return "";
        }

        public bool MoveCursorWorld(float x, float y, float z, bool symmetryEnabled, IEnumerable<ISelectable> pointsToGroup)
        {
            bool sceneChanged = false;
            if (x != 0)
            {
                Position.X += x;
                if (selectedObject != null)
                {
                    MoveCursorSelectedObject(x, 0, 0, symmetryEnabled, pointsToGroup);
                    sceneChanged = true;
                }
            }
            if (y != 0)
            {
                Position.Y += y;
                if (selectedObject != null)
                {
                    MoveCursorSelectedObject(0, y, 0, symmetryEnabled, pointsToGroup);
                    sceneChanged = true;
                }
            }
            if (z != 0)
            {
                Position.Z += z;
                if (selectedObject != null)
                {
                    MoveCursorSelectedObject(0, 0, z, symmetryEnabled, pointsToGroup);
                    sceneChanged = true;
                }
            }
            return sceneChanged;
        }

        private void MoveCursorSelectedObject(float x, float y, float z, bool symmetryEnabled, IEnumerable<ISelectable> pointsToGroup)
        {
            if (selectedObject is ISelectable p)
                if (p.IsSelectedToGroup)
                {
                    foreach (_3DObject obj in pointsToGroup)
                    {
                        obj.Position.X += x;
                        obj.Position.Y += y;
                        obj.Position.Z += z;
                        if (symmetryEnabled && obj is ArtificialPoint3D pointF)
                            if (pointF.SymmetricalPoint != null)
                            {
                                if (pointF != pointF.SymmetricalPoint)
                                {
                                    pointF.SymmetricalPoint.Position.X += x;
                                    pointF.SymmetricalPoint.Position.Y += y;
                                }
                                pointF.SymmetricalPoint.Position.Z -= z;
                            }
                    }
                    return;
                }

            selectedObject.Position.X += x;
            selectedObject.Position.Y += y;
            selectedObject.Position.Z += z;
            if (symmetryEnabled && selectedObject is ArtificialPoint3D point)
                if (point.SymmetricalPoint != null)
                {
                    if (point != point.SymmetricalPoint)
                    {
                        point.SymmetricalPoint.Position.X += x;
                        point.SymmetricalPoint.Position.Y += y;
                    }
                    point.SymmetricalPoint.Position.Z -= z;
                }
        }
    }
}
