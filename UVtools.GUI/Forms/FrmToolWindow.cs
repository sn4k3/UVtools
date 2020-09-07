/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;
using UVtools.Core.Extensions;
using UVtools.GUI.Controls;

namespace UVtools.GUI.Forms
{
    public partial class FrmToolWindow : Form
    {
        #region Properties

        public CtrlToolWindowContent Content { get; set; }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", typeof(UITypeEditor))]
        [SettingsBindable(true)]
        public string Description
        {
            get => lbDescription.Text;
            set => lbDescription.Text = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", typeof(UITypeEditor))]
        [SettingsBindable(true)]
        public string ButtonOkText
        {
            get => btnOk.Text;
            set => btnOk.Text = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", typeof(UITypeEditor))]
        [SettingsBindable(true)]
        public string ButtonCancelText
        {
            get => btnOk.Text;
            set => btnOk.Text = value;
        }

        [SettingsBindable(true)]
        public bool LayerRangeVisible
        {
            get => pnLayerRange.Visible;
            set => pnLayerRange.Visible = value;
        }

        [SettingsBindable(true)]
        public bool LayerRangeEndVisible
        {
            get => nmLayerRangeEnd.Visible;
            set =>
                nmLayerRangeEnd.Visible = 
                lbLayerRangeTo.Visible = 
                lbLayerRangeToMM.Visible =
                btnLayerRangeSelect.Visible = value;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public uint LayerRangeStart
        {
            get => (uint)nmLayerRangeStart.Value;
            set => nmLayerRangeStart.Value = value;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public uint LayerRangeEnd
        {
            get => (uint)Math.Min(nmLayerRangeEnd.Value, Program.SlicerFile.LayerCount - 1);
            set => nmLayerRangeEnd.Value = value;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public virtual string ConfirmationText { get; } = "do this action?";


        #endregion

        #region Constructors

        public FrmToolWindow()
        {
            InitializeComponent();
        }

        public FrmToolWindow(string description, string buttonOkText, bool layerRangeVisible = true, bool layerRangeEndVisible = true) : this()
        {
            if (!layerRangeVisible)
            {
                Height -= pnLayerRange.Height;
            }

            Description = description;
            ButtonOkText = buttonOkText;
            LayerRangeVisible = layerRangeVisible;
            LayerRangeEndVisible = layerRangeEndVisible;
            LayerRangeEnd = Program.SlicerFile.LayerCount - 1;

            EventValueChanged(nmLayerRangeStart, EventArgs.Empty);
            EventValueChanged(nmLayerRangeEnd, EventArgs.Empty);
        }

        public FrmToolWindow(string description, string buttonOkText, uint layerIndex) : this(description, buttonOkText)
        {
            LayerRangeStart = LayerRangeEnd = layerIndex;
        }

        public FrmToolWindow(CtrlToolWindowContent content) : this(content.Description, content.ButtonOkText, content.LayerRangeVisible, content.LayerRangeEndVisible)
        {
            Text = content.Text;
            pnContent.Controls.Add(content);
            Width = Math.Max(MinimumSize.Width, content.Width);
            Height += content.Height;
            content.Dock = DockStyle.Fill;
            btnOk.Enabled = content.ButtonOkEnabled;
            Content = content;

            content.PropertyChanged += ContentOnPropertyChanged;
        }

        private void ContentOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Content.ButtonOkEnabled))
            {
                btnOk.Enabled = Content.ButtonOkEnabled;
                return;
            }
        }

        public FrmToolWindow(CtrlToolWindowContent content, uint layerIndex) : this(content)
        {
            LayerRangeStart = LayerRangeEnd = layerIndex;
        }
        #endregion

        #region Overrides
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                // Need exclude controls that require ENTER
                if (!ReferenceEquals(ActiveControl, null))
                {
                    switch (ActiveControl)
                    {
                        case NumericUpDown _:
                        case TextBox textBox when textBox.Multiline:
                            return;
                    }
                }

                btnOk.PerformClick();
                return;
            }

            if ((ModifierKeys & Keys.Shift) == Keys.Shift && (ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (e.KeyCode == Keys.A)
                {
                    btnLayerRangeAllLayers.PerformClick();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.C)
                {
                    btnLayerRangeCurrentLayer.PerformClick();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.B)
                {
                    btnLayerRangeBottomLayers.PerformClick();
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode == Keys.N)
                {
                    btnLayerRangeNormalLayers.PerformClick();
                    e.Handled = true;
                    return;
                }
            }
        }

        #endregion

        #region Events
        private void EventClick(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, btnLayerRangeAllLayers))
            {
                nmLayerRangeStart.Value = 0;
                nmLayerRangeEnd.Value = Program.SlicerFile.LayerCount-1;
                return;
            }

