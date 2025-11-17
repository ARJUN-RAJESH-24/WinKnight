using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace WinKnightUI
{
    public partial class DiskMonitorPopup : Window
    {
        private WinKnightCore _winKnightCore;

        // --- THIS IS THE FIX ---
        // Changed "DriveInfo" back to your original class "DiskInfo"
        public DiskMonitorPopup(List<DiskInfo> drives, WinKnightCore core)
        {
            InitializeComponent();
            _winKnightCore = core;

            // Initial data load
            DriveList.ItemsSource = drives;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
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
    }
}