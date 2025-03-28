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

namespace TextTween {
    [AddComponentMenu("TextTween/Tween Manager"), ExecuteInEditMode]
    public class TweenManager : MonoBehaviour, IDisposable {
        [Range(0, 1f)] public float Progress;
        [Range(0, 1f)] public float Offset;
        
        [SerializeField] private TMP_Text[] _texts;
        [SerializeField] private List<CharModifier> _modifiers;
        
        private NativeArray<CharData> _charData;
        private NativeArray<float3> _vertices;
        private NativeArray<float4> _colors;
        private JobHandle _jobHandle;
        private float _current;
        
        private void OnEnable() {
            if (_texts == null || _texts.Length == 0) return;
            for (var i = 0; i < _texts.Length; i++) {
                _texts[i].ForceMeshUpdate(true);
            }
            
            DisposeArrays();
            CreateNativeArrays();
            ApplyModifiers(Progress);
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        }

        private void OnDisable() {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            Dispose();
        }

        public void ForceUpdate() {
            ApplyModifiers(Progress);
        }

        private void Update() {
            if (!Application.isPlaying) return;
            if (Mathf.Approximately(_current, Progress)) return;
            ApplyModifiers(Progress);
        }

        private void OnTextChanged(Object obj) {
            var found = false;
            for (var i = 0; i < _texts.Length; i++) {
                if (_texts[i] != obj) continue;
                found = true;
                break;
            }

            if (!found) return;
            
            DisposeArrays();
            CreateNativeArrays();
            ApplyModifiers(Progress);
        }

        public void CreateNativeArrays() {
            CreateMeshArrays();
            CreateCharDataArray();
        }

        private void CreateMeshArrays() {
            var vertexCount = 0;
            for (var i = 0; i < _texts.Length; i++) {
                if (_texts[i] == null) continue;
                vertexCount += _texts[i].mesh.vertexCount;
            }

            if (vertexCount == 0) return;
            
            _vertices = new NativeArray<float3>(vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _colors = new NativeArray<float4>(vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            
            var vertexOffset = 0;
            for (var i = 0; i < _texts.Length; i++) {
                var count = _texts[i].mesh.vertexCount; 
                _texts[i].mesh.vertices.MemCpy(_vertices, vertexOffset, count);
                _texts[i].mesh.colors.MemCpy(_colors, vertexOffset, count);
                vertexOffset += count;
            }
        }

        public void  CreateCharDataArray() {
            var visibleCharCount = 0;
            for (var i = 0; i < _texts.Length; i++) {
                if (_texts[i] == null) continue;
                visibleCharCount += GetVisibleCharCount(_texts[i]);
            }
            
            if (visibleCharCount == 0) return;
            if (_charData.IsCreated)
                _charData.Dispose();
            _charData = new NativeArray<CharData>(visibleCharCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            
            var indexOffset = 0;
            for (int i = 0, k = 0; i < _texts.Length; i++) {
                var text = _texts[i];
                if (text == null) continue;
                var charCount = GetVisibleCharCount(_texts[i]);
                var characterInfos = text.textInfo.characterInfo;
                var totalTime = (charCount - 1) * Offset + 1;
                var charOffset = Offset / totalTime;
                var charDuration = 1 / totalTime;
                var bounds = new float4(text.textBounds.min.x, text.textBounds.min.y, text.textBounds.max.x, text.textBounds.max.y);
                for (int j = 0, l = 0; j < characterInfos.Length; j++) {
                    if (!characterInfos[j].isVisible) continue;
                    var offset = charOffset * l;
                    var time = new float2(offset, offset + charDuration);
                    const int vertexPerChar = 4;
                    _charData[k] = new CharData(time, indexOffset + characterInfos[j].vertexIndex, vertexPerChar, bounds);
                    k++;
                    l++;
                }
                indexOffset = text.mesh.vertexCount;
            }
        }

        private void ApplyModifiers(float progress) {
            if (!_vertices.IsCreated || !_colors.IsCreated) {
                throw new Exception("Must have valid texts to apply modifiers.");
            }
            
            var vertices = new NativeArray<float3>(_vertices, Allocator.TempJob);
            var colors = new NativeArray<float4>(_colors, Allocator.TempJob);
            
            for (var i = 0; i < _modifiers.Count; i++) {
                if (_modifiers[i] == null) continue;
                _jobHandle = _modifiers[i].Schedule(progress, vertices, colors, _charData, _jobHandle);
            }
            
            _jobHandle.Complete();
            var offset = 0;
            for (var i = 0; i < _texts.Length; i++) {
                var text = _texts[i];
                if (text.mesh == null) continue;
                var count = text.mesh.vertexCount;
                text.mesh.SetVertices(vertices, offset, count);
                text.mesh.SetColors(colors, offset, count);
                offset += count;
                
                var meshInfos = text.textInfo.meshInfo;
                for (var j = 0; j < meshInfos.Length; j++) {
                    meshInfos[j].colors32 = text.mesh.colors32;
                    meshInfos[j].vertices = text.mesh.vertices;
                }
            
                text.UpdateVertexData((TMP_VertexDataUpdateFlags) 17);
            }
            
            
            _current = Progress;
            vertices.Dispose();
            colors.Dispose();
        }

        public void Dispose() {
            DisposeArrays();
        }

        private void DisposeArrays() {
            _jobHandle.Complete();
            if (_charData.IsCreated) _charData.Dispose();
            if (_vertices.IsCreated) _vertices.Dispose();
            if (_colors.IsCreated) _colors.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetVisibleCharCount(TMP_Text text) {
            var count = 0;
            var characterInfos = text.textInfo.characterInfo;
            for (var j = 0; j < characterInfos.Length; j++) {
                if (!characterInfos[j].isVisible) continue;
                count++;
            }
            return count;
        }
    }
}