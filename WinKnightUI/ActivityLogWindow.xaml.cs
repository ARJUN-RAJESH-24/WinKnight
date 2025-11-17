using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace WinKnightUI   // ✔ FIXED NAMESPACE
{
    public partial class ActivityLogWindow : Window
    {
        // Folder where logs are stored
        private readonly string reportsFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WinKnightReports");

        private const int MaxEntries = 5;

        public ActivityLogWindow()
        {
            InitializeComponent();
            LoadActivityLog();
        }

        private void LoadActivityLog()
        {
            // ✔ Read last 5 logs from ActivityLogger
            List<string> logs = ActivityLogger.ReadLastEntries(MaxEntries);

            if (logs == null || logs.Count == 0)
            {
                ActivityList.ItemsSource = new List<string>
                {
                    "No activity logged yet."
                };
                return;
            }

            // ✔ Show latest first
            ActivityList.ItemsSource = logs.Reverse<string>().ToList();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadActivityLog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(reportsFolder))
                    Directory.CreateDirectory(reportsFolder);

                Process.Start("explorer.exe", reportsFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
