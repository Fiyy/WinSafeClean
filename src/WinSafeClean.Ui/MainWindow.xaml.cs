using System.IO;
using System.Windows;
using Microsoft.Win32;
using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Quarantine;
using WinSafeClean.Core.Reporting;
using WinSafeClean.Ui.Operations;
using WinSafeClean.Ui.ViewModels;

namespace WinSafeClean.Ui;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ScanTab.DataContext = ScanReportOverviewViewModel.Empty;
        PreflightTab.DataContext = PreflightChecklistOverviewViewModel.Empty;
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

    private void OpenPreflight_Click(object sender, RoutedEventArgs e)
    {
        var dialog = CreateJsonOpenFileDialog();

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var checklist = QuarantinePreflightChecklistJsonSerializer.Deserialize(File.ReadAllText(dialog.FileName));
            PreflightTab.DataContext = PreflightChecklistOverviewViewModel.FromChecklist(checklist);
            PreflightTab.IsSelected = true;
        }
        catch (Exception exception)
        {
            ShowLoadError("Preflight checklist could not be loaded", exception);
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

    private void BuildScan_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int? maxItems = string.IsNullOrWhiteSpace(ScanMaxItemsBox.Text)
                ? null
                : int.Parse(ScanMaxItemsBox.Text);
            OperationCommandText.Text = FormatCommand(ReadOnlyOperationCommandBuilder.BuildScan(
                ScanPathBox.Text,
                ScanRecursiveBox.IsChecked == true,
                maxItems));
        }
        catch (Exception exception)
        {
            ShowLoadError("Scan command could not be built", exception);
        }
    }

    private void BuildPlan_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OperationCommandText.Text = FormatCommand(ReadOnlyOperationCommandBuilder.BuildPlan(
                PlanPathBox.Text,
                CleanerMlPathBox.Text));
        }
        catch (Exception exception)
        {
            ShowLoadError("Plan command could not be built", exception);
        }
    }

    private void BuildPreflight_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OperationCommandText.Text = FormatCommand(ReadOnlyOperationCommandBuilder.BuildPreflight(
                PreflightPlanPathBox.Text,
                PreflightMetadataPathBox.Text,
                PreflightManualConfirmationBox.IsChecked == true));
        }
        catch (Exception exception)
        {
            ShowLoadError("Preflight command could not be built", exception);
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

    private static string FormatCommand(IReadOnlyList<string> args)
    {
        return ".\\.tools\\dotnet\\dotnet.exe run --project .\\src\\WinSafeClean.Cli -- "
            + string.Join(" ", args.Select(QuoteIfNeeded));
    }

    private static string QuoteIfNeeded(string value)
    {
        return value.Contains(' ', StringComparison.Ordinal)
            ? $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : value;
    }
}
