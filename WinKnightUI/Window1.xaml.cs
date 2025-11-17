using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WinKnightUI.Services;

namespace WinKnightUI
{
    public partial class RestorePointWindow : Window
    {
        private const int MaxRestorePoints = 5;

        public RestorePointWindow()
        {
            InitializeComponent();
            _ = LoadRestorePointsAsync();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private async Task LoadRestorePointsAsync()
        {
            try
            {
                StatusText.Text = "Loading restore points...";
                RestoreListView.ItemsSource = null;

                var list = await Task.Run(() => RestoreGuardManager.ListRestorePoints());
                RestoreListView.ItemsSource = list ?? new List<RestorePointModel>();
                StatusText.Text = $"Loaded {(list?.Count ?? 0)} restore points.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Failed to load restore points.";
                MessageBox.Show($"Failed to load restore points:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CreateRestorePoint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CreateButton.IsEnabled = false;
                CreateButton.Content = "Creating...";

                string description = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter a name for the restore point:", "Manual Restore Point", "Manual RP");

                if (string.IsNullOrWhiteSpace(description)) description = "Manual RP";

                await Task.Run(() => RestoreGuardManager.CreateRestorePoint(description, 12, 100));

                await LoadRestorePointsAsync();
                MessageBox.Show("Restore point created.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                CreateButton.IsEnabled = true;
                CreateButton.Content = "Create Restore Point";
            }
        }

        private async void DeleteRestorePoint_Click(object sender, RoutedEventArgs e)
        {
            if (RestoreListView.SelectedItem is not RestorePointModel selected)
            {
                MessageBox.Show("Select a restore point first.", "No selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (selected.SourceType == "Automatic")
                    await Task.Run(() => RestoreGuardManager.DeleteAllRestorePoints());
                else
                    MessageBox.Show("Manual restore point deletion is not supported programmatically.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadRestorePointsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
