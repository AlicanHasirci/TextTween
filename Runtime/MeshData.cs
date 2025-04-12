using System;
using TextTween.Utilities;
using TMPro;
using Unity.Collections;
using Unity.Mathematics;

namespace TextTween
{
    [Serializable]
    public class MeshData
    {
        public static readonly MeshData Empty = new(null);

        public TMP_Text Text;
        public int Offset;
        public int Length;
        public int Trail => Length + Offset;

        public MeshData(TMP_Text text)
        {
            Text = text;
        }

        public void Apply(NativeArray<float3> vertices, NativeArray<float4> colors)
        {
            Text.mesh.SetVertices(vertices, Offset, Length);
            Text.mesh.SetColors(colors, Offset, Length);

            TMP_MeshInfo[] meshInfos = Text.textInfo.meshInfo;
            for (int j = 0; j < meshInfos.Length; j++)
            {
                meshInfos[j].colors32 = Text.mesh.colors32;
                meshInfos[j].vertices = Text.mesh.vertices;
            }
            Text.UpdateVertexData(
                TMP_VertexDataUpdateFlags.Colors32 | TMP_VertexDataUpdateFlags.Vertices
            );
        }

        public void Update(
            ref NativeArray<float3> vertices,
            ref NativeArray<float4> colors,
            ref NativeArray<CharData> chars,
            int offset,
            float overlap
        )
        {
            int length = Text.GetVertexCount();
            Text.mesh.vertices.MemCpy(vertices, offset, length);
            Text.mesh.colors.MemCpy(colors, offset, length);
            CreateCharData(ref vertices, ref chars, offset, length, overlap);
            Offset = offset;
            Length = length;
        }

        public void CreateCharData(
            ref NativeArray<float3> vertices,
            ref NativeArray<CharData> chars,
            int offset,
            int length,
            float overlap
        )
        {
            const int vertexPerChar = 4;
            TMP_CharacterInfo[] characterInfos = Text.textInfo.characterInfo;
            float totalTime = (characterInfos.Length - 1) * overlap + 1;
            float charOffset = overlap / totalTime;
            float charDuration = 1 / totalTime;
            float4 bounds = new(
                Text.textBounds.min.x,
                Text.textBounds.min.y,
                Text.textBounds.max.x,
                Text.textBounds.max.y
            );
            for (
                int i = 0, ci = 0;
                i < length && ci < characterInfos.Length;
                i++, ci = i / vertexPerChar
            )
            {
                float cue = charOffset * ci;
                float2 time = new(cue, cue + charDuration);
                chars[offset + i] = new CharData(vertices[offset + i], time, bounds);
            }
        }
    }
}
