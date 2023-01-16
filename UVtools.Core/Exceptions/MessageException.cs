/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Runtime.Serialization;

namespace UVtools.Core.Exceptions;

/// <summary>
/// Generic exception to show only the message instead of the full stack-trace
/// </summary>
public class MessageException : Exception
{
    public string? Title { get; }

    protected MessageException(SerializationInfo info, StreamingContext context) : base(info, context)
    { }

    public MessageException(string? message, string? title = null) : base(message)
    {
        Title = title;
    }

    public MessageException(string? message, Exception? innerException, string? title = null) : base(message, innerException)
    {
        Title = title;
    }
}