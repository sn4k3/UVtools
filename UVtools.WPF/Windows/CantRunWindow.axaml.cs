/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.SystemOS;

namespace UVtools.WPF.Windows
{
    public partial class CantRunWindow : Window
    {
        public CantRunWindow()
        {
            InitializeComponent();
            DataContext = this;
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void OpenBrowser(string url)
        {
            SystemAware.OpenBrowser(url);
        }

        public async void OpenAboutWindow()
        {
            await new AboutWindow().ShowDialog(this);
        }
    }
}
