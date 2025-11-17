using System;
using System.Windows;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;

namespace WinKnightUI
{
    public partial class SettingsPopup : Window
    {
        private WinKnightCore _core;
        private bool _settingsChanged = false;

        public string BuildDate => File.GetCreationTime(System.Reflection.Assembly.GetExecutingAssembly().Location).ToString("yyyy-MM-dd HH:mm:ss");

        public SettingsPopup(WinKnightCore core)
        {
            InitializeComponent();
            _core = core;
            LoadSettings();
            DataContext = this;
        }

        private void LoadSettings()
        {
            // Load current settings from registry or config file
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WinKnight\Settings"))
                {
                    if (key != null)
                    {
                        DisableWindowsUpdateToggle.IsChecked = Convert.ToBoolean(key.GetValue("DisableWindowsUpdate", false));
                        RunOnStartupToggle.IsChecked = Convert.ToBoolean(key.GetValue("RunOnStartup", false));
                        RunAsAdminToggle.IsChecked = Convert.ToBoolean(key.GetValue("RunAsAdmin", false));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Set version info
            VersionText.Text = GetAppVersion();
        }

        private string GetAppVersion()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
            catch
            {
                return "1.0.0";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsChanged)
            {
                var result = MessageBox.Show("You have unsaved changes. Are you sure you want to close?", "Unsaved Changes", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                    return;
            }
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsChanged)
            {
                var result = MessageBox.Show("You have unsaved changes. Are you sure you want to cancel?", "Unsaved Changes", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                    return;
            }
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveSettings();
                ApplySettings();
                _settingsChanged = false;
                MessageBox.Show("Settings saved successfully!", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSettings()
        {
            using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\WinKnight\Settings"))
            {
                key.SetValue("DisableWindowsUpdate", DisableWindowsUpdateToggle.IsChecked ?? false);
                key.SetValue("RunOnStartup", RunOnStartupToggle.IsChecked ?? false);
                key.SetValue("RunAsAdmin", RunAsAdminToggle.IsChecked ?? false);
            }
        }

        private void ApplySettings()
        {
            // Apply Windows Update setting
            if (DisableWindowsUpdateToggle.IsChecked == true)
            {
                DisableWindowsUpdates();
            }
            else
            {
                EnableWindowsUpdates();
            }

            // Apply startup setting
            if (RunOnStartupToggle.IsChecked == true)
            {
                AddToStartup();
            }
            else
            {
                RemoveFromStartup();
            }

            // Note: Run as admin setting will be applied on next startup
        }

        private void DisableWindowsUpdates()
        {
            try
            {
                // Method 1: Stop Windows Update service
                StopService("wuauserv");

                // Method 2: Disable via registry
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"))
                {
                    key.SetValue("NoAutoUpdate", 1, RegistryValueKind.DWord);
                }

                // Method 3: Disable via Group Policy (if available)
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update"))
                {
                    key.SetValue("AUOptions", 1, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not disable Windows Updates: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EnableWindowsUpdates()
        {
            try
            {
                // Method 1: Start Windows Update service
                StartService("wuauserv");

                // Method 2: Enable via registry
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"))
                {
                    key.DeleteValue("NoAutoUpdate", false);
                }

                // Method 3: Enable via Group Policy (if available)
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update"))
                {
                    key.DeleteValue("AUOptions", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not enable Windows Updates: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AddToStartup()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    key.SetValue("WinKnight", $"\"{appPath}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not add to startup: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveFromStartup()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    key.DeleteValue("WinKnight", false);
                }
            }
            catch (Exception ex)
            {
                // Ignore if key doesn't exist
            }
        }

        private void StopService(string serviceName)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "net",
                        Arguments = $"stop {serviceName}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
            }
            catch { /* Ignore errors */ }
        }

        private void StartService(string serviceName)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "net",
                        Arguments = $"start {serviceName}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
            }
            catch { /* Ignore errors */ }
        }

        private void DeleteLogsButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("This will permanently delete all log files. This action cannot be undone.\n\nAre you sure you want to continue?", 
                "Delete Log Files", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    DeleteLogFiles();
                    MessageBox.Show("All log files have been deleted successfully.", "Logs Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting log files: {ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RevertChangesButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("This will revert all system changes made by WinKnight, including:\n\n• Windows Update settings\n• Registry modifications\n• Startup entries\n\nAre you sure you want to continue?", 
                "Revert All Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    RevertAllChanges();
                    MessageBox.Show("All changes have been reverted successfully.", "Changes Reverted", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reverting changes: {ex.Message}", "Revert Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteLogFiles()
        {
            // Delete logs from common locations
            string[] logPaths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WinKnight", "Logs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinKnight", "Logs"),
                Path.GetTempPath()
            };

            foreach (string logPath in logPaths)
            {
                try
                {
                    if (Directory.Exists(logPath))
                    {
                        Directory.Delete(logPath, true);
                    }
                }
                catch { /* Continue with next location */ }
            }
        }

        private void RevertAllChanges()
        {
            // Revert Windows Update settings
            EnableWindowsUpdates();

            // Remove from startup
            RemoveFromStartup();

            // Delete settings registry key
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"SOFTWARE\WinKnight", false);
            }
            catch { /* Ignore if key doesn't exist */ }

            // Reset toggles
            DisableWindowsUpdateToggle.IsChecked = false;
            RunOnStartupToggle.IsChecked = false;
            RunAsAdminToggle.IsChecked = false;

            _settingsChanged = true;
        }

        // Event handlers for toggle changes
        private void DisableWindowsUpdateToggle_Checked(object sender, RoutedEventArgs e) => _settingsChanged = true;
        private void DisableWindowsUpdateToggle_Unchecked(object sender, RoutedEventArgs e) => _settingsChanged = true;
        private void RunOnStartupToggle_Checked(object sender, RoutedEventArgs e) => _settingsChanged = true;
        private void RunOnStartupToggle_Unchecked(object sender, RoutedEventArgs e) => _settingsChanged = true;
        private void RunAsAdminToggle_Checked(object sender, RoutedEventArgs e) => _settingsChanged = true;
        private void RunAsAdminToggle_Unchecked(object sender, RoutedEventArgs e) => _settingsChanged = true;

        // Enable dragging the window
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}