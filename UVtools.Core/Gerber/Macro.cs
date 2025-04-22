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
using System.Text.RegularExpressions;
using UVtools.Core.Gerber.Primitives;

namespace UVtools.Core.Gerber;

public class Macro : IReadOnlyList<Primitive>
{
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

        // 0 Comment: A comment string
        if (line[0] == '0')
        {
            if(line.Length > 2) Primitives.Add(new CommentPrimitive(Document, line[2..]));
            return;
        }

        var commaSplit = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if(!byte.TryParse(commaSplit[0], out var code)) return;
        switch (code)
        {
            // 1 Circle: Exposure, Diameter, Center X, Center Y[, Rotation]
            case CirclePrimitive.Code:
            {
                var primitive = new CirclePrimitive(Document, commaSplit[1], commaSplit[2], commaSplit[3], commaSplit[4]);
                if (commaSplit.Length > 5) primitive.RotationExpression = commaSplit[5];
                Primitives.Add(primitive);
                break;
            }
            // 20 Vector Line: Exposure, Width, Start X, Start Y, End X, End Y, Rotation
            case VectorLinePrimitive.Code:
            {
                var primitive = new VectorLinePrimitive(Document, commaSplit[1], commaSplit[2], commaSplit[3], commaSplit[4], commaSplit[5], commaSplit[6]);
                if (commaSplit.Length > 7) primitive.RotationExpression = commaSplit[7];
                Primitives.Add(primitive);
                break;
            }
            // 21 Center Line: Exposure, Width, Height, Center X, Center Y, Rotation
            case CenterLinePrimitive.Code:
            {
                var primitive = new CenterLinePrimitive(Document, commaSplit[1], commaSplit[2], commaSplit[3], commaSplit[4], commaSplit[5]);
                if (commaSplit.Length > 6) primitive.RotationExpression = commaSplit[6];
                Primitives.Add(primitive);
                break;
            }
            // 4 Outline: Exposure, # vertices, Start X, Start Y, Subsequent points..., Rotation
            case OutlinePrimitive.Code:
            {
                Primitives.Add(new OutlinePrimitive(Document, commaSplit[1], commaSplit[3..^1], commaSplit[^1]));
                break;
            }
            // 5 Outline: Exposure, # vertices, Center X, Center Y, Diameter, Rotation
            case PolygonPrimitive.Code:
            {
                var primitive = new PolygonPrimitive(Document, commaSplit[1], commaSplit[2], commaSplit[3], commaSplit[4], commaSplit[5]);
                if (commaSplit.Length > 6) primitive.RotationExpression = commaSplit[6];
                Primitives.Add(primitive);
                break;
            }
        }
    }

    public Macro Clone()
    {
        var macro = new Macro(Document, Name);
        foreach (var primitive in Primitives)
        {
            macro.Primitives.Add(primitive.Clone());
        }

        return macro;
    }


    public static Macro? Parse(GerberFormat document, string line)
    {
        var match = Regex.Match(line, @"%?AM([a-zA-Z0-9]+)\*?");
        if (!match.Success || match.Groups.Count < 2) return null;

        return new Macro(document, match.Groups[1].Value);
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