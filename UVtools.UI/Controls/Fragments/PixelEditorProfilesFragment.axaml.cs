using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using UVtools.Core.Dialogs;
using UVtools.Core.PixelEditor;
using UVtools.UI.Extensions;
using UVtools.UI.Structures;

namespace UVtools.UI.Controls.Fragments
{
    public class PixelEditorProfilesFragment : TemplatedControl
    {
        public static readonly DirectProperty<PixelEditorProfilesFragment, RangeObservableCollection<PixelOperation>?> ProfilesProperty =
            AvaloniaProperty.RegisterDirect<PixelEditorProfilesFragment, RangeObservableCollection<PixelOperation>?>(
                nameof(Profiles),
                o => o.Profiles,
                (o, v) => o.Profiles = v);

        private RangeObservableCollection<PixelOperation>? _profiles;

        public RangeObservableCollection<PixelOperation>? Profiles
        {
            get => _profiles;
            set => SetAndRaise(ProfilesProperty, ref _profiles, value);
        }

        public static readonly DirectProperty<PixelEditorProfilesFragment, int> SelectedProfileIndexProperty =
            AvaloniaProperty.RegisterDirect<PixelEditorProfilesFragment, int>(
                nameof(SelectedProfileIndex),
                o => o.SelectedProfileIndex,
                (o, v) => o.SelectedProfileIndex = v, -1, BindingMode.TwoWay);

        private int _selectedProfileIndex;

        public int SelectedProfileIndex
        {
            get => _selectedProfileIndex;
            set => SetAndRaise(SelectedProfileIndexProperty, ref _selectedProfileIndex, value);
        }

        public static readonly DirectProperty<PixelEditorProfilesFragment, PixelOperation?> SourceProperty =
            AvaloniaProperty.RegisterDirect<PixelEditorProfilesFragment, PixelOperation?>(
                nameof(Source),
                o => o.Source,
                (o, v) => o.Source = v);

        private PixelOperation? _source;

        public PixelOperation? Source
        {
            get => _source;
            set => SetAndRaise(SourceProperty, ref _source, value);
        }

        public void AddNewProfile()
        {
            if (_source is null || _profiles is null) return;

            if (_profiles.FirstOrDefault(operation => Equals(operation, _source)) is not null) return;

            var clone = _source.Clone();
            _profiles.Add(clone);
            PixelEditorProfiles.AddProfile(clone);
            SelectedProfileIndex = _profiles.Count - 1;
        }

        public async void DefaultSelectedProfile()
        {
            if (_selectedProfileIndex <= -1 || _profiles is null) return;

            if (await App.MainWindow.MessageBoxQuestion($"Are you sure you want to mark the selected profile as default?\n{_profiles[_selectedProfileIndex]}", "Mark the selected profile as default?") != MessageButtonResult.Yes) return;

            foreach (var profile in _profiles)
            {
                profile.ProfileIsDefault = false;
            }

            _profiles[_selectedProfileIndex].ProfileIsDefault = true;
            PixelEditorProfiles.Save();
        }

        public async void RemoveSelectedProfile()
        {
            if (_selectedProfileIndex <= -1 || _profiles is null) return;

            if (await App.MainWindow.MessageBoxQuestion($"Are you sure you want to remove the selected profile?\n{_profiles[_selectedProfileIndex]}", "Remove the selected profile?") != MessageButtonResult.Yes) return;

            PixelEditorProfiles.RemoveProfile(_profiles[_selectedProfileIndex]);
            _profiles.RemoveAt(_selectedProfileIndex);
            SelectedProfileIndex = -1;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == SelectedProfileIndexProperty)
            {
                if (_source is not null && _profiles is not null && _selectedProfileIndex >= 0)
                {
                    _profiles[_selectedProfileIndex].CopyTo(_source);
                }
            }
            base.OnPropertyChanged(change);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            if (_profiles is null || _profiles.Count == 0) return;

            for (var i = 0; i < _profiles.Count; i++)
            {
                if (!_profiles[i].ProfileIsDefault) continue;
                SelectedProfileIndex = i;
                return;
            }

            SelectedProfileIndex = 0;
        }
    }
}
