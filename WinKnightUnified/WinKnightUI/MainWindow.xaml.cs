using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;

namespace WinKnightUI
{
    public partial class MainWindow : Window
    {
        private WinKnightCore _winKnightCore;
        private DispatcherTimer? _metricsTimer;
        private bool _isTempWarningActive = false;
        private bool _isRamWarningActive = false;

        private double _warningTempAtTime = 0.0;
        private int _warningRamAtTime = 0;

        public MainWindow()
        {
            InitializeComponent();
            _winKnightCore = new WinKnightCore();

            this.MouseLeftButtonDown += (sender, e) => {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            };

            if (!WinKnightCore.IsAdministrator())
            {
                MessageBox.Show("WinKnight must be run with administrator privileges to perform system repairs. Please restart as an administrator.", "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            string userName = Environment.UserName;
            WelcomeNameText.Text = userName;

            StartMetricsTimer();
        }

        private void StartMetricsTimer()
        {
            _metricsTimer = new DispatcherTimer();
            _metricsTimer.Interval = TimeSpan.FromSeconds(1);
            _metricsTimer.Tick += MetricsTimer_Tick;
            _metricsTimer.Start();
        }
        private void OpenRestorePoints_Click(object sender, RoutedEventArgs e)
        {
            var win = new RestorePointWindow();
            win.Owner = this;
            win.ShowDialog();
        }

        private async void MetricsTimer_Tick(object? sender, EventArgs e)
        {
            var temp = await _winKnightCore.GetCpuTemperature();
            if (temp > -1)
            {
                CpuTempText.Text = $"{temp}°C";

                if (temp > 80 && !_isTempWarningActive)
                {
                    _isTempWarningActive = true;
                    _warningTempAtTime = temp;
                }
                TempWarningButton.Visibility = _isTempWarningActive ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                CpuTempText.Text = "N/A";
                TempWarningButton.Visibility = Visibility.Collapsed;
            }

            var ramUsage = await _winKnightCore.GetRamUsage();
            RamUsageText.Text = $"{ramUsage}%";

            if (ramUsage > 85 && !_isRamWarningActive)
            {
                _isRamWarningActive = true;
                _warningRamAtTime = ramUsage;
            }
            RamWarningButton.Visibility = _isRamWarningActive ? Visibility.Visible : Visibility.Collapsed;

            var diskStatus = await _winKnightCore.GetDiskHealthStatus();
            DiskHealthText.Text = diskStatus.Status;

            var startupCount = await _winKnightCore.GetStartupProgramsCount();
            StartupProgramsText.Text = startupCount.ToString();
        }

        #region Window chrome handlers
        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void MaxRestore_Click(object sender, RoutedEventArgs e) => ToggleMaxRestore();
        private void ToggleMaxRestore() => WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        #endregion

        #region Application Logic
        private void CpuTemp_Click(object sender, RoutedEventArgs e)
        {
            var popup = new TempGraphPopup(_winKnightCore);
            popup.ShowDialog();
        }

        private async void TempWarning_Click(object sender, RoutedEventArgs e)
        {
            var topProcess = await _winKnightCore.GetTopCpuProcess();

            string message = $"A high CPU temperature was detected.\n\n";
            message += $"Highest temperature: {_warningTempAtTime}°C\n";
            message += $"Possible cause: '{topProcess}'\n";

            MessageBox.Show(message, "CPU Temperature Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

            _isTempWarningActive = false;
            TempWarningButton.Visibility = Visibility.Collapsed;
        }

        private async void RamUsage_Click(object sender, RoutedEventArgs e)
        {
            var allProcesses = await _winKnightCore.GetTopRamProcess();
            var popup = new RamUsagePopup(allProcesses, _winKnightCore);
            popup.ShowDialog();
        }

        private async void RamWarning_Click(object sender, RoutedEventArgs e)
        {
            var topProcess = (await _winKnightCore.GetTopRamProcess()).FirstOrDefault();
            if (topProcess != null)
            {
                var popup = new HighRamUsageWarning(topProcess.Name, _winKnightCore);
                popup.ShowDialog();
            }

            _isRamWarningActive = false;
            RamWarningButton.Visibility = Visibility.Collapsed;
        }

        private async void DiskHealth_Click(object sender, RoutedEventArgs e)
        {
            var drives = await _winKnightCore.GetDiskHealthDetails();
            var popup = new DiskMonitorPopup(drives, _winKnightCore);
            popup.ShowDialog();
        }

        private async void StartupManager_Click(object sender, RoutedEventArgs e)
        {
            var startupPrograms = await _winKnightCore.GetStartupPrograms();
            var popup = new StartupManagerPopup(startupPrograms, _winKnightCore);
            popup.ShowDialog();
        }

        private async void RunScan_Click(object sender, RoutedEventArgs e)
        {
            RunScanButton.IsEnabled = false;
            CreateRestoreButton.IsEnabled = false;
            ScanProgressBar.IsIndeterminate = true;
            ReportLogText.Text = string.Empty;

            var logBuilder = new StringBuilder();

            CurrentStatusText.Text = "Creating system restore point...";
            var restoreReport = await _winKnightCore.CreateSystemRestorePoint("WinKnight Automated Scan");
            logBuilder.AppendLine($"[RestoreGuard] {restoreReport.Message}");
            ReportLogText.Text = logBuilder.ToString();

            if (!restoreReport.IsSuccessful)
            {
                MessageBox.Show("Creating a system restore point failed. Please enable System Restore in Windows and try again.", "System Restore Disabled", MessageBoxButton.OK, MessageBoxImage.Warning);
                CurrentStatusText.Text = "Scan failed.";
                ScanProgressBar.IsIndeterminate = false;
                RunScanButton.IsEnabled = true;
                CreateRestoreButton.IsEnabled = true;
                return;
            }
            else
            {
                LastRestoreText.Text = DateTime.Now.ToString("g");
            }

            CurrentStatusText.Text = "Running System File Checker (SFC)...";
            var sfcReport = await _winKnightCore.RunSfcScan();
            logBuilder.AppendLine($"[SelfHeal] {sfcReport.Message}");
            ReportLogText.Text = logBuilder.ToString();

            CurrentStatusText.Text = "Running Deployment Image Servicing and Management (DISM)...";
            var dismReport = await _winKnightCore.RunDismRepair();
            logBuilder.AppendLine($"[SelfHeal] {dismReport.Message}");
            ReportLogText.Text = logBuilder.ToString();

            CurrentStatusText.Text = "Clearing temporary files...";
            var cacheReport = await _winKnightCore.CleanCache();
            logBuilder.AppendLine($"[CacheCleaner] {cacheReport.Message}");
            ReportLogText.Text = logBuilder.ToString();

            CurrentStatusText.Text = "Scan Complete.";
            ScanProgressBar.IsIndeterminate = false;
            RunScanButton.IsEnabled = true;
            CreateRestoreButton.IsEnabled = true;
        }

        private async void CreateRestore_Click(object sender, RoutedEventArgs e)
        {
            CreateRestoreButton.IsEnabled = false;
            CurrentStatusText.Text = "Creating manual restore point...";

            var report = await _winKnightCore.CreateSystemRestorePoint("WinKnight Manual Backup");

            if (report.IsSuccessful)
            {
                MessageBox.Show(report.Message, "Restore Point", MessageBoxButton.OK, MessageBoxImage.Information);
                LastRestoreText.Text = DateTime.Now.ToString("g");
            }
            else
            {
                MessageBox.Show("Creating a system restore point failed. Please enable System Restore in Windows and try again.", "System Restore Disabled", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            CurrentStatusText.Text = "Idle...";
            CreateRestoreButton.IsEnabled = true;
        }
        #endregion
    }
}