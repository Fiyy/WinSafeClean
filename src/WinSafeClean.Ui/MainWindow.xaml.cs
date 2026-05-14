using System.IO;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
    private readonly RecentDocumentHistoryStore _recentDocumentHistoryStore;
    private readonly ReadOnlyRunHistoryStore _runHistoryStore;
    private CleanupPlan? _currentPlan;
    private string? _currentPlanPath;
    private bool _scanCompleted;
    private bool _planCompleted;
    private bool _preflightCompleted;

    public MainWindow()
        : this(CreateDefaultRunnerOptions(), processRunner: null, recentDocumentHistoryStore: null, runHistoryStore: null)
    {
    }

    internal MainWindow(
        ReadOnlyOperationRunnerOptions operationRunnerOptions,
        IReadOnlyOperationProcessRunner? processRunner,
        RecentDocumentHistoryStore? recentDocumentHistoryStore = null,
        ReadOnlyRunHistoryStore? runHistoryStore = null)
    {
        _operationRunnerOptions = operationRunnerOptions;
        _operationRunner = new ReadOnlyOperationRunner(operationRunnerOptions, processRunner);
        _recentDocumentHistoryStore = recentDocumentHistoryStore ?? RecentDocumentHistoryStore.CreateDefault();
        _runHistoryStore = runHistoryStore ?? ReadOnlyRunHistoryStore.CreateDefault();

        InitializeComponent();
        ScanTab.DataContext = ScanReportOverviewViewModel.Empty;
        PreflightTab.DataContext = PreflightChecklistOverviewViewModel.Empty;
        PlanTab.DataContext = PlanOverviewViewModel.Empty;
        QuickScanTargetsList.ItemsSource = QuickScanTargetProvider.CreateDefault();
        RefreshRecentDocuments();
        RefreshRunHistory();
        UpdatePrivacyHints();
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
            LoadScanReport(dialog.FileName);
            _scanCompleted = !string.IsNullOrWhiteSpace(ScanPathBox.Text);
            UpdateWorkflowState();
        }
        catch (Exception exception)
        {
            ShowLoadError("Scan report could not be loaded", exception);
        }
    }

    private void RecentDocumentsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        OpenRecentButton.IsEnabled = RecentDocumentsBox.SelectedItem is RecentDocumentEntry;
    }

    private void OpenRecent_Click(object sender, RoutedEventArgs e)
    {
        if (RecentDocumentsBox.SelectedItem is not RecentDocumentEntry entry)
        {
            return;
        }

        try
        {
            if (!File.Exists(entry.Path))
            {
                _recentDocumentHistoryStore.Remove(entry);
                RefreshRecentDocuments();
                ShowLoadError("Recent file could not be loaded", new FileNotFoundException("Recent file no longer exists.", entry.Path));
                return;
            }

            switch (entry.Kind)
            {
                case RecentDocumentKind.ScanReport:
                    LoadScanReport(entry.Path);
                    _scanCompleted = !string.IsNullOrWhiteSpace(ScanPathBox.Text);
                    break;
                case RecentDocumentKind.CleanupPlan:
                    LoadPlan(entry.Path);
                    PreflightPlanPathBox.Text = entry.Path;
                    EnsureSuggestedOutputPath(PreflightOutputPathBox, "preflight");
                    break;
                case RecentDocumentKind.PreflightChecklist:
                    LoadPreflightChecklist(entry.Path);
                    break;
            }

            UpdateWorkflowState();
        }
        catch (Exception exception)
        {
            ShowLoadError("Recent file could not be loaded", exception);
        }
    }

    private void ClearRecent_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _recentDocumentHistoryStore.Clear();
            RefreshRecentDocuments();
        }
        catch (Exception exception)
        {
            ShowLoadError("Recent files could not be cleared", exception);
        }
    }

    private void RunHistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        OpenRunHistoryOutputButton.IsEnabled = RunHistoryList.SelectedItem is ReadOnlyRunHistoryEntry entry
            && entry.CanOpenInUi;
    }

    private void RefreshRunHistory_Click(object sender, RoutedEventArgs e)
    {
        RefreshRunHistory();
    }

    private void OpenRunHistoryOutput_Click(object sender, RoutedEventArgs e)
    {
        if (RunHistoryList.SelectedItem is not ReadOnlyRunHistoryEntry entry)
        {
            return;
        }

        if (!entry.CanOpenInUi)
        {
            MessageBox.Show(
                this,
                "Only successful JSON outputs can be opened back into the UI.",
                "Run output cannot be opened",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            if (!File.Exists(entry.OutputPath))
            {
                ShowLoadError("Run output could not be loaded", new FileNotFoundException("Run output file no longer exists.", entry.OutputPath));
                return;
            }

            switch (entry.Kind)
            {
                case ReadOnlyRunHistoryKind.Scan:
                    LoadScanReport(entry.OutputPath);
                    _scanCompleted = !string.IsNullOrWhiteSpace(ScanPathBox.Text);
                    break;
                case ReadOnlyRunHistoryKind.Plan:
                    LoadPlan(entry.OutputPath);
                    PreflightPlanPathBox.Text = entry.OutputPath;
                    EnsureSuggestedOutputPath(PreflightOutputPathBox, "preflight");
                    break;
                case ReadOnlyRunHistoryKind.SafetyCheck:
                    LoadPreflightChecklist(entry.OutputPath);
                    break;
            }

            UpdateWorkflowState();
        }
        catch (Exception exception)
        {
            ShowLoadError("Run output could not be loaded", exception);
        }
    }

    private void ClearRunHistory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _runHistoryStore.Clear();
            RefreshRunHistory();
        }
        catch (Exception exception)
        {
            ShowLoadError("Run history could not be cleared", exception);
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
            LoadPreflightChecklist(dialog.FileName);
            UpdateWorkflowState();
        }
        catch (Exception exception)
        {
            ShowLoadError("Safety checklist could not be loaded", exception);
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

    private void QuickScanTarget_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: QuickScanTarget target })
        {
            return;
        }

        ScanPathBox.Text = target.Path;
        EnsureSuggestedOutputPath(ScanOutputPathBox, "scan");
        _scanCompleted = false;
        _planCompleted = false;
        _preflightCompleted = false;
        UpdateWorkflowState();
        ShowOperationStatus($"Review target set to {target.DisplayName}. Run Evidence Scan to create evidence.", isError: false);
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
        TryChooseOutputFile("Save safety checklist", "preflight", PreflightOutputPathBox);
    }

    private void BrowseGuardedOperationLog_Click(object sender, RoutedEventArgs e)
    {
        TryChooseOperationLogFile("Save operation log", GuardedOperationLogPathBox);
    }

    private void UseScanForPlan_Click(object sender, RoutedEventArgs e)
    {
        if (PreparePlanFromScan())
        {
            ShowOperationStatus("Cleanup Plan inputs are ready. Create Plan to review candidates.", isError: false);
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
                "Save safety check restore metadata input",
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
            ShowOperationStatus("Safety Check inputs are ready. Run Safety Check before building any file-moving command.", isError: false);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "Safety Check could not be prepared",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void BuildScan_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OperationCommandText.Text = FormatCommand(BuildScanArguments());
            ShowOperationStatus("Evidence Scan command preview is ready.", isError: false);
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

    private void ScanListFilter_Changed(object sender, RoutedEventArgs e)
    {
        ApplyScanListView();
    }

    private void PlanListFilter_Changed(object sender, RoutedEventArgs e)
    {
        ApplyPlanListView();
    }

    private void PrivacySelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        UpdatePrivacyHints();
    }

    private void CopyScanVisible_Click(object sender, RoutedEventArgs e)
    {
        CopyVisibleRows(
            GetVisibleItems<ScanReportOverviewItemViewModel>(ScanItemsList),
            items => OverviewListExport.CreateScanCsv(items),
            "scan rows");
    }

    private void ExportScanVisible_Click(object sender, RoutedEventArgs e)
    {
        ExportVisibleRows(
            GetVisibleItems<ScanReportOverviewItemViewModel>(ScanItemsList),
            items => OverviewListExport.CreateScanCsv(items),
            "scan rows",
            "winsafeclean-scan-visible.csv");
    }

    private void CopyPlanVisible_Click(object sender, RoutedEventArgs e)
    {
        CopyVisibleRows(
            GetVisibleItems<PlanOverviewItemViewModel>(PlanItemsList),
            items => OverviewListExport.CreatePlanCsv(items),
            "plan rows");
    }

    private void ExportPlanVisible_Click(object sender, RoutedEventArgs e)
    {
        ExportVisibleRows(
            GetVisibleItems<PlanOverviewItemViewModel>(PlanItemsList),
            items => OverviewListExport.CreatePlanCsv(items),
            "plan rows",
            "winsafeclean-plan-visible.csv");
    }

    private void BuildPlan_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OperationCommandText.Text = FormatCommand(BuildPlanArguments());
            ShowOperationStatus("Cleanup Plan command preview is ready.", isError: false);
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
            ShowOperationStatus("Safety Check command preview is ready.", isError: false);
        }
        catch (Exception exception)
        {
            OperationCommandText.Text = string.Empty;
            ShowOperationStatus(exception.Message, isError: true);
        }
    }

    private void BuildQuarantineCli_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OperationCommandText.Text = FormatCommand(BuildGuardedQuarantineArguments());
            ShowOperationStatus("Quarantine CLI command built for manual handoff. The UI did not run it.", isError: false);
        }
        catch (Exception exception)
        {
            OperationCommandText.Text = string.Empty;
            ShowOperationStatus(exception.Message, isError: true);
        }
    }

    private void BuildRestoreCli_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OperationCommandText.Text = FormatCommand(BuildGuardedRestoreArguments());
            ShowOperationStatus("Restore CLI command built for manual handoff. The UI did not run it.", isError: false);
        }
        catch (Exception exception)
        {
            OperationCommandText.Text = string.Empty;
            ShowOperationStatus(exception.Message, isError: true);
        }
    }

    private void CopyCommand_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(OperationCommandText.Text))
        {
            ShowOperationStatus("Build a command before copying it.", isError: true);
            return;
        }

        try
        {
            Clipboard.SetText(OperationCommandText.Text);
            ShowOperationStatus("Command copied to the clipboard.", isError: false);
        }
        catch (Exception exception)
        {
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
            ShowOperationStatus("Running Evidence Scan.", isError: false);

            var startedAt = DateTimeOffset.Now;
            var result = await _operationRunner.RunAsync(args);
            OperationCommandText.Text = FormatRunResult(result);
            RememberRunHistory(
                ReadOnlyRunHistoryKind.Scan,
                ScanPathBox.Text,
                ScanOutputPathBox.Text,
                GetSelectedComboBoxText(ScanFormatBox),
                startedAt,
                result);
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
                ShowOperationStatus("Evidence Scan completed and JSON report loaded. Cleanup Plan inputs are ready.", isError: false);
                return;
            }

            PreparePlanFromScan();
            _scanCompleted = true;
            UpdateWorkflowState();
            ShowOperationStatus("Evidence Scan completed. Markdown output was written but not loaded. Cleanup Plan inputs are ready.", isError: false);
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
            ShowOperationStatus("Creating Cleanup Plan.", isError: false);

            var startedAt = DateTimeOffset.Now;
            var result = await _operationRunner.RunAsync(args);
            OperationCommandText.Text = FormatRunResult(result);
            RememberRunHistory(
                ReadOnlyRunHistoryKind.Plan,
                PlanPathBox.Text,
                PlanOutputPathBox.Text,
                GetSelectedComboBoxText(PlanFormatBox),
                startedAt,
                result);
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
                ShowOperationStatus("Cleanup Plan created and loaded. Review candidates before preparing a Safety Check.", isError: false);
                return;
            }

            ShowOperationStatus("Cleanup Plan completed. Markdown output was written but not loaded.", isError: false);
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
            ShowOperationStatus("Running Safety Check.", isError: false);

            var startedAt = DateTimeOffset.Now;
            var result = await _operationRunner.RunAsync(args);
            OperationCommandText.Text = FormatRunResult(result);
            RememberRunHistory(
                ReadOnlyRunHistoryKind.SafetyCheck,
                PreflightPlanPathBox.Text,
                PreflightOutputPathBox.Text,
                GetSelectedComboBoxText(PreflightFormatBox),
                startedAt,
                result);
            if (!result.Succeeded)
            {
                ShowOperationStatus($"Safety Check failed with exit code {result.ExitCode}.", isError: true);
                return;
            }

            if (IsJsonFormat(PreflightFormatBox))
            {
                LoadPreflightChecklist(ResolveOperationPath(PreflightOutputPathBox.Text));
                _preflightCompleted = true;
                UpdateWorkflowState();
                ShowOperationStatus("Safety Check completed and JSON checklist loaded.", isError: false);
                return;
            }

            ShowOperationStatus("Safety Check completed. Markdown output was written but not loaded.", isError: false);
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
        WorkflowCurrentStepTitleText.Text = view.CurrentStepTitle;
        WorkflowCurrentStepDetailText.Text = view.CurrentStepDetail;
        WorkflowStatusText.Text = view.StatusText;
        WorkflowPrimaryActionButton.Content = view.PrimaryActionText;
        WorkflowPrimaryActionButton.IsEnabled = view.PrimaryActionEnabled;
        WorkflowSafetyBoundaryText.Text = view.SafetyBoundary;
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

    private bool TryChooseOperationLogFile(string title, TextBox target)
    {
        string suggestedPath = string.IsNullOrWhiteSpace(target.Text)
            ? Path.Combine(GetDefaultOutputDirectory(), "winsafeclean-operations.jsonl")
            : ResolveOperationPath(target.Text);

        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = "JSONL files (*.jsonl)|*.jsonl|All files (*.*)|*.*",
            AddExtension = true,
            DefaultExt = ".jsonl",
            OverwritePrompt = false,
            FileName = Path.GetFileName(suggestedPath),
            InitialDirectory = Path.GetDirectoryName(suggestedPath)
        };

        if (dialog.ShowDialog(this) != true)
        {
            return false;
        }

        target.Text = dialog.FileName;
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
        UpdatePrivacyHints();
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

    private void UpdatePrivacyHints()
    {
        if (ScanPrivacyHintText is null || PlanPrivacyHintText is null)
        {
            return;
        }

        ApplyPrivacyHint(ScanPrivacyHintText, GetSelectedComboBoxText(ScanPrivacyBox));
        ApplyPrivacyHint(PlanPrivacyHintText, GetSelectedComboBoxText(PlanPrivacyBox));
    }

    private static void ApplyPrivacyHint(TextBlock target, string privacyMode)
    {
        var advice = PrivacySharingAdvisor.Create(privacyMode);
        target.Text = advice.Message;
        target.Foreground = advice.NeedsCaution
            ? new SolidColorBrush(Color.FromRgb(0xA1, 0x62, 0x07))
            : new SolidColorBrush(Color.FromRgb(0x04, 0x78, 0x57));
    }

    private void CopyVisibleRows<T>(IReadOnlyList<T> items, Func<IReadOnlyList<T>, string> createCsv, string itemLabel)
    {
        if (items.Count == 0)
        {
            MessageBox.Show(
                this,
                $"There are no visible {itemLabel} to copy.",
                "Nothing to copy",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            Clipboard.SetText(createCsv(items));
            MessageBox.Show(
                this,
                $"Copied {items.Count} visible {itemLabel} to the clipboard.",
                "Rows copied",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            ShowLoadError("Visible rows could not be copied", exception);
        }
    }

    private void ExportVisibleRows<T>(
        IReadOnlyList<T> items,
        Func<IReadOnlyList<T>, string> createCsv,
        string itemLabel,
        string defaultFileName)
    {
        if (items.Count == 0)
        {
            MessageBox.Show(
                this,
                $"There are no visible {itemLabel} to export.",
                "Nothing to export",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export visible rows",
            FileName = defaultFileName,
            DefaultExt = ".csv",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            File.WriteAllText(dialog.FileName, createCsv(items), Encoding.UTF8);
            MessageBox.Show(
                this,
                $"Exported {items.Count} visible {itemLabel}.",
                "Rows exported",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            ShowLoadError("Visible rows could not be exported", exception);
        }
    }

    private static IReadOnlyList<T> GetVisibleItems<T>(ListBox listBox)
    {
        return listBox.Items.Cast<T>().ToList();
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

    private IReadOnlyList<string> BuildGuardedQuarantineArguments()
    {
        return GuardedFileMoveCommandBuilder.BuildQuarantine(new GuardedQuarantineCommandOptions(
            PlanPath: PreflightPlanPathBox.Text,
            MetadataPath: PreflightMetadataPathBox.Text,
            ManualConfirmation: GuardedManualConfirmationBox.IsChecked == true,
            UnderstandsFileMoves: GuardedMoveAcknowledgementBox.IsChecked == true,
            OperationLogPath: GuardedOperationLogPathBox.Text));
    }

    private IReadOnlyList<string> BuildGuardedRestoreArguments()
    {
        return GuardedFileMoveCommandBuilder.BuildRestore(new GuardedRestoreCommandOptions(
            MetadataPath: PreflightMetadataPathBox.Text,
            ManualConfirmation: GuardedManualConfirmationBox.IsChecked == true,
            UnderstandsFileMoves: GuardedMoveAcknowledgementBox.IsChecked == true,
            OperationLogPath: GuardedOperationLogPathBox.Text,
            AllowLegacyMetadataWithoutHash: GuardedAllowLegacyMetadataBox.IsChecked == true));
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
        ApplyScanListView();
        RememberRecentDocument(RecentDocumentKind.ScanReport, path);
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
        ApplyPlanListView();
        RememberRecentDocument(RecentDocumentKind.CleanupPlan, path);
    }

    private void LoadPreflightChecklist(string path)
    {
        var checklist = QuarantinePreflightChecklistJsonSerializer.Deserialize(File.ReadAllText(path));
        _preflightCompleted = true;
        PreflightTab.DataContext = PreflightChecklistOverviewViewModel.FromChecklist(checklist);
        PreflightTab.IsSelected = true;
        RememberRecentDocument(RecentDocumentKind.PreflightChecklist, path);
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

    private void ApplyScanListView()
    {
        if (ScanItemsList is null || ScanItemsList.ItemsSource is null)
        {
            return;
        }

        var view = CollectionViewSource.GetDefaultView(ScanItemsList.ItemsSource);
        if (view is null)
        {
            return;
        }

        var filter = CreateScanOverviewFilter();
        using (view.DeferRefresh())
        {
            view.Filter = item => item is ScanReportOverviewItemViewModel viewModel
                && OverviewListFilter.MatchesScanItem(viewModel, filter);
            view.SortDescriptions.Clear();
            foreach (var sortDescription in CreateScanSortDescriptions(filter.Sort))
            {
                view.SortDescriptions.Add(sortDescription);
            }
        }
    }

    private void ApplyPlanListView()
    {
        if (PlanItemsList is null || PlanItemsList.ItemsSource is null)
        {
            return;
        }

        var view = CollectionViewSource.GetDefaultView(PlanItemsList.ItemsSource);
        if (view is null)
        {
            return;
        }

        var filter = CreatePlanOverviewFilter();
        using (view.DeferRefresh())
        {
            view.Filter = item => item is PlanOverviewItemViewModel viewModel
                && OverviewListFilter.MatchesPlanItem(viewModel, filter);
            view.SortDescriptions.Clear();
            foreach (var sortDescription in CreatePlanSortDescriptions(filter.Sort))
            {
                view.SortDescriptions.Add(sortDescription);
            }
        }
    }

    private ScanOverviewFilter CreateScanOverviewFilter()
    {
        return new ScanOverviewFilter(
            SearchText: ScanListSearchBox?.Text ?? string.Empty,
            RiskLevel: GetSelectedComboBoxTextOrAll(ScanRiskFilterBox),
            ItemKind: GetSelectedComboBoxTextOrAll(ScanKindFilterBox),
            Sort: GetScanSortOption());
    }

    private PlanOverviewFilter CreatePlanOverviewFilter()
    {
        return new PlanOverviewFilter(
            SearchText: PlanListSearchBox?.Text ?? string.Empty,
            RiskLevel: GetSelectedComboBoxTextOrAll(PlanRiskFilterBox),
            Action: GetSelectedComboBoxTextOrAll(PlanActionFilterBox),
            Sort: GetPlanSortOption());
    }

    private ScanOverviewSort GetScanSortOption()
    {
        return GetSelectedComboBoxTextOrAll(ScanSortBox) switch
        {
            "Path A-Z" => ScanOverviewSort.PathAscending,
            "Risk A-Z" => ScanOverviewSort.RiskAscending,
            "Type A-Z" => ScanOverviewSort.ItemKindAscending,
            _ => ScanOverviewSort.SizeDescending
        };
    }

    private PlanOverviewSort GetPlanSortOption()
    {
        return GetSelectedComboBoxTextOrAll(PlanSortBox) switch
        {
            "Action A-Z" => PlanOverviewSort.ActionAscending,
            "Risk A-Z" => PlanOverviewSort.RiskAscending,
            _ => PlanOverviewSort.PathAscending
        };
    }

    private static IEnumerable<SortDescription> CreateScanSortDescriptions(ScanOverviewSort sort)
    {
        return sort switch
        {
            ScanOverviewSort.PathAscending =>
            [
                new SortDescription(nameof(ScanReportOverviewItemViewModel.Path), ListSortDirection.Ascending)
            ],
            ScanOverviewSort.RiskAscending =>
            [
                new SortDescription(nameof(ScanReportOverviewItemViewModel.RiskLevel), ListSortDirection.Ascending),
                new SortDescription(nameof(ScanReportOverviewItemViewModel.Path), ListSortDirection.Ascending)
            ],
            ScanOverviewSort.ItemKindAscending =>
            [
                new SortDescription(nameof(ScanReportOverviewItemViewModel.ItemKind), ListSortDirection.Ascending),
                new SortDescription(nameof(ScanReportOverviewItemViewModel.Path), ListSortDirection.Ascending)
            ],
            _ =>
            [
                new SortDescription(nameof(ScanReportOverviewItemViewModel.SizeBytes), ListSortDirection.Descending),
                new SortDescription(nameof(ScanReportOverviewItemViewModel.Path), ListSortDirection.Ascending)
            ]
        };
    }

    private static IEnumerable<SortDescription> CreatePlanSortDescriptions(PlanOverviewSort sort)
    {
        return sort switch
        {
            PlanOverviewSort.ActionAscending =>
            [
                new SortDescription(nameof(PlanOverviewItemViewModel.Action), ListSortDirection.Ascending),
                new SortDescription(nameof(PlanOverviewItemViewModel.Path), ListSortDirection.Ascending)
            ],
            PlanOverviewSort.RiskAscending =>
            [
                new SortDescription(nameof(PlanOverviewItemViewModel.RiskLevel), ListSortDirection.Ascending),
                new SortDescription(nameof(PlanOverviewItemViewModel.Path), ListSortDirection.Ascending)
            ],
            _ =>
            [
                new SortDescription(nameof(PlanOverviewItemViewModel.Path), ListSortDirection.Ascending)
            ]
        };
    }

    private static string GetSelectedComboBoxTextOrAll(ComboBox? comboBox)
    {
        return comboBox is null ? "All" : GetSelectedComboBoxText(comboBox);
    }

    private static string QuoteIfNeeded(string value)
    {
        return value.Contains(' ', StringComparison.Ordinal)
            ? $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : value;
    }

    private void RememberRecentDocument(RecentDocumentKind kind, string path)
    {
        try
        {
            _recentDocumentHistoryStore.Add(kind, ResolveOperationPath(path), DateTimeOffset.Now);
            RefreshRecentDocuments();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Recent history is a convenience feature; report loading remains the primary action.
        }
    }

    private void RememberRunHistory(
        ReadOnlyRunHistoryKind kind,
        string targetPath,
        string outputPath,
        string format,
        DateTimeOffset startedAt,
        ReadOnlyOperationRunResult result)
    {
        try
        {
            _runHistoryStore.Add(new ReadOnlyRunHistoryEntry(
                Kind: kind,
                TargetPath: ResolveOperationPath(targetPath),
                OutputPath: ResolveOperationPath(outputPath),
                Format: format,
                ExitCode: result.ExitCode,
                StartedAt: startedAt,
                CompletedAt: DateTimeOffset.Now));
            RefreshRunHistory();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            // Run history is local convenience metadata; command results remain visible even if it cannot be stored.
        }
    }

    private void RefreshRecentDocuments()
    {
        if (RecentDocumentsBox is null)
        {
            return;
        }

        IReadOnlyList<RecentDocumentEntry> entries;
        try
        {
            entries = _recentDocumentHistoryStore.Load();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            entries = [];
        }

        RecentDocumentsBox.ItemsSource = entries;
        RecentDocumentsBox.SelectedIndex = entries.Count > 0 ? 0 : -1;
        OpenRecentButton.IsEnabled = entries.Count > 0;
        ClearRecentButton.IsEnabled = entries.Count > 0;
    }

    private void RefreshRunHistory()
    {
        if (RunHistoryList is null)
        {
            return;
        }

        IReadOnlyList<ReadOnlyRunHistoryEntry> entries;
        try
        {
            entries = _runHistoryStore.Load();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            entries = [];
        }

        RunHistoryList.ItemsSource = entries;
        RunHistoryList.SelectedIndex = entries.Count > 0 ? 0 : -1;
        OpenRunHistoryOutputButton.IsEnabled = RunHistoryList.SelectedItem is ReadOnlyRunHistoryEntry entry
            && entry.CanOpenInUi;
        ClearRunHistoryButton.IsEnabled = entries.Count > 0;
        RunHistoryStatusText.Text = entries.Count == 0
            ? "No local runs recorded yet."
            : $"{entries.Count} local read-only run(s).";
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
