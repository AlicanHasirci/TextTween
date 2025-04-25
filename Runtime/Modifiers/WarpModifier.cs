namespace TextTween.Modifiers
{
    using Native;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Serialization;

    [AddComponentMenu("TextTween/Modifiers/Warp Modifier")]
    public class WarpModifier : CharModifier
    {
        [FormerlySerializedAs("_intensity")]
        public float Intensity;

        [FormerlySerializedAs("_warpCurve")]
        public AnimationCurve WarpCurve;

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
                _nWarpCurve.Update(WarpCurve, 1024);
            }
            return new Job(vertices, charData, _nWarpCurve, Intensity, progress).Schedule(
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
                float width = characterData.TextBounds.Max.x - characterData.TextBounds.Min.x;
                if (!characterData.IsValid() || width == 0)
                {
                    return;
                }
                float3 offset = Offset(_data, index, .5f);
                float x = (offset.x - characterData.TextBounds.Min.x) / width;
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

                float3 vertex = _vertices[index];
                vertex -= offset;
                vertex = math.mul(m, new float4(vertex, 1)).xyz;
                vertex += offset;
                _vertices[index] = vertex;
            }
        }
    }
}
