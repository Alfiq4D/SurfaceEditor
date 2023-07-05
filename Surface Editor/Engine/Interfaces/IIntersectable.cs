using Engine.Utilities;

namespace Engine.Interfaces
{
    public interface IIntersectable
    {
        bool IsClampedU { get; }

        bool IsClampedV { get; }

        float ClampU(float u);

        float ClampV(float v);

        float MaxU { get; }

        float MaxV { get; }

        (float u, float v) GetPoint3DUV(Vector4 pos);

        Vector4 Evaluate(float u, float v);

        Vector4 EvaluateTrimming(float u, float v);

        Vector4 EvaluateDU(float u, float v);

        Vector4 EvaluateDV(float u, float v);

        Vector4 EvaluateNormal(float u, float v);
    }
}
