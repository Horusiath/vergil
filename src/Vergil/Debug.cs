#region copyright
// -----------------------------------------------------------------------
// <copyright file="Debug.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

using System.Runtime.CompilerServices;

namespace Vergil
{
    internal class AssertionException : VergilException
    {
        public AssertionException(string message) : base(message)
        {
        }
    }

    internal static class Debug
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string msg)
        {
#if DEBUG
            if (!predicate)
                throw new AssertionException(msg);
#endif
        }
    }
}