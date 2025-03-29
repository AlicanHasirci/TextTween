using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace TextTween.Native
{
    public struct NativeCurve : IDisposable
    {
        public bool IsCreated => _values.IsCreated;

        [NativeDisableParallelForRestriction]
        [ReadOnly]
        private NativeArray<float> _values;

        private WrapMode _preWrapMode;
        private WrapMode _postWrapMode;

        private void InitializeValues(int count)
        {
            if (_values.IsCreated)
                _values.Dispose();

            _values = new NativeArray<float>(
                count,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory
            );
        }

        public void Update(AnimationCurve curve, int resolution)
        {
            curve ??= AnimationCurve.Linear(0, 0, 1, 1);

            _preWrapMode = curve.preWrapMode;
            _postWrapMode = curve.postWrapMode;

            if (!_values.IsCreated || _values.Length != resolution)
            {
                InitializeValues(resolution);
            }

            for (int i = 0; i < resolution; i++)
            {
                _values[i] = curve.Evaluate(i / (float)resolution);
            }
        }

        public readonly float Evaluate(float t)
        {
            int count = _values.Length;

            if (count == 1)
            {
                return _values[0];
            }

            t = Wrap(t);

            float it = t * (count - 1);
            int lower = (int)it;
            int upper = lower + 1;
            if (upper >= count)
            {
                upper = count - 1;
            }

            return lerp(_values[lower], _values[upper], it - lower);
        }

        public readonly float2 Velocity(float t)
        {
            int count = _values.Length;

            if (count == 1)
            {
                return new float2(1, 0);
            }

            t = Wrap(t);

            int d = count - 1;
            int i = (int)(t * d);
            int lower = i - 1;
            if (lower <= 0)
            {
                lower = 0;
            }
            int upper = i + 1;
            if (upper >= count)
            {
                upper = count - 1;
            }
            return normalize(new float2(2f / count, _values[upper] - _values[lower]));
        }

        private readonly float Wrap(float t)
        {
            t = t switch
            {
                < 0f => _preWrapMode switch
                {
                    WrapMode.Loop => 1f - abs(t) % 1f,
                    WrapMode.PingPong => PingPong(t, 1f),
                    _ => 0,
                },
                > 1f => _postWrapMode switch
                {
                    WrapMode.Loop => t % 1f,
                    WrapMode.PingPong => PingPong(t, 1f),
                    _ => 1,
                },
                _ => t,
            };
            return t;
        }

        public void Dispose()
        {
            if (_values.IsCreated)
                _values.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Repeat(float t, float length)
        {
            return clamp(t - floor(t / length) * length, 0, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float PingPong(float t, float length)
        {
            t = Repeat(t, length * 2f);
            return length - abs(t - length);
        }
    }
}
