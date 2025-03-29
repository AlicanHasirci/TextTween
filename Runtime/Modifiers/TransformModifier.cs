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

    public enum ModifierType
    {
        Position = 0,
        Rotation = 1,
        Scale = 2,
    }

    [Flags]
    public enum ModifierScale
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
        public ModifierType Type;

        [FormerlySerializedAs("_scale")]
        public ModifierScale Scale;

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
                Scale,
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
            private readonly ModifierType _type;
            private readonly ModifierScale _scale;
            private readonly float3 _intensity;
            private readonly float2 _pivot;
            private readonly float _progress;

            public Job(
                NativeArray<float3> vertices,
                NativeArray<CharData> data,
                NativeCurve curve,
                float3 intensity,
                float2 pivot,
                ModifierType type,
                ModifierScale scale,
                float progress
            )
            {
                _vertices = vertices;
                _data = data;
                _curve = curve;
                _type = type;
                _scale = scale;
                _intensity = intensity;
                _pivot = pivot;
                _progress = progress;
            }

            public void Execute(int index)
            {
                CharData characterData = _data[index];
                int vertexOffset = characterData.VertexIndex;
                float3 offset = Offset(_vertices, vertexOffset, _pivot);
                float p = _curve.Evaluate(Remap(_progress, characterData.Interval));
                float4x4 m = GetTransformation(p);
                for (int i = 0; i < characterData.VertexCount; i++)
                {
                    _vertices[vertexOffset + i] -= offset;
                    _vertices[vertexOffset + i] = math.mul(
                        m,
                        new float4(_vertices[vertexOffset + i], 1)
                    ).xyz;
                    _vertices[vertexOffset + i] += offset;
                }
            }

            private float4x4 GetTransformation(float progress)
            {
                float3 fp = float3.zero;
                quaternion fr = quaternion.identity;
                float3 fs = 1;
                switch (_type)
                {
                    case ModifierType.Position:
                        fp = _intensity * progress;
                        break;
                    case ModifierType.Rotation:
                        fr = quaternion.Euler(math.radians(_intensity * progress));
                        break;
                    case ModifierType.Scale:
                        if (_scale.HasFlagNoAlloc(ModifierScale.X))
                        {
                            fs.x = progress * _intensity.x;
                        }
                        if (_scale.HasFlagNoAlloc(ModifierScale.Y))
                        {
                            fs.y = progress * _intensity.y;
                        }
                        if (_scale.HasFlagNoAlloc(ModifierScale.Z))
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
