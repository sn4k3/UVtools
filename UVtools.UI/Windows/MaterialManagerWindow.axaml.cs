using SukiUI.MessageBox;
using System;
using System.Linq;
using System.Threading.Tasks;
using UVtools.Core.Dialogs;
using UVtools.Core.Managers;
using UVtools.UI.Extensions;

namespace UVtools.UI.Windows;

public partial class MaterialManagerWindow : GenericWindow
{
    public MaterialManager Manager => MaterialManager.Instance;

    public Core.Objects.Material Material
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    } = new();

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

    public async Task AddNewMaterial()
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
                                          $"{Material}") != SukiMessageBoxResult.Yes) return;

        Manager.Add(Material);
        Manager.SortByName();
        MaterialManager.Save();
        Material = new();
    }

    public async Task RemoveSelectedMaterials()
    {
        if (MaterialsTable.SelectedItems.Count <= 0) return;
        if (await this.MessageBoxQuestion($"Are you sure you want to remove {MaterialsTable.SelectedItems.Count} materials?") != SukiMessageBoxResult.Yes) return;
        Manager.RemoveRange(MaterialsTable.SelectedItems.Cast<Core.Objects.Material>());
        MaterialManager.Save();
    }

    public async Task ClearMaterials()
    {
        if (Manager.Count == 0) return;
        if (await this.MessageBoxQuestion($"Are you sure you want to clear {Manager.Count} materials?") != SukiMessageBoxResult.Yes) return;
        Manager.Clear(true);
    }
}