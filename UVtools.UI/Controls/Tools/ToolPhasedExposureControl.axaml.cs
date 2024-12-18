using System.Linq;
using Avalonia.Controls;
using UVtools.Core.Operations;
using UVtools.UI.Windows;

namespace UVtools.UI.Controls.Tools
{
    public partial class ToolPhasedExposureControl : ToolControl
    {
        public OperationPhasedExposure Operation => (BaseOperation as OperationPhasedExposure)!;
        public ToolPhasedExposureControl()
        {
            BaseOperation = new OperationPhasedExposure(SlicerFile!);
            if (!ValidateSpawn()) return;
            InitializeComponent();
        }

        public override void Callback(ToolWindow.Callbacks callback)
        {
            if (SlicerFile is null) return;
            switch (callback)
            {
                case ToolWindow.Callbacks.Init:
                case ToolWindow.Callbacks.AfterLoadProfile:
                    if (ParentWindow is not null) ParentWindow.ButtonOkEnabled = Operation.Count >= 2;
                    Operation.PhasedExposures.CollectionChanged += (sender, e) => ParentWindow!.ButtonOkEnabled = Operation.Count >= 2;
                    break;
            }
        }

        private void PhasedExposuresGrid_OnLoadingRow(object? sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.Index + 1;
        }

        public void AddExposure()
        {
            Operation.PhasedExposures.Add(new OperationPhasedExposure.PhasedExposure((decimal)SlicerFile!.ExposureTime, 0, (decimal)SlicerFile.ExposureTime, 0));
        }

        public void RemoveExposure()
        {
            Operation.PhasedExposures.RemoveRange(PhasedExposuresGrid.SelectedItems.OfType<OperationPhasedExposure.PhasedExposure>());
        }

        public void MoveExposureTop()
        {
            if (PhasedExposuresGrid.SelectedIndex <= 0) return;
            var selectedFile = (OperationPhasedExposure.PhasedExposure)PhasedExposuresGrid.SelectedItem;

            var list = Operation.PhasedExposures.ToList();
            list.RemoveAt(PhasedExposuresGrid.SelectedIndex);
            list.Insert(0, selectedFile);
            Operation.PhasedExposures.ReplaceCollection(list);

            PhasedExposuresGrid.SelectedIndex = 0;
            PhasedExposuresGrid.ScrollIntoView(selectedFile, PhasedExposuresGrid.Columns[0]);
        }

        public void MoveExposureUp()
        {
            if (PhasedExposuresGrid.SelectedIndex <= 0) return;
            var selectedFile = (OperationPhasedExposure.PhasedExposure)PhasedExposuresGrid.SelectedItem;
            var newIndex = PhasedExposuresGrid.SelectedIndex - 1;


            var list = Operation.PhasedExposures.ToList();
            list.RemoveAt(PhasedExposuresGrid.SelectedIndex);
            list.Insert(newIndex, selectedFile);
            Operation.PhasedExposures.ReplaceCollection(list);

            PhasedExposuresGrid.SelectedIndex = newIndex;
            PhasedExposuresGrid.ScrollIntoView(selectedFile, PhasedExposuresGrid.Columns[0]);
        }

        public void MoveExposureDown()
        {
            if (PhasedExposuresGrid.SelectedIndex == -1 || PhasedExposuresGrid.SelectedIndex == Operation.Count - 1) return;
            var selectedFile = (OperationPhasedExposure.PhasedExposure)PhasedExposuresGrid.SelectedItem;
            var newIndex = PhasedExposuresGrid.SelectedIndex + 1;

            var list = Operation.PhasedExposures.ToList();
            list.RemoveAt(PhasedExposuresGrid.SelectedIndex);
            list.Insert(newIndex, selectedFile);
            Operation.PhasedExposures.ReplaceCollection(list);

            PhasedExposuresGrid.SelectedIndex = newIndex;
            PhasedExposuresGrid.ScrollIntoView(selectedFile, PhasedExposuresGrid.Columns[0]);
        }

        public void MoveExposureBottom()
        {
            var lastIndex = Operation.Count - 1;
            if (PhasedExposuresGrid.SelectedIndex == -1 || PhasedExposuresGrid.SelectedIndex == lastIndex) return;
            var selectedFile = (OperationPhasedExposure.PhasedExposure)PhasedExposuresGrid.SelectedItem;

            var list = Operation.PhasedExposures.ToList();
            list.RemoveAt(PhasedExposuresGrid.SelectedIndex);
            list.Add(selectedFile);
            Operation.PhasedExposures.ReplaceCollection(list);

            PhasedExposuresGrid.SelectedIndex = lastIndex;
            PhasedExposuresGrid.ScrollIntoView(selectedFile, PhasedExposuresGrid.Columns[0]);
        }

        public void ResetExposures()
        {
            Operation.PhasedExposures.ReplaceCollection(new []
            {
                new OperationPhasedExposure.PhasedExposure((decimal)SlicerFile!.BottomExposureTime, 10, (decimal)SlicerFile.ExposureTime, 4),
                new OperationPhasedExposure.PhasedExposure((decimal)SlicerFile.ExposureTime, 0, (decimal)SlicerFile.ExposureTime, 0)
            });
        }
    }
}
