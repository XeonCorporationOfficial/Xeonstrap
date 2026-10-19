using System;
using System.Diagnostics;
using System.Windows.Input;
using Xeonstrap.UI.ViewModels.ContextMenu;

namespace Xeonstrap.UI.Elements.Settings.Pages
{
    public class DonoPageViewModel
    {
        public ICommand OpenUrlCommand { get; }

        public DonoPageViewModel()
        {
            OpenUrlCommand = new RelayCommand(OpenUrl);
        }

        private void OpenUrl(object? parameter)
        {
            if (parameter is string url)
            {
                Utilities.OpenWebLink(url);
            }
        }
    }
}
