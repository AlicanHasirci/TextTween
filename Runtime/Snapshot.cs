using System.Collections.Generic;

namespace TextTween
{
    using System;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;

    public readonly struct Snapshot : IDisposable
    {
        private NativeArray<float3> Vertices { get; }
        private NativeArray<float4> Colors { get; }
        private NativeArray<CharData> Chars { get; }

        public Snapshot(TextTweenManager handler)
        {
            Vertices = new NativeArray<float3>(handler.Vertices, Allocator.TempJob);
            Colors = new NativeArray<float4>(handler.Colors, Allocator.TempJob);
            Chars = handler.Chars;
        }

        public JobHandle Schedule(float progress, IEnumerable<CharModifier> modifiers)
        {
            JobHandle handle = new();
            foreach (CharModifier modifier in modifiers)
            {
                handle = modifier.Schedule(progress, Vertices, Colors, Chars, handle);
            }

            return handle;
        }

        public void Apply(IEnumerable<MeshData> meshData)
        {
            foreach (MeshData data in meshData)
            {
                data.Apply(Vertices, Colors);
            }
        }

        public void Dispose()
        {
            Vertices.Dispose();
            Colors.Dispose();
        }
    }
}
