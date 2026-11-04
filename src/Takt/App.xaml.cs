using System.Diagnostics;
using System.Windows;
using Takt.Services;
using Takt.ViewModels;
using Takt.Views;

namespace Takt;

public partial class App : Application
{
    private Mutex? _mutex;
    private bool _mutexOwned;
    private bool _restarted;
    private bool _restartOffered;
    internal AppServices? Services { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        _restarted = e.Args.Any(arg => string.Equals(arg, "--restarted", StringComparison.OrdinalIgnoreCase));
        _mutex = new Mutex(true, "Takt.SingleInstance", out _mutexOwned);
        if (!_mutexOwned)
        {
            HotkeyService.RequestShowExisting();
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            AppLog.Error("UI exception", args.Exception);
            args.Handled = true;
            OfferRestart();
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
                AppLog.Error("Unhandled exception", exception);
            SafetyNet.Release(Services);
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            AppLog.Error("Unobserved task", args.Exception);
            args.SetObserved();
            SafetyNet.Release(Services);
        };
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnUserPreference;

        System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.PerMonitorV2);

        base.OnStartup(e);

        Services = new AppServices(Dispatcher);
        AppLog.Minimum = Services.Settings.Current.General.LogLevel;
        Services.Theme.Apply(Services.Settings.Current);
        Services.Localization.Apply(Services.Settings.Current.Appearance.Language);
        AutostartService.Apply(Services.Settings.Current.General.StartWithWindows);

        var viewModel = new MainViewModel(Services);
        var window = new MainWindow(viewModel);
        MainWindow = window;
        Services.Tray.Attach(window);
        viewModel.CheckForUpdateIfEnabled();
        if (Services.Settings.LastLoadReset)
            viewModel.ShowSettingsReset();
        if (!Services.Activity.Start() && !Services.Settings.LastLoadReset)
            viewModel.ShowHookFailure();
        Services.Failsafe.Start();
        if (Services.Settings.Current.Presence.Enabled)
            Services.Presence.Start();

        window.Show();
        if (!Services.Settings.Current.General.WizardCompleted)
            new WizardWindow { Owner = window, DataContext = viewModel }.ShowDialog();
        if (Services.Settings.Current.General.StartMinimized)
            window.Hide();
    }

    private void OnUserPreference(object? sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (Services is null)
                return;
            if (Services.Settings.Current.Appearance.Theme == Models.ThemeMode.System)
                Services.Theme.Apply(Services.Settings.Current);
        });
    }

    private void OfferRestart()
    {
        SafetyNet.Release(Services);
        if (_restartOffered)
            return;
        _restartOffered = true;
        if (_restarted)
        {
            MessageBox.Show(
                "Takt hit an error again and will not restart a second time.",
                "Takt",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var answer = MessageBox.Show(
            "Takt hit an error. Restart it now? Held keys and buttons were released.",
            "Takt",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes)
            return;
        var exe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exe))
            return;
        Process.Start(exe, "--restarted");
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnUserPreference;
        SafetyNet.Release(Services);
        Services?.Dispose();
        if (_mutexOwned)
            _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
