namespace TextTween.Extensions
{
    using System.Runtime.CompilerServices;
    using Unity.Mathematics;
    using Unity.Mathematics.Geometry;

    public static class MathExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(this MinMaxAABB value)
        {
            return value.Min.IsNaN() || value.Max.IsNaN();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(this float3 value)
        {
            return float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z);
        }
    }
}
