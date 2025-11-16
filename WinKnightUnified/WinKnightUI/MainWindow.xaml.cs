using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

namespace WinKnightUI
{
    public partial class MainWindow : Window
    {
        private WinKnightCore _winKnightCore;
        private DispatcherTimer? _metricsTimer;
        private DispatcherTimer? _uptimeTimer;
        private bool _isTempWarningActive = false;
        private bool _isRamWarningActive = false;
        private bool _isDarkMode = true;

        private double _warningTempAtTime = 0.0;
        private int _warningRamAtTime = 0;
        private DateTime _systemStartTime;

        // BSOD data - now from actual Event Logs
        private int _totalCrashes = 0;
        private DateTime? _lastCrashDate = null;
        private string _lastStopCode = "No crashes detected";

        public MainWindow()
        {
            InitializeComponent();
            _winKnightCore = new WinKnightCore();
            _systemStartTime = DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount64);

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

            // Initialize with dark theme
            ApplyDarkTheme();

            LoadBSODHistory();
            StartMetricsTimer();
            StartUptimeTimer();
        }

        #region Theme Management
        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _isDarkMode = !_isDarkMode;

            if (_isDarkMode)
            {
                ApplyDarkTheme();
                ThemeIcon.Text = "🌙";
                ActivityLogger.Log("Theme switched to Dark Mode");
            }
            else
            {
                ApplyLightTheme();
                ThemeIcon.Text = "☀️";
                ActivityLogger.Log("Theme switched to Light Mode");
            }
        }

        private void ApplyDarkTheme()
        {
            // Main backgrounds
            UpdateBrush("BackgroundBrush", "#0B1A31");
            UpdateBrush("SidebarBrush", "#0A1828");
            UpdateBrush("CardBackgroundBrush", "#182A41");
            UpdateBrush("SubCardBrush", "#1A2538");
            UpdateBrush("StatusBadgeBrush", "#1E3A5F");

            // Hover and interaction states
            UpdateBrush("HoverBrush", "#1A2B42");
            UpdateBrush("PressedBrush", "#122030");

            // Fonts
            UpdateBrush("FontPrimary", "#FFFFFF");
            UpdateBrush("FontSecondary", "#8796A6");

            // Accent colors
            UpdateBrush("AccentBlue", "#3498DB");
            UpdateBrush("AccentLightBlue", "#5DADE2");

            // Status colors
            UpdateBrush("SuccessBrush", "#2ECC71");
            UpdateBrush("WarningBrush", "#F1C40F");
            UpdateBrush("ErrorBrush", "#E74C3C");
        }

        private void ApplyLightTheme()
        {
            // Main backgrounds - Light mode colors
            UpdateBrush("BackgroundBrush", "#F5F7FA");
            UpdateBrush("SidebarBrush", "#FFFFFF");
            UpdateBrush("CardBackgroundBrush", "#FFFFFF");
            UpdateBrush("SubCardBrush", "#F0F3F7");
            UpdateBrush("StatusBadgeBrush", "#E8EDF2");

            // Hover and interaction states
            UpdateBrush("HoverBrush", "#E8EDF2");
            UpdateBrush("PressedBrush", "#D6DDE5");

            // Fonts - Dark text for light mode
            UpdateBrush("FontPrimary", "#1A2332");
            UpdateBrush("FontSecondary", "#5E6C7F");

            // Accent colors - Slightly adjusted for better visibility
            UpdateBrush("AccentBlue", "#2980B9");
            UpdateBrush("AccentLightBlue", "#3498DB");

            // Status colors - Adjusted for light mode
            UpdateBrush("SuccessBrush", "#27AE60");
            UpdateBrush("WarningBrush", "#F39C12");
            UpdateBrush("ErrorBrush", "#C0392B");
        }

        private void UpdateBrush(string resourceKey, string hexColor)
        {
            // Create a new SolidColorBrush instead of modifying the frozen one
            var newBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor));
            Resources[resourceKey] = newBrush;
        }
        #endregion

        #region BSOD History Management
        private void LoadBSODHistory()
        {
            try
            {
                // Query Windows Event Log for critical errors and bug checks
                string query = "*[System[(EventID=1001 or EventID=41 or EventID=6008)]]";
                EventLogQuery eventsQuery = new EventLogQuery("System", PathType.LogName, query);
                EventLogReader logReader = new EventLogReader(eventsQuery);

                List<EventRecord> crashes = new List<EventRecord>();
                EventRecord? eventInstance;

                // Read all matching events
                while ((eventInstance = logReader.ReadEvent()) != null)
                {
                    // Only include events from the last 30 days to avoid old data
                    if (eventInstance.TimeCreated.HasValue &&
                        (DateTime.Now - eventInstance.TimeCreated.Value).TotalDays <= 30)
                    {
                        crashes.Add(eventInstance);
                    }
                }

                _totalCrashes = crashes.Count;

                if (_totalCrashes > 0)
                {
                    var lastCrash = crashes.OrderByDescending(e => e.TimeCreated).First();
                    _lastCrashDate = lastCrash.TimeCreated;
                    _lastStopCode = ExtractStopCode(lastCrash);
                }
                else
                {
                    _lastCrashDate = null;
                    _lastStopCode = "No crashes detected";
                }

                ActivityLogger.Log($"BSOD History loaded: {_totalCrashes} crashes found in last 30 days");
            }
            catch (UnauthorizedAccessException)
            {
                _totalCrashes = -1;
                _lastStopCode = "Permission denied";
                ActivityLogger.Log("BSOD History: Unable to read Event Logs (permission denied)");
            }
            catch (Exception ex)
            {
                _totalCrashes = -1;
                _lastStopCode = "Error reading logs";
                ActivityLogger.Log($"BSOD History error: {ex.Message}");
            }

            UpdateBSODDisplay();
        }

        private string ExtractStopCode(EventRecord eventRecord)
        {
            try
            {
                string description = eventRecord.FormatDescription() ?? "";

                if (description.Contains("bug check", StringComparison.OrdinalIgnoreCase) ||
                    description.Contains("stop code", StringComparison.OrdinalIgnoreCase))
                {
                    var words = description.Split(new[] { ' ', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in words)
                    {
                        if (word.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && word.Length >= 10)
                        {
                            return word.ToUpper();
                        }
                    }
                }

                switch (eventRecord.Id)
                {
                    case 41:
                        return "KERNEL_POWER_FAILURE";
                    case 6008:
                        return "UNEXPECTED_SHUTDOWN";
                    case 1001:
                        return "SYSTEM_SERVICE_EXCEPTION";
                    default:
                        return "CRITICAL_SYSTEM_ERROR";
                }
            }
            catch
            {
                return "UNKNOWN_ERROR";
            }
        }

        private void UpdateBSODDisplay()
        {
            if (_totalCrashes == -1)
            {
                LastCrashText.Text = "Unable to read crash logs";
                StopCodeText.Text = _lastStopCode;
                StopCodeText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                TotalCrashesText.Text = "Check permissions";
            }
            else if (_totalCrashes > 0 && _lastCrashDate.HasValue)
            {
                var timeSpan = DateTime.Now - _lastCrashDate.Value;
                string timeAgo;

                if (timeSpan.TotalDays >= 1)
                    timeAgo = $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays != 1 ? "s" : "")} ago";
                else if (timeSpan.TotalHours >= 1)
                    timeAgo = $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours != 1 ? "s" : "")} ago";
                else if (timeSpan.TotalMinutes >= 1)
                    timeAgo = $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes != 1 ? "s" : "")} ago";
                else
                    timeAgo = "just now";

                LastCrashText.Text = $"Last crash: {timeAgo}";
                StopCodeText.Text = _lastStopCode;
                StopCodeText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1C40F"));
                TotalCrashesText.Text = $"Total crashes: {_totalCrashes}";
            }
            else
            {
                LastCrashText.Text = "No recent crashes";
                StopCodeText.Text = "System stable ✓";
                StopCodeText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
                TotalCrashesText.Text = "Total crashes: 0";
            }
        }
        #endregion

        #region Timers
        private void StartUptimeTimer()
        {
            _uptimeTimer = new DispatcherTimer();
            _uptimeTimer.Interval = TimeSpan.FromMinutes(1);
            _uptimeTimer.Tick += UptimeTimer_Tick;
            _uptimeTimer.Start();
            UpdateUptime();
        }

        private void UpdateUptime()
        {
            var uptime = DateTime.Now - _systemStartTime;
            UptimeStatusText.Text = $"Uptime: {(int)uptime.TotalHours}h {uptime.Minutes}m";
        }

        private void UptimeTimer_Tick(object? sender, EventArgs e)
        {
            UpdateUptime();
        }

        private void StartMetricsTimer()
        {
            _metricsTimer = new DispatcherTimer();
            _metricsTimer.Interval = TimeSpan.FromSeconds(1);
            _metricsTimer.Tick += MetricsTimer_Tick;
            _metricsTimer.Start();
        }

        private async void MetricsTimer_Tick(object? sender, EventArgs e)
        {
            // CPU Temperature
            var temp = await _winKnightCore.GetCpuTemperature();
            if (temp > -1)
            {
                CpuTempText.Text = $"{temp}°C";

                if (temp < 60)
                {
                    TempZoneIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
                    TempZoneText.Text = "Normal";
                }
                else if (temp < 80)
                {
                    TempZoneIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1C40F"));
                    TempZoneText.Text = "Warm";
                }
                else
                {
                    TempZoneIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                    TempZoneText.Text = "Critical";
                }

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
                TempZoneIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8796A6"));
                TempZoneText.Text = "Unknown";
            }

            // RAM Usage
            var ramUsage = await _winKnightCore.GetRamUsage();
            RamUsageText.Text = $"{ramUsage}%";

            var totalRam = 16.0;
            var usedRam = (ramUsage / 100.0) * totalRam;
            RamDetailText.Text = $"{usedRam:F1} GB / {totalRam:F1} GB";

            if (ramUsage > 85 && !_isRamWarningActive)
            {
                _isRamWarningActive = true;
                _warningRamAtTime = ramUsage;
            }
            RamWarningButton.Visibility = _isRamWarningActive ? Visibility.Visible : Visibility.Collapsed;

            // Disk Health
            var diskStatus = await _winKnightCore.GetDiskHealthStatus();
            DiskHealthText.Text = diskStatus.Status;
            DiskSpaceText.Text = "512 GB / 1 TB free";

            // Startup Programs
            var startupCount = await _winKnightCore.GetStartupProgramsCount();
            StartupProgramsText.Text = startupCount.ToString();
            StartupImpactText.Text = "2 High Impact";

            UpdatePerformanceStatus(temp, ramUsage);
            UpdateRestorePointsCount();
        }

        private void UpdatePerformanceStatus(double cpuTemp, int ramUsage)
        {
            Color statusColor;
            string statusText;

            if (cpuTemp > 80 || ramUsage > 85)
            {
                statusText = "Performance: Critical";
                statusColor = (Color)ColorConverter.ConvertFromString("#E74C3C");
            }
            else if (cpuTemp > 70 || ramUsage > 70)
            {
                statusText = "Performance: Fair";
                statusColor = (Color)ColorConverter.ConvertFromString("#F1C40F");
            }
            else
            {
                statusText = "Performance: Good";
                statusColor = _isDarkMode ?
                    (Color)ColorConverter.ConvertFromString("#FFFFFF") :
                    (Color)ColorConverter.ConvertFromString("#1A2332");
            }

            PerformanceStatusText.Text = statusText;
            PerformanceStatusText.Foreground = new SolidColorBrush(statusColor);
        }

        private void UpdateRestorePointsCount()
        {
            RestorePointCountText.Text = "3 available";
        }
        #endregion

        #region Window Chrome Handlers
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

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsPopup = new SettingsPopup(_winKnightCore);
            settingsPopup.Owner = this;
            settingsPopup.ShowDialog();
        }

        private void OpenRestorePoints_Click(object sender, RoutedEventArgs e)
        {
            var win = new RestorePointWindow();
            win.Owner = this;
            win.ShowDialog();
        }

        private void BSODHistory_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("BSOD History details will be shown here.\n\nThis feature will display:\n- Complete crash history\n- Stop codes and error analysis\n- Potential causes and solutions\n- Crash dump file locations",
                "BSOD Analysis", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewBSODAnalysis_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Opening detailed BSOD analysis...\n\nThis will analyze:\n- Recent memory dumps\n- Driver conflicts\n- Hardware issues\n- System logs",
                "BSOD Full Analysis", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void RunScan_Click(object sender, RoutedEventArgs e)
        {
            RunScanButton.IsEnabled = false;
            CreateRestoreButton.IsEnabled = false;
            ScanProgressBar.IsIndeterminate = true;
            ReportLogText.Text = string.Empty;
            ScanStatusText.Text = "Initializing scan...";

            var logBuilder = new StringBuilder();

            CurrentStatusText.Text = "Creating system restore point...";
            ScanStatusText.Text = "Step 1/4: Creating restore point...";
            var restoreReport = await _winKnightCore.CreateSystemRestorePoint("WinKnight Automated Scan");
            logBuilder.AppendLine($"[RestoreGuard] {restoreReport.Message}");
            ReportLogText.Text = logBuilder.ToString();
            ActivityLogger.Log($"RestoreGuard: {restoreReport.Message}");

            if (!restoreReport.IsSuccessful)
            {
                MessageBox.Show("Creating a system restore point failed. Please enable System Restore in Windows and try again.", "System Restore Disabled", MessageBoxButton.OK, MessageBoxImage.Warning);
                CurrentStatusText.Text = "Scan failed.";
                ScanStatusText.Text = "Scan failed - Restore point creation error";
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
            ScanStatusText.Text = "Step 2/4: Checking system files...";
            var sfcReport = await _winKnightCore.RunSfcScan();
            logBuilder.AppendLine($"[SelfHeal] {sfcReport.Message}");
            ReportLogText.Text = logBuilder.ToString();
            ActivityLogger.Log($"SelfHeal: {sfcReport.Message}");

            CurrentStatusText.Text = "Running Deployment Image Servicing and Management (DISM)...";
            ScanStatusText.Text = "Step 3/4: Repairing system image...";
            var dismReport = await _winKnightCore.RunDismRepair();
            logBuilder.AppendLine($"[SelfHeal] {dismReport.Message}");
            ReportLogText.Text = logBuilder.ToString();
            ActivityLogger.Log($"SelfHeal: {dismReport.Message}");

            CurrentStatusText.Text = "Clearing temporary files...";
            ScanStatusText.Text = "Step 4/4: Cleaning temporary files...";
            var cacheReport = await _winKnightCore.CleanCache();
            logBuilder.AppendLine($"[CacheCleaner] {cacheReport.Message}");
            ReportLogText.Text = logBuilder.ToString();
            ActivityLogger.Log($"CacheCleaner: {cacheReport.Message}");

            CurrentStatusText.Text = "Scan Complete.";
            ScanStatusText.Text = "✓ Scan completed successfully";
            ScanProgressBar.IsIndeterminate = false;
            RunScanButton.IsEnabled = true;
            CreateRestoreButton.IsEnabled = true;

            ActivityLogger.Log("Full System Scan completed successfully.");
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
                ActivityLogger.Log($"Manual Restore: {report.Message}");
                UpdateRestorePointsCount();
            }
            else
            {
                MessageBox.Show("Creating a system restore point failed. Please enable System Restore in Windows and try again.", "System Restore Disabled", MessageBoxButton.OK, MessageBoxImage.Warning);
                ActivityLogger.Log($"Manual Restore Failed: {report.Message}");
            }

            CurrentStatusText.Text = "Idle...";
            CreateRestoreButton.IsEnabled = true;
        }

        private void ActivityLogCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var win = new ActivityLogWindow();
            win.Owner = this;
            win.ShowDialog();
        }

        private void WindowsDefender_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "windowsdefender:",
                    UseShellExecute = true
                });
            }
            catch (System.ComponentModel.Win32Exception winEx)
            {
                MessageBox.Show($"Could not open Windows Defender. The app might not be available.\n\nError: {winEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _metricsTimer?.Stop();
            _uptimeTimer?.Stop();
            base.OnClosed(e);
        }
        #endregion
    }
}