using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System;

namespace DanielWillett.ModularRpcs.Exceptions;

/// <summary>
/// Thrown when an internal member should be found but isn't.
/// </summary>
public class UnexpectedMemberAccessException : MemberAccessException
{
    public IMemberDefinition Member { get; }
    public UnexpectedMemberAccessException(IMemberDefinition member)
        : base(string.Format(Properties.Exceptions.UnexpectedMemberAccessExceptionFailedToFind, member.Format(Accessor.ExceptionFormatter)))
    {
        Member = member;
    }
}
