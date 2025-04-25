namespace TextTween
{
    using System;
    using System.Collections.Generic;
    using TMPro;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Utilities;

    public class MeshArray : IDisposable
    {
        public int Length => _vertices.Length;

        private NativeArray<float3> _vertices;
        private NativeArray<float4> _colors;
        private NativeArray<float2> _uvs0;
        private NativeArray<float2> _uvs2;
        private NativeArray<CharData> _chars;

        public MeshArray(int length, Allocator allocator)
        {
            _vertices = new NativeArray<float3>(length, allocator);
            _colors = new NativeArray<float4>(length, allocator);
            _chars = new NativeArray<CharData>(length, allocator);
            _uvs0 = new NativeArray<float2>(length, allocator);
            _uvs2 = new NativeArray<float2>(length, allocator);
        }

        public void EnsureCapacity(int length)
        {
            NativeArrayUtility.EnsureCapacity(ref _vertices, length);
            NativeArrayUtility.EnsureCapacity(ref _colors, length);
            NativeArrayUtility.EnsureCapacity(ref _chars, length);
            NativeArrayUtility.EnsureCapacity(ref _uvs0, length);
            NativeArrayUtility.EnsureCapacity(ref _uvs2, length);
        }

        public JobHandle Move(int from, int to, int length, JobHandle dependsOn = default)
        {
            return JobHandle.CombineDependencies(
                JobHandle.CombineDependencies(
                    NativeArrayUtility.Move(ref _uvs0, from, to, length, dependsOn),
                    NativeArrayUtility.Move(ref _uvs2, from, to, length, dependsOn)
                ),
                JobHandle.CombineDependencies(
                    NativeArrayUtility.Move(ref _vertices, from, to, length, dependsOn),
                    NativeArrayUtility.Move(ref _colors, from, to, length, dependsOn),
                    NativeArrayUtility.Move(ref _chars, from, to, length, dependsOn)
                )
            );
        }

        public JobHandle Schedule(float progress, IReadOnlyList<CharModifier> modifiers)
        {
            JobHandle handle = new();
            foreach (CharModifier modifier in modifiers)
            {
                if (!modifier.enabled)
                {
                    continue;
                }
                handle = modifier.Schedule(progress, _vertices, _colors, _chars, handle);
            }

            return handle;
        }

        public void CopyFrom(MeshArray source)
        {
            _vertices.CopyFrom(source._vertices);
            _colors.CopyFrom(source._colors);
            _chars.CopyFrom(source._chars);
            _uvs0.CopyFrom(source._uvs0);
            _uvs2.CopyFrom(source._uvs2);
        }

        public void CopyFrom(TMP_Text text, int length, int offset, float overlap)
        {
            text.mesh.vertices.MemCpy(_vertices, offset, length);
            text.mesh.colors.MemCpy(_colors, offset, length);
            text.mesh.uv.MemCpy(_uvs0, offset, length);
            text.mesh.uv2.MemCpy(_uvs2, offset, length);
            CreateCharData(text, offset, length, overlap);
        }

        public void CopyTo(TMP_Text text, int offset, int length)
        {
            text.mesh.SetVertices(_vertices, offset, length);
            text.mesh.SetColors(_colors, offset, length);
            text.mesh.SetUVs(0, _uvs0, offset, length);
            text.mesh.SetUVs(1, _uvs2, offset, length);

            TMP_MeshInfo[] meshInfos = text.textInfo.meshInfo;
            for (int j = 0; j < meshInfos.Length; j++)
            {
                meshInfos[j].colors32 = text.mesh.colors32;
                meshInfos[j].vertices = text.mesh.vertices;
            }

            text.UpdateVertexData(
                TMP_VertexDataUpdateFlags.Colors32 | TMP_VertexDataUpdateFlags.Vertices
            );
        }

        private void CreateCharData(TMP_Text text, int offset, int length, float overlap)
        {
            const int vertexPerChar = 4;
            TMP_CharacterInfo[] characterInfos = text.textInfo.characterInfo;
            float totalTime = (characterInfos.Length - 1) * overlap + 1;
            float charOffset = overlap / totalTime;
            float charDuration = 1 / totalTime;
            float4 bounds = new(
                text.textBounds.min.x,
                text.textBounds.min.y,
                text.textBounds.max.x,
                text.textBounds.max.y
            );
            for (
                int i = 0, ci = 0;
                i < length && ci < characterInfos.Length;
                i++, ci = i / vertexPerChar
            )
            {
                float cue = charOffset * ci;
                float2 time = new(cue, cue + charDuration);
                _chars[offset + i] = new CharData(_vertices[offset + i], time, bounds);
            }
        }

        public void Dispose()
        {
            _vertices.Dispose();
            _colors.Dispose();
            _chars.Dispose();
            _uvs0.Dispose();
            _uvs2.Dispose();
        }
    }
}
