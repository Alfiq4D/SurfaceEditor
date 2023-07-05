using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Utilities
{
    public static class FloodFill
    {
        const double maxLineLength = 0.7;
        public const int defaultPrecision = 100;

        public static bool[,] Fill(List<(float X, float Y)> points, float xStart, float yStart, bool xWrap = false, bool yWrap = false, int precision = defaultPrecision)
        {
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

        public static void ChangeTrimmingTable(bool[,] TrimmingTable)
        {
            for (int i = 0; i < TrimmingTable.GetLength(0); i++)
            {
                for (int j = 0; j < TrimmingTable.GetLength(1); j++)
                {
                    TrimmingTable[i, j] = !TrimmingTable[i, j];
                }
            }
        }

        private static int Wrap(int t, int precision)
        {
            return t < 0 ? t + precision : t % precision;
        }

        private static void DrawLines(ref bool[,] result, List<(int x, int y)> points, int precision, bool clampU, bool clampV)
        {
            for (int i = 1; i < points.Count; i++)
            {
                var pt1 = points[i - 1];
                var pt2 = points[i];
                if (clampU)
                {
                    if (pt1.x - pt2.x > maxLineLength) pt2.x++;
                    if (pt2.x - pt1.x > maxLineLength) pt1.x++;
                }
                if (clampV)
                {
                    if (pt1.y - pt2.y > maxLineLength) pt2.y++;
                    if (pt2.y - pt1.y > maxLineLength) pt1.y++;
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
