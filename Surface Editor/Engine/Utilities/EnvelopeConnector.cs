using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Utilities
{
    public class EnvelopeConnector
    {
        public List<Vector4> Positions { get; set; }
        private bool moveInDirection;
        private Vector4 moveDirection;

        public EnvelopeConnector()
        {
            Positions = new List<Vector4>();
            moveInDirection = false;
        }

        public void Clear()
        {
            Positions.Clear();
        }

        public void ConnectIntersetcion(List<Vector4> points, int consecutiveNumber = 1, bool reverse = false)
        {
            if (Positions.Count == 0)
            {
                if (reverse)
                    points.Reverse();
                Positions.AddRange(points);
            }
            else
            {
                if (reverse)
                    points.Reverse();
                HandleMoveDir(points);
                bool found = false;
                Vector4 v = new Vector4();
                int p1 = Positions.Count - 2;
                int p2 = 1;
                for (; p1 >= 0 && !found; --p1)
                {
                    for (p2 = 1; p2 < points.Count; ++p2)
                    {
                        if (FindIntersection(Positions[p1], Positions[p1 + 1], points[p2 - 1], points[p2], out v))
                        {
                            consecutiveNumber--;
                            if (consecutiveNumber == 0)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                }
                ++p1;
                if (!found)
                    throw new Exception("No intersection!");
                Positions.RemoveRange(p1 + 1, Positions.Count - p1 - 1);
                Positions.Add(v);
                Positions.AddRange(points.Skip(p2));
            }
        }

        public bool AddDirectedConnectionX(List<Vector4> points, Vector4 direction, float xLimit, int consecutiveNumber = 1)
        {
            bool found = false;
            Vector4 v = new Vector4();
            int p1 = Positions.Count - 2;
            int p2 = 1;
            for (; p1 >= 0 && !found; --p1)
            {
                for (p2 = 1; p2 < points.Count; ++p2)
                {
                    if (FindIntersection(Positions[p1], Positions[p1 + 1], points[p2 - 1], points[p2], out v))
                    {
                        consecutiveNumber--;
                        if (consecutiveNumber == 0)
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }
            ++p1;
            if (!found)
                return false;
            Positions.RemoveRange(p1 + 1, Positions.Count - p1 - 1);
            Positions.Add(v);
            Vector4 prev = points[p2];
            if (p2 < points.Count - 1 && (points[p2 + 1] - points[p2]) * direction > 0)
            {
                p2 = (p2 + 1) % points.Count;
                while (points[p2].X < xLimit)
                {
                    Positions.Add(points[p2]);
                    prev = points[p2];
                    p2 = (p2 + 1) % points.Count;
                    if (prev.X > points[p2].X)
                        break;
                }
                Positions.Add(new Vector4(xLimit, prev.Y, prev.Z));
            }
            else
            {
                p2 = (p2 - 1 + points.Count) % points.Count;
                while (points[p2].X < xLimit)
                {
                    Positions.Add(points[p2]);
                    prev = points[p2];
                    p2 = (p2 - 1 + points.Count) % points.Count;
                    if (prev.X > points[p2].X)
                        break;
                }
                Positions.Add(new Vector4(xLimit, prev.Y, prev.Z));
            }
            return true;
        }

        public bool AddDirectedConnectionY(List<Vector4> points, Vector4 direction, float yLimit, int consecutiveNumber = 1)
        {
            bool found = false;
            Vector4 v = new Vector4();
            int p1 = Positions.Count - 2;
            int p2 = 1;
            for (; p1 >= 0 && !found; --p1)
            {
                for (p2 = 1; p2 < points.Count; ++p2)
                {
                    if (FindIntersection(Positions[p1], Positions[p1 + 1], points[p2 - 1], points[p2], out v))
                    {
                        consecutiveNumber--;
                        if (consecutiveNumber == 0)
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }
            ++p1;
            if (!found)
                return false;
            Positions.RemoveRange(p1 + 1, Positions.Count - p1 - 1);
            Positions.Add(v);
            Vector4 prev = points[p2];
            if (p2 < points.Count - 1 && (points[p2 + 1] - points[p2]) * direction > 0)
            {
                p2 = (p2 + 1) % points.Count;
                while (points[p2].Y > yLimit)
                {
                    Positions.Add(points[p2]);
                    prev = points[p2];
                    p2 = (p2 + 1) % points.Count;
                    if (prev.Y < points[p2].Y)
                        break;
                }
                Positions.Add(new Vector4(prev.X, yLimit, prev.Z));
            }
            else
            {
                p2 = (p2 - 1 + points.Count) % points.Count;
                while (points[p2].Y > yLimit)
                {
                    Positions.Add(points[p2]);
                    prev = points[p2];
                    p2 = (p2 - 1 + points.Count) % points.Count;
                    if (prev.Y < points[p2].Y)
                        break;
                }
                Positions.Add(new Vector4(prev.X, yLimit, prev.Z));
            }
            return true;
        }

        public void AddIntersection(List<Vector4> points)
        {
            AdjustDirection(points);
            HandleMoveDir(points);
            Positions.AddRange(points);
        }

        public void MoveDirBefeoreNext(float value, Vector4 direction)
        {
            moveInDirection = true;
            direction = direction.Normalized();
            moveDirection = value * direction;
        }

        public void MoveDir(float value, Vector4 direction)
        {
            Vector4 pos = Positions.Last();
            Vector4 prevPos = pos + value * direction;
            Positions.Add(prevPos);
        }

        public void ConnectEnvelope(bool intersecting = true)
        {
            for (int i = Positions.Count - 2; i >= 0; i--)
            {
                if (Vector4.Distance(Positions[i] , Positions[i + 1]) < 0.0001)
                    Positions.RemoveAt(i + 1);
            }
            if (!intersecting)
            {
                var dir1 = Positions[0] - Positions[2];
                Positions.Insert(0, Positions[0] + dir1 * 100);
                var dir2 = Positions[Positions.Count - 1] - Positions[Positions.Count - 3];
                Positions.Add(Positions.Last() + dir2 * 100);
            }
            bool found = false;
            Vector4 v = new Vector4();
            int p1 = Positions.Count - 2;
            int p2 = 1;
            for (; p1 >= 0 && !found; --p1)
            {
                for (p2 = 1; p2 < Positions.Count; ++p2)
                {
                    if (p1 != p2 && p1 + 1 != p2 && p1 + 1 != p2 - 1 && p1 != p2 - 1)
                    {
                        if (FindIntersection(Positions[p1], Positions[p1 + 1], Positions[p2 - 1], Positions[p2], out v))
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }
            ++p1;
            if (!found)
                throw new Exception("No intersection!");
            Positions.RemoveRange(p1 + 1, Positions.Count - p1 - 1);
            Positions.Add(v);
            Positions.RemoveRange(0, p2);
            Positions.Insert(0, v);
        }

        private void HandleMoveDir(List<Vector4> points)
        {
            if (moveInDirection)
            {
                moveInDirection = false;
                Vector4 pos = points.First();
                Vector4 prevPos = pos - moveDirection;
                points.Insert(0, prevPos);
            }
        }

        private bool FindIntersection(Vector4 start1, Vector4 end1, Vector4 start2, Vector4 end2, out Vector4 intersection)
        {
            intersection = Vector4.Zero();
            var segment1 = end1 - start1;
            var segment2 = end2 - start2;
            var s = (-segment1.Y * (start1.X - start2.X) + segment1.X * (start1.Y - start2.Y)) / (-segment2.X * segment1.Y + segment1.X * segment2.Y);
            var t = (segment2.X * (start1.Y - start2.Y) - segment2.Y * (start1.X - start2.X)) / (-segment2.X * segment1.Y + segment1.X * segment2.Y);
            if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
            {
                intersection = (1 - t) * start1 + t * end1;
                return true;
            }
            return false;
        }

        private void AdjustDirection(List<Vector4> points)
        {
            var first = points.First();
            var last = points.Last();
            if (Vector4.SquareDistance(Positions.Last(), first) > Vector4.SquareDistance(Positions.Last(), last))
                points.Reverse();
        }
    }
}
