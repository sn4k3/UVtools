/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using UVtools.Core.Gerber.Primitives;

namespace UVtools.Core.Gerber;

public class Macro : IReadOnlyList<Primitive>
{
    private sealed record VariableAssignment(int Index, string Expression);

    private readonly List<object> _statements = [];

    #region Properties

    public GerberFormat Document { get; init; }

    /// <summary>
    /// Gets the macro name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public List<Primitive> Primitives { get; } = [];
    #endregion

    public Macro(GerberFormat document)
    {
        Document = document;
    }

    public Macro(GerberFormat document, string name) : this(document)
    {
        Name = name;
    }

    public void ParsePrimitive(string line)
    {
        line = line.TrimEnd('%', '*');

        if(line.Length == 0) return;

        if (line[0] == '$')
        {
            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 1 ||
                !int.TryParse(line.AsSpan(1, separatorIndex - 1), NumberStyles.None,
                    CultureInfo.InvariantCulture, out var variableIndex) ||
                variableIndex <= 0)
            {
                return;
            }

            var expression = line[(separatorIndex + 1)..].Trim();
            if (expression.Length > 0)
            {
                _statements.Add(new VariableAssignment(variableIndex, expression));
            }
            return;
        }

        // 0 Comment: A comment string
        if (line[0] == '0')
        {
            if(line.Length > 2) AddPrimitive(new CommentPrimitive(Document, line[2..]));
            return;
        }

        var commaSplit = line.Split(',', StringSplitOptions.TrimEntries);
        if (commaSplit.Length == 0 || !byte.TryParse(commaSplit[0], out var code)) return;
        switch (code)
        {
            // 1 Circle: Exposure, Diameter, Center X, Center Y[, Rotation]
            case CirclePrimitive.Code:
            {
                if (commaSplit.Length < 5) return;
                var primitive = new CirclePrimitive(Document, commaSplit[1], commaSplit[2], commaSplit[3], commaSplit[4]);
                if (commaSplit.Length > 5) primitive.RotationExpression = commaSplit[5];
                AddPrimitive(primitive);
                break;
            }
            // 20 Vector Line: Exposure, Width, Start X, Start Y, End X, End Y, Rotation
            case VectorLinePrimitive.Code:
            {
                if (commaSplit.Length < 7) return;
                var primitive = new VectorLinePrimitive(Document, commaSplit[1], commaSplit[2], commaSplit[3], commaSplit[4], commaSplit[5], commaSplit[6]);
                if (commaSplit.Length > 7) primitive.RotationExpression = commaSplit[7];
                AddPrimitive(primitive);
                break;
            }
            // 21 Center Line: Exposure, Width, Height, Center X, Center Y, Rotation
            case CenterLinePrimitive.Code:
            {
                if (commaSplit.Length < 6) return;
                var primitive = new CenterLinePrimitive(Document, commaSplit[1], commaSplit[2], commaSplit[3], commaSplit[4], commaSplit[5]);
                if (commaSplit.Length > 6) primitive.RotationExpression = commaSplit[6];
                AddPrimitive(primitive);
                break;
            }
            // 4 Outline: Exposure, # vertices, Start X, Start Y, Subsequent points..., Rotation
            case OutlinePrimitive.Code:
            {
                if (commaSplit.Length < 12 || (commaSplit.Length - 4) % 2 != 0) return;
                AddPrimitive(new OutlinePrimitive(Document, commaSplit[1], commaSplit[2],
                    commaSplit[3..^1], commaSplit[^1]));
                break;
            }
            // 5 Polygon: Exposure, # vertices, Center X, Center Y, Diameter, Rotation
            case PolygonPrimitive.Code:
            {
                if (commaSplit.Length < 6) return;
                var primitive = new PolygonPrimitive(Document, commaSplit[1], commaSplit[2], commaSplit[3], commaSplit[4], commaSplit[5]);
                if (commaSplit.Length > 6) primitive.RotationExpression = commaSplit[6];
                AddPrimitive(primitive);
                break;
            }
        }
    }

    public Macro Clone()
    {
        var macro = new Macro(Document, Name);
        foreach (var statement in _statements)
        {
            if (statement is VariableAssignment assignment)
            {
                macro._statements.Add(assignment);
            }
            else if (statement is Primitive primitive)
            {
                macro.AddPrimitive(primitive.Clone());
            }
        }

        return macro;
    }

    public bool ParseExpressions(string[] suppliedArguments)
    {
        var arguments = (string[])suppliedArguments.Clone();
        using var evaluator = new DataTable();
        foreach (var statement in _statements)
        {
            if (statement is VariableAssignment assignment)
            {
                if (!Primitive.TryEvaluateExpression(evaluator, assignment.Expression,
                        arguments, out var result))
                {
                    return false;
                }

                if (assignment.Index >= arguments.Length)
                {
                    var previousLength = arguments.Length;
                    Array.Resize(ref arguments, assignment.Index + 1);
                    Array.Fill(arguments, "0", previousLength,
                        arguments.Length - previousLength);
                }
                arguments[assignment.Index] = result.ToString("R", CultureInfo.InvariantCulture);
            }
            else if (statement is Primitive primitive)
            {
                primitive.ParseExpressions(arguments);
                if (!primitive.IsParsed) return false;
            }
        }

        return true;
    }

    private void AddPrimitive(Primitive primitive)
    {
        Primitives.Add(primitive);
        _statements.Add(primitive);
    }


    public static Macro? Parse(GerberFormat document, string line)
    {
        line = line.Trim();
        if (line.StartsWith('%')) line = line[1..];
        if (!line.StartsWith("AM", StringComparison.Ordinal)) return null;

        var nameEnd = line.IndexOfAny(['*', '%'], 2);
        var name = (nameEnd < 0 ? line[2..] : line[2..nameEnd]).Trim();
        if (name.Length == 0 ||
            name.IndexOfAny([' ', '\t', '\r', '\n', ',']) >= 0)
        {
            return null;
        }

        return new Macro(document, name);
    }

    public IEnumerator<Primitive> GetEnumerator()
    {
        return Primitives.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable) Primitives).GetEnumerator();
    }

    public int Count => Primitives.Count;

    public Primitive this[int index] => Primitives[index];
}
