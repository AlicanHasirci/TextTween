namespace TextTween.Modifiers
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    [AddComponentMenu("TextTween/Modifiers/Interval Modifier")]
    public class IntervalModifier : CharModifier
    {
        [SerializeField, Range(0, 1)]
        public float Overlap;

        public override JobHandle Schedule(
            float progress,
            NativeArray<float3> vertices,
            NativeArray<float4> colors,
            NativeArray<CharData> charData,
            JobHandle dependency
        )
        {
            return new Job(charData, Overlap).Schedule(charData.Length, 64, dependency);
        }

        public override void Dispose() { }

        [BurstCompile]
        private struct Job : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            private NativeArray<CharData> _data;
            private readonly float _overlap;

            public Job(NativeArray<CharData> data, float overlap)
            {
                _data = data;
                _overlap = overlap;
            }

            public void Execute(int index)
            {
                CharData data = _data[index];
                float totalTime = (data.CharIndex.y - 1) * _overlap + 1;
                float charOffset = _overlap / totalTime;
                float charDuration = 1 / totalTime;
                float cue = charOffset * data.CharIndex.x;
                data.Interval = new float2(cue, cue + charDuration);
                _data[index] = data;
            }
        }
    }
}
