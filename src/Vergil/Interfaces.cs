#region copyright
// -----------------------------------------------------------------------
// <copyright file="Interfaces.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

namespace Vergil
{
    /// <summary>
    /// Allows for a partial comparison of values. Unlike normal comparison
    /// which recognizes, only greater than, less than and equal correspondence,
    /// partial comparison also allows to detect concurrent values (represented
    /// by null result).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPartiallyComparable<in T>
    {
        int? PartiallyCompareTo(T other);
    }

    /// <summary>
    /// Allows for a partial comparison of values. Unlike normal comparison
    /// which recognizes, only greater than, less than and equal correspondence,
    /// partial comparison also allows to detect concurrent values (represented
    /// by null result).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPartialComparer<in T>
    {
        int? PartiallyCompare(T x, T y);
    }

    /// <summary>
    /// Interface which can be implemented to assure the convergence of two structures of the same type.
    /// Convergent types specify a <see cref="Merge"/> operation, which given two instances with potentially
    /// different and conflicting state will produce a result with a state that is a consistent product of those.
    ///
    /// A <see cref="Merge"/> must satisfy following properties:
    /// 1. Associativity
    /// 2. Commutativity
    /// 3. Idempotence
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConvergent<T> where T : IConvergent<T>
    {
        T Merge(T other);
    }
}