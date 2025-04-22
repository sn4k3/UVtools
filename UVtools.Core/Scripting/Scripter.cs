using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Threading.Tasks;
using Emgu.CV;
using System.Runtime.InteropServices;

namespace UVtools.Core.Scripting;

public class Scripter
{
    public static string[] Imports =>
    [
        "System",
        "System.Collections.Generic",
        "System.Math",
        "System.IO",
        "System.Linq",
        "System.Threading",
        "System.Threading.Tasks",
        "UVtools.Core",
        "UVtools.Core.EmguCV",
        "UVtools.Core.Extensions",
        "UVtools.Core.FileFormats",
        "UVtools.Core.GCode",
        "UVtools.Core.Gerber",
        "UVtools.Core.Layers",
        "UVtools.Core.Managers",
        "UVtools.Core.MeshFormats",
        "UVtools.Core.Objects",
        "UVtools.Core.Operations",
        "UVtools.Core.PixelEditor",
        "UVtools.Core.Printer",
        "UVtools.Core.Slicer",
        "UVtools.Core.Suggestions",
        "UVtools.Core.SystemOS"
    ];

    public static Task<ScriptState<object>> RunScript(string text, object? globals = null, CancellationToken token = default)
    {
        return CSharpScript.RunAsync(text,
            ScriptOptions.Default
                .AddReferences(typeof(About).Assembly)
                .AddImports(Imports
                )
                .WithAllowUnsafe(true), globals, null, token);
    }
}