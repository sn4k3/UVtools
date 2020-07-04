using System;
using System.Threading;

namespace UVtools.Core.Operations
{
    public sealed class OperationProgress
    {
        public const string StatusDecodeThumbnails = "Decoding Thumbnails";
        public const string StatusGatherLayers = "Gathering Layers";
        public const string StatusDecodeLayers = "Decoding Layers";
        public const string StatusEncodeLayers = "Encoding Layers";
        public const string StatusWritingFile = "Writing File";
        public const string StatusEncodeGcode = "Encoding GCode";

        public const string StatusOptimizingBounds = "Gathering Bounds";
        public const string StatusCalculatingBounds = "Calculating Bounds";

        public const string StatusExtracting = "Extracting";

        public const string StatusIslands = "Islands";
        public const string StatusResinTraps = "Resin traps";
        public const string StatusRepairLayers = "Repair Layers";

        public object Mutex = new object();

        public CancellationTokenSource TokenSource { get; } = new CancellationTokenSource();
        public CancellationToken Token => TokenSource.Token;

        private bool _canCancel = true;

        public OperationProgress()
        {
        }

        public OperationProgress(bool canCancel)
        {
            _canCancel = canCancel;
        }

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
            set => _canCancel = value;
        }

        /// <summary>
        /// Gets or sets the item name for the operation
        /// </summary>
        public string ItemName { get; set; } = StatusDecodeLayers;

        /// <summary>
        /// Gets or sets the number of processed items
        /// </summary>
        public uint ProcessedItems { get; set; }

        /// <summary>
        /// Gets or sets the total of item count on this operation
        /// </summary>
        public uint ItemCount { get; set; }

        /// <summary>
        /// Gets the remaining items to be processed
        /// </summary>
        public uint RemainingItems => ItemCount - ProcessedItems;

        public int ProgressStep => (int) Math.Min(ProcessedItems * 100 / ItemCount, 100);

        /// <summary>
        /// Gets the progress from 0 to 100%
        /// </summary>
        public double ProgressPercent => Math.Round(ProcessedItems * 100.0 / ItemCount, 2);

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

        public void Reset(string name = "", uint itemCount = 0, uint items = 0)
        {
            ItemName = name;
            ItemCount = itemCount;
            ProcessedItems = items;
        }


        public override string ToString()
        {
            return ItemCount == 0 ?
                $"{ProcessedItems} / ? {ItemName}" :
                $"{RemainingItems} / {ItemCount} / {ProcessedItems} | {ItemName} | {ProgressPercent:0.00}%";
        }
    }
}
