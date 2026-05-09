using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Quarantine;
using WinSafeClean.Core.Reporting;
using WinSafeClean.Ui.Operations;
using WinSafeClean.Ui.ViewModels;

namespace WinSafeClean.Ui;

public partial class MainWindow : Window
{
    private readonly ReadOnlyOperationRunnerOptions _operationRunnerOptions;
    private readonly ReadOnlyOperationRunner _operationRunner;

    public MainWindow()
        : this(CreateDefaultRunnerOptions(), processRunner: null)
    {
    }

    internal MainWindow(
        ReadOnlyOperationRunnerOptions operationRunnerOptions,
        IReadOnlyOperationProcessRunner? processRunner)
    {
        _operationRunnerOptions = operationRunnerOptions;
        _operationRunner = new ReadOnlyOperationRunner(operationRunnerOptions, processRunner);

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
            OperationCommandText.Text = FormatCommand(BuildScanArguments());
            ShowOperationStatus("Scan command built.", isError: false);
        }
        catch (Exception exception)
        {
            OperationCommandText.Text = string.Empty;
            ShowOperationStatus(exception.Message, isError: true);
        }
    }

    private void BuildPlan_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OperationCommandText.Text = FormatCommand(BuildPlanArguments());
            ShowOperationStatus("Plan command built.", isError: false);
        }
        catch (Exception exception)
        {
            OperationCommandText.Text = string.Empty;
            ShowOperationStatus(exception.Message, isError: true);
        }
    }

    private void BuildPreflight_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OperationCommandText.Text = FormatCommand(BuildPreflightArguments());
            ShowOperationStatus("Preflight command built.", isError: false);
        }
        catch (Exception exception)
        {
            OperationCommandText.Text = string.Empty;
            ShowOperationStatus(exception.Message, isError: true);
        }
    }

    private async void RunScan_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireOutputPath(ScanOutputPathBox.Text);
            var args = BuildScanArguments();
            OperationCommandText.Text = FormatCommand(args);
            ShowOperationStatus("Running scan command.", isError: false);

            var result = await _operationRunner.RunAsync(args);
            OperationCommandText.Text = FormatRunResult(result);
            if (!result.Succeeded)
            {
                ShowOperationStatus($"Scan failed with exit code {result.ExitCode}.", isError: true);
                return;
            }

            if (IsJsonFormat(ScanFormatBox))
            {
                LoadScanReport(ResolveOperationPath(ScanOutputPathBox.Text));
                ShowOperationStatus("Scan completed and JSON report loaded.", isError: false);
                return;
            }

            ShowOperationStatus("Scan completed. Markdown output was written but not loaded.", isError: false);
        }
        catch (Exception exception)
        {
            ShowOperationStatus(exception.Message, isError: true);
        }
    }

    private async void RunPlan_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireOutputPath(PlanOutputPathBox.Text);
            var args = BuildPlanArguments();
            OperationCommandText.Text = FormatCommand(args);
            ShowOperationStatus("Running plan command.", isError: false);

            var result = await _operationRunner.RunAsync(args);
            OperationCommandText.Text = FormatRunResult(result);
            if (!result.Succeeded)
            {
                ShowOperationStatus($"Plan failed with exit code {result.ExitCode}.", isError: true);
                return;
            }

            if (IsJsonFormat(PlanFormatBox))
            {
                LoadPlan(ResolveOperationPath(PlanOutputPathBox.Text));
                ShowOperationStatus("Plan completed and JSON cleanup plan loaded.", isError: false);
                return;
            }

            ShowOperationStatus("Plan completed. Markdown output was written but not loaded.", isError: false);
        }
        catch (Exception exception)
        {
            ShowOperationStatus(exception.Message, isError: true);
        }
    }

    private async void RunPreflight_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RequireOutputPath(PreflightOutputPathBox.Text);
            var args = BuildPreflightArguments();
            OperationCommandText.Text = FormatCommand(args);
            ShowOperationStatus("Running preflight command.", isError: false);

            var result = await _operationRunner.RunAsync(args);
            OperationCommandText.Text = FormatRunResult(result);
            if (!result.Succeeded)
            {
                ShowOperationStatus($"Preflight failed with exit code {result.ExitCode}.", isError: true);
                return;
            }

            if (IsJsonFormat(PreflightFormatBox))
            {
                LoadPreflightChecklist(ResolveOperationPath(PreflightOutputPathBox.Text));
                ShowOperationStatus("Preflight completed and JSON checklist loaded.", isError: false);
                return;
            }

            ShowOperationStatus("Preflight completed. Markdown output was written but not loaded.", isError: false);
        }
        catch (Exception exception)
        {
            ShowOperationStatus(exception.Message, isError: true);
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

    private void ShowOperationStatus(string message, bool isError)
    {
        OperationStatusText.Text = message;
        OperationStatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(0xA5, 0x1D, 0x2D))
            : new SolidColorBrush(Color.FromRgb(0x04, 0x78, 0x57));
    }

    private IReadOnlyList<string> BuildScanArguments()
    {
        int? maxItems = ReadOnlyOperationCommandBuilder.ParseMaxItems(ScanMaxItemsBox.Text);
        return ReadOnlyOperationCommandBuilder.BuildScan(new ReadOnlyScanCommandOptions(
            Path: ScanPathBox.Text,
            Recursive: ScanRecursiveBox.IsChecked == true,
            MaxItems: maxItems,
            Format: GetSelectedComboBoxText(ScanFormatBox),
            Privacy: GetSelectedComboBoxText(ScanPrivacyBox),
            OutputPath: ScanOutputPathBox.Text,
            CleanerMlPath: ScanCleanerMlPathBox.Text,
            IncludeDirectorySizes: ScanDirectorySizesBox.IsChecked == true));
    }

    private IReadOnlyList<string> BuildPlanArguments()
    {
        int? maxItems = ReadOnlyOperationCommandBuilder.ParseMaxItems(PlanMaxItemsBox.Text);
        return ReadOnlyOperationCommandBuilder.BuildPlan(new ReadOnlyPlanCommandOptions(
            Path: PlanPathBox.Text,
            Recursive: PlanRecursiveBox.IsChecked == true,
            MaxItems: maxItems,
            CleanerMlPath: PlanCleanerMlPathBox.Text,
            Format: GetSelectedComboBoxText(PlanFormatBox),
            Privacy: GetSelectedComboBoxText(PlanPrivacyBox),
            OutputPath: PlanOutputPathBox.Text,
            IncludeDirectorySizes: PlanDirectorySizesBox.IsChecked == true));
    }

    private IReadOnlyList<string> BuildPreflightArguments()
    {
        return ReadOnlyOperationCommandBuilder.BuildPreflight(new ReadOnlyPreflightCommandOptions(
            PlanPath: PreflightPlanPathBox.Text,
            MetadataPath: PreflightMetadataPathBox.Text,
            ManualConfirmation: PreflightManualConfirmationBox.IsChecked == true,
            Format: GetSelectedComboBoxText(PreflightFormatBox),
            OutputPath: PreflightOutputPathBox.Text));
    }

    private static string FormatCommand(IReadOnlyList<string> args)
    {
        return ".\\.tools\\dotnet\\dotnet.exe run --project .\\src\\WinSafeClean.Cli -- "
            + string.Join(" ", args.Select(QuoteIfNeeded));
    }

    private static string FormatRunResult(ReadOnlyOperationRunResult result)
    {
        var output = result.CommandText;
        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            output += Environment.NewLine + Environment.NewLine + result.StandardOutput.TrimEnd();
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            output += Environment.NewLine + Environment.NewLine + result.StandardError.TrimEnd();
        }

        return output;
    }

    private static void RequireOutputPath(string? outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new InvalidOperationException("Output path is required before running from the UI.");
        }
    }

    private string ResolveOperationPath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(_operationRunnerOptions.WorkingDirectory, path));
    }

    private void LoadScanReport(string path)
    {
        var report = ScanReportJsonSerializer.Deserialize(File.ReadAllText(path));
        ScanTab.DataContext = ScanReportOverviewViewModel.FromReport(report);
        ScanTab.IsSelected = true;
    }

    private void LoadPlan(string path)
    {
        var plan = CleanupPlanJsonSerializer.Deserialize(File.ReadAllText(path));
        PlanTab.DataContext = PlanOverviewViewModel.FromPlan(plan);
        PlanTab.IsSelected = true;
    }

    private void LoadPreflightChecklist(string path)
    {
        var checklist = QuarantinePreflightChecklistJsonSerializer.Deserialize(File.ReadAllText(path));
        PreflightTab.DataContext = PreflightChecklistOverviewViewModel.FromChecklist(checklist);
        PreflightTab.IsSelected = true;
    }

    private static string GetSelectedComboBoxText(ComboBox comboBox)
    {
        return comboBox.SelectedItem is ComboBoxItem item
            ? item.Content?.ToString() ?? string.Empty
            : comboBox.Text;
    }

    private static bool IsJsonFormat(ComboBox comboBox)
    {
        return GetSelectedComboBoxText(comboBox).Equals("json", StringComparison.OrdinalIgnoreCase);
    }

    private static string QuoteIfNeeded(string value)
    {
        return value.Contains(' ', StringComparison.Ordinal)
            ? $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : value;
    }

    private static ReadOnlyOperationRunnerOptions CreateDefaultRunnerOptions()
    {
        var workingDirectory = FindRepositoryRoot(Directory.GetCurrentDirectory())
            ?? FindRepositoryRoot(AppContext.BaseDirectory)
            ?? Directory.GetCurrentDirectory();
        var localDotNet = Path.Combine(workingDirectory, ".tools", "dotnet", "dotnet.exe");
        var dotNetPath = File.Exists(localDotNet) ? localDotNet : "dotnet";
        var cliProjectPath = Path.Combine(workingDirectory, "src", "WinSafeClean.Cli");

        return new ReadOnlyOperationRunnerOptions(dotNetPath, cliProjectPath, workingDirectory);
    }

    private static string? FindRepositoryRoot(string startPath)
    {
        var directory = Directory.Exists(startPath)
            ? new DirectoryInfo(startPath)
            : new DirectoryInfo(Path.GetDirectoryName(startPath) ?? startPath);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WinSafeClean.sln"))
                && Directory.Exists(Path.Combine(directory.FullName, "src", "WinSafeClean.Cli")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
