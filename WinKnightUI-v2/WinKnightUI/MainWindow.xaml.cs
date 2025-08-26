// 
// MainWindow.xaml.cs
// This file contains the C# code-behind for the WPF application.
// It handles UI events, updates the display, and calls the core logic.
//

using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

namespace WinKnightUI
{
    public partial class MainWindow : Window
    {
        private WinKnightCore _winKnightCore;

        public MainWindow()
        {
            InitializeComponent();

            // Allow the window to be dragged by the top bar.
            this.MouseLeftButtonDown += (sender, e) => {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            };

            // Check for admin privileges on startup.
            if (!WinKnightCore.IsAdministrator())
            {
                MessageBox.Show("WinKnight must be run with administrator privileges to perform system repairs. Please restart as an administrator.", "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Initialize the core logic class
            _winKnightCore = new WinKnightCore();

            // Set the welcome text to the current user's name
            string userName = Environment.UserName;
            WelcomeNameText.Text = userName;
        }

        #region Window chrome handlers
        // Minimizes the window
        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        // Toggles between maximized and normal window state
        private void MaxRestore_Click(object sender, RoutedEventArgs e) => ToggleMaxRestore();
        private void ToggleMaxRestore() => WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        // Closes the application
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        #endregion

        #region Application Logic
        // Handles the "Run Full System Scan" button click.
        private async void RunScan_Click(object sender, RoutedEventArgs e)
        {
            // Disable buttons to prevent re-running the scan
            RunScanButton.IsEnabled = false;
            CreateRestoreButton.IsEnabled = false;
            ScanProgressBar.IsIndeterminate = true;

            var logBuilder = new StringBuilder();
            ReportLogText.Text = string.Empty; // Clear the previous log

            // Step 1: Create a System Restore Point
            CurrentStatusText.Text = "Creating system restore point...";
            var restoreReport = await _winKnightCore.CreateSystemRestorePoint("WinKnight Automated Scan");
            logBuilder.AppendLine($"[RestoreGuard] {restoreReport.Message}");
            ReportLogText.Text = logBuilder.ToString();

            // If the restore point creation fails, alert the user and stop the scan.
            if (!restoreReport.IsSuccessful)
            {
                MessageBox.Show(
                    "Creating a system restore point failed. Please enable System Restore in Windows and try again.",
                    "System Restore Disabled",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                CurrentStatusText.Text = "Scan failed.";
                ScanProgressBar.IsIndeterminate = false;
                RunScanButton.IsEnabled = true;
                CreateRestoreButton.IsEnabled = true;
                return;
            }
            else
            {
                // Update the last backup time on success
                LastRestoreText.Text = DateTime.Now.ToString("g");
            }

            // Step 2: Run SFC
            CurrentStatusText.Text = "Running System File Checker (SFC)...";
            var sfcReport = await _winKnightCore.RunSfcScan();
            logBuilder.AppendLine($"[SelfHeal] {sfcReport.Message}");
            ReportLogText.Text = logBuilder.ToString();

            // Step 3: Run DISM
            CurrentStatusText.Text = "Running Deployment Image Servicing and Management (DISM)...";
            var dismReport = await _winKnightCore.RunDismRepair();
            logBuilder.AppendLine($"[SelfHeal] {dismReport.Message}");
            ReportLogText.Text = logBuilder.ToString();

            // Step 4: Clean Cache
            CurrentStatusText.Text = "Clearing temporary files...";
            var cacheReport = await _winKnightCore.CleanCache();
            logBuilder.AppendLine($"[CacheCleaner] {cacheReport.Message}");
            ReportLogText.Text = logBuilder.ToString();

            // Final message and re-enable buttons
            CurrentStatusText.Text = "Scan Complete.";
            ScanProgressBar.IsIndeterminate = false;
            RunScanButton.IsEnabled = true;
            CreateRestoreButton.IsEnabled = true;
        }

        // Handles the "Create Manual Restore Point" button click.
        private async void CreateRestore_Click(object sender, RoutedEventArgs e)
        {
            CreateRestoreButton.IsEnabled = false;
            CurrentStatusText.Text = "Creating manual restore point...";

            var report = await _winKnightCore.CreateSystemRestorePoint("WinKnight Manual Backup");

            // Check if the restore point was successfully created.
            if (report.IsSuccessful)
            {
                MessageBox.Show(report.Message, "Restore Point", MessageBoxButton.OK, MessageBoxImage.Information);
                // Update the last backup time on success
                LastRestoreText.Text = DateTime.Now.ToString("g");
            }
            else
            {
                MessageBox.Show(
                    "Creating a system restore point failed. Please enable System Restore in Windows and try again.",
                    "System Restore Disabled",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }

            CurrentStatusText.Text = "Idle...";
            CreateRestoreButton.IsEnabled = true;
        }
        #endregion
    }
}
