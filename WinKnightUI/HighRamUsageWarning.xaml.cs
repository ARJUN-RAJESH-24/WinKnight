using System.Windows;
using System.Windows.Input;

namespace WinKnightUI
{
    public partial class HighRamUsageWarning : Window
    {
        private string _processName;
        private WinKnightCore _winKnightCore;

        public HighRamUsageWarning(string processName, WinKnightCore core)
        {
            InitializeComponent();
            _processName = processName;
            _winKnightCore = core;
            WarningMessageText.Text = $"The app '{_processName}' is consuming a large amount of RAM. Do you want to close it to improve system performance?";
        }

        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            _winKnightCore.CloseProcess(_processName);
            MessageBox.Show($"The app '{_processName}' has been closed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        private void Dismiss_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // This is the new method to allow the window to be dragged.
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}