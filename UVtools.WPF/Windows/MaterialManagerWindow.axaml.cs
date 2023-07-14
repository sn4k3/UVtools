using System;
using System.Linq;
using UVtools.Core.Dialogs;
using UVtools.Core.Managers;
using UVtools.Core.Objects;
using UVtools.WPF.Controls;
using UVtools.WPF.Extensions;

namespace UVtools.WPF.Windows;

public partial class MaterialManagerWindow : WindowEx
{
    private Material _material = new();
    public MaterialManager Manager => MaterialManager.Instance;

    public Material Material
    {
        get => _material;
        set => RaiseAndSetIfChanged(ref _material, value);
    }

    public MaterialManagerWindow()
    {
        InitializeComponent();


        MaterialManager.Load(); // Reload

        DataContext = this;
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
                                          $"{Material}") != MessageButtonResult.Yes) return;

        Manager.Add(Material);
        Manager.SortByName();
        MaterialManager.Save();
        Material = new();
    }

    public async void RemoveSelectedMaterials()
    {
        if (MaterialsTable.SelectedItems.Count <= 0) return;
        if (await this.MessageBoxQuestion($"Are you sure you want to remove {MaterialsTable.SelectedItems.Count} materials?") != MessageButtonResult.Yes) return;
        Manager.RemoveRange(MaterialsTable.SelectedItems.Cast<Material>());
        MaterialManager.Save();
    }

    public async void ClearMaterials()
    {
        if (Manager.Count == 0) return;
        if (await this.MessageBoxQuestion($"Are you sure you want to clear {Manager.Count} materials?") != MessageButtonResult.Yes) return;
        Manager.Clear(true);
    }
}