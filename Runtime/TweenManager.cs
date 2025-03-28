using System;
using System.Collections.Generic;
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
        
        [SerializeField] private TMP_Text _text;
        [SerializeField] private List<CharModifier> _modifiers;
        
        private NativeArray<CharData> _charData;
        private NativeArray<float3> _vertices;
        private NativeArray<float4> _colors;
        private JobHandle _jobHandle;
        private float4 _bounds;
        private float _current;
        
        private void OnEnable() {
            if (_text == null) return;
            _text.ForceMeshUpdate(true);
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
            if (_text != obj) return;
            DisposeArrays();
            CreateNativeArrays();
            ApplyModifiers(Progress);
        }

        private void CreateNativeArrays() {
            CreateMeshArrays();
            CreateCharDataArray();
        }

        private void CreateMeshArrays() {
            _vertices = new NativeArray<float3>(_text.mesh.vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _colors = new NativeArray<float4>(_text.mesh.vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _text.mesh.vertices.MemCpy(_vertices);
            _text.mesh.colors.MemCpy(_colors);
        }

        public void  CreateCharDataArray() {
            var characterInfos = _text.textInfo.characterInfo;
            var charCount = 0;
            for (var i = 0; i < characterInfos.Length; i++) {
                if (!characterInfos[i].isVisible) continue;
                charCount++;
            }
            
            if (!_charData.IsCreated)
                _charData = new NativeArray<CharData>(charCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var totalTime = (_charData.Length - 1) * Offset + 1;
            var charOffset = Offset / totalTime;
            var charDuration = 1 / totalTime;
            for (int i = 0, j = 0; i < characterInfos.Length; i++) {
                if (!characterInfos[i].isVisible) continue;
                var offset = charOffset * j;
                var time = new float2(offset, offset + charDuration);
                const int vertexPerChar = 4;
                _charData[j++] = new CharData(time, characterInfos[i].vertexIndex, vertexPerChar);
            }

            var bounds = _text.textBounds;
            _bounds = new float4(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
        }

        private void ApplyModifiers(float progress) {
            if (!_vertices.IsCreated || !_colors.IsCreated) {
                throw new Exception("Vertices and Colors must be created before applying modifiers.");
            }
            if (_text == null || _text.mesh == null) {
                throw new Exception("Text must be bound before applying modifiers.");
            }
            var vertices = new NativeArray<float3>(_vertices, Allocator.TempJob);
            var colors = new NativeArray<float4>(_colors, Allocator.TempJob);
            
            for (var i = 0; i < _modifiers.Count; i++) {
                if (_modifiers[i] == null) continue;
                _jobHandle = _modifiers[i].Schedule(_bounds, progress, vertices, colors, _charData, _jobHandle);
            }

            _jobHandle.Complete();

            if (_text.mesh != null) {
                _text.mesh.SetVertices(vertices);
                _text.mesh.SetColors(colors);
                var meshInfos = _text.textInfo.meshInfo;
                for (var i = 0; i < meshInfos.Length; i++) {
                    meshInfos[i].colors32 = _text.mesh.colors32;
                    meshInfos[i].vertices = _text.mesh.vertices;
                }

                _text.UpdateVertexData((TMP_VertexDataUpdateFlags) 17);
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
    }
}