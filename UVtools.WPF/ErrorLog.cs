using System;
using System.Diagnostics;
using System.IO;
using UVtools.Core;

namespace UVtools.WPF;

public static class ErrorLog
{
    private const string Filename = "errors.log";

    public static string FullPath => Path.Combine(CoreSettings.DefaultSettingsFolderAndEnsureCreation, Filename);

    public static void AppendLine(string errorType, string text)
    {
        try
        {
            File.AppendAllText(FullPath, $"[v{About.VersionStr}] ({errorType}) @ {DateTime.Now}: {text}{Environment.NewLine}");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            Debug.WriteLine(exception);
        }
    }

    public static StreamWriter GetStreamWriter()
    {
        return File.AppendText(FullPath);
    }
}