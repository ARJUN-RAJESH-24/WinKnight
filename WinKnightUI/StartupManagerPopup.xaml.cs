using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq; // Added for safety in future updates

namespace WinKnightUI
{
    public partial class StartupManagerPopup : Window
    {
        private WinKnightCore _winKnightCore;

        public StartupManagerPopup(List<StartupProgram> programs, WinKnightCore core)
        {
            InitializeComponent();
            _winKnightCore = core;
            StartupList.ItemsSource = programs;
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
            this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Toggle_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is string programName)
            {
                // Find the program in the list to get its current enabled state
                var selectedProgram = StartupList.Items.Cast<StartupProgram>().FirstOrDefault(p => p.Name == programName);
                if (selectedProgram != null)
                {
                    bool newEnabledState = !selectedProgram.IsEnabled;
                    _winKnightCore.ToggleStartupProgram(programName, newEnabledState);

                    // Update the UI to reflect the change
                    selectedProgram.IsEnabled = newEnabledState;

                    // Refresh the ListBox to show the updated button text/color
                    StartupList.Items.Refresh();

                    MessageBox.Show($"'{programName}' has been toggled to {(newEnabledState ? "Enabled" : "Disabled")}.", "Startup Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}