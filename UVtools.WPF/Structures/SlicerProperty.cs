using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Avalonia;
using ReactiveUI;

namespace UVtools.WPF.Structures
{
    public class SlicerProperty : ReactiveObject
    {
        private string _name;
        private string _value;
        private string _group;

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public string Value
        {
            get => _value;
            set => this.RaiseAndSetIfChanged(ref _value, value);
        }

        public string Group
        {
            get => _group;
            set => this.RaiseAndSetIfChanged(ref _group, value);
        }

        public SlicerProperty(string name, string value, string group = null)
        {
            _name = name;
            _value = value;
            _group = group;
        }
    }
}
