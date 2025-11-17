using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace WinKnightUI
{
    public partial class RamUsagePopup : Window
    {
        private WinKnightCore _winKnightCore;
        private DispatcherTimer _refreshTimer;
        private List<ProcessInfo> _allProcesses;
        private double _totalRamGB = 16.0; // Will be updated from system

        public RamUsagePopup(List<ProcessInfo> processes, WinKnightCore core)
        {
            InitializeComponent();
            _winKnightCore = core;
            _allProcesses = processes ?? new List<ProcessInfo>();

            // Initialize RAM stats
            InitializeRamStats();

            // Display processes
            UpdateProcessList(processes);

            // Start auto-refresh timer
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(2);
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            // Update timestamp
            UpdateStatus();
        }

        private void InitializeRamStats()
        {
            // TODO: Get actual total RAM from WinKnightCore
            // For now using placeholder
            _totalRamGB = 16.0;
            TotalRamText.Text = $"{_totalRamGB:F1} GB";
            TotalRamPercentText.Text = "100%";
        }

        private void UpdateRamStats()
        {
            if (_allProcesses == null || !_allProcesses.Any()) return;

            // Calculate used RAM
            double usedRamMB = _allProcesses.Sum(p => p.MemoryUsageMb);
            double usedRamGB = usedRamMB / 1024.0;
            double usagePercent = (usedRamGB / _totalRamGB) * 100;

            UsedRamText.Text = $"{usedRamGB:F1} GB";
            RamUsageBar.Value = usagePercent;

            // Update color based on usage
            if (usagePercent > 85)
            {
                UsedRamText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(231, 76, 60)); // Red
            }
            else if (usagePercent > 70)
            {
                UsedRamText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(241, 196, 15)); // Yellow
            }
            else
            {
                UsedRamText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(46, 204, 113)); // Green
            }

            // Update process count
            ProcessCountText.Text = _allProcesses.Count.ToString();

            // Update selected count
            int selectedCount = _allProcesses.Count(p => p.IsChecked);
            SelectedCountText.Text = selectedCount > 0 ? $"{selectedCount} selected" : "0 selected";
            EndTaskButton.IsEnabled = selectedCount > 0;
        }

        private void UpdateProcessList(List<ProcessInfo>? processes)
        {
            if (processes == null)
            {
                _allProcesses = new List<ProcessInfo>();
                ProcessList.ItemsSource = _allProcesses;
                return;
            }

            _allProcesses = processes.OrderByDescending(p => p.MemoryUsageBytes).ToList();

            ProcessList.ItemsSource = null;
            ProcessList.ItemsSource = _allProcesses;

            UpdateRamStats();
        }

        private async void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                var updatedProcesses = await _winKnightCore.GetTopRamProcess();

                // Preserve checkbox states
                if (updatedProcesses != null)
                {
                    foreach (var updated in updatedProcesses)
                    {
                        var existing = _allProcesses.FirstOrDefault(p => p.Name == updated.Name);
                        if (existing != null)
                        {
                            updated.IsChecked = existing.IsChecked;
                        }
                    }
                }

                UpdateProcessList(updatedProcesses);
                UpdateStatus();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        private void UpdateStatus(string? customMessage = null)
        {
            if (customMessage != null)
            {
                StatusTextBlock.Text = customMessage;
            }
            else
            {
                StatusTextBlock.Text = $"Last updated: {DateTime.Now:HH:mm:ss}";
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_allProcesses == null) return;

            string searchText = SearchBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                ProcessList.ItemsSource = _allProcesses;
            }
            else
            {
                var filtered = _allProcesses.Where(p =>
                    p.Name.ToLower().Contains(searchText)
                ).ToList();

                ProcessList.ItemsSource = filtered;
            }

            UpdateRamStats();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (_allProcesses == null) return;

            bool allSelected = _allProcesses.All(p => p.IsChecked);

            foreach (var process in _allProcesses)
            {
                process.IsChecked = !allSelected;
            }

            ProcessList.Items.Refresh();
            UpdateRamStats();

            SelectAllButton.Content = allSelected ? "☑ Select All" : "☐ Deselect All";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshTimer_Tick(null, EventArgs.Empty);
            UpdateStatus("Refreshed manually");
        }

        private async void CloseSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedProcesses = _allProcesses.Where(p => p.IsChecked).ToList();

            if (!selectedProcesses.Any())
            {
                MessageBox.Show("No processes selected.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to end {selectedProcesses.Count} process(es)?\n\nThis may cause data loss if applications have unsaved work.",
                "Confirm End Tasks",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var processNamesToClose = selectedProcesses.Select(p => p.Name).ToList();
                _winKnightCore.CloseProcesses(processNamesToClose);

                // Wait a moment for processes to close
                await System.Threading.Tasks.Task.Delay(500);

                // Refresh the list
                var updatedProcesses = await _winKnightCore.GetTopRamProcess();
                UpdateProcessList(updatedProcesses);

                MessageBox.Show(
                    $"Successfully ended {selectedProcesses.Count} process(es).",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                UpdateStatus($"Ended {selectedProcesses.Count} tasks");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error ending processes: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _refreshTimer?.Stop();
            this.Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaxRestore_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = (this.WindowState == WindowState.Maximized)
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _refreshTimer?.Stop();
            base.OnClosed(e);
        }
    }
}