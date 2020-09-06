/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;

namespace UVtools.GUI.Controls
{
    public partial class CtrlToolWindowContent : UserControl
    {
        #region Properties

        [Editor("System.ComponentModel.Design.MultilineStringEditor", typeof(UITypeEditor))]
        [SettingsBindable(true)]
        public string Description { get; set; }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", typeof(UITypeEditor))]
        [SettingsBindable(true)]
        public string ButtonOkText { get; set; } = "Ok";

        [Editor("System.ComponentModel.Design.MultilineStringEditor", typeof(UITypeEditor))]
        [SettingsBindable(true)]
        public string ButtonCancelText { get; set; } = "Cancel";

        [SettingsBindable(true)] public bool LayerRangeVisible { get; set; } = true;

        [SettingsBindable(true)] public bool LayerRangeEndVisible { get; set; } = true;

        /*[SettingsBindable(true)]
        public uint LayerRangeStart { get; set; }

        [SettingsBindable(true)]
        public uint LayerRangeEnd { get; set; }*/

        [Editor("System.ComponentModel.Design.MultilineStringEditor", typeof(UITypeEditor))]
        [SettingsBindable(true)]
        public virtual string ConfirmationText { get; } = "do this action?";

        #endregion

        #region Constructor
        public CtrlToolWindowContent()
        {
            InitializeComponent();
        }
        #endregion

        #region Methods

        public virtual bool ValidateForm() => true;

        #endregion
    }
}
