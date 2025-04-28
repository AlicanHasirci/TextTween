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
            MinMaxAABB bounds = chars[index].CharBounds;
            float3 size = bounds.Max - bounds.Min;
            return new float3(
                bounds.Min.x + size.x * pivot.x,
                bounds.Min.y + size.y * pivot.y,
                bounds.Min.z + size.z * .5f
            );
        }

        private void OnDisable()
        {
            Dispose();
        }

        public abstract void Dispose();
    }
}
