using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DynamicData;
using MessageBox.Avalonia.Enums;
using UVtools.Core.Managers;
using UVtools.Core.Objects;
using UVtools.WPF.Controls;
using UVtools.WPF.Extensions;

namespace UVtools.WPF.Windows
{
    public class MaterialManagerWindow : WindowEx
    {
        private Material _material = new();
        private readonly DataGrid _grid;
        public MaterialManager Manager => MaterialManager.Instance;

        public Material Material
        {
            get => _material;
            set => RaiseAndSetIfChanged(ref _material, value);
        }

        public MaterialManagerWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            _grid = this.FindControl<DataGrid>("MaterialsTable");

            MaterialManager.Load(); // Reload

            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            MaterialManager.Save(); // Apply changes
        }

        public void RefreshStatistics()
        {
            Manager.RaisePropertiesChanged();
        }

        public async void AddNewMaterial()
        {
            if (string.IsNullOrWhiteSpace(Material.Name))
            {
                await this.MessageBoxError("Material name can't be empty");
                return;
            }

            if (Manager.Contains(Material))
            {
                await this.MessageBoxError("A material with same name already exists.");
                return;
            }

            Material.BottleRemainingVolume = Material.BottleVolume;

            if (await this.MessageBoxQuestion("Are you sure you want to add the following material:\n" +
                                             $"{Material}") != ButtonResult.Yes) return;

            Manager.Add(Material);
            Manager.SortByName();
            MaterialManager.Save();
            Material = new();
        }

        public async void RemoveSelectedMaterials()
        {
            if (_grid.SelectedItems.Count <= 0) return;
            if (await this.MessageBoxQuestion($"Are you sure you want to remove {_grid.SelectedItems.Count} materials?") != ButtonResult.Yes) return;
            Manager.RemoveMany(_grid.SelectedItems.Cast<Material>());
            MaterialManager.Save();
        }

        public async void ClearMaterials()
        {
            if (Manager.Count == 0) return;
            if (await this.MessageBoxQuestion($"Are you sure you want to clear {Manager.Count} materials?") != ButtonResult.Yes) return;
            Manager.Clear(true);
        }
    }
}
