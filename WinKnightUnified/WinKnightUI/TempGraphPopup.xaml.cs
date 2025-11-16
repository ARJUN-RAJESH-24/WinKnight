using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WinKnightUI
{
    public partial class TempGraphPopup : Window
    {
        private WinKnightCore _winKnightCore;
        private DispatcherTimer _graphTimer;
        private List<double> cpuTemps = new List<double>();
        private List<double> gpuTemps = new List<double>();
        private List<double> systemTemps = new List<double>();
        private const int MaxDataPoints = 60;

        public TempGraphPopup(WinKnightCore core)
        {
            InitializeComponent();
            _winKnightCore = core;

            this.Loaded += TempGraphPopup_Loaded;

            _graphTimer = new DispatcherTimer();
            _graphTimer.Interval = TimeSpan.FromSeconds(1);
            _graphTimer.Tick += GraphTimer_Tick;
            _graphTimer.Start();
        }

        private void TempGraphPopup_Loaded(object sender, RoutedEventArgs e)
        {
            GraphCanvas.SizeChanged += (s, args) => DrawGraph();
            DrawGridLines();
        }

        private async void GraphTimer_Tick(object? sender, EventArgs e)
        {
            var cpuTemp = await _winKnightCore.GetCpuTemperature();
            var gpuTemp = await _winKnightCore.GetGpuTemperature();
            var systemTemp = await _winKnightCore.GetSystemTemperature();

            UpdateDataPoints(cpuTemps, cpuTemp);
            UpdateDataPoints(gpuTemps, gpuTemp);
            UpdateDataPoints(systemTemps, systemTemp);

            UpdateCurrentTemperatures();
            DrawGraph();
            UpdateTimestamp();
        }

        private void UpdateDataPoints(List<double> data, double newDataPoint)
        {
            if (newDataPoint > -1) // Only add valid temperatures
            {
                data.Add(newDataPoint);
                if (data.Count > MaxDataPoints)
                {
                    data.RemoveAt(0);
                }
            }
        }

        private void UpdateCurrentTemperatures()
        {
            // CPU
            if (cpuTemps.Any())
            {
                CpuCurrentTemp.Text = $"{cpuTemps.Last():F1}°C";
                CpuMinTemp.Text = $"{cpuTemps.Min():F1}°C";
                CpuMaxTemp.Text = $"{cpuTemps.Max():F1}°C";
            }

            // GPU
            if (gpuTemps.Any())
            {
                GpuCurrentTemp.Text = $"{gpuTemps.Last():F1}°C";
                GpuMinTemp.Text = $"{gpuTemps.Min():F1}°C";
                GpuMaxTemp.Text = $"{gpuTemps.Max():F1}°C";
            }

            // System
            if (systemTemps.Any())
            {
                SystemCurrentTemp.Text = $"{systemTemps.Last():F1}°C";
                SystemMinTemp.Text = $"{systemTemps.Min():F1}°C";
                SystemMaxTemp.Text = $"{systemTemps.Max():F1}°C";
            }
        }

        private void UpdateTimestamp()
        {
            UpdateTimeText.Text = $"Updated: {DateTime.Now:HH:mm:ss}";
        }

        private void DrawGridLines()
        {
            if (GraphCanvas.ActualWidth == 0 || GraphCanvas.ActualHeight == 0) return;

            GridCanvas.Children.Clear();

            // Horizontal grid lines
            for (int i = 0; i <= 5; i++)
            {
                double y = (GraphCanvas.ActualHeight / 5) * i;
                var line = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = GraphCanvas.ActualWidth,
                    Y2 = y,
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A5F")),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 5, 5 }
                };
                GridCanvas.Children.Add(line);
            }

            // Vertical grid lines
            for (int i = 0; i <= 6; i++)
            {
                double x = (GraphCanvas.ActualWidth / 6) * i;
                var line = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = GraphCanvas.ActualHeight,
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A5F")),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 5, 5 }
                };
                GridCanvas.Children.Add(line);
            }
        }

        private void DrawGraph()
        {
            if (GraphCanvas.ActualWidth == 0 || GraphCanvas.ActualHeight == 0) return;

            GraphCanvas.Children.Clear();
            DrawGridLines();

            if (!cpuTemps.Any() && !gpuTemps.Any() && !systemTemps.Any()) return;

            // Draw temperature zone backgrounds
            DrawTemperatureZones();

            // Draw lines
            if (cpuTemps.Any()) DrawLine(cpuTemps, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")), 3);
            if (gpuTemps.Any()) DrawLine(gpuTemps, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB")), 3);
            if (systemTemps.Any()) DrawLine(systemTemps, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")), 3);
        }

        private void DrawTemperatureZones()
        {
            double canvasHeight = GraphCanvas.ActualHeight;
            double canvasWidth = GraphCanvas.ActualWidth;

            // Assuming max temp of 100°C for visualization
            double maxTemp = 100.0;
            double normalZoneHeight = (60.0 / maxTemp) * canvasHeight;
            double warmZoneHeight = (20.0 / maxTemp) * canvasHeight;

            // Critical zone (80-100°C) - Top
            var criticalZone = new Rectangle
            {
                Width = canvasWidth,
                Height = canvasHeight - normalZoneHeight - warmZoneHeight,
                Fill = new SolidColorBrush(Color.FromArgb(20, 231, 76, 60))
            };
            Canvas.SetTop(criticalZone, 0);
            GraphCanvas.Children.Add(criticalZone);

            // Warm zone (60-80°C) - Middle
            var warmZone = new Rectangle
            {
                Width = canvasWidth,
                Height = warmZoneHeight,
                Fill = new SolidColorBrush(Color.FromArgb(20, 241, 196, 15))
            };
            Canvas.SetTop(warmZone, canvasHeight - normalZoneHeight - warmZoneHeight);
            GraphCanvas.Children.Add(warmZone);

            // Normal zone (0-60°C) - Bottom
            var normalZone = new Rectangle
            {
                Width = canvasWidth,
                Height = normalZoneHeight,
                Fill = new SolidColorBrush(Color.FromArgb(20, 46, 204, 113))
            };
            Canvas.SetTop(normalZone, canvasHeight - normalZoneHeight);
            GraphCanvas.Children.Add(normalZone);
        }

        private void DrawLine(List<double> data, SolidColorBrush color, double thickness)
        {
            if (!data.Any()) return;

            var polyline = new Polyline
            {
                Stroke = color,
                StrokeThickness = thickness,
                StrokeLineJoin = PenLineJoin.Round
            };

            // Calculate temperature range for all data
            double maxTemp = Math.Max(
                cpuTemps.Any() ? cpuTemps.Max() : 0,
                Math.Max(
                    gpuTemps.Any() ? gpuTemps.Max() : 0,
                    systemTemps.Any() ? systemTemps.Max() : 0
                )
            );

            double minTemp = Math.Min(
                cpuTemps.Any() ? cpuTemps.Min() : 100,
                Math.Min(
                    gpuTemps.Any() ? gpuTemps.Min() : 100,
                    systemTemps.Any() ? systemTemps.Min() : 100
                )
            );

            // Add padding to the range
            double range = maxTemp - minTemp;
            if (range < 10) range = 10; // Minimum range for better visualization

            double paddedMin = minTemp - (range * 0.1);
            double paddedMax = maxTemp + (range * 0.1);
            double paddedRange = paddedMax - paddedMin;

            for (int i = 0; i < data.Count; i++)
            {
                double x = (double)i / (MaxDataPoints - 1) * GraphCanvas.ActualWidth;
                double normalizedValue = (data[i] - paddedMin) / paddedRange;
                double y = (1 - normalizedValue) * GraphCanvas.ActualHeight;

                polyline.Points.Add(new Point(x, y));
            }

            GraphCanvas.Children.Add(polyline);

            // Draw dots at data points for better visibility
            if (data.Count <= 10)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    double x = (double)i / (MaxDataPoints - 1) * GraphCanvas.ActualWidth;
                    double normalizedValue = (data[i] - paddedMin) / paddedRange;
                    double y = (1 - normalizedValue) * GraphCanvas.ActualHeight;

                    var dot = new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = color,
                        Stroke = Brushes.White,
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(dot, x - 3);
                    Canvas.SetTop(dot, y - 3);
                    GraphCanvas.Children.Add(dot);
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            // Clear all data and restart
            cpuTemps.Clear();
            gpuTemps.Clear();
            systemTemps.Clear();
            GraphCanvas.Children.Clear();
            DrawGridLines();
            UpdateTimeText.Text = "Refreshed - collecting data...";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _graphTimer.Stop();
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

        protected override void OnClosed(EventArgs e)
        {
            _graphTimer?.Stop();
            base.OnClosed(e);
        }
    }
}