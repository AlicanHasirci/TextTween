using Unity.Collections;
using Unity.Jobs;

namespace TextTween
{
    // public struct Move<TA> : IJobParallelFor
    //     where TA : unmanaged
    // {
    //     [ReadOnly]
    //     private NativeArray<TA> _source;
    //
    //     [NativeDisableParallelForRestriction]
    //     private NativeArray<TA> _target;
    //     private readonly int _from;
    //     private readonly int _to;
    //
    //     public Move(NativeArray<TA> source, NativeArray<TA> target, int from, int to)
    //     {
    //         _source = source;
    //         _target = target;
    //         _from = from;
    //         _to = to;
    //     }
    //
    //     public void Execute(int index)
    //     {
    //         _target[_to + index] = _source[_from + index];
    //     }
    // }
    //
    // public struct IntraMove<TA> : IJobParallelFor
    //     where TA : unmanaged
    // {
    //     [NativeDisableParallelForRestriction]
    //     private NativeArray<TA> _source;
    //     private readonly int _from;
    //     private readonly int _to;
    //
    //     public IntraMove(NativeArray<TA> source, int from, int to)
    //     {
    //         _source = source;
    //         _from = from;
    //         _to = to;
    //     }
    //
    //     public void Execute(int index)
    //     {
    //         _source[_to + index] = _source[_from + index];
    //     }
    // }
}
