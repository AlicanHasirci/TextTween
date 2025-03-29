/*
 https://github.com/wallstop/unity-helpers

 * Copyright 2023 Eli Pinkerton
   Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
   The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 */

using System;
using System.Runtime.CompilerServices;

namespace TextTween.Extensions
{
    public static class EnumExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasFlagNoAlloc<T>(this T value, T flag)
            where T : unmanaged, Enum
        {
            ulong valueUnderlying = GetUInt64(value);
            ulong flagUnderlying = GetUInt64(flag);
            return (valueUnderlying & flagUnderlying) == flagUnderlying;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong GetUInt64<T>(T value)
            where T : unmanaged
        {
            /*
            Works because T is constrained to unmanaged, so it's safe to reinterpret
            All enums are value types and have a fixed size
         */
            return sizeof(T) switch
            {
                1 => *(byte*)&value,
                2 => *(ushort*)&value,
                4 => *(uint*)&value,
                8 => *(ulong*)&value,
                _ => throw new ArgumentException(
                    $"Unsupported enum size: {sizeof(T)} for type {typeof(T)}"
                ),
            };
        }
    }
}
