using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BrightIdeasSoftware;

namespace UVtools.GUI.Controls
{
    public sealed class LogItem : INotifyPropertyChanged
    {
        private int _index;
        private string _startTime;
        private decimal _elapsedTime;
        private string _description;

        [OLVColumn(Width = 40, Title = "#")]
        public int Index
        {
            get => _index;
            set => SetField(ref _index, value);
        }

        [OLVColumn(Width = 80, Title = "Started")]
        public string StartTime
        {
            get => _startTime;
            set => SetField(ref _startTime, value);
        }

        [OLVColumn(Width = 70, Title = "Time(s)")]
        public decimal ElapsedTime
        {
            get => _elapsedTime;
            set => SetField(ref _elapsedTime, Math.Round(value, 2));
        }

        [OLVColumn(Width = 0)]
        public string Description
        {
            get => _description;
            set => SetField(ref _description, value);
        }

        public LogItem(int index, string description, decimal elapsedTime = 0)
        {
            _index = index;
            _description = description;
            _elapsedTime = elapsedTime;
            _startTime = DateTime.Now.ToString("HH:mm:ss");
        }

        public LogItem(string description, uint elapsedTime = 0) : this(0, description, elapsedTime)
        { }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

    }
}
