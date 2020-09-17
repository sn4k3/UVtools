/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.FileFormats;
using UVtools.WPF.Controls;
using UVtools.WPF.Extensions;
using UVtools.WPF.ViewModels;
using UVtools.WPF.Windows;

namespace UVtools.WPF
{
    public class MainWindow : Window, INotifyPropertyChanged
    {
        #region BindableBase
        /// <summary>
        ///     Multicast event for property change notifications.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Checks if a property already matches a desired value.  Sets the property and
        ///     notifies listeners only when necessary.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to a property with both getter and setter.</param>
        /// <param name="value">Desired value for the property.</param>
        /// <param name="propertyName">
        ///     Name of the property used to notify listeners.  This
        ///     value is optional and can be provided automatically when invoked from compilers that
        ///     support CallerMemberName.
        /// </param>
        /// <returns>
        ///     True if the value was changed, false if the existing value matched the
        ///     desired value.
        /// </returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        ///     Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">
        ///     Name of the property used to notify listeners.  This
        ///     value is optional and can be provided automatically when invoked from compilers
        ///     that support <see cref="CallerMemberNameAttribute" />.
        /// </param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = PropertyChanged;
            eventHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public MainWindowViewModel ViewModel { get; }



        public AdvancedImageBox LayerImage;
        public Button ZoomToFitButton;
        public Button CenterButton;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            //this.AttachDevTools();
#endif
            App.Selector?.EnableThemes(this);

            ViewModel = new MainWindowViewModel(this);
            DataContext = ViewModel;

            LayerImage = this.FindControl<AdvancedImageBox>("Layer.Image");
            ZoomToFitButton = this.FindControl<Button>("zoomtofit");
            CenterButton = this.FindControl<Button>("center");
            //LayerImage.LoadImage(@"D:\Test.png");

            //ZoomToFitButton.Click += (sender, args) => LayerImage.ZoomToFit();
            //CenterButton.Click += (sender, args) => LayerImage.CenterAt(1440/2,2560/2);
            //var layerImage = this.FindControl<AdvancedPictureBox>("Layer.ImageOld");
            LayerImage.LoadImage(@"D:\Tiago\Desktop\UVtools\body_Tough0.1mm_SL1_5h16m_HOLLOW_DRAIN\body_Tough0.1mm_SL1_5h16m_HOLLOW_DRAIN00000.png");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ProcessFiles(string[] files, bool openNewWindow = false)
        {
            if (files is null || files.Length == 0) return;

            for (int i = 0; i < files.Length; i++)
            {
                if (i == 0 && !openNewWindow)
                {
                    ProcessFile(files[i]);
                    continue;
                }


            }
        }

        void ReloadFile(uint actualLayer = 0)
        {
            if (App.SlicerFile is null) return;
            ProcessFile(App.SlicerFile.FileFullPath, actualLayer);
        }

        void ProcessFile(string fileName, uint actualLayer = 0)
        {
            var fileNameOnly = Path.GetFileName(fileName);
            App.SlicerFile = FileFormat.FindByExtension(fileName, true, true);
            if (App.SlicerFile is null) return;

            ViewModel.IsGUIEnabled = false;

            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    App.SlicerFile.Decode(fileName);
                }
                catch (OperationCanceledException)
                {
                    App.SlicerFile.Clear();
                }
                finally
                {
                   /* Invoke((MethodInvoker)delegate
                    {
                        // Running on the UI thread
                        EnableGUI(true);
                    });*/
                }
            });

            //ProgressWindow progressWindow = new ProgressWindow();
            //progressWindow.ShowDialog(this);



            task.Wait();

            var mat = App.SlicerFile[0].LayerMat;
            var matRgb = App.SlicerFile[0].BrgMat;

            Debug.WriteLine("4K grayscale - BGRA convertion:");
            var bitmap = mat.ToBitmap();
            Debug.WriteLine("4K BGR - BGRA convertion:");
            var bitmapRgb = matRgb.ToBitmap();
            LayerImage.Image = bitmapRgb;
            
            ViewModel.IsGUIEnabled = true;
        }
    }
}
