using Engine.Models;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Engine.Utilities
{
    public static class SpaceManager
    {
        private static readonly float selectEpsilon = 1.2f; //select by cursor
        private static readonly float mouseSelectEpsilon = 8; //select by mouse
        private static readonly float voxelSelectEpsilon = 15; //select voxel by mouse

        public static void SetPositionToPointsSet(IEnumerable<_3DObject> set, bool xEnable, bool yEnable, bool zEnable, bool symmetryEnabled, float x, float y, float z)
        {
            foreach (_3DObject p in set)
            {
                if (xEnable)
                    p.Position.X = x;
                if (yEnable)
                    p.Position.Y = y;
                if (zEnable)
                    p.Position.Z = z;

                if (symmetryEnabled && p is ArtificialPoint3D pointF)
                    if (pointF.SymmetricalPoint != null)
                    {
                        if (pointF != pointF.SymmetricalPoint)
                        {
                            if (xEnable)
                                pointF.SymmetricalPoint.Position.X = x;
                            if (yEnable)
                                pointF.SymmetricalPoint.Position.Y = y;
                            if (zEnable)
                                pointF.SymmetricalPoint.Position.Z = z;
                        }
                    }
            }
        }

        public static _3DObject SelectColosestObjectByScreenPosition(Point mousePos, IEnumerable<_3DObject> models)
        {
            _3DObject closest = null;
            foreach (var ob in models)
            {
                if (ob.IsVisible)
                {
                    if ((Math.Abs(ob.ScreenPosition.X - mousePos.X) < mouseSelectEpsilon) && (Math.Abs(ob.ScreenPosition.Y - mousePos.Y) < mouseSelectEpsilon))
                    {
                        if (closest == null)
                            closest = ob;
                        else
                        {
                            if ((Math.Abs(ob.ScreenPosition.X - mousePos.X) <= Math.Abs(closest.ScreenPosition.X - mousePos.X)) && (Math.Abs(ob.ScreenPosition.Y - mousePos.Y) <= Math.Abs(closest.ScreenPosition.Y - mousePos.Y)))
                            {
                                closest = ob;
                            }
                        }
                    }
                    foreach (var p in ob.DisplayPoints)
                    {
                        if ((Math.Abs(p.ScreenPosition.X - mousePos.X) < mouseSelectEpsilon) && (Math.Abs(p.ScreenPosition.Y - mousePos.Y) < mouseSelectEpsilon))
                        {
                            if (closest == null)
                                closest = p;
                            else
                            {
                                if ((Math.Abs(p.ScreenPosition.X - mousePos.X) <= Math.Abs(closest.ScreenPosition.X - mousePos.X)) && (Math.Abs(p.ScreenPosition.Y - mousePos.Y) <= Math.Abs(closest.ScreenPosition.Y - mousePos.Y)))
                                {
                                    closest = p;
                                }
                            }
                        }
                    }
                }
            }
            return closest;
        }

        public static _3DObject SelectColosestObjectBy3DPosition(Vector4 position, IEnumerable<_3DObject> models)
        {
            _3DObject closest = null;
            foreach (var ob in models)
            {
                if (ob.IsVisible)
                {
                    if ((Math.Abs(ob.Position.X - position.X) < selectEpsilon) && (Math.Abs(ob.Position.Y - position.Y) < selectEpsilon))
                    {
                        if (closest == null)
                            closest = ob;
                        else
                        {
                            if ((Math.Abs(ob.Position.X - position.X) <= Math.Abs(closest.Position.X - position.X)) && (Math.Abs(ob.Position.Y - position.Y) <= Math.Abs(closest.Position.Y - position.Y)))
                            {
                                closest = ob;
                            }
                        }
                    }
                    foreach (var p in ob.DisplayPoints)
                    {
                        if ((Math.Abs(p.Position.X - position.X) < selectEpsilon) && (Math.Abs(p.Position.Y - position.Y) < selectEpsilon))
                        {
                            if (closest == null)
                                closest = p;
                            else
                            {
                                if ((Math.Abs(p.Position.X - position.X) <= Math.Abs(closest.Position.X - position.X)) && (Math.Abs(p.Position.Y - position.Y) <= Math.Abs(closest.Position.Y - position.Y)))
                                {
                                    closest = p;
                                }
                            }
                        }
                    }
                }
            }
            return closest;
        }

        public static List<_3DObject> SelectAllPointsInRect(Point start, Point end, IEnumerable<_3DObject> models)
        {
            List<_3DObject> points = new List<_3DObject>();
            float minX, maxX, minY, maxY;
            minX = (float)Math.Min(start.X, end.X);
            maxX = (float)Math.Max(start.X, end.X);
            minY = (float)Math.Min(start.Y, end.Y);
            maxY = (float)Math.Max(start.Y, end.Y);
            foreach (var ob in models)
            {
                if (ob.IsVisible)
                {
                    if (ob.ScreenPosition.X > minX && ob.ScreenPosition.X < maxX && ob.ScreenPosition.Y > minY && ob.ScreenPosition.Y < maxY)
                    {
                        if (ob is Point3D || ob is ArtificialPoint3D)
                            points.Add(ob);
                    }
                    foreach (var p in ob.DisplayPoints)
                    {
                        if (p.ScreenPosition.X > minX && p.ScreenPosition.X < maxX && p.ScreenPosition.Y > minY && p.ScreenPosition.Y < maxY)
                        {
                            if (p is Point3D || p is ArtificialPoint3D)
                                points.Add(p);
                        }
                    }
                }
            }
            return points;
        }

        public static Voxel FindClosestVoxel(Point mousePos, IEnumerable<_3DObject> models, bool active)
        {
            Voxel closest = null;
            foreach (var ob in models)
            {
                if (ob is VoxelGrid grid && grid.IsVisible)
                {
                    foreach (var v in grid.Voxels)
                    {
                        if (v.isActive == active)
                        {
                            if ((Math.Abs(v.ScreenPosition.X - mousePos.X) < voxelSelectEpsilon) && (Math.Abs(v.ScreenPosition.Y - mousePos.Y) < voxelSelectEpsilon))
                            {
                                if (closest == null)
                                    closest = v;
                                else
                                {
                                    if ((Math.Abs(v.ScreenPosition.X - mousePos.X) <= Math.Abs(closest.ScreenPosition.X - mousePos.X)) && (Math.Abs(v.ScreenPosition.Y - mousePos.Y) <= Math.Abs(closest.ScreenPosition.Y - mousePos.Y)))
                                    {
                                        closest = v;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return closest;
        }
    }
}
