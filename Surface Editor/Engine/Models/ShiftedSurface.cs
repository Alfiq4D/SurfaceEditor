using Engine.Interfaces;
using Engine.Utilities;

namespace Engine.Models
{
    public class ShiftedSurface: IIntersectable
    {
        private readonly IIntersectable surface;
        private readonly float offset;

        public ShiftedSurface(IIntersectable surface, float offset)
        {
            this.surface = surface;
            this.offset = offset;
        }

        public bool IsClampedU => surface.IsClampedU;

        public bool IsClampedV => surface.IsClampedV;

        public float MaxU => surface.MaxU;

        public float MaxV => surface.MaxV;

        public float ClampU(float u)
        {
            return surface.ClampU(u);
        }

        public float ClampV(float v)
        {
            return surface.ClampV(v);
        }

        public Vector4 Evaluate(float u, float v)
        {
            var val = surface.Evaluate(u, v);
            var ret = val + EvaluateNormal(u, v) * offset;
            return ret;
        }

        public Vector4 EvaluateDU(float u, float v)
        {
            return surface.EvaluateDU(u, v);
        }

        public Vector4 EvaluateDV(float u, float v)
        {
            return surface.EvaluateDV(u, v);
        }

        public (float u, float v) GetPoint3DUV(Vector4 pos)
        {
            float bestU = 0, bestV = 0;
            Vector4 bestPos = Evaluate(bestU, bestV);
            float minDistance = Vector4.Distance(bestPos, pos);
            const int N = 128; //number of samples
            float stepU = (int)MaxU * 1.0f / (N - 1);
            float stepV = (int)MaxV * 1.0f / (N - 1);
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    Vector4 p = Evaluate(i * stepU, j * stepV);
                    float distance = Vector4.Distance(p, pos);
                    if (distance < minDistance)
                    {
                        bestU = i * stepU;
                        bestV = j * stepV;
                        minDistance = distance;
                    }
                }
            }
            return (bestU, bestV);

            //return surface.GetPoint3DUV(pos);
        }

        public Vector4 EvaluateNormal(float u, float v)
        {
            var d1 = EvaluateDU(u, v).Normalized();
            var d2 = EvaluateDV(u, v).Normalized();
            var n = (d1 ^ d2);
            n.W = 0;
            return n.Normalized();
        }

        public Vector4 EvaluateTrimming(float u, float v)
        {
            var val = surface.EvaluateTrimming(u, v);
            var ret = val + EvaluateNormal(u, v) * offset;
            return ret;
        }
    }
}
