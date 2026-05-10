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
    private CleanupPlan? _currentPlan;
    private string? _currentPlanPath;
    private bool _scanCompleted;
    private bool _planCompleted;
    private bool _preflightCompleted;

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
        UpdateWorkflowState();
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
            _scanCompleted = !string.IsNullOrWhiteSpace(ScanPathBox.Text);
            ScanTab.IsSelected = true;
            UpdateWorkflowState();
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
            _preflightCompleted = true;
            PreflightTab.IsSelected = true;
            UpdateWorkflowState();
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
            LoadPlan(dialog.FileName);
            PreflightPlanPathBox.Text = dialog.FileName;
            EnsureSuggestedOutputPath(PreflightOutputPathBox, "preflight");
        }
        catch (Exception exception)
        {
            ShowLoadError("Plan could not be loaded", exception);
        }
    }

    private void BrowseScanFolder_Click(object sender, RoutedEventArgs e)
    {
        if (TryChooseFolder("Select scan folder", ScanPathBox))
        {
            EnsureSuggestedOutputPath(ScanOutputPathBox, "scan");
        }
    }

    private void BrowseScanFile_Click(object sender, RoutedEventArgs e)
    {
        if (TryChooseFile("Select scan file", "All files (*.*)|*.*", ScanPathBox))
        {
            EnsureSuggestedOutputPath(ScanOutputPathBox, "scan");
        }
    }

    private void BrowseScanOutput_Click(object sender, RoutedEventArgs e)
    {
        TryChooseOutputFile("Save scan report", "scan", ScanOutputPathBox);
    }

    private void BrowseScanCleanerMlFolder_Click(object sender, RoutedEventArgs e)
    {
        TryChooseFolder("Select CleanerML folder", ScanCleanerMlPathBox);
    }

    private void BrowseScanCleanerMlFile_Click(object sender, RoutedEventArgs e)
    {
        TryChooseFile("Select CleanerML file", "CleanerML or JSON files (*.xml;*.json)|*.xml;*.json|All files (*.*)|*.*", ScanCleanerMlPathBox);
    }

    private void BrowsePlanFolder_Click(object sender, RoutedEventArgs e)
    {
        if (TryChooseFolder("Select plan target folder", PlanPathBox))
        {
            EnsureSuggestedOutputPath(PlanOutputPathBox, "plan");
        }
    }

    private void BrowsePlanFile_Click(object sender, RoutedEventArgs e)
    {
        if (TryChooseFile("Select plan target file", "All files (*.*)|*.*", PlanPathBox))
        {
            EnsureSuggestedOutputPath(PlanOutputPathBox, "plan");
        }
    }

    private void BrowsePlanOutput_Click(object sender, RoutedEventArgs e)
    {
        TryChooseOutputFile("Save cleanup plan", "plan", PlanOutputPathBox);
    }

    private void BrowsePlanCleanerMlFolder_Click(object sender, RoutedEventArgs e)
    {
        TryChooseFolder("Select CleanerML folder", PlanCleanerMlPathBox);
    }

    private void BrowsePlanCleanerMlFile_Click(object sender, RoutedEventArgs e)
    {
        TryChooseFile("Select CleanerML file", "CleanerML or JSON files (*.xml;*.json)|*.xml;*.json|All files (*.*)|*.*", PlanCleanerMlPathBox);
    }

    private void BrowsePreflightPlan_Click(object sender, RoutedEventArgs e)
    {
        if (TryChooseFile("Select cleanup plan JSON", "JSON files (*.json)|*.json|All files (*.*)|*.*", PreflightPlanPathBox))
        {
            EnsureSuggestedOutputPath(PreflightOutputPathBox, "preflight");
        }
    }

    private void BrowsePreflightMetadata_Click(object sender, RoutedEventArgs e)
    {
        if (TryChooseFile("Select restore metadata JSON", "JSON files (*.json)|*.json|All files (*.*)|*.*", PreflightMetadataPathBox))
        {
            EnsureSuggestedOutputPath(PreflightOutputPathBox, "preflight");
        }
    }

    private void BrowsePreflightOutput_Click(object sender, RoutedEventArgs e)
    {
        TryChooseOutputFile("Save preflight checklist", "preflight", PreflightOutputPathBox);
    }

    private void UseScanForPlan_Click(object sender, RoutedEventArgs e)
    {
        if (PreparePlanFromScan())
        {
            ShowOperationStatus("Plan inputs are ready. Run Plan to review cleanup candidates.", isError: false);
            return;
        }

        ShowOperationStatus("Choose a scan target path before preparing Plan.", isError: true);
    }

    private void PreparePreflightFromPlanItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentPlan is null)
            {
                throw new InvalidOperationException("Load or run a cleanup plan before preparing preflight.");
            }

            if (PlanItemsList.SelectedItem is not PlanOverviewItemViewModel selectedItem)
            {
                throw new InvalidOperationException("Select a cleanup plan item before preparing preflight.");
            }

            var metadata = PlanPreflightPreparation.CreateRestoreMetadataForPlanItem(
                _currentPlan,
                selectedItem.Path,
                selectedItem.RestoreMetadataPath,
                DateTimeOffset.Now);

            string? metadataInputPath = ChooseOutputFilePath(
                "Save preflight restore metadata input",
                "restore-metadata",
                currentPath: null);
            if (string.IsNullOrWhiteSpace(metadataInputPath))
            {
                return;
            }

            string? parentDirectory = Path.GetDirectoryName(metadataInputPath);
            if (!string.IsNullOrWhiteSpace(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            File.WriteAllText(metadataInputPath, RestoreMetadataJsonSerializer.Serialize(metadata));

            if (!string.IsNullOrWhiteSpace(_currentPlanPath))
            {
                PreflightPlanPathBox.Text = _currentPlanPath;
            }

            PreflightMetadataPathBox.Text = metadataInputPath;
            EnsureSuggestedOutputPath(PreflightOutputPathBox, "preflight");
            ReadOnlyOpsTab.IsSelected = true;
            UpdateWorkflowState();
            ShowOperationStatus("Preflight inputs are ready. Run Preflight before any file-moving command.", isError: false);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "Preflight could not be prepared",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
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

    private void WorkflowInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender == ScanPathBox)
        {
            _scanCompleted = false;
            _planCompleted = false;
            _preflightCompleted = false;
        }
        else if (sender == PlanPathBox)
        {
            _planCompleted = false;
            _preflightCompleted = false;
        }
        else if (sender == PreflightPlanPathBox || sender == PreflightMetadataPathBox)
        {
            _preflightCompleted = false;
        }

        UpdateWorkflowState();
    }

    private void WorkflowPrimaryAction_Click(object sender, RoutedEventArgs e)
    {
        var view = ReadOnlyWorkflowPresenter.Create(CreateWorkflowSnapshot());

        switch (view.PrimaryAction)
        {
            case ReadOnlyWorkflowAction.RunScan:
                EnsureSuggestedOutputPath(ScanOutputPathBox, "scan");
                RunScan_Click(sender, e);
                break;
            case ReadOnlyWorkflowAction.RunPlan:
                if (string.IsNullOrWhiteSpace(PlanPathBox.Text))
                {
                    PreparePlanFromScan();
                }

                EnsureSuggestedOutputPath(PlanOutputPathBox, "plan");
                RunPlan_Click(sender, e);
                break;
            case ReadOnlyWorkflowAction.ReviewPlan:
                PlanTab.IsSelected = true;
                break;
            case ReadOnlyWorkflowAction.RunPreflight:
                EnsureSuggestedOutputPath(PreflightOutputPathBox, "preflight");
                RunPreflight_Click(sender, e);
                break;
            case ReadOnlyWorkflowAction.ReviewPreflight:
                PreflightTab.IsSelected = true;
                break;
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
                PreparePlanFromScan();
                _scanCompleted = true;
                UpdateWorkflowState();
                ShowOperationStatus("Scan completed and JSON report loaded. Plan inputs are ready.", isError: false);
                return;
            }

            PreparePlanFromScan();
            _scanCompleted = true;
            UpdateWorkflowState();
            ShowOperationStatus("Scan completed. Markdown output was written but not loaded. Plan inputs are ready.", isError: false);
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
                PreflightPlanPathBox.Text = PlanOutputPathBox.Text;
                EnsureSuggestedOutputPath(PreflightOutputPathBox, "preflight");
                UpdateWorkflowState();
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
                _preflightCompleted = true;
                UpdateWorkflowState();
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

    private void UpdateWorkflowState()
    {
        if (WorkflowPrimaryActionButton is null)
        {
            return;
        }

        var view = ReadOnlyWorkflowPresenter.Create(CreateWorkflowSnapshot());
        SetWorkflowStepStatus(WorkflowScanStatusText, view.ScanStatus);
        SetWorkflowStepStatus(WorkflowPlanStatusText, view.PlanStatus);
        SetWorkflowStepStatus(WorkflowPreflightStatusText, view.PreflightStatus);
        WorkflowStatusText.Text = view.StatusText;
        WorkflowPrimaryActionButton.Content = view.PrimaryActionText;
        WorkflowPrimaryActionButton.IsEnabled = view.PrimaryActionEnabled;
    }

    private ReadOnlyWorkflowSnapshot CreateWorkflowSnapshot()
    {
        return new ReadOnlyWorkflowSnapshot(
            HasScanTarget: !string.IsNullOrWhiteSpace(ScanPathBox.Text),
            ScanCompleted: _scanCompleted,
            PlanCompleted: _planCompleted,
            PreflightInputsReady: !string.IsNullOrWhiteSpace(PreflightPlanPathBox.Text)
                && !string.IsNullOrWhiteSpace(PreflightMetadataPathBox.Text),
            PreflightCompleted: _preflightCompleted);
    }

    private static void SetWorkflowStepStatus(TextBlock target, string status)
    {
        target.Text = status;
        target.Foreground = status switch
        {
            "Done" => new SolidColorBrush(Color.FromRgb(0x04, 0x78, 0x57)),
            "Ready" => new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0x89)),
            "Needs target" or "Needs candidate" => new SolidColorBrush(Color.FromRgb(0xA1, 0x62, 0x07)),
            _ => new SolidColorBrush(Color.FromRgb(0x5B, 0x64, 0x70))
        };
    }

    private bool TryChooseFolder(string title, TextBox target)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false
        };

        if (Directory.Exists(target.Text))
        {
            dialog.InitialDirectory = target.Text;
        }

        if (dialog.ShowDialog(this) != true)
        {
            return false;
        }

        target.Text = dialog.FolderName;
        return true;
    }

    private bool TryChooseFile(string title, string filter, TextBox target)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false
        };

        string? initialDirectory = GetInitialDirectory(target.Text);
        if (!string.IsNullOrWhiteSpace(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        if (dialog.ShowDialog(this) != true)
        {
            return false;
        }

        target.Text = dialog.FileName;
        return true;
    }

    private bool TryChooseOutputFile(string title, string operationName, TextBox target)
    {
        string? path = ChooseOutputFilePath(title, operationName, target.Text);
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        target.Text = path;
        return true;
    }

    private string? ChooseOutputFilePath(string title, string operationName, string? currentPath)
    {
        string suggestedPath = string.IsNullOrWhiteSpace(currentPath)
            ? SuggestOutputPath(operationName)
            : ResolveOperationPath(currentPath);

        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            AddExtension = true,
            DefaultExt = ".json",
            OverwritePrompt = true,
            FileName = Path.GetFileName(suggestedPath),
            InitialDirectory = Path.GetDirectoryName(suggestedPath)
        };

        return dialog.ShowDialog(this) == true
            ? dialog.FileName
            : null;
    }

    private void EnsureSuggestedOutputPath(TextBox outputBox, string operationName)
    {
        if (!string.IsNullOrWhiteSpace(outputBox.Text))
        {
            return;
        }

        outputBox.Text = SuggestOutputPath(operationName);
    }

    private string SuggestOutputPath(string operationName)
    {
        string directory = GetDefaultOutputDirectory();
        return ReadOnlyOperationOutputPathSuggester.SuggestJsonPath(directory, operationName, DateTimeOffset.Now);
    }

    private string GetDefaultOutputDirectory()
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        return string.IsNullOrWhiteSpace(desktop)
            ? _operationRunnerOptions.WorkingDirectory
            : desktop;
    }

    private static string? GetInitialDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (Directory.Exists(path))
        {
            return path;
        }

        string? parent = Path.GetDirectoryName(path);
        return Directory.Exists(parent) ? parent : null;
    }

    private bool PreparePlanFromScan()
    {
        if (string.IsNullOrWhiteSpace(ScanPathBox.Text))
        {
            return false;
        }

        PlanPathBox.Text = ScanPathBox.Text;
        PlanRecursiveBox.IsChecked = ScanRecursiveBox.IsChecked;
        PlanDirectorySizesBox.IsChecked = ScanDirectorySizesBox.IsChecked;
        PlanMaxItemsBox.Text = ScanMaxItemsBox.Text;
        PlanCleanerMlPathBox.Text = ScanCleanerMlPathBox.Text;
        PlanFormatBox.SelectedIndex = ScanFormatBox.SelectedIndex;
        PlanPrivacyBox.SelectedIndex = ScanPrivacyBox.SelectedIndex;
        EnsureSuggestedOutputPath(PlanOutputPathBox, "plan");
        return true;
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
        _currentPlan = plan;
        _currentPlanPath = path;
        _planCompleted = true;
        _preflightCompleted = false;
        PlanTab.DataContext = PlanOverviewViewModel.FromPlan(plan);
        PlanTab.IsSelected = true;
    }

    private void LoadPreflightChecklist(string path)
    {
        var checklist = QuarantinePreflightChecklistJsonSerializer.Deserialize(File.ReadAllText(path));
        _preflightCompleted = true;
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
