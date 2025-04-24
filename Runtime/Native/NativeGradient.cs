namespace TextTween.Native
{
    using System;
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;

    public struct NativeGradient : IDisposable
    {
        public bool IsCreated => _values.IsCreated;

        [NativeDisableParallelForRestriction]
        private NativeArray<float4> _values;

        public void Update(Gradient gradient, int resolution)
        {
            if (!_values.IsCreated || _values.Length != resolution)
            {
                InitializeValues(resolution);
            }

            for (int i = 0; i < resolution; i++)
            {
                Color c = gradient.Evaluate(i / (float)resolution);
                _values[i] = new float4(c.r, c.g, c.b, c.a);
            }
        }

        public readonly float4 Evaluate(float t)
        {
            int count = _values.Length;

            if (count == 1)
            {
                return _values[0];
            }

            t = math.saturate(t);

            float it = t * (count - 1);
            int lower = (int)it;
            int upper = lower + 1;
            if (upper >= count)
            {
                upper = count - 1;
            }

            return math.lerp(_values[lower], _values[upper], it - lower);
        }

        private void InitializeValues(int count)
        {
            if (_values.IsCreated)
            {
                _values.Dispose();
            }

            _values = new NativeArray<float4>(
                count,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory
            );
        }

        public void Dispose()
        {
            if (_values.IsCreated)
            {
                _values.Dispose();
            }
        }
    }
}
