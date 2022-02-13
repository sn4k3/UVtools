/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Avalonia;
using Avalonia.Markup.Xaml;
using MessageBox.Avalonia.Enums;
using UVtools.WPF.Controls;
using UVtools.WPF.Extensions;

namespace UVtools.WPF.Windows
{
    public partial class MissingInformationWindow : WindowEx
    {
        #region Members
        private decimal _layerHeight;
        private decimal _displayWidth;
        private decimal _displayHeight;
        #endregion

        #region Properties
        public decimal LayerHeight
        {
            get => _layerHeight;
            set => RaiseAndSetIfChanged(ref _layerHeight, value);
        }

        public bool LayerHeightIsVisible => SlicerFile?.LayerHeight <= 0;

        public decimal DisplayWidth
        {
            get => _displayWidth;
            set => RaiseAndSetIfChanged(ref _displayWidth, value);
        }

        public bool DisplayWidthIsVisible => SlicerFile?.DisplayWidth <= 0;

        public decimal DisplayHeight
        {
            get => _displayHeight;
            set => RaiseAndSetIfChanged(ref _displayHeight, value);
        }

        public bool DisplayHeightIsVisible => SlicerFile?.DisplayHeight <= 0;
        #endregion

        public MissingInformationWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            if (SlicerFile is not null)
            {
                _layerHeight = (decimal) SlicerFile.LayerHeight;
                _displayWidth = (decimal) SlicerFile.DisplayWidth;
                _displayHeight = (decimal) SlicerFile.DisplayHeight;
            }

            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public async void Apply()
        {
            if (await this.MessageBoxQuestion("Are you sure you want to submit and apply the information?", "Submit and apply the information?") != ButtonResult.Yes) return;

            if ((decimal)SlicerFile.DisplayWidth != _displayWidth && _displayWidth > 0)
            {
                SlicerFile.DisplayWidth = (float) _displayWidth;
            }
            if ((decimal)SlicerFile.DisplayHeight != _displayHeight && _displayHeight > 0)
            {
                SlicerFile.DisplayHeight = (float)_displayHeight;
            }
            if ((decimal)SlicerFile.LayerHeight != _layerHeight && _layerHeight > 0)
            {
                SlicerFile.LayerHeight = (float)_layerHeight;
                SlicerFile.LayerManager.RebuildLayersProperties();
            }

            DialogResult = DialogResults.OK;
            Close();
        }
    }
}
