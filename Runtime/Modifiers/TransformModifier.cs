namespace TextTween.Modifiers
{
    using System;
    using Extensions;
    using Native;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Serialization;

    public enum TransformType
    {
        Position = 0,
        Rotation = 1,
        Scale = 2,
    }

    [Flags]
    public enum ScaleMask
    {
        X = 1 << 0,
        Y = 1 << 1,
        Z = 1 << 2,
    }

    [AddComponentMenu("TextTween/Modifiers/Transform Modifier")]
    public class TransformModifier : CharModifier
    {
        [FormerlySerializedAs("_curve")]
        public AnimationCurve Curve;

        [FormerlySerializedAs("_type")]
        public TransformType Type;

        [FormerlySerializedAs("_scale")]
        public ScaleMask ScaleMask;

        [FormerlySerializedAs("_intensity")]
        public float3 Intensity;

        [FormerlySerializedAs("_pivot")]
        public float2 Pivot;

        private NativeCurve _nCurve;

        public override JobHandle Schedule(
            float progress,
            NativeArray<float3> vertices,
            NativeArray<float4> colors,
            NativeArray<CharData> charData,
            JobHandle dependency
        )
        {
            if (!_nCurve.IsCreated)
            {
                _nCurve.Update(Curve, 1024);
            }
            return new Job(
                vertices,
                charData,
                _nCurve,
                Intensity,
                Pivot,
                Type,
                ScaleMask,
                progress
            ).Schedule(charData.Length, 64, dependency);
        }

        public override void Dispose()
        {
            if (_nCurve.IsCreated)
            {
                _nCurve.Dispose();
            }
        }

        private struct Job : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            private NativeArray<float3> _vertices;

            [ReadOnly]
            private NativeArray<CharData> _data;
            private readonly NativeCurve _curve;
            private readonly TransformType _type;
            private readonly ScaleMask _scaleMask;
            private readonly float3 _intensity;
            private readonly float2 _pivot;
            private readonly float _progress;

            public Job(
                NativeArray<float3> vertices,
                NativeArray<CharData> data,
                NativeCurve curve,
                float3 intensity,
                float2 pivot,
                TransformType type,
                ScaleMask scaleMask,
                float progress
            )
            {
                _vertices = vertices;
                _data = data;
                _curve = curve;
                _type = type;
                _scaleMask = scaleMask;
                _intensity = intensity;
                _pivot = pivot;
                _progress = progress;
            }

            public void Execute(int index)
            {
                CharData characterData = _data[index];
                if (!characterData.IsValid())
                {
                    return;
                }
                float3 offset = Offset(_data, index, _pivot);
                float p = _curve.Evaluate(Remap(_progress, characterData.Interval));
                float4x4 m = GetTransformation(p);

                float3 vertex = _vertices[index];
                vertex -= offset;
                vertex = math.mul(m, new float4(vertex, 1)).xyz;
                vertex += offset;
                _vertices[index] = vertex;
            }

            private float4x4 GetTransformation(float progress)
            {
                float3 fp = float3.zero;
                quaternion fr = quaternion.identity;
                float3 fs = 1;
                switch (_type)
                {
                    case TransformType.Position:
                        fp = _intensity * progress;
                        break;
                    case TransformType.Rotation:
                        fr = quaternion.Euler(math.radians(_intensity * progress));
                        break;
                    case TransformType.Scale:
                        if (_scaleMask.HasFlagNoAlloc(ScaleMask.X))
                        {
                            fs.x = progress * _intensity.x;
                        }
                        if (_scaleMask.HasFlagNoAlloc(ScaleMask.Y))
                        {
                            fs.y = progress * _intensity.y;
                        }
                        if (_scaleMask.HasFlagNoAlloc(ScaleMask.Z))
                        {
                            fs.z = progress * _intensity.z;
                        }
                        break;
                    default:
                        return float4x4.identity;
                }

                return float4x4.TRS(fp, fr, fs);
            }
        }
    }
}
