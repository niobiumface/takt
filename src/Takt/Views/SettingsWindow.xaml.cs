using System.Windows;
using System.Windows.Input;
using Takt.Models;
using Takt.Services;
using Takt.ViewModels;

namespace Takt.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        Closed += (_, _) =>
        {
            if (DataContext is SettingsViewModel viewModel)
                viewModel.CancelCapture();
        };
        SourceInitialized += (_, _) =>
        {
            var dark = DataContext is SettingsViewModel viewModel &&
                       viewModel.Settings.Appearance.Theme != ThemeMode.Light;
            TitleBarTheme.Apply(this, dark);
        };
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.TryCompleteCapture(e);
    }
}
