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

            _graphTimer = new DispatcherTimer();
            _graphTimer.Interval = TimeSpan.FromSeconds(1);
            _graphTimer.Tick += GraphTimer_Tick;
            _graphTimer.Start();
        }

        private async void GraphTimer_Tick(object? sender, EventArgs e) // Corrected sender parameter
        {
            var cpuTemp = await _winKnightCore.GetCpuTemperature();
            var gpuTemp = await _winKnightCore.GetGpuTemperature();
            var systemTemp = await _winKnightCore.GetSystemTemperature();

            UpdateDataPoints(cpuTemps, cpuTemp);
            UpdateDataPoints(gpuTemps, gpuTemp);
            UpdateDataPoints(systemTemps, systemTemp);

            DrawGraph();
        }

        private void UpdateDataPoints(List<double> data, double newDataPoint)
        {
            data.Add(newDataPoint);
            if (data.Count > MaxDataPoints)
            {
                data.RemoveAt(0);
            }
        }

        private void DrawGraph()
        {
            GraphCanvas.Children.Clear();
            if (!cpuTemps.Any()) return;

            DrawLine(cpuTemps, Brushes.Red);
            DrawLine(gpuTemps, Brushes.Blue);
            DrawLine(systemTemps, Brushes.Green);
        }

        private void DrawLine(List<double> data, SolidColorBrush color)
        {
            var polyline = new Polyline
            {
                Stroke = color,
                StrokeThickness = 2
            };

            double maxTemp = Math.Max(cpuTemps.Any() ? cpuTemps.Max() : 0,
                                      Math.Max(gpuTemps.Any() ? gpuTemps.Max() : 0,
                                               systemTemps.Any() ? systemTemps.Max() : 0));
            double minTemp = Math.Min(cpuTemps.Any() ? cpuTemps.Min() : 100,
                                      Math.Min(gpuTemps.Any() ? gpuTemps.Min() : 100,
                                               systemTemps.Any() ? systemTemps.Min() : 100));
            double range = maxTemp - minTemp;
            if (range == 0) range = 1;

            for (int i = 0; i < data.Count; i++)
            {
                double x = (double)i / (MaxDataPoints - 1) * GraphCanvas.ActualWidth;
                double y = (1 - (data[i] - minTemp) / range) * GraphCanvas.ActualHeight;
                polyline.Points.Add(new Point(x, y));
            }

            GraphCanvas.Children.Add(polyline);
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
    }
}