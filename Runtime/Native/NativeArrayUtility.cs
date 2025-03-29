using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace TextTween.Native
{
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
    }
}
