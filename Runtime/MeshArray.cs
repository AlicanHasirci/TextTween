namespace TextTween
{
    using System;
    using System.Collections.Generic;
    using TMPro;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using Utilities;

    public class MeshArray : IDisposable
    {
        public int Length => _vertices.Length;

        private NativeArray<float3> _vertices;
        private NativeArray<float4> _colors;
        private NativeArray<float2> _uvs0;
        private NativeArray<float2> _uvs2;
        private NativeArray<CharData> _chars;

        // Keep lazy since only one MeshArray will be doing operations
        private HashSet<CharModifier> _uniqueModifiers;

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
            _uniqueModifiers ??= new HashSet<CharModifier>();
            _uniqueModifiers.Clear();
            for (int i = 0; i < modifiers.Count; i++)
            {
                CharModifier modifier = modifiers[i];
                if (modifier == null || !modifier.enabled || !_uniqueModifiers.Add(modifier))
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

        public void CopyFrom(TMP_Text text, int offset, int length)
        {
            Mesh mesh = text.mesh;
            mesh.vertices.MemCpy(_vertices, offset, length);
            mesh.colors.MemCpy(_colors, offset, length);
            mesh.uv.MemCpy(_uvs0, offset, length);
            mesh.uv2.MemCpy(_uvs2, offset, length);
            CreateCharData(text, offset, length);
        }

        public void CopyTo(TMP_Text text, int offset, int length)
        {
            Mesh mesh = text.mesh;
            mesh.SetVertices(_vertices, offset, length);
            mesh.SetColors(_colors, offset, length);
            mesh.SetUVs(0, _uvs0, offset, length);
            mesh.SetUVs(1, _uvs2, offset, length);

            TMP_MeshInfo[] meshInfos = text.textInfo.meshInfo;
            for (int j = 0; j < meshInfos.Length; j++)
            {
                TMP_MeshInfo meshInfo = meshInfos[j];
                meshInfo.colors32 = mesh.colors32;
                meshInfo.vertices = mesh.vertices;
                meshInfos[j] = meshInfo;
            }

            text.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
        }

        private void CreateCharData(TMP_Text text, int offset, int length)
        {
            const int vertexPerChar = 4;
            TMP_CharacterInfo[] characterInfos = text.textInfo.characterInfo;
            int charLength = text.textInfo.characterCount;
            MinMaxAABB textBounds = new(text.textBounds.min, text.textBounds.max);
            for (int i = 0, ci = 0; i < length && ci < charLength; i++, ci = i / vertexPerChar)
            {
                TMP_CharacterInfo characterInfo = characterInfos[ci];
                MinMaxAABB charBounds = new(characterInfo.bottomRight, characterInfo.topLeft);
                _chars[offset + i] = new CharData(
                    new int2(ci, charLength),
                    new float2(0, 1),
                    charBounds,
                    textBounds
                );
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
