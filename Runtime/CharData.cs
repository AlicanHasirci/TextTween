using Unity.Mathematics;

namespace TextTween
{
    public struct CharData
    {
        public float3 Position { get; }
        public float2 Interval { get; }
        public float4 Bounds { get; }

        public CharData(float3 position, float2 interval, float4 bounds)
        {
            Position = position;
            Interval = interval;
            Bounds = bounds;
        }
    }
}
