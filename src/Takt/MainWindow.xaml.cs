using System.ComponentModel;
using System.Windows;
using Takt.ViewModels;

namespace Takt;

public partial class MainWindow : Window
{
    internal MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.AttachHotkeys(this);
        Closing += OnClosing;
        StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && ShouldHideToTray())
            Hide();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (ShouldHideToTray())
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            Application.Current.Shutdown();
        }
    }

    private bool ShouldHideToTray()
    {
        return DataContext is MainViewModel vm && vm.Settings.General.MinimizeToTray;
    }
}
