using System;
using System.Collections.Generic;
using System.Linq;
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

        public void ForceUpdate() {
            ApplyModifiers(Progress);
        }
        
        private void OnDisable() {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            Dispose();
        }

        private void Update() {
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
            var chars = _text.textInfo.characterInfo.Where(info => info.isVisible).ToArray();
            if (!_charData.IsCreated)
                _charData = new NativeArray<CharData>(chars.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var totalTime = (_charData.Length - 1) * Offset + 1;
            var co = Offset / totalTime;
            var cd = 1 / totalTime;
            for (var i = 0; i < _charData.Length; i++) {
                var o = co * i;
                var time = new float2(o, o + cd);
                _charData[i] = new CharData(time, chars[i].vertexIndex, 4, chars[i].isVisible);
            }

            var bounds = _text.textBounds;
            _bounds = new float4(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
        }

        private void ApplyModifiers(float progress) {
            if (!_vertices.IsCreated || !_colors.IsCreated) return;
            if (_text == null || _text.mesh == null) return;
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