using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Principal;
using System.Linq;
using System.Management;
using Microsoft.Win32;
using System.Windows;

namespace WinKnightUI
{
    // A simple class to hold the results of a diagnostic scan.
    public class ScanReport
    {
        public bool IsSuccessful { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> LogEntries { get; set; } = new List<string>();
    }

    // New: A simple class to hold process data for RAM and CPU usage
    public class ProcessInfo
    {
        public string Name { get; set; } = string.Empty;
        public long MemoryUsageBytes { get; set; }
        public double MemoryUsageMb => MemoryUsageBytes / 1024.0 / 1024.0;
        public float CpuUsage { get; set; }
        public bool IsChecked { get; set; }
    }

    // NEW: Class to hold disk health data
    public class DiskInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long FreeSpaceGb => FreeSpaceBytes / (1024 * 1024 * 1024);
        public long TotalSpaceGb => TotalSpaceBytes / (1024 * 1024 * 1024);
        public long UsedSpaceGb => TotalSpaceGb - FreeSpaceGb;
        public double UsagePercentage => TotalSpaceGb > 0 ? (double)UsedSpaceGb / TotalSpaceGb * 100 : 0;
        public long FreeSpaceBytes { get; set; }
        public long TotalSpaceBytes { get; set; }
        public string DiskType { get; set; } = string.Empty;
        public string ReadSpeed { get; set; } = "-- MB/s";
        public string WriteSpeed { get; set; } = "-- MB/s";
        public string ModelName { get; set; } = string.Empty;
        public double Temperature { get; set; } = -1;
    }

    // NEW: Class to hold BSOD report info
    public class BsodReport
    {
        public string FileName { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string AnalysisSummary { get; set; } = string.Empty;
    }

    // NEW: Class to hold startup program info
    public class StartupProgram
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Impact { get; set; } = "Low";
        public bool IsEnabled { get; set; }
    }

    public class WinKnightCore : IDisposable
    {
        private readonly Services.HardwareMonitorService _hardwareMonitor;
        private double _totalPhysicalMemoryMb = 0;
        private bool _isDisposed;

