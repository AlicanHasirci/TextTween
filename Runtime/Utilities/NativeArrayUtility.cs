namespace TextTween.Utilities
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    public static class NativeArrayUtility
    {
        public static unsafe void MemCpy<TS, TD>(
            this TS[] src,
            NativeArray<TD> dst,
            int index,
            int length
        )
            where TS : unmanaged
            where TD : unmanaged
        {
            fixed (void* managedArrayPointer = src)
            {
                UnsafeUtility.MemCpy(
                    (TD*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(dst) + index,
                    managedArrayPointer,
                    length * (long)UnsafeUtility.SizeOf<TS>()
                );
            }
        }

        public static JobHandle Move<TA>(
            ref NativeArray<TA> array,
            int from,
            int to,
            int length,
            JobHandle dependsOn = default
        )
            where TA : unmanaged
        {
            if (to == from)
            {
                return default;
            }
            bool reverse = to > from;
            return new IntraMove<TA>(array, from, to, length, reverse).Schedule(dependsOn);
        }

        public static JobHandle Move<TA>(
            ref NativeArray<TA> src,
            ref NativeArray<TA> dst,
            int from,
            int to,
            int length,
            JobHandle dependsOn = default
        )
            where TA : unmanaged
        {
            return new InterMove<TA>(src, dst, from, to).Schedule(length, default, dependsOn);
        }

        public static void EnsureCapacity<TA>(ref NativeArray<TA> array, int length)
            where TA : unmanaged
        {
            if (!array.IsCreated)
            {
                array = new NativeArray<TA>(
                    length,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory
                );
            }
            else
            {
                if (array.Length >= length)
                {
                    return;
                }

                try
                {
                    NativeArray<TA> newArray = new(
                        length,
                        Allocator.Persistent,
                        NativeArrayOptions.UninitializedMemory
                    );
                    JobHandle handle = Move(ref array, ref newArray, 0, 0, array.Length);
                    handle.Complete();
                    array.Dispose();
                    array = newArray;
                }
                catch
                {
                    array.Dispose();
                }
            }
        }

        private struct InterMove<TA> : IJobParallelFor
            where TA : unmanaged
        {
            [ReadOnly]
            private NativeArray<TA> _source;

            [NativeDisableParallelForRestriction]
            private NativeArray<TA> _target;
            private readonly int _from;
            private readonly int _to;

            public InterMove(NativeArray<TA> source, NativeArray<TA> target, int from, int to)
            {
                _source = source;
                _target = target;
                _from = from;
                _to = to;
            }

            public void Execute(int index)
            {
                _target[_to + index] = _source[_from + index];
            }
        }

        private struct IntraMove<TA> : IJob
            where TA : unmanaged
        {
            [NativeDisableParallelForRestriction]
            private NativeArray<TA> _source;
            private readonly int _from;
            private readonly int _to;
            private readonly int _length;
            private readonly bool _reverse;

            public IntraMove(NativeArray<TA> source, int from, int to, int length, bool reverse)
            {
                _source = source;
                _from = from;
                _to = to;
                _length = length;
                _reverse = reverse;
            }

            public void Execute()
            {
                if (!_reverse)
                {
                    for (int i = 0; i < _length; i++)
                    {
                        if (_to + i >= _source.Length || _from + i >= _source.Length)
                        {
                            return;
                        }
                        _source[_to + i] = _source[_from + i];
                    }
                }
                else
                {
                    for (int i = _length - 1; i >= 0; i--)
                    {
                        if (_to + i >= _source.Length || _from + i >= _source.Length)
                        {
                            continue;
                        }
                        _source[_to + i] = _source[_from + i];
                    }
                }
            }
        }
    }
}
