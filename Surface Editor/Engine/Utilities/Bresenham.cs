using System.Collections.Generic;

namespace Engine.Utilities
{
    public static class Bresenham
    {
        public static List<int[]> BresenhamLine(int x1, int y1, int x2, int y2)
        {
            List<int[]> pixels = new List<int[]>();
            int d, dx, dy, ai, bi, xi, yi;
            int x = x1, y = y1;
            // Drawing direction
            if (x1 < x2)
            {
                xi = 1;
                dx = x2 - x1;
            }
            else
            {
                xi = -1;
                dx = x1 - x2;
            }
            // Drawing direction
            if (y1 < y2)
            {
                yi = 1;
                dy = y2 - y1;
            }
            else
            {
                yi = -1;
                dy = y1 - y2;
            }
            // First pixel
            pixels.Add(new[] { x, y });
            // X axis
            if (dx > dy)
            {
                ai = (dy - dx) * 2;
                bi = dy * 2;
                d = bi - dx;
                while (x != x2)
                {
                    if (d >= 0)
                    {
                        x += xi;
                        y += yi;
                        d += ai;
                    }
                    else
                    {
                        d += bi;
                        x += xi;
                    }
                    pixels.Add(new[] { x, y });
                }
            }
            // Y axis
            else
            {
                ai = (dx - dy) * 2;
                bi = dx * 2;
                d = bi - dy;
                while (y != y2)
                {
                    if (d >= 0)
                    {
                        x += xi;
                        y += yi;
                        d += ai;
                    }
                    else
                    {
                        d += bi;
                        y += yi;
                    }
                    pixels.Add(new[] { x, y });
                }
            }
            return pixels;
        }
    }
}
