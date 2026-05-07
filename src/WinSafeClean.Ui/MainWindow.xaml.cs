using System.IO;
using System.Windows;
using Microsoft.Win32;
using WinSafeClean.Core.Planning;
using WinSafeClean.Ui.ViewModels;

namespace WinSafeClean.Ui;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OpenPlan_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Cleanup plan JSON (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var plan = CleanupPlanJsonSerializer.Deserialize(File.ReadAllText(dialog.FileName));
            DataContext = PlanOverviewViewModel.FromPlan(plan);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "Plan could not be loaded",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
