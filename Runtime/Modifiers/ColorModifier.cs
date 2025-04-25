namespace TextTween.Modifiers
{
    using Native;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Serialization;

    [AddComponentMenu("TextTween/Modifiers/Color Modifier")]
    public class ColorModifier : CharModifier
    {
        [FormerlySerializedAs("_gradient")]
        public Gradient Gradient;

        private NativeGradient _nativeGradient;

        public override JobHandle Schedule(
            float progress,
            NativeArray<float3> vertices,
            NativeArray<float4> colors,
            NativeArray<CharData> charData,
            JobHandle dependency
        )
        {
            _nativeGradient.Update(Gradient, 1024);
            return new Job(colors, charData, _nativeGradient, progress).Schedule(
                charData.Length,
                64,
                dependency
            );
        }

        public override void Dispose()
        {
            if (_nativeGradient.IsCreated)
            {
                _nativeGradient.Dispose();
            }
        }

        private struct Job : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            private NativeArray<float4> _colors;

            [ReadOnly]
            private NativeArray<CharData> _data;
            private readonly NativeGradient _gradient;
            private readonly float _progress;

            public Job(
                NativeArray<float4> colors,
                NativeArray<CharData> data,
                NativeGradient gradient,
                float progress
            )
            {
                _colors = colors;
                _data = data;
                _gradient = gradient;
                _progress = progress;
            }

            public void Execute(int index)
            {
                CharData characterData = _data[index];
                float p = Remap(_progress, characterData.Interval);
                float4 color = _gradient.Evaluate(p);
                _colors[index] *= color;
            }
        }
    }
}
