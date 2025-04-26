namespace TextTween
{
    using System;
    using System.Runtime.CompilerServices;
    using Unity.Mathematics;

    /// <summary>
    ///     Minimal port of Unity's MinMaxAABB, but not internal
    /// </summary>
    [Serializable]
    public readonly struct TextTweenMinMaxAABB : IEquatable<TextTweenMinMaxAABB>
    {
        public readonly float3 Min;

        public readonly float3 Max;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TextTweenMinMaxAABB(float3 min, float3 max)
        {
            Min = min;
            Max = max;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(TextTweenMinMaxAABB other)
        {
            return Min.Equals(other.Min) && Max.Equals(other.Max);
        }

        public override bool Equals(object obj)
        {
            return obj is TextTweenMinMaxAABB other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Min.GetHashCode() * 37 + Max.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return $"MinMaxAABB({Min}, {Max})";
        }
    }
}
