using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WinKnightUI
{
    public partial class BSODAnalysisWindow : Window
    {
        public class CrashInfo
        {
            public string TimeAgo { get; set; } = "";
            public string FullDate { get; set; } = "";
            public string StopCode { get; set; } = "";
            public string Description { get; set; } = "";
            public DateTime CrashTime { get; set; }
        }

        public BSODAnalysisWindow()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (sender, e) =>
            {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            };

            LoadCrashData();
        }

        private void LoadCrashData()
        {
            try
            {
                string query = "*[System[(EventID=1001 or EventID=41 or EventID=6008)]]";
                EventLogQuery eventsQuery = new EventLogQuery("System", PathType.LogName, query);
                EventLogReader logReader = new EventLogReader(eventsQuery);

                List<CrashInfo> crashes = new List<CrashInfo>();
                EventRecord? eventInstance;

                while ((eventInstance = logReader.ReadEvent()) != null)
                {
                    if (eventInstance.TimeCreated.HasValue &&
                        (DateTime.Now - eventInstance.TimeCreated.Value).TotalDays <= 30)
                    {
                        crashes.Add(new CrashInfo
                        {
                            CrashTime = eventInstance.TimeCreated.Value,
                            TimeAgo = FormatTimeAgo(DateTime.Now - eventInstance.TimeCreated.Value),
                            FullDate = eventInstance.TimeCreated.Value.ToString("dddd, MMMM dd, yyyy h:mm tt"),
                            StopCode = ExtractStopCode(eventInstance),
                            Description = GetEventDescription(eventInstance)
                        });
                    }
                }

                crashes = crashes.OrderByDescending(c => c.CrashTime).ToList();

                TotalCrashesText.Text = crashes.Count.ToString();
                int recentCrashes = crashes.Count(c => (DateTime.Now - c.CrashTime).TotalDays <= 7);
                RecentCrashesText.Text = recentCrashes.ToString();

                if (crashes.Count > 0)
                {
                    LastCrashTimeText.Text = crashes[0].TimeAgo;
                    CrashHistoryList.ItemsSource = crashes;
                    NoCrashesText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    LastCrashTimeText.Text = "Never";
                    CrashHistoryList.Visibility = Visibility.Collapsed;
                    NoCrashesText.Visibility = Visibility.Visible;
                }

                GenerateRecommendations(crashes);

                ActivityLogger.Log($"BSOD Analysis: Loaded {crashes.Count} crash records");
            }
            catch (UnauthorizedAccessException)
            {
                ShowError("Unable to read Event Logs. Please run WinKnight as Administrator.");
            }
            catch (Exception ex)
            {
                ShowError($"Error loading crash data: {ex.Message}");
            }
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

                return eventRecord.Id switch
                {
                    41 => "KERNEL_POWER_FAILURE",
                    6008 => "UNEXPECTED_SHUTDOWN",
                    1001 => "SYSTEM_SERVICE_EXCEPTION",
                    _ => "CRITICAL_SYSTEM_ERROR"
                };
            }
            catch
            {
                return "UNKNOWN_ERROR";
            }
        }

        private string GetEventDescription(EventRecord eventRecord)
        {
            return eventRecord.Id switch
            {
                41 => "The system rebooted without cleanly shutting down. This may indicate a power failure or system crash.",
                6008 => "The previous system shutdown was unexpected, possibly due to a crash or power loss.",
                1001 => "A system service crashed or encountered a critical error.",
                _ => "A critical system error occurred causing the system to restart."
            };
        }

        private void GenerateRecommendations(List<CrashInfo> crashes)
        {
            RecommendationsPanel.Children.Clear();

            if (crashes.Count == 0)
            {
                AddRecommendation("✓", "Your system is stable with no recent crashes.", "#2ECC71");
                AddRecommendation("💡", "Keep Windows and drivers updated to maintain stability.", "#3498DB");
                return;
            }

            if (crashes.Count >= 5)
            {
                AddRecommendation("⚠️", "Frequent crashes detected. Consider running hardware diagnostics.", "#E74C3C");
            }

            var recentCrashes = crashes.Count(c => (DateTime.Now - c.CrashTime).TotalDays <= 7);
            if (recentCrashes >= 2)
            {
                AddRecommendation("🔧", "Multiple recent crashes. Update your graphics and chipset drivers.", "#F1C40F");
            }

            var powerFailures = crashes.Count(c => c.StopCode.Contains("KERNEL_POWER"));
            if (powerFailures >= 2)
            {
                AddRecommendation("⚡", "Power-related crashes detected. Check your PSU and power settings.", "#F1C40F");
            }

            AddRecommendation("💾", "Create a system restore point before making system changes.", "#3498DB");
            AddRecommendation("🔍", "Run Windows Memory Diagnostic to check for RAM issues.", "#3498DB");
            AddRecommendation("📋", "Check Windows Event Viewer for more detailed error logs.", "#3498DB");
        }

        private void AddRecommendation(string icon, string text, string color)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2538")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 16,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var messageText = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };

            stackPanel.Children.Add(iconText);
            stackPanel.Children.Add(messageText);
            border.Child = stackPanel;

            RecommendationsPanel.Children.Add(border);
        }

        private string FormatTimeAgo(TimeSpan time)
        {
            if (time.TotalDays >= 1)
                return $"{(int)time.TotalDays} day{((int)time.TotalDays > 1 ? "s" : "")} ago";
            else if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours} hour{((int)time.TotalHours > 1 ? "s" : "")} ago";
            else if (time.TotalMinutes >= 1)
                return $"{(int)time.TotalMinutes} minute{((int)time.TotalMinutes > 1 ? "s" : "")} ago";
            else
                return "Just now";
        }

        private void ShowError(string message)
        {
            TotalCrashesText.Text = "Error";
            RecentCrashesText.Text = "N/A";
            LastCrashTimeText.Text = "Unable to load";

            var errorText = new TextBlock
            {
                Text = $"❌ {message}",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")),
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0)
            };
        }

        // ✅ MISSING HANDLER ADDED
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
