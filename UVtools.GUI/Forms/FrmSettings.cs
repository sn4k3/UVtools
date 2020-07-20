using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using UVtools.Core.FileFormats;
using UVtools.GUI.Properties;

namespace UVtools.GUI.Forms
{
    public partial class FrmSettings : Form
    {
        public FrmSettings()
        {
            InitializeComponent();

            Text = $"UVtools - Settings [v{FrmAbout.AssemblyVersion}]";

            var fileFormats = new List<string>
            {
                FileFormat.AllSlicerFiles.Replace("*", string.Empty)
            };
            fileFormats.AddRange(from format in FileFormat.AvaliableFormats from extension in format.FileExtensions select $"{extension.Description} (.{extension.Extension})");
            cbDefaultOpenFileExtension.Items.AddRange(fileFormats.ToArray());

            Init();
        }

        public void Init()
        {
            try
            {
                cbCheckForUpdatesOnStartup.Checked = Settings.Default.CheckForUpdatesOnStartup;
                cbStartMaximized.Checked = Settings.Default.StartMaximized;
                cbDefaultOpenFileExtension.SelectedIndex = Settings.Default.DefaultOpenFileExtension;
                tbFileOpenDefaultDirectory.Text = Settings.Default.FileOpenDefaultDirectory;
                tbFileSaveDefaultDirectory.Text = Settings.Default.FileSaveDefaultDirectory;
                tbFileExtractDefaultDirectory.Text = Settings.Default.FileExtractDefaultDirectory;
                tbFileConvertDefaultDirectory.Text = Settings.Default.FileConvertDefaultDirectory;
                cbFileSavePromptOverwrite.Checked = Settings.Default.FileSavePromptOverwrite;
                tbFileSaveNamePreffix.Text = Settings.Default.FileSaveNamePreffix;
                tbFileSaveNameSuffix.Text = Settings.Default.FileSaveNameSuffix;

                btnPreviousNextLayerColor.BackColor = Settings.Default.PreviousNextLayerColor;
                btnPreviousLayerColor.BackColor = Settings.Default.PreviousLayerColor;
                btnNextLayerColor.BackColor = Settings.Default.NextLayerColor;
                btnIslandColor.BackColor = Settings.Default.IslandColor;
                btnResinTrapColor.BackColor = Settings.Default.ResinTrapColor;
                btnTouchingBoundsColor.BackColor = Settings.Default.TouchingBoundsColor;

                btnOutlinePrintVolumeBoundsColor.BackColor = Settings.Default.OutlinePrintVolumeBoundsColor;
                btnOutlineLayerBoundsColor.BackColor = Settings.Default.OutlineLayerBoundsColor;
                btnOutlineHollowAreasColor.BackColor = Settings.Default.OutlineHollowAreasColor;

                nmOutlinePrintVolumeBoundsLineThickness.Value = Settings.Default.OutlinePrintVolumeBoundsLineThickness;
                nmOutlineLayerBoundsLineThickness.Value = Settings.Default.OutlineLayerBoundsLineThickness;
                nmOutlineHollowAreasLineThickness.Value = Settings.Default.OutlineHollowAreasLineThickness;

                cbOutlinePrintVolumeBounds.Checked = Settings.Default.OutlinePrintVolumeBounds;
                cbOutlineLayerBounds.Checked = Settings.Default.OutlineLayerBounds;
                cbOutlineHollowAreas.Checked = Settings.Default.OutlineHollowAreas;

                cbLayerAutoRotateBestView.Checked = Settings.Default.LayerAutoRotateBestView;
                cbLayerZoomToFit.Checked = Settings.Default.LayerZoomToFit;
                cbZoomToFitPrintVolumeBounds.Checked = Settings.Default.ZoomToFitPrintVolumeBounds;
                cbAutoZoomIssues.Checked = Settings.Default.AutoZoomIssues;
                cbLayerDifferenceDefault.Checked = Settings.Default.LayerDifferenceDefault;
                cbComputeIssuesOnLoad.Checked = Settings.Default.ComputeIssuesOnLoad;
                cbComputeIslands.Checked = Settings.Default.ComputeIslands;
                cbComputeResinTraps.Checked = Settings.Default.ComputeResinTraps;
                cbAutoComputeIssuesClickOnTab.Checked = Settings.Default.AutoComputeIssuesClickOnTab;

                nmIslandBinaryThreshold.Value = Settings.Default.IslandBinaryThreshold;
                nmIslandRequiredAreaToProcessCheck.Value = Settings.Default.IslandRequiredAreaToProcessCheck;
                nmIslandRequiredPixelBrightnessToProcessCheck.Value = Settings.Default.IslandRequiredPixelBrightnessToProcessCheck;
                nmIslandRequiredPixelsToSupport.Value = Settings.Default.IslandRequiredPixelsToSupport;
                nmIslandRequiredPixelBrightnessToSupport.Value = Settings.Default.IslandRequiredPixelBrightnessToSupport;

                nmResinTrapBinaryThreshold.Value = Settings.Default.ResinTrapBinaryThreshold;
                nmResinTrapRequiredAreaToProcessCheck.Value = Settings.Default.ResinTrapRequiredAreaToProcessCheck;
                nmResinTrapRequiredBlackPixelsToDrain.Value = Settings.Default.ResinTrapRequiredBlackPixelsToDrain;
                nmResinTrapMaximumPixelBrightnessToDrain.Value = Settings.Default.ResinTrapMaximumPixelBrightnessToDrain;

                btnPixelEditorAddPixelColor.BackColor = Settings.Default.PixelEditorAddPixelColor;
                btnPixelEditorRemovePixelColor.BackColor = Settings.Default.PixelEditorRemovePixelColor;
                btnPixelEditorSupportColor.BackColor = Settings.Default.PixelEditorSupportColor;
                btnPixelEditorDrainHoleColor.BackColor = Settings.Default.PixelEditorDrainHoleColor;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to use current settings, a reset will be performed.\n{ex.Message}",
                    "Unable to use current settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Settings.Default.Reset();
                Settings.Default.Save();
                Init();
            }
        }