            if (ReferenceEquals(sender, btnLayerRangeCurrentLayer))
            {
                nmLayerRangeStart.Value = Program.FrmMain.ActualLayer;
                nmLayerRangeEnd.Value = Program.FrmMain.ActualLayer;
                return;
            }

            if (ReferenceEquals(sender, btnLayerRangeBottomLayers))
            {
                nmLayerRangeStart.Value = 0;
                nmLayerRangeEnd.Value = Program.SlicerFile.InitialLayerCount-1;
                return;
            }

            if (ReferenceEquals(sender, btnLayerRangeNormalLayers))
            {
                nmLayerRangeStart.Value = Program.SlicerFile.InitialLayerCount - 1;
                nmLayerRangeEnd.Value = Program.SlicerFile.LayerCount - 1;
                return;
            }

            if (ReferenceEquals(sender, btnOk))
            {
                if (!btnOk.Enabled) return;

                if (LayerRangeStart > LayerRangeEnd)
                {
                    MessageErrorBox("Layer range start can't be higher than layer end.\nPlease fix and try again.");
                    nmLayerRangeStart.Select();
                    return;
                }

                if (!ValidateForm()) return;
                if (!ReferenceEquals(Content, null) && !Content.ValidateForm()) return;

                if (MessageQuestionBox($"Are you sure you want to {Content?.ConfirmationText ?? ConfirmationText}") == DialogResult.Yes)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }

                return;
            }

            if (ReferenceEquals(sender, btnCancel))
            {
                DialogResult = DialogResult.Cancel;
                return;
            }
        }

        private void EventValueChanged(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, nmLayerRangeStart) || ReferenceEquals(sender, nmLayerRangeEnd))
            {
                uint layerIndex = (uint) ((NumericUpDown) sender).Value;
                if (layerIndex >= Program.SlicerFile.LayerCount) return;
                var layer = Program.SlicerFile[layerIndex];
                var text = $"({layer.PositionZ}mm)";

                uint layerCount = LayerRangeEndVisible ? (uint) Math.Max(0, nmLayerRangeEnd.Value - nmLayerRangeStart.Value + 1) : 1;
                lbLayerRangeCount.Text = $"({layerCount} layer / {Program.SlicerFile.LayerHeight * layerCount}mm)";

                if (layerCount == 0)
                {
                    lbLayerRangeCount.ForeColor = Color.Red;
                    btnOk.Enabled = false;
                }
                else
                {
                    lbLayerRangeCount.ForeColor = Color.Black;
                    btnOk.Enabled = true;
                }
                

                if (ReferenceEquals(sender, nmLayerRangeStart))
                {
                    lbLayerRangeFromMM.Text = text;
                }
                else if (ReferenceEquals(sender, nmLayerRangeEnd))
                {
                    lbLayerRangeToMM.Text = text;
                }
                
                return;
            }
        }


        #endregion

        #region Methods

        public virtual bool ValidateForm() => true;

        public DialogResult MessageErrorBox(string message) => GUIExtensions.MessageErrorBox($"{Text} Error", message);
        public DialogResult MessageQuestionBox(string message, string title = null) => GUIExtensions.MessageQuestionBox($"{title ?? Text}", message);

        #endregion

    }
}