        public WinKnightCore()
        {
            _hardwareMonitor = new Services.HardwareMonitorService();
            _totalPhysicalMemoryMb = GetTotalPhysicalMemory();
        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        #region Real-time Metrics
        /// <summary>
        /// Gets CPU temperature using LibreHardwareMonitor.
        /// Returns -1 if temperature cannot be read.
        /// </summary>
        public async Task<double> GetCpuTemperature()
        {
            try
            {
                return await _hardwareMonitor.GetCpuTemperatureAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCpuTemperature error: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Gets GPU temperature using LibreHardwareMonitor.
        /// Returns -1 if temperature cannot be read.
        /// </summary>
        public async Task<double> GetGpuTemperature()
        {
            try
            {
                return await _hardwareMonitor.GetGpuTemperatureAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetGpuTemperature error: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Gets system/motherboard temperature.
        /// Currently returns CPU temp as a proxy; can be enhanced with motherboard sensors.
        /// </summary>
        public async Task<double> GetSystemTemperature()
        {
            try
            {
                // Use CPU temperature as proxy for system temp
                var cpuTemp = await GetCpuTemperature();
                // System temp is typically slightly lower than CPU
                return cpuTemp > 0 ? Math.Max(cpuTemp - 10, 20) : -1;
            }
            catch
            {
                return -1;
            }
        }

        public async Task<int> GetRamUsage()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                    var totalRam = _totalPhysicalMemoryMb > 0 ? _totalPhysicalMemoryMb : GetTotalPhysicalMemory();

                    if (totalRam <= 0) return 0;

                    double availableRam = ramCounter.NextValue();
                    int usage = (int)Math.Round(((totalRam - availableRam) / totalRam) * 100);
                    return Math.Clamp(usage, 0, 100);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GetRamUsage error: {ex.Message}");
                    return 0;
                }
            });
        }

        /// <summary>
        /// Gets total physical RAM in gigabytes.
        /// </summary>
        public double GetTotalRamGb()
        {
            return Math.Round(_totalPhysicalMemoryMb / 1024.0, 1);
        }

        /// <summary>
        /// Gets used RAM in gigabytes.
        /// </summary>
        public async Task<double> GetUsedRamGbAsync()
        {
            var usage = await GetRamUsage();
            var totalGb = GetTotalRamGb();
            return Math.Round((usage / 100.0) * totalGb, 1);
        }

        private double GetTotalPhysicalMemory()
        {
            double totalPhysicalMemory = 0;
            try
            {
                using (var mos = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                {
                    foreach (var mo in mos.Get())
                    {
                        totalPhysicalMemory = Convert.ToDouble(mo["TotalPhysicalMemory"]) / (1024.0 * 1024.0);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting total physical memory: {ex.Message}");
            }
            return totalPhysicalMemory;
        }

        public async Task<string> GetTopCpuProcess()
        {
            await Task.Delay(100);
            var processes = Process.GetProcesses();
            Process? topProcess = null;
            TimeSpan maxCpuTime = TimeSpan.Zero;

            foreach (var p in processes)
            {
                try
                {
                    if (p.TotalProcessorTime > maxCpuTime)
                    {
                        maxCpuTime = p.TotalProcessorTime;
                        topProcess = p;
                    }
                }
                catch { /* Ignore processes that can't be accessed */ }
            }
            return topProcess?.ProcessName ?? "N/A";
        }

        public async Task<List<ProcessInfo>> GetTopRamProcess()
        {
            await Task.Delay(100);
            var processes = Process.GetProcesses();
            var processList = new List<ProcessInfo>();

            foreach (var p in processes)
            {
                try
                {
                    processList.Add(new ProcessInfo
                    {
                        Name = p.ProcessName,
                        MemoryUsageBytes = p.WorkingSet64
                    });
                }
                catch { /* Ignore processes that can't be accessed */ }
            }

            return processList.OrderByDescending(p => p.MemoryUsageBytes).Take(10).ToList();
        }

        public void CloseProcess(string processName)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not close process '{processName}': {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void CloseProcesses(List<string> processNames)
        {
            foreach (var processName in processNames)
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not close process '{processName}': {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        public async Task<DiskInfo> GetDiskHealthStatus()
        {
            await Task.Delay(100);
            var drive = new DriveInfo("C");
            var status = (drive.IsReady && drive.TotalFreeSpace > (drive.TotalSize * 0.1)) ? "Good" : "Warning";
            return new DiskInfo
            {
                Name = "C:",
                Status = status,
                FreeSpaceBytes = drive.TotalFreeSpace,
                TotalSpaceBytes = drive.TotalSize,
            };
        }

        public async Task<List<DiskInfo>> GetDiskHealthDetails()
        {
            return await Task.Run(() =>
            {
                var driveInfos = DriveInfo.GetDrives().Where(d => d.IsReady);
                var diskInfoList = new List<DiskInfo>();

                foreach (var drive in driveInfos)
                {
                    diskInfoList.Add(new DiskInfo
                    {
                        Name = drive.Name,
                        Status = (drive.IsReady && drive.TotalFreeSpace > (drive.TotalSize * 0.1)) ? "Good" : "Warning",
                        FreeSpaceBytes = drive.TotalFreeSpace,
                        TotalSpaceBytes = drive.TotalSize,
                        DiskType = GetDiskDriveType(drive.Name),
                        ModelName = GetDiskModel(drive.Name)
                    });
                }
                return diskInfoList;
            });
        }

        private string GetDiskDriveType(string driveName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE Caption IS NOT NULL"))
                {
                    foreach (ManagementObject drive in searcher.Get())
                    {
                        var caption = drive["Caption"]?.ToString() ?? string.Empty;
                        if (caption.Contains(driveName.TrimEnd('\\', ':')))
                        {
                            var mediaType = drive["MediaType"]?.ToString() ?? string.Empty;
                            var model = drive["Model"]?.ToString() ?? string.Empty;
                            if (mediaType.Contains("SSD")) return "SSD";
                            if (mediaType.Contains("Hard disk media")) return "HDD";
                            if (model.Contains("NVMe")) return "NVMe SSD";
                        }
                    }
                }
            }
            catch { return "Unknown"; }
            return "Unknown";
        }

        private string GetDiskModel(string driveName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE Caption IS NOT NULL"))
                {
                    foreach (ManagementObject drive in searcher.Get())
                    {
                        var caption = drive["Caption"]?.ToString() ?? string.Empty;
                        if (caption.Contains(driveName.TrimEnd('\\', ':')))
                        {
                            return drive["Model"]?.ToString() ?? "Unknown";
                        }
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        // NEW: Get real-time disk performance details
        public async Task<List<DiskInfo>> GetDiskPerformanceDetails()
        {
            var diskInfoList = await GetDiskHealthDetails();

            foreach (var diskInfo in diskInfoList)
            {
                try
                {
                    var driveLetter = diskInfo.Name.TrimEnd('\\', ':');
                    var readCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", driveLetter);
                    var writeCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", driveLetter);

                    // Must call NextValue() twice with a delay to get a meaningful reading
                    readCounter.NextValue();
                    writeCounter.NextValue();
                    await Task.Delay(1000);

                    double readSpeed = readCounter.NextValue() / 1024.0 / 1024.0;
                    double writeSpeed = writeCounter.NextValue() / 1024.0 / 1024.0;

                    diskInfo.ReadSpeed = $"{readSpeed:F2} MB/s";
                    diskInfo.WriteSpeed = $"{writeSpeed:F2} MB/s";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting disk performance for {diskInfo.Name}: {ex.Message}");
                    diskInfo.ReadSpeed = "N/A";
                    diskInfo.WriteSpeed = "N/A";
                }
            }
            return diskInfoList;
        }


        public async Task<int> GetStartupProgramsCount()
        {
            await Task.Delay(100);
            var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            return key?.ValueCount ?? 0;
        }

        public async Task<List<StartupProgram>> GetStartupPrograms()
        {
            await Task.Delay(100);
            var startupPrograms = new List<StartupProgram>();
            using (var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"))
            {
                if (key != null)
                {
                    foreach (var valueName in key.GetValueNames())
                    {
                        var impact = (valueName.ToLower().Contains("microsoft") || valueName.ToLower().Contains("onedrive") || valueName.ToLower().Contains("steam"))
                            ? "High"
                            : "Low";

                        startupPrograms.Add(new StartupProgram
                        {
                            Name = valueName,
                            Path = key.GetValue(valueName)?.ToString() ?? string.Empty,
                            Impact = impact,
                            IsEnabled = true
                        });
                    }
                }
            }
            return startupPrograms;
        }

        public void ToggleStartupProgram(string name, bool isEnabled)
        {
            using (var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key == null) return;

                if (isEnabled)
                {
                    // Re-enable by adding the key back (stub logic)
                }
                else
                {
                    key.DeleteValue(name, false);
                }
            }
        }

        public async Task<List<BsodReport>> AnalyzeCrashDumps()
        {
            await Task.Delay(500); // Simulate scanning
            var crashDumps = new List<BsodReport>();

            var dumpDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Minidump");

            if (Directory.Exists(dumpDir))
            {
                var dmpFiles = Directory.GetFiles(dumpDir, "*.dmp");
                foreach (var file in dmpFiles)
                {
                    var fileInfo = new FileInfo(file);
                    crashDumps.Add(new BsodReport
                    {
                        FileName = fileInfo.Name,
                        Timestamp = fileInfo.CreationTime.ToString("g"),
                        AnalysisSummary = "Analysis not available in this version. File may contain details about the crash."
                    });
                }
            }

            return crashDumps;
        }

        #endregion

        #region RestoreGuard Module
        public async Task<ScanReport> CreateSystemRestorePoint(string description)
        {
            if (!IsAdministrator())
            {
                return new ScanReport { IsSuccessful = false, Message = "Administrator privileges are required to create a system restore point." };
            }

            var report = new ScanReport();
            try
            {
                string command = "powershell.exe";
                string arguments = $" -Command \"Checkpoint-Computer -Description '{description}' -RestorePointType 'MODIFY_SETTINGS'\"";

                report.LogEntries.Add($"Executing: {command} {arguments}");

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit());

                report.LogEntries.Add(output);
                if (process.ExitCode == 0)
                {
                    report.IsSuccessful = true;
                    report.Message = "System restore point created successfully.";
                }
                else
                {
                    report.IsSuccessful = false;
                    report.Message = $"Failed to create system restore point. Error: {error}";
                    report.LogEntries.Add($"Error: {error}");
                }
            }
            catch (Exception ex)
            {
                report.IsSuccessful = false;
                report.Message = $"An error occurred: {ex.Message}";
                report.LogEntries.Add($"Exception: {ex.Message}");
            }
            return report;
        }
        #endregion

        #region SelfHeal Module
        public async Task<ScanReport> RunSfcScan()
        {
            if (!IsAdministrator())
            {
                return new ScanReport { IsSuccessful = false, Message = "Administrator privileges are required to run SFC." };
            }

            var report = new ScanReport();
            report.LogEntries.Add("Starting SFC /scannow...");

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sfc.exe",
                        Arguments = "/scannow",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit());

                report.LogEntries.Add(output);
                if (output.Contains("Windows Resource Protection did not find any integrity violations."))
                {
                    report.IsSuccessful = true;
                    report.Message = "SFC scan completed. No integrity violations found.";
                }
                else if (output.Contains("successfully repaired corrupt files."))
                {
                    report.IsSuccessful = true;
                    report.Message = "SFC scan completed. Corrupt files were repaired.";
                }
                else
                {
                    report.IsSuccessful = false;
                    report.Message = "SFC scan completed, but issues were found that could not be repaired.";
                }
            }
            catch (Exception ex)
            {
                report.IsSuccessful = false;
                report.Message = $"An error occurred during the SFC scan: {ex.Message}";
                report.LogEntries.Add($"Exception: {ex.Message}");
            }
            return report;
        }

        public async Task<ScanReport> RunDismRepair()
        {
            if (!IsAdministrator())
            {
                return new ScanReport { IsSuccessful = false, Message = "Administrator privileges are required to run DISM." };
            }

            var report = new ScanReport();
            report.LogEntries.Add("Starting DISM /RestoreHealth...");
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = "/Online /Cleanup-Image /RestoreHealth",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit());

                report.LogEntries.Add(output);
                if (output.Contains("The operation completed successfully."))
                {
                    report.IsSuccessful = true;
                    report.Message = "DISM scan and repair completed successfully.";
                }
                else
                {
                    report.IsSuccessful = false;
                    report.Message = "DISM failed to complete the repair operation.";
                }
            }
            catch (Exception ex)
            {
                report.IsSuccessful = false;
                report.Message = $"An error occurred during the DISM repair: {ex.Message}";
                report.LogEntries.Add($"Exception: {ex.Message}");
            }
            return report;
        }
        #endregion

        #region CacheCleaner Module
        public async Task<ScanReport> CleanCache()
        {
            return await Task.Run(() =>
            {
                var report = new ScanReport { IsSuccessful = true, Message = "Cache cleanup completed." };
                List<string> tempDirectories = new List<string>
                {
                    Path.GetTempPath(),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch")
                };

                foreach (var directory in tempDirectories)
                {
                    report.LogEntries.Add($"Attempting to clean: {directory}");
                    try
                    {
                        var directoryInfo = new DirectoryInfo(directory);
                        if (directoryInfo.Exists)
                        {
                            foreach (var file in directoryInfo.GetFiles())
                            {
                                try { file.Delete(); report.LogEntries.Add($"Deleted file: {file.FullName}"); }
                                catch (Exception ex) { report.LogEntries.Add($"Could not delete file {file.FullName}: {ex.Message}"); }
                            }
                            foreach (var subDir in directoryInfo.GetDirectories())
                            {
                                try { subDir.Delete(true); report.LogEntries.Add($"Deleted directory: {subDir.FullName}"); }
                                catch (Exception ex) { report.LogEntries.Add($"Could not delete directory {subDir.FullName}: {ex.Message}"); }
                            }
                        }
                        else
                        {
                            report.LogEntries.Add("Directory does not exist.");
                        }
                    }
                    catch (Exception ex)
                    {
                        report.IsSuccessful = false;
                        report.Message = "Cache cleanup failed in one or more locations.";
                        report.LogEntries.Add($"An error occurred while cleaning {directory}: {ex.Message}");
                    }
                }
                return report;
            });
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                _hardwareMonitor?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WinKnightCore Dispose error: {ex.Message}");
            }

            GC.SuppressFinalize(this);
        }
        #endregion
    }
}