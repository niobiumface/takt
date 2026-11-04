using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using Takt.Helpers;
using Takt.Models;
using Takt.Native;
using Takt.Services;
using Takt.ViewModels;

namespace Takt;

public partial class MainWindow : Window
{
    internal MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelChanged;
        viewModel.AttachHotkeys(this);
        Closing += OnClosing;
        StateChanged += OnStateChanged;
        SizeChanged += OnSizeChanged;
        SourceInitialized += (_, _) =>
        {
            ApplyTitleBar();
            AttachSessionWatch();
            if (PresentationSource.FromVisual(this) is HwndSource source)
                source.AddHook(WndProc);
        };
        Closed += (_, _) => _session?.Detach();
    }

    private SessionWatch? _session;

    private void AttachSessionWatch()
    {
        _session = new SessionWatch();
        _session.LockChanged += locked =>
        {
            if (DataContext is MainViewModel viewModel)
                viewModel.Services.Presence.ReportSessionLock(locked);
        };
        _session.Attach(this);
    }

    private void ApplyTitleBar()
    {
        var dark = DataContext is MainViewModel viewModel &&
                   ThemeService.IsDark(viewModel.Settings.Appearance.Theme);
        TitleBarTheme.Apply(this, dark);
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
        return DataContext is MainViewModel vm && vm.Settings.General.CloseBehavior != CloseBehavior.Exit;
    }

    private bool _stacked;

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width < 1)
            return;
        ArrangeCards(e.NewSize.Width < 500);
    }

    private void ArrangeCards(bool stack)
    {
        if (stack == _stacked || CardHost is null)
            return;
        _stacked = stack;
        CardHost.ColumnDefinitions[0].MinWidth = stack ? 0 : 220;
        CardHost.ColumnDefinitions[2].MinWidth = stack ? 0 : 220;
        CardHost.ColumnDefinitions[1].Width = new GridLength(stack ? 0 : 12);
        CardHost.RowDefinitions[0].Height = stack ? GridLength.Auto : new GridLength(1, GridUnitType.Star);
        CardHost.RowDefinitions[3].Height = new GridLength(stack ? 12 : 0);

        Grid.SetColumn(JigglerCard, 0);
        Grid.SetColumnSpan(JigglerCard, stack ? 3 : 1);
        Grid.SetColumn(ClickerCard, stack ? 0 : 2);
        Grid.SetColumnSpan(ClickerCard, stack ? 3 : 1);
        Grid.SetRow(ClickerCard, stack ? 2 : 0);
        Grid.SetRow(TyperCard, stack ? 4 : 2);
        Grid.SetColumn(TyperCard, 0);
        Grid.SetColumnSpan(TyperCard, 3);
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control && DataContext is MainViewModel viewModel)
        {
            viewModel.OpenPaletteCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(MainViewModel.JigglerOn) or nameof(MainViewModel.ClickerOn)
            or nameof(MainViewModel.TyperOn) or nameof(MainViewModel.PresenceOn)))
            return;
        var duration = ClientAnimation.Fade(ClientAnimation.ShouldAnimate(SystemQuery.ClientAreaAnimationEnabled()));
        if (duration == TimeSpan.Zero)
            return;
        BeginAnimation(OpacityProperty, new DoubleAnimation(0.86, 1, duration));
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == 0x001A && DataContext is MainViewModel viewModel &&
            viewModel.Settings.Appearance.Theme == ThemeMode.System)
            viewModel.Services.Theme.Apply(viewModel.Settings);
        return nint.Zero;
    }
}
