using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Utilities
{
    public static class ImprovedFloodFill
    {
        static int maxLineLength;
        public const int defaultPrecision = 32;
        public static bool[,] DoFloodFill(List<(float X, float Y)> points, float xStart, float yStart, bool xWrap = false, bool yWrap = false, int precision = defaultPrecision)
        {
            maxLineLength = (int)(precision * 0.7f);
            var pts = points.Select(p =>
            (
                x: Math.Min((int)(p.X * precision), precision - 1),
                y: Math.Min((int)(p.Y * precision), precision - 1)
            )).ToList();
            Func<int, int> xWrapF;
            if (xWrap)
                xWrapF = (x) => x < 0 ? x + precision : x % precision;
            else
                xWrapF = x => x;
            Func<int, int> yWrapF;
            if (yWrap)
                yWrapF = (x) => x < 0 ? x + precision : x % precision;
            else
                yWrapF = x => x;
            bool[,] result = new bool[precision, precision];
            bool[,] visited = new bool[precision, precision];
            DrawLines(ref result, pts, precision, xWrap, yWrap);
            Stack<(int x, int y)> stack = new Stack<(int x, int y)>();
            stack.Push((Math.Min((int)(xStart * precision), precision - 1), Math.Min((int)(yStart * precision), precision - 1)));
            while (stack.Count > 0)
            {
                var p = stack.Pop();
                var x = p.x;
                var y = p.y;
                if (x < 0 || x >= precision || y < 0 || y >= precision) continue;
                if (visited[x, y] == true) continue;
                visited[x, y] = true;
                if (result[x, y] == true) continue;
                result[x, y] = true;
                stack.Push((xWrapF(x + 1), yWrapF(y)));
                stack.Push((xWrapF(x - 1), yWrapF(y)));
                stack.Push((xWrapF(x), yWrapF(y + 1)));
                stack.Push((xWrapF(x), yWrapF(y - 1)));
            }
            return result;
        }

        public static int Wrap(int q, int precision)
        {
            return q < 0 ? q + precision : q % precision;
        }

        private static void DrawLines(ref bool[,] result, List<(int x, int y)> pts, int precision, bool xWrap, bool yWrap)
        {
            for (int i = 1; i < pts.Count; i++)
            {
                var pt1 = pts[i - 1];
                var pt2 = pts[i];
                if (xWrap)
                {
                    if (pt1.x - pt2.x > maxLineLength) { result[pt2.x, pt2.y] = true; pt2.x += precision; }
                    if (pt2.x - pt1.x > maxLineLength) { result[pt1.x, pt1.y] = true; pt1.x += precision; }

                }
                if (yWrap)
                {
                    if (pt1.y - pt2.y > maxLineLength) pt2.y += precision;
                    if (pt2.y - pt1.y > maxLineLength) pt1.y += precision;
                }
                foreach (var pt in Bresenham.BresenhamLine(pt1.x, pt1.y, pt2.x, pt2.y))
                {
                    int p0 = Wrap(pt[0], precision);
                    int p1 = Wrap(pt[1], precision);
                    result[p0, p1] = true;
                }
            }
        }
    }
}
