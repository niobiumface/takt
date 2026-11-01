using System.Windows;
using Takt.Services;
using Takt.ViewModels;

namespace Takt;

public partial class App : Application
{
    private Mutex? _mutex;
    private bool _mutexOwned;
    internal AppServices? Services { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
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
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
                AppLog.Error("Unhandled exception", exception);
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            AppLog.Error("Unobserved task", args.Exception);
            args.SetObserved();
        };

        System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.PerMonitorV2);

        base.OnStartup(e);

        Services = new AppServices(Dispatcher);
        Services.Theme.Apply(Services.Settings.Current);
        Services.Localization.Apply(Services.Settings.Current.Appearance.Language);
        AutostartService.Apply(Services.Settings.Current.General.StartWithWindows);

        var viewModel = new MainViewModel(Services);
        var window = new MainWindow(viewModel);
        MainWindow = window;
        Services.Tray.Attach(window);
        if (Services.Settings.LastLoadReset)
            viewModel.ShowSettingsReset();
        if (!Services.Activity.Start() && !Services.Settings.LastLoadReset)
            viewModel.ShowHookFailure();
        Services.Failsafe.Start();

        window.Show();
        if (Services.Settings.Current.General.StartMinimized)
            window.Hide();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Services?.Dispose();
        if (_mutexOwned)
            _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
