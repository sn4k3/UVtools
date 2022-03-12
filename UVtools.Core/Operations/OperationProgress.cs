/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Diagnostics;
using System.Threading;
using UVtools.Core.Extensions;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations;

public sealed class OperationProgress : BindableBase
{
    public const string StatusDecodePreviews = "Decoded Previews";
    public const string StatusGatherLayers = "Gathered Layers";
    public const string StatusDecodeLayers = "Decoded Layers";
    public const string StatusEncodePreviews = "Encoded Previews";
    public const string StatusEncodeLayers = "Encoded Layers";
    public const string StatusWritingFile = "Writing File";
    public const string StatusDecodeGcode = "Decoding GCode";
    public const string StatusEncodeGcode = "Encoding GCode";

    public const string StatusOptimizingBounds = "Gathering Bounds";
    public const string StatusCalculatingBounds = "Calculating Bounds";

    public const string StatusExtracting = "Extracting";

    public const string StatusIslands = "Layers processed (Islands/Overhangs/Resin traps)";
    public const string StatusResinTrapsOptimized = "Layers optimized (Resin traps)";
    public const string StatusResinTraps = "Layers processed (Resin traps)";
    public const string StatusRepairLayers = "Repaired Layers";

    public readonly object Mutex = new();

    public CancellationTokenSource TokenSource { get; private set; } = null!;
    public CancellationToken Token => TokenSource.Token;

    private bool _canCancel = true;
    private string _title = "Operation";
    private string _itemName = "Initializing";
    private uint _processedItems;
    private uint _itemCount;
        

    public OperationProgress()
    {
        Init();
    }

    public OperationProgress(string name, uint value = 0) : this()
    {
        Reset(name, value);
    }

    public OperationProgress(bool canCancel) : this()
    {
        _canCancel = canCancel;
    }

    public Stopwatch StopWatch { get; } = new ();

    /// <summary>
    /// Gets or sets if operation can be cancelled
    /// </summary>
    public bool CanCancel
    {
        get
        {
            if (!_canCancel) return _canCancel;
            return !Token.IsCancellationRequested && Token.CanBeCanceled && _canCancel;
        }
        set => RaiseAndSetIfChanged(ref _canCancel, value);
    }

    /// <summary>
    /// Gets or sets the item name for the operation
    /// </summary>
    public string Title
    {
        get => _title;
        set => RaiseAndSetIfChanged(ref _title, value);
    }

    public string ElapsedTimeStr => $"{StopWatch.Elapsed.Minutes}m {StopWatch.Elapsed.Seconds}s";
    //{StopWatch.Elapsed.Milliseconds} ms

    /// <summary>
    /// Gets or sets the item name for the operation
    /// </summary>
    public string ItemName
    {
        get => _itemName;
        set
        {
            if(!RaiseAndSetIfChanged(ref _itemName, value)) return;
            RaisePropertyChanged(nameof(Description));
        }
    }

    /// <summary>
    /// Gets or sets the number of processed items
    /// </summary>
    public uint ProcessedItems
    {
        get => _processedItems;
        set
        {
            //_processedItems = value;
            if(!RaiseAndSetIfChanged(ref _processedItems, value)) return;
            RaisePropertyChanged(nameof(ProgressPercent));
            RaisePropertyChanged(nameof(Description));
        }
    }

    /// <summary>
    /// Gets or sets the total of item count on this operation
    /// </summary>
    public uint ItemCount
    {
        get => _itemCount;
        set
        {
            RaiseAndSetIfChanged(ref _itemCount, value);
            RaisePropertyChanged(nameof(IsIndeterminate));
            RaisePropertyChanged(nameof(ProgressPercent));
            RaisePropertyChanged(nameof(Description));
        }
    }

    /// <summary>
    /// Gets or sets an tag
    /// </summary>
    public object? Tag { get; set; }

    /// <summary>
    /// Gets the remaining items to be processed
    /// </summary>
    public uint RemainingItems => _itemCount - _processedItems;

    public int ProgressStep => (int)ProgressPercent;

    public string Description => ToString();

    public bool IsIndeterminate => _itemCount == 0;

    /// <summary>
    /// Gets the progress from 0 to 100%
    /// </summary>
    public double ProgressPercent => _itemCount == 0 ? 0 : Math.Round(_processedItems * 100.0 / _itemCount, 2).Clamp(0, 100);

    public static OperationProgress operator +(OperationProgress progress, uint value)
    {
        progress.ProcessedItems += value;
        return progress;
    }

    public static OperationProgress operator ++(OperationProgress progress)
    {
        progress.ProcessedItems++;
        return progress;
    }

    public static OperationProgress operator --(OperationProgress progress)
    {
        progress.ProcessedItems--;
        return progress;
    }

    public void Init(bool canCancel = true)
    {
        CanCancel = canCancel;
        Title = "Operation";
        ItemName = "Initializing";
        ItemCount = 0;
        ProcessedItems = 0;

        TokenSource = new();
        RaisePropertyChanged(nameof(CanCancel));
    }

    public void ResetAll(string title, string name = "", uint itemCount = 0, uint items = 0)
    {
        Title = title;
        Reset(name, itemCount, items);
    }

    public void Reset(string name = "", uint itemCount = 0, uint items = 0)
    {
        ItemName = name;
        ItemCount = itemCount;
        ProcessedItems = items;
    }

    public void ResetNameAndProcessed(string name = "", uint items = 0)
    {
        ItemName = name;
        ProcessedItems = items;
    }


    public override string ToString()
    {
        if (_itemCount == 0 && _processedItems == 0)
        {
            return $"{_itemName}";
        }

        if (_itemCount == 0 && _processedItems > 0)
        {
            return $"{_processedItems} {_itemName}";
        }

        return $"{_processedItems.ToString().PadLeft(_itemCount.ToString().Length, '0')}/{_itemCount} {_itemName} | {ProgressPercent:F2}%";
        /*return _itemCount == 0 ?
$"{_processedItems}/? {_itemName}" :
$"{_processedItems.ToString().PadLeft(_itemCount.ToString().Length, '0')}/{_itemCount} {_itemName} | {ProgressPercent:F2}%";*/
    }

    public void TriggerRefresh()
    {
        RaisePropertyChanged(nameof(ElapsedTimeStr));
        RaisePropertyChanged(nameof(CanCancel));
        //OnPropertyChanged(nameof(ProgressPercent));
        //OnPropertyChanged(nameof(Description));
    }

    public void LockAndIncrement()
    {
        /*lock (Mutex)
        {
            ProcessedItems++;
        }*/
        Interlocked.Increment(ref _processedItems);
        RaisePropertyChanged(nameof(ProcessedItems));
        RaisePropertyChanged(nameof(ProgressPercent));
        RaisePropertyChanged(nameof(Description));
    }
}