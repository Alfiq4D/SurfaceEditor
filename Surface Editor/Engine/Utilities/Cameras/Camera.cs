using System;

namespace Engine.Utilities
{
    public class Camera : ICamera
    {
        public Vector4 CameraPosition { get; set; }

        public Vector4 CameraTarget {
            get
            {
                return CameraPosition + new Vector4(
                    (float)(Math.Sin(Pitch) * Math.Cos(Yaw)),
                    (float)Math.Cos(Pitch),
                    (float)(Math.Sin(Pitch) * Math.Sin(Yaw)),
                    0
                    );
            }
        }

        public Vector4 UpVector { get; set; }

        private readonly float minPitch = (float)(Math.PI / 1800), maxPitch = (float)(179.9 * Math.PI / 180);
        private float pitch = (float)Math.PI / 2;
        public float Pitch
        {
            get { return pitch; }
            set
            {
                if (pitch == value) return;
                if (value < minPitch) return;
                if (value > maxPitch) return;
                pitch = value;
            }
        }

        private float yaw = (float)Math.PI / 2;
        public float Yaw
        {
            get { return yaw; }
            set
            {
                if (yaw == value) return;
                if (value < 0)
                {
                    Yaw = (float)(value + 2 * Math.PI);
                    return;
                }
                if (value > 2 * Math.PI)
                {
                    Yaw = value - (float)(2 * Math.PI);
                    return;
                }
                yaw = value;
            }
        }

        public Vector4 Forward
        {
            get => CameraTarget - CameraPosition;
        }

        public Camera() { }

        public Camera(Vector4 position, float pitch, float yaw, Vector4 upvector = null)
        {
            CameraPosition = position;
            Pitch = pitch;
            Yaw = yaw;
            if (upvector == null)
                UpVector = new Vector4(0, 1, 0, 0);
            else
                UpVector = upvector;
        }

        public Matrix4 ViewMatrix()
        {
            Vector4 _upvector = UpVector.Normalized();
            Vector4 front = (CameraTarget - CameraPosition).Normalized();
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

        public Matrix4 LeftViewMatrix(float offset)
        {
            Vector4 _upvector = UpVector.Normalized();
            Vector4 front = (CameraTarget - CameraPosition).Normalized();
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
            Vector4 front = (CameraTarget - CameraPosition).Normalized();
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

        public void Zoom(float value)
        {
            CameraPosition = CameraPosition + Forward * value;
        }

        public void Move(float x, float y)
        {
            Yaw += x;
            Pitch += y;
        }

        public void MovePosition(float x, float y)
        {
            CameraPosition.X += x;
            CameraPosition.Y += y;
        }

        public void SetCameraTarget(Vector4 target)
        {
            Vector4 v = target - CameraPosition;
            Yaw = (float)(Math.Atan(v.Z / v.X) + ((v.X < 0) ? 0 : Math.PI));
            Pitch = (float)((Math.PI - Math.Atan(Math.Sqrt(v.X * v.X + v.Z * v.Z) / v.Y)) % Math.PI);
        }
    }
}