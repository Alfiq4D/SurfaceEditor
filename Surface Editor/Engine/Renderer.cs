using Engine.Models;
using Engine.Utilities;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Engine
{
    public class Renderer: INotifyPropertyChanged
    {
        public enum Mode
        {
            Exact,
            Approximate
        }

        WriteableBitmap bitmap;
        WriteableBitmap secondBitmap;
        IntPtr backBuffer;
        readonly int backBufferStride;
        readonly byte[] blackBlock;
        public Mode mode = Mode.Approximate;
        public int ApproximationFactor
        {
            get { return approximationFactor; }
            set
            {
                if (approximationFactor == value) return;
                approximationFactor = value;
                NotifyPropertyChanged();
            }
        }
        private int approximationFactor;
        Random r = new Random();
        public bool ArePointVisible { get; set; }
        private Color selectedPointColor = Colors.Orange;
        private Color selectionColor = Colors.DodgerBlue;

        public Renderer(WriteableBitmap bitmap)
        {
            this.bitmap = bitmap;
            backBuffer = bitmap.BackBuffer;
            backBufferStride = bitmap.BackBufferStride;
            secondBitmap = new WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight, 96, 96, PixelFormats.Bgr32, null);
            blackBlock = Enumerable.Repeat((byte)0, bitmap.PixelWidth * bitmap.PixelHeight * 4).ToArray();
            ArePointVisible = true;
            ApproximationFactor = 40;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void DrawPixel(int x, int y, Color color)
        {
            if (x >= 0 && y >= 0 && x < bitmap.PixelWidth && y < bitmap.PixelHeight)
                unsafe
                {
                    // Get a pointer to the back buffer. 
                    int pBackBuffer = (int)bitmap.BackBuffer;

                    // Find the address of the pixel to draw.
                    pBackBuffer += y * bitmap.BackBufferStride;
                    pBackBuffer += x * 4;

                    // Compute the pixel's color. 
                    int color_data = color.R << 16; // R
                    color_data |= color.G << 8;   // G
                    color_data |= color.B << 0;   // B 

                    // Assign the color data to the pixel.
                    *((int*)pBackBuffer) |= color_data;
                }
        }

        private Vector4 MultiplyVectorProjView(Matrix4 projView, Vector4 vector, int width, int height)
        {
            Vector4 ret = projView * vector;
            ret /= ret.W;
            ret = new Vector4(
              (ret.X + 1) * width / 2,
              height - 1 - (ret.Y + 1) * height / 2,
              ret.Z
              );
            return ret;
        }

        private IEnumerable<Vector4> MultiplyPointsProjView(Matrix4 projView, IEnumerable<Vector4> points, int width, int height)
        {
            return (projView * points).Select(p => p / p.W).Select(p => new Vector4(
                 (p.X + 1) * width / 2,
                 height - 1 - (p.Y + 1) * height / 2,
                 p.Z
                 )).ToList();
        }

        private void DrawEdges(IEnumerable<Vector4> points, IEnumerable<Edge> edges, Color color, int width, int height)
        {
            foreach (var e in edges)
            {
                var p1 = points.ElementAt(e.Index1);
                var p2 = points.ElementAt(e.Index2);
                DrawLineOnBitmap(p1, p2, width, height, color, bitmap);
            }
        }

        private void DrawEdgesStereographic(IEnumerable<Vector4> points1, IEnumerable<Vector4> points2, IEnumerable<Edge> edges, Color rightColor, Color leftColor, int width, int height)
        {
            foreach (var e in edges)
            {
                var p1 = points1.ElementAt(e.Index1);
                var p2 = points1.ElementAt(e.Index2);
                DrawLineOnBitmap(p1, p2, width, height, leftColor, bitmap);

                p1 = points2.ElementAt(e.Index1);
                p2 = points2.ElementAt(e.Index2);
                DrawLineOnBitmap(p1, p2, width, height, rightColor, secondBitmap);
            }
        }

        private void DrawLineOnBitmap(Vector4 p1, Vector4 p2, int width, int height, Color color, WriteableBitmap bitmap)
        {
            if (!(p1.Z <= -1 || p1.Z >= 1 || p2.Z <= -1 || p2.Z >= 1))
                if (!((p1.X < 0 && p2.X < 0) || (p1.X > width && p2.X > width) || (p1.Y < 0 && p2.Y < 0) || (p1.Y > height && p2.Y > height)))
                    bitmap.DrawLineBresenham((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, color);
        }

        public void Clear()
        {
            bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), blackBlock, bitmap.PixelWidth * 4, 0);
        }

        public void Render(IEnumerable<_3DObject> models, Cursor3D cursor, ICamera cam, Projection proj, int width, int height, bool isOrthographic = false)
        {
            Matrix4 projView;
            if (isOrthographic)
                projView = proj.OrtographicProjectionMatrix() * cam.ViewMatrix();
            else
                projView = proj.PerspectiveProjectionMatrix() * cam.ViewMatrix();
            Clear();
            models = models.OrderBy(m => m.Position.Z);
            foreach (_3DObject obj in models)
            {
                if (obj.IsVisible)
                    obj.Render(this, projView, width, height);
            }
            cursor.Render(this, projView, width, height);
        }

        public void RenderStereographic(IEnumerable<_3DObject> models, Cursor3D cursor, ICamera cam, Projection proj, int width, int height, bool isOrthographic = false)
        {
            Matrix4 leftProjView = proj.LeftStereoMatrix() * cam.LeftViewMatrix(proj.D / 20);
            Matrix4 rightProjView = proj.RightStereoMatrix() * cam.RightViewMatrix(proj.D / 20);
            Matrix4 projView;
            if (isOrthographic)
                projView = proj.OrtographicProjectionMatrix() * cam.ViewMatrix();
            else
                projView = proj.PerspectiveProjectionMatrix() * cam.ViewMatrix();
            Clear();
            secondBitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), blackBlock, bitmap.PixelWidth * 4, 0);
            models.OrderBy(m => m.Position.Z);
            foreach (_3DObject obj in models)
            {
                if (obj.IsVisible)
                    obj.RenderSterographic(this, projView, leftProjView, rightProjView, width, height);
            }
            cursor.RenderSterographic(this, projView, leftProjView, rightProjView, width, height);
            Rect rect = new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
            bitmap.Blit(rect, secondBitmap, rect, WriteableBitmapExtensions.BlendMode.Additive);
        }

        private void DrawCursorEdge(WriteableBitmap bitmap, Cursor3D cursor, Matrix4 projView, int width, int height, int index, Color color)
        {
            IEnumerable<Vector4> points;
            Vector4 p1, p2;
            points = MultiplyPointsProjView(projView * cursor.ModelMatrix, cursor.Points, width, height);
            p1 = points.ElementAt(cursor.Edges[index].Index1);
            p2 = points.ElementAt(cursor.Edges[index].Index2);
            DrawLineOnBitmap(p1, p2, width, height, color, bitmap);
        }

        private void SetCursorScreenPoition(Cursor3D cursor, Matrix4 projView, int width, int height)
        {
            Vector4 cursorScreenPos = MultiplyVectorProjView(projView, cursor.Position, width, height);
            if (!double.IsNaN(cursorScreenPos.X) && !double.IsNaN(cursorScreenPos.Y) && !double.IsNaN(cursorScreenPos.Z))
            {
                cursor.ScreenPosition.X = cursorScreenPos.X;
                cursor.ScreenPosition.Y = cursorScreenPos.Y;
                cursor.ScreenPosition.Z = cursorScreenPos.Z;
            }
        }

        public void RenderVoxel(Voxel voxel, Matrix4 projView, int width, int height, Color color, VoxelGrid.RenderingMode mode = VoxelGrid.RenderingMode.squares)
        {
            Vector4 point = MultiplyVectorProjView(projView, voxel.Position, width, height);
            voxel.ScreenPosition = point;

            if (mode == VoxelGrid.RenderingMode.squares)
            {
                if (!(point.Z <= -1 || point.Z >= 1 || point.X < 0 || point.X > width || point.Y < 0 || point.Y > height))
                    bitmap.FillRectangle((int)point.X - 3, (int)point.Y - 3, (int)point.X + 3, (int)point.Y + 3, color);
            }
            else if (mode == VoxelGrid.RenderingMode.qubes)
            {
                Vector4 p1, p2;
                IEnumerable<Vector4> points = MultiplyPointsProjView(projView, voxel.Points, width, height);
                foreach (var e in voxel.Edges)
                {
                    p1 = points.ElementAt(e.Index1);
                    p2 = points.ElementAt(e.Index2);
                    DrawLineOnBitmap(p1, p2, width, height, color, bitmap);
                }
            }
        }

        public void RenderStereographicVoxel(Voxel voxel, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height, bool isSelected = false, VoxelGrid.RenderingMode mode = VoxelGrid.RenderingMode.squares)
        {
            Color leftColor, rightColor;
            if (isSelected)
            {
                leftColor = Colors.Red;
                rightColor = Colors.Blue;
            }
            else
            {
                leftColor = Colors.Red;
                rightColor = Colors.Cyan;
            }
            Vector4 screenPos = MultiplyVectorProjView(projView, voxel.Position, width, height);
            voxel.ScreenPosition = screenPos;

            if (mode == VoxelGrid.RenderingMode.squares)
            {
                Vector4 point1 = MultiplyVectorProjView(leftProjView, voxel.Position, width, height);
                Vector4 point2 = MultiplyVectorProjView(rightProjView, voxel.Position, width, height);
                if (!(point1.Z <= -1 || point1.Z >= 1 || point1.X < 0 || point1.X > width || point1.Y < 0 || point1.Y > height))
                    bitmap.FillRectangle((int)point1.X - 3, (int)point1.Y - 3, (int)point1.X + 3, (int)point1.Y + 3, leftColor);
                if (!(point2.Z <= -1 || point2.Z >= 1 || point2.X < 0 || point2.X > width || point2.Y < 0 || point2.Y > height))
                    secondBitmap.FillRectangle((int)point2.X - 3, (int)point2.Y - 3, (int)point2.X + 3, (int)point2.Y + 3, rightColor);
            }
            else if (mode == VoxelGrid.RenderingMode.qubes)
            {
                Vector4 p1, p2;
                IEnumerable<Vector4> points1 = MultiplyPointsProjView(leftProjView, voxel.Points, width, height);
                IEnumerable<Vector4> points2 = MultiplyPointsProjView(rightProjView, voxel.Points, width, height);
                foreach (var e in voxel.Edges)
                {
                    p1 = points1.ElementAt(e.Index1);
                    p2 = points1.ElementAt(e.Index2);
                    DrawLineOnBitmap(p1, p2, width, height, leftColor, bitmap);

                    p1 = points2.ElementAt(e.Index1);
                    p2 = points2.ElementAt(e.Index2);
                    DrawLineOnBitmap(p1, p2, width, height, rightColor, secondBitmap);
                }
            }
        }

        public void RenderVoxelGrid(VoxelGrid grid, Matrix4 projView, int width, int height, Color color)
        {
            if (grid.isSelected)
                color = selectionColor;
            foreach (var voxel in grid.Voxels)
                if (voxel.isActive)
                    RenderVoxel(voxel, projView, width, height, color, grid.renderingMode);
        }

        public void RenderStereographicVoxelGrid(VoxelGrid grid, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            foreach (var voxel in grid.Voxels)
                if (voxel.isActive)
                    RenderStereographicVoxel(voxel, projView, leftProjView, rightProjView, width, height, grid.IsSelected, grid.renderingMode);
        }

        public void RenderObj(_3DObject model, Matrix4 projView, int width, int height, Color color)
        {
            if (model.isSelected)
                color = selectionColor;
            Matrix4 M = model.ModelMatrix;
            IEnumerable<Vector4> points = MultiplyPointsProjView(projView * M, model.Points, width, height);
            Vector4 screenPos = MultiplyVectorProjView(projView, model.Position, width, height);
            model.ScreenPosition = screenPos;
            DrawEdges(points, model.Edges, color, width, height);
        }

        public void RenderPointMesh(MeshObjModel model, Matrix4 projView, int width, int height, Color color)
        {
            if (model.isSelected)
                color = selectionColor;
            IEnumerable<Vector4> points = MultiplyPointsProjView(projView, model.ControlPoints.Select(p => p.Position), width, height);
            Vector4 screenPos = MultiplyVectorProjView(projView, model.Position, width, height);
            model.ScreenPosition = screenPos;
            for (int i = 0; i < points.Count(); i++)
            {
                model.ControlPoints[i].ScreenPosition = points.ElementAt(i);
            }
            for (int i = 0; i < points.Count(); i++)
                if (ArePointVisible && !(points.ElementAt(i).Z <= -1 || points.ElementAt(i).Z >= 1 || points.ElementAt(i).X < 0 || points.ElementAt(i).X > width || points.ElementAt(i).Y < 0 || points.ElementAt(i).Y > height))
                    bitmap.FillEllipseCentered((int)points.ElementAt(i).X, (int)points.ElementAt(i).Y, 3, 3, model.ControlPoints[i].isSelected ? selectedPointColor : color);
            DrawEdges(points, model.Edges, color, width, height);
        }

        public void RenderSterographicObj(_3DObject model, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            Color leftColor, rightColor;
            if (model.isSelected)
            {
                leftColor = Colors.Red;
                rightColor = Colors.Blue;
            }
            else
            {
                leftColor = Colors.Red;
                rightColor = Colors.Cyan;
            }
            Matrix4 M = model.ModelMatrix;
            IEnumerable<Vector4> points1 = MultiplyPointsProjView(leftProjView * M, model.Points, width, height);
            IEnumerable<Vector4> points2 = MultiplyPointsProjView(rightProjView * M, model.Points, width, height);
            Vector4 screenPos = MultiplyVectorProjView(projView, model.Position, width, height);
            model.ScreenPosition = screenPos;
            DrawEdgesStereographic(points1, points2, model.Edges, rightColor, leftColor, width, height);
        }

        public void RenderSterographicPointMesh(MeshObjModel model, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            Color leftColor, rightColor;
            if (model.isSelected)
            {
                leftColor = Colors.Red;
                rightColor = Colors.Blue;
            }
            else
            {
                leftColor = Colors.Red;
                rightColor = Colors.Cyan;
            }
            IEnumerable<Vector4> points1 = MultiplyPointsProjView(leftProjView, model.ControlPoints.Select(p => p.Position), width, height);
            IEnumerable<Vector4> points2 = MultiplyPointsProjView(rightProjView, model.ControlPoints.Select(p => p.Position), width, height);
            Vector4 screenPos = MultiplyVectorProjView(projView, model.Position, width, height);
            model.ScreenPosition = screenPos;
            foreach (var p in points1)
                if (ArePointVisible && !(p.Z <= -1 || p.Z >= 1 || p.X < 0 || p.X > width || p.Y < 0 || p.Y > height))
                    bitmap.FillEllipseCentered((int)p.X, (int)p.Y, 3, 3, leftColor);
            foreach (var p in points2)
                if (ArePointVisible && !(p.Z <= -1 || p.Z >= 1 || p.X < 0 || p.X > width || p.Y < 0 || p.Y > height))
                    secondBitmap.FillEllipseCentered((int)p.X, (int)p.Y, 3, 3, rightColor);
            DrawEdgesStereographic(points1, points2, model.Edges, rightColor, leftColor, width, height);
        }

        public void Render3DModel(_3DModel model, Matrix4 projView, int width, int height, Color color)
        {
            if (model.isSelected)
                color = selectionColor;
            Matrix4 M = model.ModelMatrix;
            IEnumerable<Vector4> points = MultiplyPointsProjView(projView * M, model.Points, width, height);
            Vector4 screenPos = MultiplyVectorProjView(projView, model.Position, width, height);
            model.ScreenPosition = screenPos;
            DrawEdges(points, model.Edges, color, width, height);
        }

        public void RenderSteroegraphic3DModel(_3DModel model, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            Color leftColor, rightColor;
            if (model.isSelected)
            {
                leftColor = Colors.Red;
                rightColor = Colors.Blue;
            }
            else
            {
                leftColor = Colors.Red;
                rightColor = Colors.Cyan;
            }
            Matrix4 M = model.ModelMatrix;
            IEnumerable<Vector4> points1 = MultiplyPointsProjView(leftProjView * M, model.Points, width, height);
            IEnumerable<Vector4> points2 = MultiplyPointsProjView(rightProjView * M, model.Points, width, height);
            Vector4 screenPos = MultiplyVectorProjView(projView, model.Position, width, height);
            model.ScreenPosition = screenPos;
            DrawEdgesStereographic(points1, points2, model.Edges, rightColor, leftColor, width, height);
        }

        public void RenderCursor(Cursor3D cursor, Matrix4 projView, int width, int height)
        {
            DrawCursorEdge(bitmap, cursor, projView, width, height, 0, Colors.Red);
            DrawCursorEdge(bitmap, cursor, projView, width, height, 1, Colors.Green);
            DrawCursorEdge(bitmap, cursor, projView, width, height, 2, Colors.Blue);
            SetCursorScreenPoition(cursor, projView, width, height);
        }

        public void RenderStereographicCursor(Cursor3D cursor, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            DrawCursorEdge(bitmap, cursor, leftProjView, width, height, 0, Colors.Red);
            DrawCursorEdge(secondBitmap, cursor, rightProjView, width, height, 0, Colors.Cyan);
            DrawCursorEdge(bitmap, cursor, leftProjView, width, height, 1, Colors.Red);
            DrawCursorEdge(secondBitmap, cursor, rightProjView, width, height, 1, Colors.Cyan);
            DrawCursorEdge(bitmap, cursor, leftProjView, width, height, 2, Colors.Red);
            DrawCursorEdge(secondBitmap, cursor, rightProjView, width, height, 2, Colors.Cyan);
            SetCursorScreenPoition(cursor, projView, width, height);
        }

        public void RenderPoint(Models.Point3D point, Matrix4 projView, int width, int height, Color color)
        {
            if (point.isSelected)
                color = selectionColor;
            if (point.IsSelectedToGroup)
                color = selectedPointColor;
            Matrix4 M = point.ModelMatrix;
            IEnumerable<Vector4> points = MultiplyPointsProjView(projView * M, point.Points, width, height);
            Vector4 screenPos = MultiplyVectorProjView(projView, point.Position, width, height);
            point.ScreenPosition = screenPos;
            foreach (var p in points)
                if (ArePointVisible && !(p.Z <= -1 || p.Z >= 1 || p.X < 0 || p.X > width || p.Y < 0 || p.Y > height))
                    bitmap.FillEllipseCentered((int)p.X, (int)p.Y, 3, 3, color);
        }

        public void RenderStereographicPoint(Models.Point3D point, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            Color leftColor, rightColor;
            if (point.isSelected)
            {
                leftColor = Colors.Red;
                rightColor = Colors.Blue;
            }
            else
            {
                leftColor = Colors.Red;
                rightColor = Colors.Cyan;
            }
            Matrix4 M = point.ModelMatrix;
            IEnumerable<Vector4> points1 = MultiplyPointsProjView(leftProjView * M, point.Points, width, height);
            IEnumerable<Vector4> points2 = MultiplyPointsProjView(rightProjView * M, point.Points, width, height);
            Vector4 screenPos = MultiplyVectorProjView(projView, point.Position, width, height);
            point.ScreenPosition = screenPos;
            foreach (var p in points1)
                if (ArePointVisible && !(p.Z <= -1 || p.Z >= 1 || p.X < 0 || p.X > width || p.Y < 0 || p.Y > height))
                    bitmap.FillEllipseCentered((int)p.X, (int)p.Y, 3, 3, leftColor);
            foreach (var p in points2)
                if (ArePointVisible && !(p.Z <= -1 || p.Z >= 1 || p.X < 0 || p.X > width || p.Y < 0 || p.Y > height))
                    secondBitmap.FillEllipseCentered((int)p.X, (int)p.Y, 3, 3, rightColor);
        }

        public void RenderBezier(BezierCurve bezier, Matrix4 projView, int width, int height, Color color)
        {
            Color baseColor = color;
            Vector4 p;
            List<Vector4> allPpoints = new List<Vector4>();
            foreach (Models.Point3D point in bezier.ControlPoints)
            {
                if (bezier.isSelected || point.isSelected)
                    color = selectionColor;
                else if (point.IsSelectedToGroup)
                    color = selectedPointColor;
                else
                    color = baseColor;
                p = MultiplyVectorProjView(projView, point.Position, width, height);
                point.ScreenPosition = p;
                if (ArePointVisible && !(p.Z <= -1 || p.Z >= 1 || p.X < 0 || p.X > width || p.Y < 0 || p.Y > height))
                    bitmap.FillEllipseCentered((int)p.X, (int)p.Y, 3, 3, color);
                allPpoints.Add(p);
            }
            float t = 0;
            if (bezier.isSelected)
                color = selectionColor;
            else
                color = baseColor;
            foreach (BezierCurve.Segment segment in bezier.Segments)
            {
                int n = segment.GetBezierPolygonCircuit();
                if (n > 0)
                {
                    if (mode == Mode.Exact)
                    {
                        for (int i = 0; i <= n; i++)
                        {
                            t = 1f * i / n;
                            Vector4 bezierPoint = segment.GetBezierValue(t);
                            bezierPoint = MultiplyVectorProjView(projView, bezierPoint, width, height);
                            if (bezierPoint.Z >= -1 && bezierPoint.Z <= 1 && bezierPoint.X > 0 && bezierPoint.X < width && bezierPoint.Y > 0 && bezierPoint.Y < height)
                                DrawPixel((int)bezierPoint.X, (int)bezierPoint.Y, color);
                        }
                    }
                    else if (mode == Mode.Approximate)
                    {
                        n /= ApproximationFactor;
                        Vector4 vector1, vector2;
                        vector1 = segment.GetBezierValue(0);
                        vector1 = MultiplyVectorProjView(projView, vector1, width, height);
                        for (int i = 1; i <= n; i++)
                        {
                            t = 1f * i / n;
                            vector2 = segment.GetBezierValue(t);
                            vector2 = MultiplyVectorProjView(projView, vector2, width, height);
                            if (!(vector1.Z <= -1 || vector1.Z >= 1 || vector2.Z <= -1 || vector2.Z >= 1))
                                if (!((vector1.X < 0 && vector2.X < 0) || (vector1.X > width && vector2.X > width) || (vector1.Y < 0 && vector2.Y < 0) || (vector1.Y > height && vector2.Y > height)))
                                    bitmap.DrawLineBresenham((int)vector1.X, (int)vector1.Y, (int)vector2.X, (int)vector2.Y, color);
                            vector1 = vector2;
                        }
                    }                    
                }
            }
            if (bezier.DrawPoly)
                DrawEdges(allPpoints, bezier.Edges, color, width, height);
        }

        public void RenderBezierC2(BezierCurveC2 bezier, Matrix4 projView, int width, int height, Color color)
        {
            Color baseColor = color;
            List<Vector4> allPpoints = new List<Vector4>();
            List<_3DObject> controlPoints = new List<_3DObject>();
            if (bezier.representation == BezierCurveC2.Representation.Bezier)
                controlPoints = bezier.bezierPoints.Cast<_3DObject>().ToList();
            else if (bezier.representation == BezierCurveC2.Representation.BSpline)
                controlPoints = bezier.ControlPoints.Cast<_3DObject>().ToList();
            foreach (_3DObject point in controlPoints)
            {
                if (bezier.isSelected || point.isSelected)
                    color = selectionColor;
                else
                    color = baseColor;
                if (point is Models.Point3D po)
                {
                    if (po.IsSelectedToGroup)
                        color = selectedPointColor;
                }
                else if (point is Models.ArtificialPoint3D fp)
                    if (fp.IsSelectedToGroup)
                        color = selectedPointColor;
                Vector4 p = MultiplyVectorProjView(projView, point.Position, width, height);
                point.ScreenPosition = p;
                if (ArePointVisible && !(p.Z <= -1 || p.Z >= 1 || p.X < 0 || p.X > width || p.Y < 0 || p.Y > height))
                    bitmap.FillEllipseCentered((int)p.X, (int)p.Y, 3, 3, color);
                allPpoints.Add(p);
            }
            float t = 0;
            List<BezierCurveC2.ISegment> s = new List<BezierCurveC2.ISegment>();
            if (bezier.representation == BezierCurveC2.Representation.Bezier)
                s = bezier.BezierSegments.Cast<BezierCurveC2.ISegment>().ToList();
            else if (bezier.representation == BezierCurveC2.Representation.BSpline)
                s = bezier.BSplineSegments.Cast<BezierCurveC2.ISegment>().ToList();
            foreach (BezierCurveC2.ISegment segment in s)
            {
                if (bezier.isSelected)
                    color = selectionColor;
                else
                    color = baseColor;
                for (int i = 0; i < segment.Points.Count; i++)
                {
                    segment.ScreenPoints[i] = MultiplyVectorProjView(projView, segment.Points[i], width, height);
                }
                int n = segment.GetPolygonCircuit();
                if (n > 0)
                {
                    if (mode == Mode.Exact)
                    {
                        byte[] bytes = BitConverter.GetBytes(r.Next(
                            BitConverter.ToInt32(new byte[] { Colors.Black.B, Colors.Black.G, Colors.Black.R, 0x00 }, 0),
                            BitConverter.ToInt32(new byte[] { Colors.White.B, Colors.White.G, Colors.White.R, 0x00 }, 0)));
                        color = Color.FromRgb(bytes[2], bytes[1], bytes[0]);
                        for (int i = 0; i <= n; i++)
                        {
                            t = 1f * i / n;
                            Vector4 bezierPoint = segment.GetValue(t);
                            bezierPoint = MultiplyVectorProjView(projView, bezierPoint, width, height);
                            if (bezierPoint.Z >= -1 && bezierPoint.Z <= 1 && bezierPoint.X > 0 && bezierPoint.X < width && bezierPoint.Y > 0 && bezierPoint.Y < height)
                                DrawPixel((int)bezierPoint.X, (int)bezierPoint.Y, color);
                        }
                    }
                    else if (mode == Mode.Approximate)
                    {
                        n /= ApproximationFactor;
                        Vector4 vector1, vector2;
                        vector1 = segment.GetValue(0);
                        vector1 = MultiplyVectorProjView(projView, vector1, width, height);
                        for (int i = 1; i <= n; i++)
                        {
                            t = 1f * i / n;
                            vector2 = segment.GetValue(t);
                            vector2 = MultiplyVectorProjView(projView, vector2, width, height);
                            if (!(vector1.Z <= -1 || vector1.Z >= 1 || vector2.Z <= -1 || vector2.Z >= 1))
                                if (!((vector1.X < 0 && vector2.X < 0) || (vector1.X > width && vector2.X > width) || (vector1.Y < 0 && vector2.Y < 0) || (vector1.Y > height && vector2.Y > height)))
                                    bitmap.DrawLineBresenham((int)vector1.X, (int)vector1.Y, (int)vector2.X, (int)vector2.Y, color);
                            vector1 = vector2;
                        }
                    }                    
                }
            }
            if (bezier.DrawPoly)
                DrawEdges(allPpoints, bezier.Edges, color, width, height);
        }

        public void RenderStereographicBezier(BezierCurve bezier, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            Color leftColor = Colors.White, rightColor = Colors.White;
            List<Vector4> allPpoints1 = new List<Vector4>();
            List<Vector4> allPpoints2 = new List<Vector4>();
            foreach (Models.Point3D point in bezier.ControlPoints)
            {
                if (bezier.isSelected || point.isSelected)
                {
                    leftColor = Colors.Red;
                    rightColor = Colors.Blue;
                }
                else
                {
                    leftColor = Colors.Red;
                    rightColor = Colors.Cyan;
                }
                Matrix4 M = point.ModelMatrix;

                Vector4 point1 = MultiplyVectorProjView(leftProjView, point.Position, width, height);
                Vector4 point2 = MultiplyVectorProjView(rightProjView, point.Position, width, height);
                Vector4 screenPos = MultiplyVectorProjView(projView, point.Position, width, height);
                point.ScreenPosition = screenPos;
                if (ArePointVisible && !(point1.Z <= -1 || point1.Z >= 1 || point1.X < 0 || point1.X > width || point1.Y < 0 || point1.Y > height))
                    bitmap.FillEllipseCentered((int)point1.X, (int)point1.Y, 3, 3, leftColor);
                if (ArePointVisible && !(point2.Z <= -1 || point2.Z >= 1 || point2.X < 0 || point2.X > width || point2.Y < 0 || point2.Y > height))
                    secondBitmap.FillEllipseCentered((int)point2.X, (int)point2.Y, 3, 3, rightColor);
                allPpoints1.Add(point1);
                allPpoints2.Add(point2);
            }
            float t = 0;
            foreach (BezierCurve.Segment segment in bezier.Segments)
            {
                if (bezier.isSelected)
                {
                    leftColor = Colors.Red;
                    rightColor = Colors.Blue;
                }
                else
                {
                    leftColor = Colors.Red;
                    rightColor = Colors.Cyan;
                }
                int n = segment.GetBezierPolygonCircuit();
                if (n > 0)
                {
                    if (mode == Mode.Exact)
                    {
                        for (int i = 0; i <= n; i++)
                        {
                            t = 1f * i / n;
                            Vector4 val = segment.GetBezierValue(t);
                            Vector4 bezierPoint1 = MultiplyVectorProjView(leftProjView, val, width, height);
                            Vector4 bezierPoint2 = MultiplyVectorProjView(rightProjView, val, width, height);
                            if (bezierPoint1.Z >= -1 && bezierPoint1.Z <= 1 && bezierPoint2.Z >= -1 && bezierPoint2.Z <= 1)
                                if (bezierPoint1.X > 0 && bezierPoint1.X < width && bezierPoint1.Y > 0 && bezierPoint1.Y < height && bezierPoint2.X > 0 && bezierPoint2.X < width && bezierPoint2.Y > 0 && bezierPoint2.Y < height)
                                {
                                    DrawPixel((int)bezierPoint1.X, (int)bezierPoint1.Y, leftColor);
                                    DrawPixel((int)bezierPoint2.X, (int)bezierPoint2.Y, rightColor);
                                }
                        }
                    }
                    else if (mode == Mode.Approximate)
                    {
                        n /= ApproximationFactor;
                        Vector4 bezierPoint1Left, bezierPoint2Left, bezierPoint1Right, bezierPoint2Right;
                        Vector4 val = segment.GetBezierValue(0);
                        bezierPoint1Left = MultiplyVectorProjView(leftProjView, val, width, height);
                        bezierPoint1Right = MultiplyVectorProjView(rightProjView, val, width, height);
                        for (int i = 1; i <= n; i++)
                        {
                            t = 1f * i / n;
                            val = segment.GetBezierValue(t);
                            bezierPoint2Left = MultiplyVectorProjView(leftProjView, val, width, height);
                            bezierPoint2Right = MultiplyVectorProjView(rightProjView, val, width, height);
                            if (bezierPoint2Left.Z >= -1 && bezierPoint2Left.Z <= 1 && bezierPoint2Right.Z >= -1 && bezierPoint2Right.Z <= 1)
                                if (bezierPoint2Left.X > 0 && bezierPoint2Left.X < width && bezierPoint2Left.Y > 0 && bezierPoint2Left.Y < height && bezierPoint2Right.X > 0 && bezierPoint2Right.X < width && bezierPoint2Right.Y > 0 && bezierPoint2Right.Y < height)
                                {
                                    bitmap.DrawLineBresenham((int)bezierPoint1Left.X, (int)bezierPoint1Left.Y, (int)bezierPoint2Left.X, (int)bezierPoint2Left.Y, leftColor);
                                    secondBitmap.DrawLineBresenham((int)bezierPoint1Right.X, (int)bezierPoint1Right.Y, (int)bezierPoint2Right.X, (int)bezierPoint2Right.Y, rightColor);
                                }
                            bezierPoint1Left = bezierPoint2Left;
                            bezierPoint1Right = bezierPoint2Right;
                        }
                    }
                }
            }
            if (bezier.DrawPoly)
                DrawEdgesStereographic(allPpoints1, allPpoints2, bezier.Edges, rightColor, leftColor, width, height);
        }

        public void RenderStereographicBezierC2(BezierCurveC2 bezier, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            Color leftColor = Colors.White, rightColor = Colors.White;
            List<Vector4> allPpoints1 = new List<Vector4>();
            List<Vector4> allPpoints2 = new List<Vector4>();
            List<_3DObject> controlPoints = new List<_3DObject>();
            if (bezier.representation == BezierCurveC2.Representation.Bezier)
                controlPoints = bezier.bezierPoints.Cast<_3DObject>().ToList();
            else if (bezier.representation == BezierCurveC2.Representation.BSpline)
                controlPoints = bezier.ControlPoints.Cast<_3DObject>().ToList();
            foreach (Models._3DObject point in controlPoints)
            {
                if (bezier.isSelected || point.isSelected)
                {
                    leftColor = Colors.Red;
                    rightColor = Colors.Blue;
                }
                else
                {
                    leftColor = Colors.Red;
                    rightColor = Colors.Cyan;
                }
                Matrix4 M = point.ModelMatrix;

                Vector4 point1 = MultiplyVectorProjView(leftProjView, point.Position, width, height);
                Vector4 point2 = MultiplyVectorProjView(rightProjView, point.Position, width, height);
                point.ScreenPosition = MultiplyVectorProjView(projView, point.Position, width, height);

                if (ArePointVisible && !(point1.Z <= -1 || point1.Z >= 1 || point1.X < 0 || point1.X > width || point1.Y < 0 || point1.Y > height))
                    bitmap.FillEllipseCentered((int)point1.X, (int)point1.Y, 3, 3, leftColor);
                if (ArePointVisible && !(point2.Z <= -1 || point2.Z >= 1 || point2.X < 0 || point2.X > width || point2.Y < 0 || point2.Y > height))
                    secondBitmap.FillEllipseCentered((int)point2.X, (int)point2.Y, 3, 3, rightColor);
                allPpoints1.Add(point1);
                allPpoints2.Add(point2);
            }
            List<BezierCurveC2.ISegment> s = new List<BezierCurveC2.ISegment>();
            if (bezier.representation == BezierCurveC2.Representation.Bezier)
                s = bezier.BezierSegments.Cast<BezierCurveC2.ISegment>().ToList();
            else if (bezier.representation == BezierCurveC2.Representation.BSpline)
                s = bezier.BSplineSegments.Cast<BezierCurveC2.ISegment>().ToList();
            float t = 0;
            foreach (BezierCurveC2.ISegment segment in s)
            {
                if (bezier.isSelected)
                {
                    leftColor = Colors.Red;
                    rightColor = Colors.Blue;
                }
                else
                {
                    leftColor = Colors.Red;
                    rightColor = Colors.Cyan;
                }
                for (int i = 0; i < segment.Points.Count; i++)
                {
                    segment.ScreenPoints[i] = MultiplyVectorProjView(projView, segment.Points[i], width, height);
                }
                int n = segment.GetPolygonCircuit();
                if (n > 0)
                {
                    if (mode == Mode.Exact)
                    {
                        for (int i = 0; i <= n; i++)
                        {
                            t = 1f * i / n;
                            Vector4 val = segment.GetValue(t);
                            Vector4 bezierPoint1 = MultiplyVectorProjView(leftProjView, val, width, height);
                            Vector4 bezierPoint2 = MultiplyVectorProjView(rightProjView, val, width, height);
                            if (bezierPoint1.Z >= -1 && bezierPoint1.Z <= 1 && bezierPoint2.Z >= -1 && bezierPoint2.Z <= 1)
                                if (bezierPoint1.X > 0 && bezierPoint1.X < width && bezierPoint1.Y > 0 && bezierPoint1.Y < height && bezierPoint2.X > 0 && bezierPoint2.X < width && bezierPoint2.Y > 0 && bezierPoint2.Y < height)
                                {
                                    DrawPixel((int)bezierPoint1.X, (int)bezierPoint1.Y, leftColor);
                                    DrawPixel((int)bezierPoint2.X, (int)bezierPoint2.Y, rightColor);
                                }
                        }
                    }
                    else if (mode == Mode.Approximate)
                    {
                        n /= ApproximationFactor;
                        Vector4 bezierPoint1Left, bezierPoint2Left, bezierPoint1Right, bezierPoint2Right;
                        Vector4 val = segment.GetValue(0);
                        bezierPoint1Left = MultiplyVectorProjView(leftProjView, val, width, height);
                        bezierPoint1Right = MultiplyVectorProjView(rightProjView, val, width, height);
                        for (int i = 1; i <= n; i++)
                        {
                            t = 1f * i / n;
                            val = segment.GetValue(t);
                            bezierPoint2Left = MultiplyVectorProjView(leftProjView, val, width, height);
                            bezierPoint2Right = MultiplyVectorProjView(rightProjView, val, width, height);
                            if (bezierPoint2Left.Z >= -1 && bezierPoint2Left.Z <= 1 && bezierPoint2Right.Z >= -1 && bezierPoint2Right.Z <= 1)
                                if (bezierPoint2Left.X > 0 && bezierPoint2Left.X < width && bezierPoint2Left.Y > 0 && bezierPoint2Left.Y < height && bezierPoint2Right.X > 0 && bezierPoint2Right.X < width && bezierPoint2Right.Y > 0 && bezierPoint2Right.Y < height)
                                {
                                    bitmap.DrawLineBresenham((int)bezierPoint1Left.X, (int)bezierPoint1Left.Y, (int)bezierPoint2Left.X, (int)bezierPoint2Left.Y, leftColor);
                                    secondBitmap.DrawLineBresenham((int)bezierPoint1Right.X, (int)bezierPoint1Right.Y, (int)bezierPoint2Right.X, (int)bezierPoint2Right.Y, rightColor);
                                }
                            bezierPoint1Left = bezierPoint2Left;
                            bezierPoint1Right = bezierPoint2Right;
                        }
                    }
                }
            }
            if (bezier.DrawPoly)
                DrawEdgesStereographic(allPpoints1, allPpoints2, bezier.Edges, rightColor, leftColor, width, height);
        }

        public void RenderInterpolationCurve(InterpolationCurve curve, Matrix4 projView, int width, int height, Color color)
        {
            Color baseColor = color;
            Vector4 p;
            List<Vector4> allPpoints = new List<Vector4>();
            foreach (Models.Point3D point in curve.ControlPoints)
            {
                if (curve.isSelected || point.isSelected)
                    color = selectionColor;
                else if (point.IsSelectedToGroup)
                    color = selectedPointColor;
                else
                    color = baseColor;
                p = MultiplyVectorProjView(projView, point.Position, width, height);
                point.ScreenPosition = p;
                if (ArePointVisible && !(p.Z <= -1 || p.Z >= 1 || p.X < 0 || p.X > width || p.Y < 0 || p.Y > height))
                    bitmap.FillEllipseCentered((int)p.X, (int)p.Y, 3, 3, color);
                allPpoints.Add(p);
            }
            if (curve.isSelected)
                color = selectionColor;
            else
                color = baseColor;
            float t = 0;
            int n = 2 * curve.GetPolygonCircuit();
            if (n > 0)
            {
                if (mode == Mode.Exact)
                {
                    for (int i = 0; i <= n; i++)
                    {
                        //t = 1f * i / n;
                        t = curve.tau[1] + (curve.tau[curve.tau.Count - 2] - curve.tau[1]) * i / n;
                        Vector4 point = curve.GetCurveValue(t);
                        point = MultiplyVectorProjView(projView, point, width, height);
                        if (point.Z >= -1 && point.Z <= 1 && point.X > 0 && point.X < width && point.Y > 0 && point.Y < height)
                            DrawPixel((int)point.X, (int)point.Y, color);
                    }
                }
                else if (mode == Mode.Approximate)
                {
                    //t = 0;
                    t = curve.tau[1];
                    n /= ApproximationFactor;
                    Vector4 vector1, vector2;
                    vector1 = curve.GetCurveValue(t);
                    vector1 = MultiplyVectorProjView(projView, vector1, width, height);
                    for (int i = 1; i <= n; i++)
                    {
                        // t = 1f * i / n;
                        t = curve.tau[1] + (curve.tau[curve.tau.Count - 2] - curve.tau[1]) * i / n;
                        vector2 = curve.GetCurveValue(t);
                        vector2 = MultiplyVectorProjView(projView, vector2, width, height);
                        if (!(vector1.Z <= -1 || vector1.Z >= 1 || vector2.Z <= -1 || vector2.Z >= 1))
                            if (!((vector1.X < 0 && vector2.X < 0) || (vector1.X > width && vector2.X > width) || (vector1.Y < 0 && vector2.Y < 0) || (vector1.Y > height && vector2.Y > height)))
                                bitmap.DrawLineBresenham((int)vector1.X, (int)vector1.Y, (int)vector2.X, (int)vector2.Y, color);
                        vector1 = vector2;
                    }
                }
            }
            if (curve.DrawPoly)
                DrawEdges(allPpoints, curve.Edges, color, width, height);
        }

        public void RenderStereographicInterpolationCurve(InterpolationCurve curve, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            Color leftColor = Colors.White, rightColor = Colors.White;
            List<Vector4> allPpoints1 = new List<Vector4>();
            List<Vector4> allPpoints2 = new List<Vector4>();
            foreach (Models.Point3D point in curve.ControlPoints)
            {
                if (curve.isSelected || point.isSelected)
                {
                    leftColor = Colors.Red;
                    rightColor = Colors.Blue;
                }
                else
                {
                    leftColor = Colors.Red;
                    rightColor = Colors.Cyan;
                }
                Matrix4 M = point.ModelMatrix;

                Vector4 point1 = MultiplyVectorProjView(leftProjView, point.Position, width, height);
                Vector4 point2 = MultiplyVectorProjView(rightProjView, point.Position, width, height);
                point.ScreenPosition = MultiplyVectorProjView(projView, point.Position, width, height);
                if (ArePointVisible && !(point1.Z <= -1 || point1.Z >= 1 || point1.X < 0 || point1.X > width || point1.Y < 0 || point1.Y > height))
                    bitmap.FillEllipseCentered((int)point1.X, (int)point1.Y, 3, 3, leftColor);
                if (ArePointVisible && !(point2.Z <= -1 || point2.Z >= 1 || point2.X < 0 || point2.X > width || point2.Y < 0 || point2.Y > height))
                    secondBitmap.FillEllipseCentered((int)point2.X, (int)point2.Y, 3, 3, rightColor);
                allPpoints1.Add(point1);
                allPpoints2.Add(point2);
            }
            float t = 0;
            if (curve.isSelected)
            {
                leftColor = Colors.Red;
                rightColor = Colors.Blue;
            }
            else
            {
                leftColor = Colors.Red;
                rightColor = Colors.Cyan;
            }
            int n = 2 * curve.GetPolygonCircuit();
            if (n > 0)
            {
                if (mode == Mode.Exact)
                {
                    for (int i = 0; i <= n; i++)
                    {
                        t = curve.tau[1] + (curve.tau[curve.tau.Count - 2] - curve.tau[1]) * i / n;
                        Vector4 val = curve.GetCurveValue(t);
                        Vector4 bezierPoint1 = MultiplyVectorProjView(leftProjView, val, width, height);
                        Vector4 bezierPoint2 = MultiplyVectorProjView(rightProjView, val, width, height);
                        if (bezierPoint1.Z >= -1 && bezierPoint1.Z <= 1 && bezierPoint2.Z >= -1 && bezierPoint2.Z <= 1)
                            if (bezierPoint1.X > 0 && bezierPoint1.X < width && bezierPoint1.Y > 0 && bezierPoint1.Y < height && bezierPoint2.X > 0 && bezierPoint2.X < width && bezierPoint2.Y > 0 && bezierPoint2.Y < height)
                            {
                                DrawPixel((int)bezierPoint1.X, (int)bezierPoint1.Y, leftColor);
                                DrawPixel((int)bezierPoint2.X, (int)bezierPoint2.Y, rightColor);
                            }
                    }
                }
                else if (mode == Mode.Approximate)
                {
                    n /= ApproximationFactor;
                    Vector4 bezierPoint1Left, bezierPoint2Left, bezierPoint1Right, bezierPoint2Right;
                    Vector4 val = curve.GetCurveValue(t);
                    bezierPoint1Left = MultiplyVectorProjView(leftProjView, val, width, height);
                    bezierPoint1Right = MultiplyVectorProjView(rightProjView, val, width, height);
                    for (int i = 1; i <= n; i++)
                    {
                        t = curve.tau[1] + (curve.tau[curve.tau.Count - 2] - curve.tau[1]) * i / n;
                        val = curve.GetCurveValue(t);
                        bezierPoint2Left = MultiplyVectorProjView(leftProjView, val, width, height);
                        bezierPoint2Right = MultiplyVectorProjView(rightProjView, val, width, height);
                        if (bezierPoint2Left.Z >= -1 && bezierPoint2Left.Z <= 1 && bezierPoint2Right.Z >= -1 && bezierPoint2Right.Z <= 1)
                            if (bezierPoint2Left.X > 0 && bezierPoint2Left.X < width && bezierPoint2Left.Y > 0 && bezierPoint2Left.Y < height && bezierPoint2Right.X > 0 && bezierPoint2Right.X < width && bezierPoint2Right.Y > 0 && bezierPoint2Right.Y < height)
                            {
                                bitmap.DrawLineBresenham((int)bezierPoint1Left.X, (int)bezierPoint1Left.Y, (int)bezierPoint2Left.X, (int)bezierPoint2Left.Y, leftColor);
                                secondBitmap.DrawLineBresenham((int)bezierPoint1Right.X, (int)bezierPoint1Right.Y, (int)bezierPoint2Right.X, (int)bezierPoint2Right.Y, rightColor);
                            }
                        bezierPoint1Left = bezierPoint2Left;
                        bezierPoint1Right = bezierPoint2Right;
                    }

                }
                if (curve.DrawPoly)
                    DrawEdgesStereographic(allPpoints1, allPpoints2, curve.Edges, rightColor, leftColor, width, height);
            }
        }

        public void RenderIntersectionCurve(IntersectionCurve curve, Matrix4 projView, int width, int height, Color color)
        {
            Color baseColor = color;
            List<Vector4> allPoints = new List<Vector4>();

            allPoints = MultiplyPointsProjView(projView, curve.Points, width, height).ToList();

            if (curve.isSelected)
                color = selectionColor;
            else
                color = baseColor;
            foreach (var p in allPoints)
                if (ArePointVisible && !(p.Z <= -1 || p.Z >= 1 || p.X < 0 || p.X > width || p.Y < 0 || p.Y > height))
                    bitmap.FillEllipseCentered((int)p.X, (int)p.Y, 2, 2, color);

            DrawEdges(allPoints, curve.Edges, color, width, height);
        }

        public void RenderStereographicIntersectionCurve(IntersectionCurve curve, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            Color leftColor = Colors.White, rightColor = Colors.White;
            List<Vector4> allPoints1 = new List<Vector4>();
            List<Vector4> allPoints2 = new List<Vector4>();
            if (curve.isSelected)
            {
                leftColor = Colors.Red;
                rightColor = Colors.Blue;
            }
            else
            {
                leftColor = Colors.Red;
                rightColor = Colors.Cyan;
            }
            allPoints1 = MultiplyPointsProjView(leftProjView, curve.Points, width, height).ToList();
            allPoints2 = MultiplyPointsProjView(rightProjView, curve.Points, width, height).ToList();

            foreach (var point1 in allPoints1)
                if (ArePointVisible && !(point1.Z <= -1 || point1.Z >= 1 || point1.X < 0 || point1.X > width || point1.Y < 0 || point1.Y > height))
                    bitmap.FillEllipseCentered((int)point1.X, (int)point1.Y, 2, 2, leftColor);
            foreach (var point2 in allPoints2)
                if (ArePointVisible && !(point2.Z <= -1 || point2.Z >= 1 || point2.X < 0 || point2.X > width || point2.Y < 0 || point2.Y > height))
                    secondBitmap.FillEllipseCentered((int)point2.X, (int)point2.Y, 2, 2, rightColor);

            DrawEdgesStereographic(allPoints1, allPoints2, curve.Edges, rightColor, leftColor, width, height);
        }

        private void DrawSinglePatch(SurfacePatch patch, SurfacePatch.SinglePatch singlePatch, Matrix4 projView, int width, int height, int n, Color color)
        {
            float t = 0;
            float stepU = patch.GetStepU();
            for (float u = 0; u <= 1; u += stepU)
            {
                t = 0;
                Vector4 vector1, vector2;
                vector1 = singlePatch.GetPatchTrimmingValue(u, t);
                if (vector1 != null)
                {
                    vector1 = MultiplyVectorProjView(projView, vector1, width, height);
                }
                for (int i = 1; i <= n; i++)
                {
                    t = 1f * i / n;
                    vector2 = singlePatch.GetPatchTrimmingValue(u, t);
                    if (vector2 != null)
                    {
                        vector2 = MultiplyVectorProjView(projView, vector2, width, height);
                    }
                    if (vector1 != null && vector2 != null && !(vector1.Z <= -1 || vector1.Z >= 1 || vector2.Z <= -1 || vector2.Z >= 1))
                        if (!((vector1.X < 0 && vector2.X < 0) || (vector1.X > width && vector2.X > width) || (vector1.Y < 0 && vector2.Y < 0) || (vector1.Y > height && vector2.Y > height)))
                            bitmap.DrawLineBresenham((int)vector1.X, (int)vector1.Y, (int)vector2.X, (int)vector2.Y, color);
                    vector1 = vector2;
                }
            }
            float stepV = patch.GetStepV();
            for (float v = 0; v <= 1; v += stepV)
            {
                t = 0;
                Vector4 vector1, vector2;
                vector1 = singlePatch.GetPatchTrimmingValue(t, v);
                if (vector1 != null)
                {
                    vector1 = MultiplyVectorProjView(projView, vector1, width, height);
                }
                for (int i = 1; i <= n; i++)
                {
                    t = 1f * i / n;
                    vector2 = singlePatch.GetPatchTrimmingValue(t, v);
                    if (vector2 != null)
                    {
                        vector2 = MultiplyVectorProjView(projView, vector2, width, height);
                    }
                    if (vector1 != null && vector2 != null && !(vector1.Z <= -1 || vector1.Z >= 1 || vector2.Z <= -1 || vector2.Z >= 1))
                        if (!((vector1.X < 0 && vector2.X < 0) || (vector1.X > width && vector2.X > width) || (vector1.Y < 0 && vector2.Y < 0) || (vector1.Y > height && vector2.Y > height)))
                            bitmap.DrawLineBresenham((int)vector1.X, (int)vector1.Y, (int)vector2.X, (int)vector2.Y, color);
                    vector1 = vector2;
                }
            }
        }

        private void DrawStereographicSinglePatch(SurfacePatch patch, SurfacePatch.SinglePatch singlePatch, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height, int n, Color leftColor, Color rightColor)
        {
            float t;
            float stepU = patch.GetStepU();
            for (float u = 0; u <= 1; u += stepU)
            {
                t = 0;
                Vector4 vector1Left, vector1Right, vector2Left, vector2Right;
                Vector4 val = singlePatch.GetPatchValue(u, t);
                vector1Left = MultiplyVectorProjView(leftProjView, val, width, height);
                vector1Right = MultiplyVectorProjView(rightProjView, val, width, height);
                for (int i = 1; i <= n; i++)
                {
                    t = 1f * i / n;
                    val = singlePatch.GetPatchValue(u, t);
                    vector2Left = MultiplyVectorProjView(leftProjView, val, width, height);
                    vector2Right = MultiplyVectorProjView(rightProjView, val, width, height);
                    if (vector2Left.Z >= -1 && vector2Left.Z <= 1 && vector2Right.Z >= -1 && vector2Right.Z <= 1)
                        if (vector2Left.X > 0 && vector2Left.X < width && vector2Left.Y > 0 && vector2Left.Y < height && vector2Right.X > 0 && vector2Right.X < width && vector2Right.Y > 0 && vector2Right.Y < height)
                        {
                            bitmap.DrawLineBresenham((int)vector1Left.X, (int)vector1Left.Y, (int)vector2Left.X, (int)vector2Left.Y, leftColor);
                            secondBitmap.DrawLineBresenham((int)vector1Right.X, (int)vector1Right.Y, (int)vector2Right.X, (int)vector2Right.Y, rightColor);
                        }
                    vector1Left = vector2Left;
                    vector1Right = vector2Right;
                }
            }
            float stepV = patch.GetStepV();
            for (float v = 0; v <= 1; v += stepV)
            {
                t = 0;
                Vector4 vector1Left, vector1Right, vector2Left, vector2Right;
                Vector4 val = singlePatch.GetPatchValue(t, v);
                vector1Left = MultiplyVectorProjView(leftProjView, val, width, height);
                vector1Right = MultiplyVectorProjView(rightProjView, val, width, height);
                for (int i = 1; i <= n; i++)
                {
                    t = 1f * i / n;
                    val = singlePatch.GetPatchValue(t, v);
                    vector2Left = MultiplyVectorProjView(leftProjView, val, width, height);
                    vector2Right = MultiplyVectorProjView(rightProjView, val, width, height);
                    if (vector2Left.Z >= -1 && vector2Left.Z <= 1 && vector2Right.Z >= -1 && vector2Right.Z <= 1)
                        if (vector2Left.X > 0 && vector2Left.X < width && vector2Left.Y > 0 && vector2Left.Y < height && vector2Right.X > 0 && vector2Right.X < width && vector2Right.Y > 0 && vector2Right.Y < height)
                        {
                            bitmap.DrawLineBresenham((int)vector1Left.X, (int)vector1Left.Y, (int)vector2Left.X, (int)vector2Left.Y, leftColor);
                            secondBitmap.DrawLineBresenham((int)vector1Right.X, (int)vector1Right.Y, (int)vector2Right.X, (int)vector2Right.Y, rightColor);
                        }
                    vector1Left = vector2Left;
                    vector1Right = vector2Right;
                }
            }
        }

        public void RenderBezierPatch(SurfacePatch patch, Matrix4 projView, int width, int height, Color color)
        {
            Color baseColor = color;
            Vector4 p;
            List<Vector4> allPpoints = new List<Vector4>();
            for (int i = 0; i < patch.ControlPoints.GetLength(0); i++)
            {
                for (int j = 0; j < patch.ControlPoints.GetLength(1); j++)
                {
                    if (patch.isSelected || patch.ControlPoints[i, j].isSelected)
                        color = selectionColor;
                    else if (patch.ControlPoints[i, j].IsSelectedToGroup)
                        color = selectedPointColor;
                    else
                        color = baseColor;
                    p = MultiplyVectorProjView(projView, patch.ControlPoints[i, j].Position, width, height);
                    patch.ControlPoints[i, j].ScreenPosition = p;
                    if (ArePointVisible && !(p.Z <= -1 || p.Z >= 1 || p.X < 0 || p.X > width || p.Y < 0 || p.Y > height))
                        bitmap.FillEllipseCentered((int)p.X, (int)p.Y, 3, 3, color);
                    allPpoints.Add(p);
                }
            }
            if (patch.isSelected)
                color = selectionColor;
            else
                color = baseColor;
            int precision;

            foreach (SurfacePatch.SinglePatch singlePatch in patch.SinglePatches)
            {
                if (mode == Mode.Exact)
                {
                    precision = 20;
                    DrawSinglePatch(patch, singlePatch, projView, width, height, precision, color);
                }
                if (mode == Mode.Approximate)
                {
                    precision = 10;
                    DrawSinglePatch(patch, singlePatch, projView, width, height, precision, color);
                }
            }
            if (patch.DrawPoly)
                DrawEdges(allPpoints, patch.Edges, color, width, height);
        }

        public void RenderBezierPatchStereographic(SurfacePatch patch, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            Color leftColor = Colors.White, rightColor = Colors.White;
            List<Vector4> allPpoints1 = new List<Vector4>();
            List<Vector4> allPpoints2 = new List<Vector4>();
            for (int i = 0; i < patch.ControlPoints.GetLength(0); i++)
            {
                for (int j = 0; j < patch.ControlPoints.GetLength(1); j++)
                {
                    if (patch.isSelected || patch.ControlPoints[i, j].isSelected)
                    {
                        leftColor = Colors.Red;
                        rightColor = Colors.Blue;
                    }
                    else
                    {
                        leftColor = Colors.Red;
                        rightColor = Colors.Cyan;
                    }
                    Matrix4 M = patch.ControlPoints[i, j].ModelMatrix;

                    Vector4 point1 = MultiplyVectorProjView(leftProjView, patch.ControlPoints[i, j].Position, width, height);
                    Vector4 point2 = MultiplyVectorProjView(rightProjView, patch.ControlPoints[i, j].Position, width, height);

                    patch.ControlPoints[i, j].ScreenPosition = MultiplyVectorProjView(projView, patch.ControlPoints[i, j].Position, width, height);
                    if (ArePointVisible && !(point1.Z <= -1 || point1.Z >= 1 || point1.X < 0 || point1.X > width || point1.Y < 0 || point1.Y > height))
                        bitmap.FillEllipseCentered((int)point1.X, (int)point1.Y, 3, 3, leftColor);
                    if (ArePointVisible && !(point2.Z <= -1 || point2.Z >= 1 || point2.X < 0 || point2.X > width || point2.Y < 0 || point2.Y > height))
                        secondBitmap.FillEllipseCentered((int)point2.X, (int)point2.Y, 3, 3, rightColor);
                    allPpoints1.Add(point1);
                    allPpoints2.Add(point2);
                }
            }
            if (patch.isSelected)
            {
                leftColor = Colors.Red;
                rightColor = Colors.Blue;
            }
            else
            {
                leftColor = Colors.Red;
                rightColor = Colors.Cyan;
            }
            foreach (SurfacePatch.SinglePatch singlePatch in patch.SinglePatches)
            {
                if (mode == Mode.Exact)
                {
                    DrawStereographicSinglePatch(patch, singlePatch, projView, leftProjView, rightProjView, width, height, 20, leftColor, rightColor);
                }
                if (mode == Mode.Approximate)
                {
                    DrawStereographicSinglePatch(patch, singlePatch, projView, leftProjView, rightProjView, width, height, 10, leftColor, rightColor);
                }
            }
            if (patch.DrawPoly)
                DrawEdgesStereographic(allPpoints1, allPpoints2, patch.Edges, rightColor, leftColor, width, height);
        }

        public void RenderGregoryPatch(GregoryPatch patch, Matrix4 projView, int width, int height, Color color)
        {
            if (patch.isSelected)
                color = selectionColor;
            IEnumerable<Vector4> points = MultiplyPointsProjView(projView, patch.netPoints, width, height);
            foreach (var p in points)
                if (patch.DrawPoly && ArePointVisible && !(p.Z <= -1 || p.Z >= 1 || p.X < 0 || p.X > width || p.Y < 0 || p.Y > height))
                    bitmap.FillEllipseCentered((int)p.X, (int)p.Y, 3, 3, color);
            foreach (SurfacePatch.SinglePatch singlePatch in patch.SinglePatches)
            {
                if (mode == Mode.Exact)
                {
                    DrawSinglePatch(patch, singlePatch, projView, width, height, 20, color);
                }
                if (mode == Mode.Approximate)
                {
                    DrawSinglePatch(patch, singlePatch, projView, width, height, 10, color);
                }
            }
            if (patch.DrawPoly)
                DrawEdges(points, patch.Edges, color, width, height);
        }

        public void RenderStereographicGregoryPatch(GregoryPatch patch, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height)
        {
            Color leftColor = Colors.White, rightColor = Colors.White;
            if (patch.isSelected)
            {
                leftColor = Colors.Red;
                rightColor = Colors.Blue;
            }
            else
            {
                leftColor = Colors.Red;
                rightColor = Colors.Cyan;
            }
            List<Vector4> allPpoints1 = new List<Vector4>();
            List<Vector4> allPpoints2 = new List<Vector4>();
            IEnumerable<Vector4> points1 = MultiplyPointsProjView(leftProjView, patch.netPoints, width, height);
            IEnumerable<Vector4> points2 = MultiplyPointsProjView(rightProjView, patch.netPoints, width, height);
            for (int i = 0; i < patch.netPoints.Count; i++)
            {
                if (patch.DrawPoly && ArePointVisible && !(points1.ElementAt(i).Z <= -1 || points1.ElementAt(i).Z >= 1 || points1.ElementAt(i).X < 0 || points1.ElementAt(i).X > width || points1.ElementAt(i).Y < 0 || points1.ElementAt(i).Y > height))
                    bitmap.FillEllipseCentered((int)points1.ElementAt(i).X, (int)points1.ElementAt(i).Y, 3, 3, leftColor);
                if (patch.DrawPoly && ArePointVisible && !(points2.ElementAt(i).Z <= -1 || points2.ElementAt(i).Z >= 1 || points2.ElementAt(i).X < 0 || points2.ElementAt(i).X > width || points2.ElementAt(i).Y < 0 || points2.ElementAt(i).Y > height))
                    secondBitmap.FillEllipseCentered((int)points2.ElementAt(i).X, (int)points2.ElementAt(i).Y, 3, 3, rightColor);
                allPpoints1.Add(points1.ElementAt(i));
                allPpoints2.Add(points2.ElementAt(i));
            }
            foreach (SurfacePatch.SinglePatch singlePatch in patch.SinglePatches)
            {
                if (mode == Mode.Exact)
                {
                    DrawStereographicSinglePatch(patch, singlePatch, projView, leftProjView, rightProjView, width, height, 20, leftColor, rightColor);
                }
                if (mode == Mode.Approximate)
                {
                    DrawStereographicSinglePatch(patch, singlePatch, projView, leftProjView, rightProjView, width, height, 10, leftColor, rightColor);
                }
            }
            if (patch.DrawPoly)
                DrawEdgesStereographic(allPpoints1, allPpoints2, patch.Edges, rightColor, leftColor, width, height);
        }
    }
}
