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

    public void FocusSection(string id)
    {
        var panel = id switch
        {
            "general" => SectionGeneral,
            "hotkeys" => SectionHotkeys,
            "clicker" => SectionClicker,
            "jiggler" => SectionJiggler,
            "schedule" => SectionSchedule,
            "typer" => SectionTyper,
            "macro" => SectionMacro,
            "system" => SectionSystem,
            "health" => SectionHealth,
            _ => null
        };
        panel?.BringIntoView();
    }

    public void FocusTarget(string? section, string? target)
    {
        if (!string.IsNullOrWhiteSpace(section))
            FocusSection(section);
        if (string.IsNullOrWhiteSpace(target) || FindName(target) is not FrameworkElement element)
            return;
        element.BringIntoView();
        element.Focus();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control && DataContext is SettingsViewModel undo)
        {
            undo.UndoSettingsCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (DataContext is SettingsViewModel vm)
            vm.TryCompleteCapture(e);
    }
}
