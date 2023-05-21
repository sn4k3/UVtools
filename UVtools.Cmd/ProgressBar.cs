/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
namespace UVtools.Cmd;

using System;
using System.Text;
using System.Threading;

/// <summary>
/// An ASCII progress bar
/// </summary>
public class ProgressBar : IDisposable
{
    private const byte BlockCount = 10;
    private readonly TimeSpan _animationInterval = TimeSpan.FromMilliseconds(125);
    private const string Animation = @"|/-\";

    private readonly Timer _timer;

    private string _currentText = string.Empty;
    private bool _disposed;
    private int _animationIndex;

    private double _lastProgressPercent = -1;

    private readonly StringBuilder _outputBuilder = new();

    public ProgressBar()
    {
        _timer = new Timer(TimerHandler);

        // A progress bar is only for temporary display in a console window.
        // If the console output is redirected to a file, draw nothing.
        // Otherwise, we'll end up with a lot of garbage in the target file.
        if (Program.Quiet || Program.NoProgress) return;
        if (Console.IsOutputRedirected) Console.WriteLine();
        ResetTimer();
    }

    private void TimerHandler(object? state)
    {
        lock (_timer)
        {
            if (_disposed) return;


            if (Math.Abs(_lastProgressPercent - Program.Progress.ProgressPercent) > 0.009)
            {
                _lastProgressPercent = Program.Progress.ProgressPercent;
                var progressBlockCount = (int)(_lastProgressPercent * BlockCount / 100);

                if (Console.IsOutputRedirected)
                {
                    var text = string.Format("[{0}{1}] {2:F2}% ({3:F2}s)",
                        new string('#', progressBlockCount), new string('-', BlockCount - progressBlockCount),
                        Program.Progress.ProgressPercent,
                        Program.StopWatch.ElapsedMilliseconds / 1000.0);
                    Console.WriteLine(text);
                }
                else
                {
                    var text = string.Format("[{0}{1}] {2:F2}% {3} {4:F2}s",
                        new string('#', progressBlockCount), new string('-', BlockCount - progressBlockCount),
                        Program.Progress.ProgressPercent,
                        Animation[_animationIndex++ % Animation.Length],
                        Program.StopWatch.ElapsedMilliseconds / 1000.0);
                    UpdateText(text);
                }
            }

            ResetTimer();
        }
    }

    private void UpdateText(string text)
    {
        if (Console.IsOutputRedirected) return;
        // Get length of common portion
        var commonPrefixLength = 0;
        var commonLength = Math.Min(_currentText.Length, text.Length);
        while (commonPrefixLength < commonLength && text[commonPrefixLength] == _currentText[commonPrefixLength])
        {
            commonPrefixLength++;
        }

        // Backtrack to the first differing character
        _outputBuilder.Clear();
        _outputBuilder.Append('\b', _currentText.Length - commonPrefixLength);

        // Output new suffix
        _outputBuilder.Append(text[commonPrefixLength..]);

        // If the new text is shorter than the old one: delete overlapping characters
        var overlapCount = _currentText.Length - text.Length;
        if (overlapCount > 0)
        {
            _outputBuilder.Append(' ', overlapCount);
            _outputBuilder.Append('\b', overlapCount);
        }

        Console.Write(_outputBuilder);
        _currentText = text;
    }

    private void ResetTimer()
    {
        _timer.Change(_animationInterval, TimeSpan.FromMilliseconds(-1));
    }

    public void Dispose()
    {
        TimerHandler(null);
        lock (_timer)
        {
            _disposed = true;
            UpdateText(string.Empty);
        }
        _timer.Dispose();
    }

}