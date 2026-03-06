using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using GorillaMelonManager.ViewModels;

namespace GorillaMelonManager.Views
{
    public partial class ModBrowser : UserControl
    {
        public ModBrowser()
        {
            InitializeComponent();
            this.DataContext = new ModBrowserViewModel();
        }

        public void OnInstallButtonClick(object sender, RoutedEventArgs e)
        {
            string url = ((Button)sender).Name;
            (DataContext as ModBrowserViewModel).OnInstallClick(url);
        }

        public void OnPageClick(object sender, RoutedEventArgs e)
        {

        }
    }
}
