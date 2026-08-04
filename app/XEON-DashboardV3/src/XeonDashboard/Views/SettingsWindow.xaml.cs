using System.Windows;
using XeonDashboard.ViewModels;

namespace XeonDashboard.Views;

public partial class SettingsWindow : Window
{
    public SettingsViewModel ViewModel { get; }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        viewModel.RequestClose += OnRequestClose;
        InitializeComponent();
    }

    private void OnRequestClose(bool saved)
    {
        // DialogResult is only valid when shown via ShowDialog; guard for the
        // non-modal case.
        try { DialogResult = saved; }
        catch (InvalidOperationException) { /* shown non-modally */ }
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        ViewModel.RequestClose -= OnRequestClose;
        base.OnClosed(e);
    }
}
