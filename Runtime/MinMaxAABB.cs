namespace TextTween
{
    using System;
    using System.Runtime.CompilerServices;
    using Unity.Mathematics;

    /// <summary>
    /// TextTween's version of Unity's MinMaxAAB. In Unity versions less than 6, the Unity.Mathematics.Geometry
    /// MinMaxAABB is internal and not accessible. We need/want some version of this (all that we care about is a
    /// min/max), so this is that. If this causes conflicts, please specify the type precisely (TextTween.MinMaxAAB)
    /// in calling code, or use `using TTMinMaxAAB = TextTween.MinMaxAAB` or similar.
    /// </summary>
    [Serializable]
    public readonly struct MinMaxAABB : IEquatable<MinMaxAABB>
    {
        public readonly float3 Min;
        public readonly float3 Max;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MinMaxAABB(float3 min, float3 max)
        {
            Min = min;
            Max = max;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(MinMaxAABB other)
        {
            return Min.Equals(other.Min) && Max.Equals(other.Max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is MinMaxAABB other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            unchecked
            {
                return (Min.GetHashCode() * 397) ^ Max.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return $"MinMaxAABB({Min}, {Max})";
        }
    }
}
