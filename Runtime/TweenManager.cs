using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TextTween.Native;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TextTween
{
    [AddComponentMenu("TextTween/Tween Manager")]
    [ExecuteInEditMode]
    public class TweenManager : MonoBehaviour, IDisposable
    {
        [Range(0, 1f)]
        public float Progress;

        [Range(0, 1f)]
        public float Offset;

        [SerializeField]
        private TMP_Text[] _texts;

        [SerializeField]
        private List<CharModifier> _modifiers;

        private NativeArray<CharData> _charData;
        private NativeArray<float3> _vertices;
        private NativeArray<float4> _colors;
        private JobHandle _jobHandle;
        private float _current;

        private void OnEnable()
        {
            if (_texts == null || _texts.Length == 0)
            {
                return;
            }
            for (int i = 0; i < _texts.Length; i++)
            {
                _texts[i].ForceMeshUpdate(true);
            }

            DisposeArrays(_texts);
            CreateNativeArrays();
            ApplyModifiers(Progress);
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            Dispose();
        }

        public void ForceUpdate()
        {
            ApplyModifiers(Progress);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (Mathf.Approximately(_current, Progress))
            {
                return;
            }
            ApplyModifiers(Progress);
        }

        private void OnTextChanged(Object obj)
        {
            bool found = false;
            for (int i = 0; i < _texts.Length; i++)
            {
                if (_texts[i] != obj)
                {
                    continue;
                }
                found = true;
                break;
            }

            if (!found)
            {
                return;
            }

            DisposeArrays(_texts);
            CreateNativeArrays();
            ApplyModifiers(Progress);
        }

        public void CreateNativeArrays()
        {
            CreateMeshArrays();
            CreateCharDataArray();
        }

        private void CreateMeshArrays()
        {
            int vertexCount = 0;
            for (int i = 0; i < _texts.Length; i++)
            {
                if (_texts[i] == null)
                {
                    continue;
                }
                vertexCount += _texts[i].mesh.vertexCount;
            }

            if (vertexCount == 0)
            {
                return;
            }

            _vertices = new NativeArray<float3>(
                vertexCount,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory
            );
            _colors = new NativeArray<float4>(
                vertexCount,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory
            );

            int vertexOffset = 0;
            for (int i = 0; i < _texts.Length; i++)
            {
                int count = _texts[i].mesh.vertexCount;
                _texts[i].mesh.vertices.MemCpy(_vertices, vertexOffset, count);
                _texts[i].mesh.colors.MemCpy(_colors, vertexOffset, count);
                vertexOffset += count;
            }
        }

        private void CreateCharDataArray()
        {
            int visibleCharCount = 0;
            for (int i = 0; i < _texts.Length; i++)
            {
                if (_texts[i] == null)
                {
                    continue;
                }
                visibleCharCount += GetVisibleCharCount(_texts[i]);
            }

            if (visibleCharCount == 0)
            {
                return;
            }
            if (_charData.IsCreated)
            {
                _charData.Dispose();
            }
            _charData = new NativeArray<CharData>(
                visibleCharCount,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory
            );

            int indexOffset = 0;
            for (int i = 0, k = 0; i < _texts.Length; i++)
            {
                TMP_Text text = _texts[i];
                if (text == null)
                {
                    continue;
                }
                int charCount = GetVisibleCharCount(_texts[i]);
                TMP_CharacterInfo[] characterInfos = text.textInfo.characterInfo;
                float totalTime = (charCount - 1) * Offset + 1;
                float charOffset = Offset / totalTime;
                float charDuration = 1 / totalTime;
                float4 bounds = new(
                    text.textBounds.min.x,
                    text.textBounds.min.y,
                    text.textBounds.max.x,
                    text.textBounds.max.y
                );
                for (int j = 0, l = 0; j < characterInfos.Length; j++)
                {
                    if (!characterInfos[j].isVisible)
                    {
                        continue;
                    }
                    float offset = charOffset * l;
                    float2 time = new(offset, offset + charDuration);
                    const int vertexPerChar = 4;
                    _charData[k] = new CharData(
                        time,
                        indexOffset + characterInfos[j].vertexIndex,
                        vertexPerChar,
                        bounds
                    );
                    k++;
                    l++;
                }

                indexOffset = text.mesh.vertexCount;
            }
        }

        private void ApplyModifiers(float progress)
        {
            if (!_vertices.IsCreated || !_colors.IsCreated)
            {
                throw new Exception("Must have valid texts to apply modifiers.");
            }

            using NativeArray<float3> vertices = new(_vertices, Allocator.TempJob);
            using NativeArray<float4> colors = new(_colors, Allocator.TempJob);

            for (int i = 0; i < _modifiers.Count; i++)
            {
                if (_modifiers[i] == null || !_modifiers[i].enabled)
                {
                    continue;
                }
                _jobHandle = _modifiers[i].Schedule(progress, vertices, colors, _charData, _jobHandle);
            }

            _jobHandle.Complete();

            UpdateMeshes(_texts, vertices, colors);

            _current = Progress;
        }

        private void UpdateMeshes(
            IReadOnlyList<TMP_Text> texts,
            NativeArray<float3> vertices,
            NativeArray<float4> colors
        )
        {
            int offset = 0;
            for (int i = 0; i < texts.Count; i++)
            {
                TMP_Text text = texts[i];
                if (text.mesh == null)
                {
                    continue;
                }
                
                int count = text.mesh.vertexCount;
                text.mesh.SetVertices(vertices, offset, count);
                text.mesh.SetColors(colors, offset, count);
                offset += count;

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
        }

        public void Dispose()
        {
            Dispose(_texts);
        }

        public void Dispose(IReadOnlyList<TMP_Text> texts)
        {
            DisposeArrays(texts);
        }

        private void DisposeArrays(IReadOnlyList<TMP_Text> texts)
        {
            _jobHandle.Complete();
            if (_charData.IsCreated)
            {
                _charData.Dispose();
            }
            if (_vertices.IsCreated && _colors.IsCreated)
            {
                UpdateMeshes(texts, _vertices, _colors);
                _vertices.Dispose();
                _colors.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetVisibleCharCount(TMP_Text text)
        {
            int count = 0;
            TMP_CharacterInfo[] characterInfos = text.textInfo.characterInfo;
            for (int j = 0; j < characterInfos.Length; j++)
            {
                if (!characterInfos[j].isVisible)
                    continue;
                count++;
            }

            return count;
        }
    }
}
