/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace UVtools.Core.Scripting;

public static class ScriptParser
{
    public static string ParseScriptFromFile(string path)
    {
        return ParseScriptFromText(File.ReadAllText(path));
    }

    /// <summary>
    /// Parse the script and clean forbidden keywords
    /// </summary>
    /// <param name="text">Text to parse</param>
    /// <returns>The parsed text</returns>
    public static string ParseScriptFromText(string text)
    {
        if(!Regex.Match(text, @"(void\s+ScriptInit\s*\(\s*\))").Success)
        {
            throw new ArgumentException("The method \"void ScriptInit()\" was not found on script, please verify the script.");
        }
        if (!Regex.Match(text, @"(string\s*[?]?\s+ScriptValidate\s*\(\s*\))").Success)
        {
            throw new ArgumentException("The method \"string ScriptValidate()\" was not found on script, please verify the script.");
        }
        if (!Regex.Match(text, @"(bool\s+ScriptExecute\s*\(\s*\))").Success)
        {
            throw new ArgumentException("The method \"bool ScriptExecute()\" was not found on script, please verify the script.");
        }

        var textLength = text.Length;
        sbyte bracketsToRemove = 0;
        text = Regex.Replace(text, @"(namespace\s+.+\n*\s*{)", string.Empty);
        if (textLength != text.Length) bracketsToRemove++;
        else text = Regex.Replace(text, @"(namespace\s+.+\n*\s*;)", string.Empty); // NET >= 6.0

        textLength = text.Length;
        var regex = new Regex(@"(.*class\s+.*\n*.*{)");
        text = regex.Replace(text, string.Empty, 1);
        if (textLength != text.Length) bracketsToRemove++;

        if (bracketsToRemove <= 0) return text;

        for (textLength = text.Length - 1; textLength >= 0 && bracketsToRemove > 0; textLength--)
        {
            if (text[textLength] == '}') bracketsToRemove--;
        }

        return text[..textLength];
    }
}