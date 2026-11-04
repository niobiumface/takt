using System.Windows;
using Takt.ViewModels;

namespace Takt.Views;

public partial class WizardWindow : Window
{
    private int _step;

    public WizardWindow() => InitializeComponent();

    private void OnSkip(object sender, RoutedEventArgs e) => Finish();

    private void OnBack(object sender, RoutedEventArgs e)
    {
        if (_step == 0)
            return;
        _step--;
        ShowStep();
    }

    private void OnNext(object sender, RoutedEventArgs e)
    {
        if (_step < 2)
        {
            _step++;
            ShowStep();
            return;
        }

        Finish();
    }

    private void ShowStep()
    {
        StepHotkeys.Visibility = _step == 0 ? Visibility.Visible : Visibility.Collapsed;
        StepHours.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed;
        StepPresence.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed;
        NextButton.Content = _step == 2 ? FindResource("Wizard.Finish") : FindResource("Wizard.Next");
    }

    private void Finish()
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.Settings.General.WizardCompleted = true;
        DialogResult = true;
        Close();
    }
}
