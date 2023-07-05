using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Models.PredefinedModels
{
    public class Grid : _3DModel
    {
        private int sizeX;
        public int SizeX
        {
            get { return sizeX; }
            set
            {
                if (sizeX == value) return;
                if (value < 0)
                    sizeX = 1;
                else
                    sizeX = value;
                Create();
                NotifyPropertyChanged();
            }
        }

        private int sizeY;
        public int SizeY
        {
            get { return sizeY; }
            set
            {
                if (sizeY == value) return;
                if (value < 0)
                    sizeY = 1;
                else
                    sizeY = value;
                Create();
                NotifyPropertyChanged();
            }
        }

        public Grid(Vector4 initialPosition, Vector4 rotation, Vector4 scale, int _sizeX, int _sizeY, int _v, int _u) : base(initialPosition, rotation, scale)
        {
            Name = "Grid" + counter++.ToString();
            UComplexity = _u;
            VComplexity = _v;
            SizeX = _sizeX;
            SizeY = _sizeY;
        }

        public override _3DObject Clone()
        {
            return new Grid(Position, Rotation, ModelScale, SizeX, SizeY, VComplexity, UComplexity);
        }

        public override _3DObject CloneMirrored()
        {
            return new Grid(new Vector4(Position.X, Position.Y, -Position.Z), Rotation, ModelScale, SizeX, SizeY, VComplexity, UComplexity);
        }

        override public _3DModel Create()
        {
            if (UComplexity > 0 && VComplexity > 0)
            {
                float x = 0;
                float y = 0;
                Edges = new List<Edge>();
                var points = new Vector4[(VComplexity + 1) * (UComplexity + 1)];
                for (int i = 0; i <= VComplexity; i++)
                {
                    x = -sizeX + i * 2 * (float)sizeX / VComplexity;
                    for (int j = 0; j <= UComplexity; j++)
                    {
                        y = -sizeY + j * 2 * (float)sizeY / UComplexity;
                        points[i * (UComplexity + 1) + j] = new Vector4(
                           x,
                           0,
                           y
                            );

                        Edges.Add(new Edge(i * (UComplexity + 1) + j, ((i + 1) % (VComplexity + 1)) * (UComplexity + 1) + j));
                        Edges.Add(new Edge(i * (UComplexity + 1) + j, i * (UComplexity + 1) + ((j + 1) % (UComplexity + 1))));
                        Points = points.ToList();
                    }
                }
            }
            return this;
        }

        public override string Save()
        {
            return "";
        }
    }
}
