using System.ComponentModel;
using System.Windows.Forms;

namespace UVtools.GUI.Controls
{
    public partial class CtrlDescriptionPanel : UserControl
    {
        public string Description
        {
            get => lbDescription.Text;
            set => lbDescription.Text = value;
        }

        public CtrlDescriptionPanel()
        {
            InitializeComponent();
        }
    }
}
