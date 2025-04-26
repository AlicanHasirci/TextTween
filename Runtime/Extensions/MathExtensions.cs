namespace TextTween.Extensions
{
    using System.Runtime.CompilerServices;
    using Unity.Mathematics;

    internal static class MathExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsNaN(this TextTweenMinMaxAABB value)
        {
            return value.Min.IsNaN() || value.Max.IsNaN();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsNaN(this float3 value)
        {
            return float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z);
        }
    }
}
