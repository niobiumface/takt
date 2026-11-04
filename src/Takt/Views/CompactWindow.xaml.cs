using System.ComponentModel;
using System.Windows;
using Takt.ViewModels;

namespace Takt.Views;

public partial class CompactWindow : Window
{
    public CompactWindow() => InitializeComponent();

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (Application.Current?.Dispatcher.HasShutdownStarted == true)
            return;
        e.Cancel = true;
        Hide();
    }

    private void OnMoved(object? sender, EventArgs e)
    {
        if (!IsLoaded || DataContext is not MainViewModel viewModel)
            return;
        viewModel.RememberCompact(Left, Top);
    }
}
