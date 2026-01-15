using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;

namespace WinKnightUI.Services
{
    /// <summary>
    /// Service for monitoring hardware sensors using LibreHardwareMonitor.
    /// Provides real CPU, GPU, and system temperatures along with other metrics.
    /// </summary>
    public class HardwareMonitorService : IDisposable
    {
        private readonly Computer _computer;
        private readonly object _lock = new();
        private bool _isDisposed;
        private DateTime _lastUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMilliseconds(500);

        // Cached values
        private double _cpuTemperature = -1;
        private double _gpuTemperature = -1;
        private double _cpuLoad = -1;
        private double _gpuLoad = -1;
        private double _usedMemoryMb = 0;

        public HardwareMonitorService()
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsStorageEnabled = true,
                IsMotherboardEnabled = true
            };

            try
            {
                _computer.Open();
                _computer.Accept(new UpdateVisitor());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HardwareMonitorService: Failed to open computer: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates all hardware sensors. Call this periodically to refresh values.
        /// </summary>
        public void Update()
        {
            if (_isDisposed) return;

            lock (_lock)
            {
                if (DateTime.Now - _lastUpdate < _cacheTimeout)
                    return; // Use cached values

                try
                {
                    foreach (var hardware in _computer.Hardware)
                    {
                        hardware.Update();
                        foreach (var subHardware in hardware.SubHardware)
                        {
                            subHardware.Update();
                        }
                    }

                    UpdateCachedValues();
                    _lastUpdate = DateTime.Now;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HardwareMonitorService: Update failed: {ex.Message}");
                }
            }
        }

        private void UpdateCachedValues()
        {
            foreach (var hardware in _computer.Hardware)
            {
                switch (hardware.HardwareType)
                {
                    case HardwareType.Cpu:
                        UpdateCpuSensors(hardware);
                        break;
                    case HardwareType.GpuNvidia:
                    case HardwareType.GpuAmd:
                    case HardwareType.GpuIntel:
                        UpdateGpuSensors(hardware);
                        break;
                    case HardwareType.Memory:
                        UpdateMemorySensors(hardware);
                        break;
                }
            }
        }

        private void UpdateCpuSensors(IHardware hardware)
        {
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
                {
                    _cpuTemperature = sensor.Value ?? -1;
                }
                else if (sensor.SensorType == SensorType.Temperature && _cpuTemperature < 0)
                {
                    // Fallback to any CPU temperature sensor
                    _cpuTemperature = sensor.Value ?? -1;
                }

                if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("Total", StringComparison.OrdinalIgnoreCase))
                {
                    _cpuLoad = sensor.Value ?? -1;
                }
            }
        }

        private void UpdateGpuSensors(IHardware hardware)
        {
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
                {
                    _gpuTemperature = sensor.Value ?? -1;
                }
                else if (sensor.SensorType == SensorType.Temperature && _gpuTemperature < 0)
                {
                    _gpuTemperature = sensor.Value ?? -1;
                }

                if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
                {
                    _gpuLoad = sensor.Value ?? -1;
                }
            }
        }

        private void UpdateMemorySensors(IHardware hardware)
        {
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Data)
                {
                    if (sensor.Name.Contains("Used", StringComparison.OrdinalIgnoreCase))
                    {
                        _usedMemoryMb = (sensor.Value ?? 0) * 1024; // Convert GB to MB
                    }
                    else if (sensor.Name.Contains("Available", StringComparison.OrdinalIgnoreCase))
                    {
                        // We'll calculate total from used + available
                    }
                }
            }
        }

        /// <summary>
        /// Gets CPU temperature in Celsius. Returns -1 if unavailable.
        /// </summary>
        public async Task<double> GetCpuTemperatureAsync()
        {
            await Task.Run(() => Update());
            return Math.Round(_cpuTemperature, 1);
        }

        /// <summary>
        /// Gets GPU temperature in Celsius. Returns -1 if unavailable.
        /// </summary>
        public async Task<double> GetGpuTemperatureAsync()
        {
            await Task.Run(() => Update());
            return Math.Round(_gpuTemperature, 1);
        }

        /// <summary>
        /// Gets CPU load percentage. Returns -1 if unavailable.
        /// </summary>
        public async Task<double> GetCpuLoadAsync()
        {
            await Task.Run(() => Update());
            return Math.Round(_cpuLoad, 1);
        }

        /// <summary>
        /// Gets GPU load percentage. Returns -1 if unavailable.
        /// </summary>
        public async Task<double> GetGpuLoadAsync()
        {
            await Task.Run(() => Update());
            return Math.Round(_gpuLoad, 1);
        }

        /// <summary>
        /// Gets detailed hardware information for all sensors.
        /// </summary>
        public List<HardwareSensorInfo> GetAllSensors()
        {
            var sensors = new List<HardwareSensorInfo>();

            lock (_lock)
            {
                foreach (var hardware in _computer.Hardware)
                {
                    hardware.Update();
                    foreach (var sensor in hardware.Sensors)
                    {
                        sensors.Add(new HardwareSensorInfo
                        {
                            HardwareName = hardware.Name,
                            HardwareType = hardware.HardwareType.ToString(),
                            SensorName = sensor.Name,
                            SensorType = sensor.SensorType.ToString(),
                            Value = sensor.Value ?? 0,
                            Min = sensor.Min ?? 0,
                            Max = sensor.Max ?? 0
                        });
                    }

                    foreach (var subHardware in hardware.SubHardware)
                    {
                        subHardware.Update();
                        foreach (var sensor in subHardware.Sensors)
                        {
                            sensors.Add(new HardwareSensorInfo
                            {
                                HardwareName = $"{hardware.Name} > {subHardware.Name}",
                                HardwareType = subHardware.HardwareType.ToString(),
                                SensorName = sensor.Name,
                                SensorType = sensor.SensorType.ToString(),
                                Value = sensor.Value ?? 0,
                                Min = sensor.Min ?? 0,
                                Max = sensor.Max ?? 0
                            });
                        }
                    }
                }
            }

            return sensors;
        }

        /// <summary>
        /// Gets disk temperatures for all storage devices.
        /// </summary>
        public Dictionary<string, double> GetDiskTemperatures()
        {
            var temps = new Dictionary<string, double>();

            lock (_lock)
            {
                foreach (var hardware in _computer.Hardware)
                {
                    if (hardware.HardwareType == HardwareType.Storage)
                    {
                        hardware.Update();
                        var tempSensor = hardware.Sensors
                            .FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                        
                        if (tempSensor != null)
                        {
                            temps[hardware.Name] = tempSensor.Value ?? -1;
                        }
                    }
                }
            }

            return temps;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            
            lock (_lock)
            {
                _isDisposed = true;
                try
                {
                    _computer.Close();
                }
                catch { /* Ignore disposal errors */ }
            }
        }
    }

    /// <summary>
    /// Visitor pattern implementation for updating hardware sensors.
    /// </summary>
    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var subHardware in hardware.SubHardware)
            {
                subHardware.Accept(this);
            }
        }

        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }

    /// <summary>
    /// Data transfer object for hardware sensor information.
    /// </summary>
    public class HardwareSensorInfo
    {
        public string HardwareName { get; set; } = string.Empty;
        public string HardwareType { get; set; } = string.Empty;
        public string SensorName { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public float Value { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }

        public override string ToString() => $"{HardwareName} - {SensorName}: {Value:F1} ({SensorType})";
    }
}
