using TextTween.Native;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace TextTween.Modifiers {
    [AddComponentMenu("TextTween/Modifiers/Warp Modifier")]
    public class WarpModifier : CharModifier {
        [SerializeField] private float _intensity;
        [SerializeField] private AnimationCurve _warpCurve;
        private NativeCurve _nWarpCurve;
        
        public override JobHandle Schedule(
            float4 bounds, 
            float progress, 
            NativeArray<float3> vertices, 
            NativeArray<float4> colors, 
            NativeArray<CharData> charData, 
            JobHandle dependency) {
            if (!_nWarpCurve.IsCreated) _nWarpCurve.Update(_warpCurve, 1024);
            return new Job(
                vertices, 
                charData, 
                _nWarpCurve, 
                bounds, 
                _intensity, 
                progress)
                .Schedule(charData.Length, 64, dependency);
        }

        public override void Dispose() {
            if (_nWarpCurve.IsCreated) _nWarpCurve.Dispose();
        }

        [BurstCompile]
        public struct Job : IJobParallelFor {
            [NativeDisableParallelForRestriction] private NativeArray<float3> _vertices;
            [ReadOnly] private NativeArray<CharData> _data;
            private readonly NativeCurve _warpCurve;
            private readonly float _intensity;
            private readonly float _progress;
            private readonly float4 _bounds;

            public Job(
                NativeArray<float3> vertices, 
                NativeArray<CharData> data, 
                NativeCurve warpCurve, 
                float4 bounds, 
                float intensity, 
                float progress) {
                _vertices = vertices;
                _data = data;
                _warpCurve = warpCurve;
                _bounds = bounds;
                _intensity = intensity;
                _progress = progress;
            }

            public void Execute(int index) {
                var characterData = _data[index];
                var vertexOffset = characterData.VertexIndex;
                var offset = Offset(_vertices, vertexOffset, .5f);
                var width = _bounds.z - _bounds.x;
                var x = (offset.x - _bounds.x) / width;
                var p = Remap(_progress, characterData.Interval);
                var y = _warpCurve.Evaluate(x) * p * _intensity;
                var v = _warpCurve.Velocity(x);
                var t = math.normalize(new float2(v.x * width, v.y * p * _intensity));
                var a = math.atan2(t.y, t.x);
                var m = float4x4.TRS(new float3(0, y, 0), quaternion.Euler(0, 0, a), new float3(1, 1, 1)); 
                for (var i = 0; i < characterData.VertexCount; i++) {
                    _vertices[vertexOffset + i] -= offset;
                    _vertices[vertexOffset + i] = math.mul(m, new float4(_vertices[vertexOffset + i], 1)).xyz;
                    _vertices[vertexOffset + i] += offset;
                }
            }
        }
    }
}