        private void EventClick(object sender, EventArgs e)
        {
            if (
                ReferenceEquals(sender, btnPreviousNextLayerColor) ||
                ReferenceEquals(sender, btnPreviousLayerColor) ||
                ReferenceEquals(sender, btnNextLayerColor) ||
                ReferenceEquals(sender, btnIslandColor) ||
                ReferenceEquals(sender, btnResinTrapColor) ||
                ReferenceEquals(sender, btnTouchingBoundsColor) ||
                ReferenceEquals(sender, btnOutlinePrintVolumeBoundsColor) ||
                ReferenceEquals(sender, btnOutlineLayerBoundsColor) ||
                ReferenceEquals(sender, btnOutlineHollowAreasColor) ||
                ReferenceEquals(sender, btnPixelEditorAddPixelColor) ||
                ReferenceEquals(sender, btnPixelEditorRemovePixelColor) ||
                ReferenceEquals(sender, btnPixelEditorSupportColor) ||
                ReferenceEquals(sender, btnPixelEditorDrainHoleColor)
                )
            {
                Button btn = sender as Button;
                colorDialog.Color = btn.BackColor;
                if (colorDialog.ShowDialog() != DialogResult.OK) return;
                
                btn.BackColor = colorDialog.Color;

                return;

            }

            if (ReferenceEquals(sender, btnFileOpenDefaultDirectorySearch) || ReferenceEquals(sender, btnFileSaveDefaultDirectorySearch) || ReferenceEquals(sender, btnFileConvertDefaultDirectorySearch) || ReferenceEquals(sender, btnFileExtractDefaultDirectorySearch))
            {
                using (FolderBrowserDialog folder = new FolderBrowserDialog())
                {
                    if (folder.ShowDialog() != DialogResult.OK) return;
                    if (ReferenceEquals(sender, btnFileOpenDefaultDirectorySearch)) tbFileOpenDefaultDirectory.Text = folder.SelectedPath;
                    else if (ReferenceEquals(sender, btnFileSaveDefaultDirectorySearch)) tbFileSaveDefaultDirectory.Text = folder.SelectedPath;
                    else if (ReferenceEquals(sender, btnFileExtractDefaultDirectorySearch)) tbFileExtractDefaultDirectory.Text = folder.SelectedPath;
                    else if (ReferenceEquals(sender, btnFileConvertDefaultDirectorySearch)) tbFileConvertDefaultDirectory.Text = folder.SelectedPath;
                }

                return;
            }

            if (ReferenceEquals(sender, btnFileOpenDefaultDirectoryClear))
            {
                tbFileOpenDefaultDirectory.Text = string.Empty;
                return;
            }
            if (ReferenceEquals(sender, btnFileSaveDefaultDirectoryClear))
            {
                tbFileSaveDefaultDirectory.Text = string.Empty;
                return;
            }
            if (ReferenceEquals(sender, btnFileExtractDefaultDirectoryClear))
            {
                tbFileExtractDefaultDirectory.Text = string.Empty;
                return;
            }
            if (ReferenceEquals(sender, btnFileConvertDefaultDirectoryClear))
            {
                tbFileConvertDefaultDirectory.Text = string.Empty;
                return;
            }

            if (ReferenceEquals(sender, btnReset))
            {
                if (MessageBox.Show("Are you sure you want to reset the settings to the default values?",
                        "Reset settings?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) !=
                    DialogResult.Yes) return;

                Settings.Default.Reset();
                Init();

                return;
            }
            
            if (ReferenceEquals(sender, btnSave))
            {
                Settings.Default.CheckForUpdatesOnStartup = cbCheckForUpdatesOnStartup.Checked;
                Settings.Default.StartMaximized = cbStartMaximized.Checked;
                Settings.Default.DefaultOpenFileExtension = (byte) cbDefaultOpenFileExtension.SelectedIndex;
                Settings.Default.FileOpenDefaultDirectory = tbFileOpenDefaultDirectory.Text;
                Settings.Default.FileSaveDefaultDirectory = tbFileSaveDefaultDirectory.Text;
                Settings.Default.FileExtractDefaultDirectory = tbFileExtractDefaultDirectory.Text;
                Settings.Default.FileConvertDefaultDirectory = tbFileConvertDefaultDirectory.Text;
                Settings.Default.FileSavePromptOverwrite = cbFileSavePromptOverwrite.Checked;
                Settings.Default.FileSaveNamePreffix = tbFileSaveNamePreffix.Text.Trim();
                Settings.Default.FileSaveNameSuffix = tbFileSaveNameSuffix.Text.Trim();

                Settings.Default.PreviousNextLayerColor = btnPreviousNextLayerColor.BackColor;
                Settings.Default.PreviousLayerColor = btnPreviousLayerColor.BackColor;
                Settings.Default.NextLayerColor = btnNextLayerColor.BackColor;
                Settings.Default.IslandColor = btnIslandColor.BackColor;
                Settings.Default.ResinTrapColor = btnResinTrapColor.BackColor;
                Settings.Default.TouchingBoundsColor = btnTouchingBoundsColor.BackColor;

                Settings.Default.OutlinePrintVolumeBoundsColor = btnOutlinePrintVolumeBoundsColor.BackColor;
                Settings.Default.OutlineLayerBoundsColor = btnOutlineLayerBoundsColor.BackColor;
                Settings.Default.OutlineHollowAreasColor = btnOutlineHollowAreasColor.BackColor;

                Settings.Default.OutlinePrintVolumeBoundsLineThickness = (byte) nmOutlinePrintVolumeBoundsLineThickness.Value;
                Settings.Default.OutlineLayerBoundsLineThickness = (byte) nmOutlineLayerBoundsLineThickness.Value;
                Settings.Default.OutlineHollowAreasLineThickness = (sbyte) nmOutlineHollowAreasLineThickness.Value;

                Settings.Default.OutlinePrintVolumeBounds = cbOutlinePrintVolumeBounds.Checked;
                Settings.Default.OutlineLayerBounds = cbOutlineLayerBounds.Checked;
                Settings.Default.OutlineHollowAreas = cbOutlineHollowAreas.Checked;

                Settings.Default.LayerAutoRotateBestView = cbLayerAutoRotateBestView.Checked;
                Settings.Default.LayerZoomToFit = cbLayerZoomToFit.Checked;
                Settings.Default.ZoomToFitPrintVolumeBounds = cbZoomToFitPrintVolumeBounds.Checked;
                Settings.Default.AutoZoomIssues = cbAutoZoomIssues.Checked;
                Settings.Default.LayerDifferenceDefault = cbLayerDifferenceDefault.Checked;
                Settings.Default.ComputeIssuesOnLoad = cbComputeIssuesOnLoad.Checked;
                Settings.Default.ComputeIslands = cbComputeIslands.Checked;
                Settings.Default.ComputeResinTraps = cbComputeResinTraps.Checked;
                Settings.Default.AutoComputeIssuesClickOnTab = cbAutoComputeIssuesClickOnTab.Checked;

                Settings.Default.IslandBinaryThreshold = (byte)nmIslandBinaryThreshold.Value;
                Settings.Default.IslandRequiredAreaToProcessCheck = (byte) nmIslandRequiredAreaToProcessCheck.Value;
                Settings.Default.IslandRequiredPixelBrightnessToProcessCheck = (byte)nmIslandRequiredPixelBrightnessToProcessCheck.Value;
                Settings.Default.IslandRequiredPixelsToSupport = (byte)nmIslandRequiredPixelsToSupport.Value;
                Settings.Default.IslandRequiredPixelBrightnessToSupport = (byte)nmIslandRequiredPixelBrightnessToSupport.Value;

                Settings.Default.ResinTrapBinaryThreshold = (byte) nmResinTrapBinaryThreshold.Value;
                Settings.Default.ResinTrapRequiredAreaToProcessCheck = (byte)nmResinTrapRequiredAreaToProcessCheck.Value;
                Settings.Default.ResinTrapRequiredBlackPixelsToDrain = (byte)nmResinTrapRequiredBlackPixelsToDrain.Value;
                Settings.Default.ResinTrapMaximumPixelBrightnessToDrain = (byte)nmResinTrapMaximumPixelBrightnessToDrain.Value;

                Settings.Default.PixelEditorAddPixelColor = btnPixelEditorAddPixelColor.BackColor;
                Settings.Default.PixelEditorRemovePixelColor = btnPixelEditorRemovePixelColor.BackColor;
                Settings.Default.PixelEditorSupportColor = btnPixelEditorSupportColor.BackColor;
                Settings.Default.PixelEditorDrainHoleColor = btnPixelEditorDrainHoleColor.BackColor;

                Settings.Default.Save();
                DialogResult = DialogResult.OK;
                return;
            }
        }
    }
}
