namespace Engine.Utilities
{
    public interface ICamera
    {
        Vector4 CameraPosition { get; set; }

        Vector4 CameraTarget { get;}

        Vector4 UpVector { get; set; }

        Vector4 Forward { get; }

        Matrix4 ViewMatrix();

        void Move(float x, float y);

        void MovePosition(float x, float y);

        void Zoom(float value);

        void SetCameraTarget(Vector4 target);

        Matrix4 LeftViewMatrix(float offset);

        Matrix4 RightViewMatrix(float offset);
    }
}
