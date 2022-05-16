/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Avalonia;
using Avalonia.Markup.Xaml;
using UVtools.Core.SystemOS;
using UVtools.WPF.Controls;

namespace UVtools.WPF.Windows
{
    public partial class CantRunWindow : WindowEx
    {
        public CantRunWindow()
        {
            InitializeComponent();
            DataContext = this;
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
