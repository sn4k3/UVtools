using System.Drawing;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls
{
    public sealed class OperationMenuItem
    {
        public Operation Operation { get; }

        public Image Icon { get; }

        public OperationMenuItem(Operation operation, Image icon = null)
        {
            Operation = operation;
            Icon = icon;
        }
    }
}
