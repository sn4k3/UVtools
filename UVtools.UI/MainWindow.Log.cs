/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UVtools.Core.SystemOS;
using UVtools.UI.Structures;

namespace UVtools.UI;

public partial class MainWindow
{
    public RangeObservableCollection<LogItem> Logs { get; } = new();
    private bool _isVerbose;

    public bool IsVerbose
    {
        get => _isVerbose;
        set => RaiseAndSetIfChanged(ref _isVerbose, value);
    }

    public void AddLog(LogItem log)
    {
        log.Index = Logs.Count;
        Logs.Insert(0, log);

        if (log.ElapsedTime >= Settings.General.NotificationBeepActivateAboveTime)
        {
            App.BeepIfAble();
        }
    }

    public void AddLog(string description, double elapsedTime = 0)
    {
        AddLog(new LogItem(Logs.Count, description, elapsedTime));
    }

    public void AddLogVerbose(string description, double elapsedTime = 0)
    {
        Debug.WriteLine($"{description} ({elapsedTime}s)");
        if (!_isVerbose) return;
        AddLog(description, elapsedTime);
    }
}