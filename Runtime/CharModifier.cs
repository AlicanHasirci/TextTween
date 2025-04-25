namespace TextTween
{
    using System;
    using System.Runtime.CompilerServices;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    [ExecuteInEditMode]
    public abstract class CharModifier : MonoBehaviour, IDisposable
    {
        public abstract JobHandle Schedule(
            float progress,
            NativeArray<float3> vertices,
            NativeArray<float4> colors,
            NativeArray<CharData> charData,
            JobHandle dependency
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static float Remap(float progress, float2 interval)
        {
            return math.saturate((progress - interval.x) / (interval.y - interval.x));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static float3 Offset(NativeArray<CharData> chars, int index, float2 pivot)
        {
            int ci = index / 4;
            float3 min = chars[ci * 4].Position;
            float3 max = chars[ci * 4 + 2].Position;
            float2 size = new(max.x - min.x, max.y - min.y);
            return new float3(min.x + pivot.x * size.x, min.y + pivot.y * size.y, 0);
        }

        private void OnDisable()
        {
            Dispose();
        }

        public abstract void Dispose();
    }
}
