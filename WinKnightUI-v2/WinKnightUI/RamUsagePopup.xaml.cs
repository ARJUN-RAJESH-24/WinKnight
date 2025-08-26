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

        public RamUsagePopup(List<ProcessInfo> processes, WinKnightCore core)
        {
            InitializeComponent();
            _winKnightCore = core;
            ProcessList.ItemsSource = processes.OrderByDescending(p => p.MemoryUsageBytes);

            // Start a timer to refresh the process list every second
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(1);
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();
        }

        private async void RefreshTimer_Tick(object sender, System.EventArgs e)
        {
            // Refresh the list of processes and their memory usage
            var updatedProcesses = await _winKnightCore.GetTopRamProcess();
            ProcessList.ItemsSource = updatedProcesses.OrderByDescending(p => p.MemoryUsageBytes);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _refreshTimer.Stop();
            this.Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaxRestore_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private async void CloseSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedProcesses = ProcessList.Items.Cast<ProcessInfo>().Where(p => p.IsChecked).ToList();
            if (!selectedProcesses.Any())
            {
                MessageBox.Show("No processes selected.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var processNamesToClose = selectedProcesses.Select(p => p.Name).ToList();
            _winKnightCore.CloseProcesses(processNamesToClose);

            var updatedProcesses = await _winKnightCore.GetTopRamProcess();
            ProcessList.ItemsSource = updatedProcesses.OrderByDescending(p => p.MemoryUsageBytes);

            MessageBox.Show($"{processNamesToClose.Count} apps have been closed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}