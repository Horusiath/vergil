#region copyright
// -----------------------------------------------------------------------
// <copyright file="Exceptions.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Runtime.Serialization;

namespace Vergil
{
    public abstract class VergilException : Exception
    {
        protected VergilException()
        {
        }

        protected VergilException(string message) : base(message)
        {
        }

        protected VergilException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected VergilException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}