namespace TextTween
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

        private readonly HashSet<CharModifier> _seen = new();

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
            _seen.Clear();
            JobHandle handle = new();
            for (int i = 0; i < modifiers.Count; i++)
            {
                CharModifier modifier = modifiers[i];
                if (modifier == null || !modifier.enabled)
                {
                    continue;
                }

                if (_seen.Add(modifier))
                {
                    handle = modifier.Schedule(progress, _vertices, _colors, _chars, handle);
                }
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

        public bool CopyFrom(TMP_Text text, int length, int offset)
        {
            text.mesh.vertices.MemCpy(_vertices, offset, length);
            text.mesh.colors.MemCpy(_colors, offset, length);
            if (length != 0)
            {
                // uv can be null in the editor sometimes?
                if (text.mesh.uv is not { Length: > 0 })
                {
                    return false;
                }
                text.mesh.uv.MemCpy(_uvs0, offset, length);
            }

            if (length != 0)
            {
                // Same with uv2, strange stuff
                if (text.mesh.uv2 is not { Length: > 0 })
                {
                    return false;
                }
                text.mesh.uv2.MemCpy(_uvs2, offset, length);
            }
            CreateCharData(text, offset, length);
            return true;
        }

        public void CopyTo(TMP_Text text, int offset, int length)
        {
            text.mesh.SetVertices(_vertices, offset, length);
            text.mesh.SetColors(_colors, offset, length);
            text.mesh.SetUVs(0, _uvs0, offset, length);
            text.mesh.SetUVs(1, _uvs2, offset, length);

            TMP_MeshInfo[] meshInfos = text.textInfo.meshInfo;
            for (int i = 0; i < meshInfos.Length; i++)
            {
                TMP_MeshInfo meshInfo = meshInfos[i];
                if (meshInfo.colors32?.Length == text.mesh.colors32.Length)
                {
                    Array.Copy(text.mesh.colors32, meshInfo.colors32, length);
                }
                else
                {
                    meshInfo.colors32 = text.mesh.colors32.ToArray();
                }

                if (meshInfo.vertices?.Length == text.mesh.vertices.Length)
                {
                    Array.Copy(text.mesh.vertices, meshInfo.vertices, length);
                }
                else
                {
                    meshInfo.vertices = text.mesh.vertices.ToArray();
                }

                meshInfos[i] = meshInfo;
            }

            text.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
        }

        private void CreateCharData(TMP_Text text, int offset, int length)
        {
            const int vertexPerChar = 4;
            TMP_CharacterInfo[] characterInfos = text.textInfo.characterInfo;
            int charLength = text.textInfo.characterCount;
            TextTweenMinMaxAABB textBounds = new(text.textBounds.min, text.textBounds.max);
            for (int i = 0, ci = 0; i < length && ci < charLength; i++, ci = i / vertexPerChar)
            {
                TMP_CharacterInfo characterInfo = characterInfos[ci];
                TextTweenMinMaxAABB charBounds = new(
                    characterInfo.bottomRight,
                    characterInfo.topLeft
                );
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
            if (_vertices.IsCreated)
            {
                _vertices.Dispose();
            }

            if (_colors.IsCreated)
            {
                _colors.Dispose();
            }

            if (_chars.IsCreated)
            {
                _chars.Dispose();
            }

            if (_uvs0.IsCreated)
            {
                _uvs0.Dispose();
            }

            if (_uvs2.IsCreated)
            {
                _uvs2.Dispose();
            }
        }
    }
}
