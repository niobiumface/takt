using System.Windows;
using System.Windows.Input;
using Takt.ViewModels;

namespace Takt.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.TryCompleteCapture(e);
    }
}
