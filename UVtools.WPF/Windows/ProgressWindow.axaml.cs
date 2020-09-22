/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Diagnostics;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using UVtools.Core.Operations;
using UVtools.WPF.Structures;

namespace UVtools.WPF.Windows
{
    public class ProgressWindow : Window, IDisposable
    {
        public Stopwatch StopWatch { get; } = new Stopwatch();
        public OperationProgress Progress { get; } = new OperationProgress();

        private LogItem _logItem = new LogItem();

        private Timer _timer;

        public bool CanCancel => Progress?.CanCancel ?? false;

        public ProgressWindow()
        {
            InitializeComponent();
            DataContext = Progress;
            _timer = new Timer(100) {AutoReset = true};
            _timer.Elapsed += (sender, args) =>
            {
                Progress.TriggerRefresh();
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _timer.Stop();
            Progress.StopWatch.Stop();
            _logItem.ElapsedTime = Math.Round(Progress.StopWatch.Elapsed.TotalSeconds, 2);
            App.MainWindow.AddLog(_logItem);
        }

        public void SetTitle(string title)
        {
            Progress.Title = title;
            _logItem.Description = title;
        }

        public OperationProgress RestartProgress(bool canCancel = true)
        {
            Progress.CanCancel = canCancel;
            Progress.StopWatch.Restart();

            if (!_timer.Enabled)
            {
                _timer.Enabled = true;
                _timer.Start();
            }

            Dispatcher.UIThread.InvokeAsync(() => DataContext = Progress);
            return Progress;
        }

        public void Dispose()
        {
            _timer.Close();
            _timer.Dispose();
        }
    }
}
