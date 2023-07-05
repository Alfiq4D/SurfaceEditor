using System;

namespace Engine.Utilities
{
    public class PointCamera : ICamera
    {
        public Vector4 CameraPosition { get; set; }

        public Vector4 CameraTarget { get; set; }

        public Vector4 UpVector { get; set; }

        public Vector4 Forward
        {
            get => CameraTarget - CameraPosition;
        }

        public PointCamera() { }

        public PointCamera(Vector4 position, Vector4 target, Vector4 upvector = null)
        {
            CameraPosition = position;
            CameraTarget = target;
            if (upvector == null)
                UpVector = new Vector4(0, 1, 0, 0);
            else
                UpVector = upvector;
        }

        private float radius = 10;
        private float vertical = 90;
        private int horizontal;
        private readonly int maxHorizontal = 80;
        private readonly int minHorizontal = -10;
        private readonly float zoomMultiplier = 0.16f;
        private readonly Vector4 movementDistance = new Vector4(0, 0.6f, 0);
        private readonly float rotationStep = 3f;
        private readonly float maxZoom = 50;
        private readonly float minZoom = 5;

        public Matrix4 ViewMatrix()
        {
            Vector4 _upvector = UpVector.Normalized();
            Vector4 front = -(CameraTarget - CameraPosition).Normalized();
            front.W = 0;
            Vector4 right;

            if (front == _upvector || front == _upvector * (-1))
                right = (_upvector ^ (front + new Vector4(1, 0, 0)));
            else
                right = (_upvector ^ front).Normalized();

            right.W = 0;
            Vector4 up = (front ^ right).Normalized();
            up.W = 0;
            Matrix4 m1 = new Matrix4(right, up, front, new Vector4(0, 0, 0)),
            m2 = new Matrix4(1, 0, 0, -CameraPosition.X,
                                   0, 1, 0, -CameraPosition.Y,
                                   0, 0, 1, -CameraPosition.Z,
                                   0, 0, 0, 1);
            return m1 * m2;
        }

        public void Zoom(float value)
        {
            if (value < 0 && radius > minZoom )
            {
                radius = radius / (1 + zoomMultiplier);
            }
            else if(value > 0 && radius < maxZoom)
            {
                radius = radius * (1 + zoomMultiplier);
            }
            UpdateCamraPosition();
        }

        public void Move(float x, float y)
        {
            MoveCamera(x, y);
            UpdateCamraPosition();
        }

        public void MovePosition(float x, float y)
        {
            MoveCamera(x, y);
            UpdateCamraPosition();
        }

        private void MoveCamera(float x, float y)
        {
            if (Math.Abs(y) > Math.Abs(x))
            {
                if (y > 0 && horizontal < maxHorizontal)
                {
                    horizontal++;
                    CameraPosition += movementDistance;
                }
                else if (y < 0 && horizontal > minHorizontal)
                {
                    horizontal--;
                    CameraPosition -= movementDistance;
                }
            }
            else
            {
                if (x < 0)
                    vertical = (vertical + rotationStep) % 360;
                else if (x > 0)
                    vertical = (vertical + (360 - rotationStep)) % 360;
            }
        }

        public void SetCameraTarget(Vector4 target)
        {
            CameraTarget = target;
            UpdateCamraPosition();
        }

        public Matrix4 LeftViewMatrix(float offset)
        {
            Vector4 _upvector = UpVector.Normalized();
            Vector4 front = -(CameraTarget - CameraPosition).Normalized();
            front.W = 0;
            Vector4 right;

            if (front == _upvector || front == _upvector * (-1))
                right = (_upvector ^ (front + new Vector4(1, 0, 0)));
            else
                right = (_upvector ^ front).Normalized();

            right.W = 0;
            Vector4 up = (front ^ right).Normalized();
            up.W = 0;
            var pos = CameraPosition - right * offset;
            Matrix4 m1 = new Matrix4(right, up, front, new Vector4(0, 0, 0)),
            m2 = new Matrix4(1, 0, 0, -pos.X,
                                   0, 1, 0, -pos.Y,
                                   0, 0, 1, -pos.Z,
                                   0, 0, 0, 1);
            return m1 * m2;
        }

        public Matrix4 RightViewMatrix(float offset)
        {
            Vector4 _upvector = UpVector.Normalized();
            Vector4 front = -(CameraTarget - CameraPosition).Normalized();
            front.W = 0;
            Vector4 right;

            if (front == _upvector || front == _upvector * (-1))
                right = (_upvector ^ (front + new Vector4(1, 0, 0)));
            else
                right = (_upvector ^ front).Normalized();

            right.W = 0;
            Vector4 up = (front ^ right).Normalized();
            up.W = 0;
            var pos = CameraPosition + right * offset;
            Matrix4 m1 = new Matrix4(right, up, front, new Vector4(0, 0, 0)),
            m2 = new Matrix4(1, 0, 0, -pos.X,
                                   0, 1, 0, -pos.Y,
                                   0, 0, 1, -pos.Z,
                                   0, 0, 0, 1);
            return m1 * m2;
        }

        private void UpdateCamraPosition()
        {
            double rFi = (radius * radius) / Math.Sqrt(Math.Pow(radius * Math.Cos(vertical * Math.PI / 180), 2) + Math.Pow(radius * Math.Sin(vertical * Math.PI / 180), 2));
            CameraPosition.X = CameraTarget.X + (float)(rFi * Math.Cos(vertical * Math.PI / 180));
            CameraPosition.Z = CameraTarget.Z + (float)(rFi * Math.Sin(vertical * Math.PI / 180));
        }

    }
}
