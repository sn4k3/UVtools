using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BrightIdeasSoftware;

namespace UVtools.GUI.Controls
{
    public sealed class Log : INotifyPropertyChanged
    {
        private int _index;
        private string _time;
        private string _description;

        [OLVColumn(Width = 50, Title = "#")]
        public int Index
        {
            get => _index;
            set => SetField(ref _index, value);
        }

        [OLVColumn(Width = 90)]
        public string Time
        {
            get => _time;
            set => SetField(ref _time, value);
        }

        [OLVColumn(Width = 0)]
        public string Description
        {
            get => _description;
            set => SetField(ref _description, value);
        }

        public Log(int index, string description)
        {
            _index = index;
            _description = description;
            _time = DateTime.Now.ToString("HH:mm:ss");
        }

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
