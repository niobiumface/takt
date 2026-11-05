using System.Windows;
using System.Windows.Input;
using Takt.Helpers;
using Takt.Services;
using Takt.ViewModels;

namespace Takt.Views;

public partial class CommandPaletteWindow : Window
{
    private CommandEntry[] _all = [];

    public CommandPaletteWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            _all = Build();
            ApplyFilter();
            QueryBox.Focus();
        };
    }

    private void OnQueryChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ApplyFilter();

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            PickSelected();
            e.Handled = true;
        }
    }

    private void OnListKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            PickSelected();
    }

    private void OnPick(object sender, MouseButtonEventArgs e) => PickSelected();

    private void ApplyFilter()
    {
        var ranked = CommandSearch.Rank(QueryBox.Text, _all);
        Results.ItemsSource = ranked;
        if (ranked.Count > 0)
            Results.SelectedIndex = 0;
    }

    private void PickSelected()
    {
        if (Results.SelectedItem is not CommandEntry entry || DataContext is not MainViewModel viewModel)
            return;
        switch (entry.Id)
        {
            case "jiggler": viewModel.JigglerOn = !viewModel.JigglerOn; break;
            case "clicker": viewModel.ClickerOn = !viewModel.ClickerOn; break;
            case "typer": viewModel.TyperOn = !viewModel.TyperOn; break;
            case "presence": viewModel.PresenceOn = !viewModel.PresenceOn; break;
            case "panic": viewModel.PanicCommand.Execute(null); break;
            case "pause": viewModel.TogglePauseCommand.Execute(null); break;
            case "test-click": viewModel.TestClickCommand.Execute(null); break;
            case "test-type": viewModel.TestTypeCommand.Execute(null); break;
            case "compact": viewModel.ToggleCompactCommand.Execute(null); break;
            case "wizard": viewModel.OpenWizardCommand.Execute(null); break;
            case "settings": viewModel.ShowSettings(null); break;
            default:
                viewModel.ShowSettings(entry.Section, string.IsNullOrEmpty(entry.Target) ? null : entry.Target);
                break;
        }

        Close();
    }

    private static CommandEntry[] Build()
    {
        string T(string key, string fallback) => LocalizationService.Text(key, fallback);
        return
        [
            new("jiggler", T("Card.Jiggler", "Jiggler"), "start stop", "jiggler"),
            new("clicker", T("Card.Clicker", "Clicker"), "start stop", "clicker"),
            new("typer", T("Card.Typer", "Typer"), "text", "typer"),
            new("presence", T("Presence.Toggle", "Keep presence"), "teams idle", "jiggler"),
            new("panic", T("Action.Panic", "Stop all"), "failsafe", "general"),
            new("pause", T("Action.Pause", "Pause or resume"), "hold memory", "general"),
            new("test-click", T("Clicker.Test", "Test one click"), "once", "clicker"),
            new("test-type", T("Typer.Test", "Test once"), "text", "typer"),
            new("compact", T("Compact.Title", "Compact"), "mini always on top", "general"),
            new("wizard", T("Wizard.Title", "First-run wizard"), "setup", "general"),
            new("settings", T("Action.Settings", "Settings"), "gear", "general"),
            new("section-general", T("Nav.General", "General"), "theme language autostart", "general"),
            new("section-hotkeys", T("Nav.Hotkeys", "Hotkeys"), "shortcut", "hotkeys"),
            new("section-clicker", T("Nav.Clicker", "Clicker"), "interval button", "clicker"),
            new("section-jiggler", T("Nav.Jiggler", "Jiggler"), "presence radius", "jiggler"),
            new("section-schedule", T("Nav.Schedule", "Hours"), "work holiday", "schedule"),
            new("section-typer", T("Nav.Typer", "Typer"), "date clipboard", "typer"),
            new("section-macro", T("Nav.Macro", "Macro"), "loop json", "macro"),
            new("section-system", T("Nav.System", "Notifications"), "sound log", "system"),
            new("section-health", T("Nav.Health", "Stats"), "diagnosis timer", "health"),
            new("go-theme", T("Appearance.Theme", "Theme"), "dark light system", "general", "SetTheme"),
            new("go-accent", T("Appearance.Accent", "Accent"), "color hex", "general", "SetAccent"),
            new("go-interval", T("Label.Interval", "Interval"), "click ms", "clicker", "SetInterval"),
            new("go-burst-pause", T("Clicker.BurstPause", "Burst pause"), "burst", "clicker", "SetBurstPause"),
            new("go-stop", T("Timer.After", "Stop after"), "timer minutes", "health", "SetStopMinutes"),
            new("go-date", T("Typer.DateFormat", "Date format"), "placeholder", "typer", "SetDateFormat")
        ];
    }
}
