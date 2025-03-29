using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace TextTween
{
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
        protected static float3 Offset(NativeArray<float3> vertices, int index, float2 pivot)
        {
            float3 min = vertices[index + 0];
            float3 max = vertices[index + 2];
            float2 size = max.xy - min.xy;
            return new float3(min.x + pivot.x * size.x, min.y + pivot.y * size.y, 0);
        }

        private void OnDisable()
        {
            Dispose();
        }

        public abstract void Dispose();
    }
}
