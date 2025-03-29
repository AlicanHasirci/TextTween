using TextTween.Native;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace TextTween.Modifiers
{
    [AddComponentMenu("TextTween/Modifiers/Warp Modifier")]
    public class WarpModifier : CharModifier
    {
        [SerializeField]
        private float _intensity;

        [SerializeField]
        private AnimationCurve _warpCurve;
        private NativeCurve _nWarpCurve;

        public override JobHandle Schedule(
            float progress,
            NativeArray<float3> vertices,
            NativeArray<float4> colors,
            NativeArray<CharData> charData,
            JobHandle dependency
        )
        {
            if (!_nWarpCurve.IsCreated)
            {
                _nWarpCurve.Update(_warpCurve, 1024);
            }
            return new Job(vertices, charData, _nWarpCurve, _intensity, progress).Schedule(
                charData.Length,
                64,
                dependency
            );
        }

        public override void Dispose()
        {
            if (_nWarpCurve.IsCreated)
                _nWarpCurve.Dispose();
        }

        [BurstCompile]
        private struct Job : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            private NativeArray<float3> _vertices;

            [ReadOnly]
            private NativeArray<CharData> _data;
            private readonly NativeCurve _warpCurve;
            private readonly float _intensity;
            private readonly float _progress;

            public Job(
                NativeArray<float3> vertices,
                NativeArray<CharData> data,
                NativeCurve warpCurve,
                float intensity,
                float progress
            )
            {
                _vertices = vertices;
                _data = data;
                _warpCurve = warpCurve;
                _intensity = intensity;
                _progress = progress;
            }

            public void Execute(int index)
            {
                CharData characterData = _data[index];
                int vertexOffset = characterData.VertexIndex;
                float3 offset = Offset(_vertices, vertexOffset, .5f);
                float width = characterData.Bounds.z - characterData.Bounds.x;
                float x = (offset.x - characterData.Bounds.x) / width;
                float p = Remap(_progress, characterData.Interval);
                float y = _warpCurve.Evaluate(x) * p * _intensity;
                float2 v = _warpCurve.Velocity(x);
                float2 t = math.normalize(new float2(v.x * width, v.y * p * _intensity));
                float a = math.atan2(t.y, t.x);
                float4x4 m = float4x4.TRS(
                    new float3(0, y, 0),
                    quaternion.Euler(0, 0, a),
                    new float3(1, 1, 1)
                );
                for (int i = 0; i < characterData.VertexCount; i++)
                {
                    _vertices[vertexOffset + i] -= offset;
                    _vertices[vertexOffset + i] = math.mul(m, new float4(_vertices[vertexOffset + i], 1)).xyz;
                    _vertices[vertexOffset + i] += offset;
                }
            }
        }
    }
}
