using System;
using System.Diagnostics;
using System.IO;

namespace UVtools.WPF
{
    public static class ErrorLog
    {
        private const string Filename = "errors.log";

        public static string FullPath = Path.Combine(UserSettings.SettingsFolder, Filename);

        public static void AppendLine(string errorType, string text)
        {
            try
            {
                File.AppendAllText(FullPath,
                    $"({errorType}) @ {DateTime.Now}: {text}{Environment.NewLine}");
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }

        public static StreamWriter AppendText()
        {
            return File.AppendText(FullPath);
        }
    }
}
