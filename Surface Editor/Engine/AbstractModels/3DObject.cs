using Engine.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Engine.Models
{
    public abstract class _3DObject : INotifyPropertyChanged
    {
        protected static int counter = 0;
        protected int number;
        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                if (name == value) return;
                else
                    name = value;
                NotifyPropertyChanged();
            }
        }
        private bool isVisible = true;
        public bool IsVisible
        {
            get { return isVisible; }
            set
            {
                if (isVisible == value) return;
                else
                    isVisible = value;
                NotifyPropertyChanged();
            }
        }
        private Color color = Colors.Gainsboro;
        public Color Color
        {
            get { return color; }
            set
            {
                if (color == value) return;
                else
                    color = value;
                NotifyPropertyChanged();
            }
        }

        public List<Edge> Edges { get; set; }
        public List<Vector4> Points { get; set; }
        public Vector4 Position { get; set; }
        public Vector4 ScreenPosition { get; set; }
        public Vector4 ModelScale { get; set; }
        public Vector4 Rotation { get; set; }
        public Matrix4 ModelMatrix { get; set; } = Matrix4.ElementalMatrix();
        public event PropertyChangedEventHandler PropertyChanged;
        public bool isSelected = false;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (isSelected == value) return;
                else
                    isSelected = value;
                NotifyPropertyChanged();
            }
        }
        public virtual IEnumerable<_3DObject> DisplayPoints { get { return new List<_3DObject>(); } }

        public abstract void Render(Renderer renderer, Matrix4 projView, int width, int height);
        public abstract void RenderSterographic(Renderer renderer, Matrix4 projView, Matrix4 leftProjView, Matrix4 rightProjView, int width, int height);

        protected _3DObject()
        {

        }

        protected _3DObject(Vector4 initialPosition, Vector4 rotation, Vector4 scale)
        {
            ScreenPosition = new Vector4();
            //ScreenPosition.PropertyChanged += ScreenPosition_PropertyChanged;
            Position = initialPosition;
            Position.PropertyChanged += InitialPosition_PropertyChanged;
            Rotation = rotation;
            Rotation.PropertyChanged += Rotation_PropertyChanged;
            ModelScale = scale;
            ModelScale.PropertyChanged += ModelScale_PropertyChanged;
            UpdateModelMatrix();
        }

        private void ScreenPosition_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged();
        }

        private void ModelScale_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateModelMatrix();
            NotifyPropertyChanged();
        }

        private void Rotation_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateModelMatrix();
            NotifyPropertyChanged();
        }

        public void InitialPosition_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateModelMatrix();
            NotifyPropertyChanged();
        }

        public Matrix4 Transform(float x, float y, float z)
        {
            return new Matrix4(1, 0, 0, x,
                0, 1, 0, y,
                0, 0, 1, z,
                0, 0, 0, 1);
        }

        public Matrix4 Scale(float x, float y, float z)
        {
            return new Matrix4(x, 0, 0, 0,
                0, y, 0, 0,
                0, 0, z, 0,
                0, 0, 0, 1);
        }

        public Matrix4 RotateX(float alpha)
        {
            return new Matrix4(1, 0, 0, 0,
                0, (float)Math.Cos(alpha), (float)-Math.Sin(alpha), 0,
                0, (float)Math.Sin(alpha), (float)Math.Cos(alpha), 0,
                0, 0, 0, 1); ;
        }

        public Matrix4 RotateY(float alpha)
        {
            return new Matrix4((float)Math.Cos(alpha), 0, (float)-Math.Sin(alpha), 0,
               0, 1, 0, 0,
               (float)Math.Sin(alpha), 0, (float)Math.Cos(alpha), 0,
               0, 0, 0, 1);
        }

        public Matrix4 RotateZ(float alpha)
        {
            return new Matrix4((float)Math.Cos(alpha), (float)-Math.Sin(alpha), 0, 0,
               (float)Math.Sin(alpha), (float)Math.Cos(alpha), 0, 0,
               0, 0, 1, 0,
               0, 0, 0, 1);
        }

        public virtual void UpdateModelMatrix()
        {
            ModelMatrix = Transform(Position.X, Position.Y, Position.Z) * RotateX(Rotation.X) * RotateY(Rotation.Y) * RotateZ(Rotation.Z) * Scale(ModelScale.X, ModelScale.Y, ModelScale.Z);
        }

        protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return Name;
        }

        public abstract string Save();

        public abstract _3DObject Clone();

        public abstract _3DObject CloneMirrored();
    }
}
