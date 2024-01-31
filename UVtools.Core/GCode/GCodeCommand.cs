/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;

namespace UVtools.Core.GCode;

public class GCodeCommand
{
    /// <summary>
    /// Gets or sets if this command is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the command name
    /// </summary>
    public string Command { get; set; } = null!;

    /// <summary>
    /// Gets or sets the arguments for this command
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the comment
    /// </summary>
    public string? Comment { get; set; }

    public GCodeCommand() { }

    public GCodeCommand(string command, string? arguments = null, string? comment = null, bool enabled = true)
    {
        Enabled = enabled;
        Command = command;
        Arguments = arguments;
        Comment = comment;
    }

    public void Set(string command, string? arguments, string? comment, bool enabled = true)
    {
        Enabled = enabled;
        Command = command;
        Arguments = arguments;
        Comment = comment;
    }

    public void Set(string command, string arguments)
    {
        Command = command;
        Arguments = arguments;
    }

    public void Set(string command)
    {
        Command = command;
    }


    public string ToString(bool showComment, bool showTailComma = true, string? overrideComment = null)
    {
        var result = Command;
        if (!string.IsNullOrWhiteSpace(Arguments))
            result += $" {Arguments}";

        if (result[0] == ';') return result;

        var comment = string.IsNullOrWhiteSpace(overrideComment) ? Comment : overrideComment;
        if (showComment && !string.IsNullOrWhiteSpace(comment))
        {
            result += $";{comment}";
        }
        else if (showTailComma)
        {
            result += ';';
        }
            
        return result;
    }
    public string ToString(bool showComment, bool showTailComma = true, params object[] args) =>
        string.Format(ToString(showComment, showTailComma), args);
    public string ToStringOverrideComment(string comment, params object[] args) => ToStringOverrideComment(true, true, comment, args);
    public string ToStringOverrideComment(bool showComment, bool showTailComma, string? comment, params object[] args) =>
        string.Format(ToString(showComment, showTailComma, comment), args);
    public string ToString(params object[] args) => ToString(true, true, args);
    public string ToStringWithoutComments() => ToString(false, false);
    public string ToStringWithoutComments(params object[] args) => ToString(false, false, args);
        
    public override string ToString()
    {
        var result = Command;
        if (!string.IsNullOrWhiteSpace(Arguments))
            result += $" {Arguments}";
        if (!string.IsNullOrWhiteSpace(Comment))
            result += $";{Comment}";
        return result;
    }

    protected bool Equals(GCodeCommand other)
    {
        return Command == other.Command;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((GCodeCommand) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Command, Arguments);
    }
}