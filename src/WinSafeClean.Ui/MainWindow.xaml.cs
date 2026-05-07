using System.IO;
using System.Windows;
using Microsoft.Win32;
using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Reporting;
using WinSafeClean.Ui.ViewModels;

namespace WinSafeClean.Ui;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ScanTab.DataContext = ScanReportOverviewViewModel.Empty;
        PlanTab.DataContext = PlanOverviewViewModel.Empty;
    }

    private void OpenScanReport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = CreateJsonOpenFileDialog();

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var report = ScanReportJsonSerializer.Deserialize(File.ReadAllText(dialog.FileName));
            ScanTab.DataContext = ScanReportOverviewViewModel.FromReport(report);
            ScanTab.IsSelected = true;
        }
        catch (Exception exception)
        {
            ShowLoadError("Scan report could not be loaded", exception);
        }
    }

    private void OpenPlan_Click(object sender, RoutedEventArgs e)
    {
        var dialog = CreateJsonOpenFileDialog();

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var plan = CleanupPlanJsonSerializer.Deserialize(File.ReadAllText(dialog.FileName));
            PlanTab.DataContext = PlanOverviewViewModel.FromPlan(plan);
            PlanTab.IsSelected = true;
        }
        catch (Exception exception)
        {
            ShowLoadError("Plan could not be loaded", exception);
        }
    }

    private static OpenFileDialog CreateJsonOpenFileDialog()
    {
        return new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
    }

    private void ShowLoadError(string title, Exception exception)
    {
        MessageBox.Show(
            this,
            exception.Message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}
