using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WinKnightUI.Services
{
    /// <summary>
    /// Service for managing Windows power plans and battery status.
    /// </summary>
    public class PowerManagementService
    {
        // Known power plan GUIDs
        public static readonly Guid BalancedPlanGuid = new("381b4222-f694-41f0-9685-ff5bb260df2e");
        public static readonly Guid HighPerformancePlanGuid = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
        public static readonly Guid PowerSaverPlanGuid = new("a1841308-3541-4fab-bc81-f71556f20b4a");

        public class PowerPlanInfo
        {
            public Guid Guid { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public string Description { get; set; } = string.Empty;
        }

        public class BatteryInfo
        {
            public bool HasBattery { get; set; }
            public int ChargePercent { get; set; }
            public string Status { get; set; } = "Unknown";
            public bool IsCharging { get; set; }
            public TimeSpan? EstimatedRuntime { get; set; }
            public string BatteryHealth { get; set; } = "Unknown";
            public int DesignCapacity { get; set; }
            public int FullChargeCapacity { get; set; }
            public int CycleCount { get; set; }
        }

        /// <summary>
        /// Gets all available power plans.
        /// </summary>
        public async Task<List<PowerPlanInfo>> GetPowerPlansAsync()
        {
            return await Task.Run(() =>
            {
                var plans = new List<PowerPlanInfo>();

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powercfg",
                        Arguments = "/list",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    };

                    using var process = Process.Start(psi);
                    if (process == null) return plans;

                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    foreach (var line in output.Split('\n'))
                    {
                        if (line.Contains("GUID:"))
                        {
                            var guidStart = line.IndexOf("GUID:") + 5;
                            var guidEnd = line.IndexOf(" ", guidStart + 2);
                            if (guidEnd < 0) guidEnd = line.Length;

                            var guidStr = line.Substring(guidStart, guidEnd - guidStart).Trim();
                            if (Guid.TryParse(guidStr, out var guid))
                            {
                                var nameStart = line.IndexOf("(");
                                var nameEnd = line.IndexOf(")");
                                var name = nameStart >= 0 && nameEnd > nameStart
                                    ? line.Substring(nameStart + 1, nameEnd - nameStart - 1)
                                    : "Unknown";

                                var isActive = line.Contains("*");

                                plans.Add(new PowerPlanInfo
                                {
                                    Guid = guid,
                                    Name = name,
                                    IsActive = isActive,
                                    Description = GetPlanDescription(name)
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"PowerManagementService: Error getting power plans: {ex.Message}");
                }

                return plans;
            });
        }

        /// <summary>
        /// Sets the active power plan.
        /// </summary>
        public async Task<bool> SetActivePowerPlanAsync(Guid planGuid)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powercfg",
                        Arguments = $"/setactive {planGuid}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    process?.WaitForExit();
                    return process?.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"PowerManagementService: Error setting power plan: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Gets current battery information.
        /// </summary>
        public async Task<BatteryInfo> GetBatteryInfoAsync()
        {
            return await Task.Run(() =>
            {
                var info = new BatteryInfo();

                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");
                    var batteries = searcher.Get();

                    foreach (ManagementObject battery in batteries)
                    {
                        info.HasBattery = true;
                        info.ChargePercent = Convert.ToInt32(battery["EstimatedChargeRemaining"] ?? 0);
                        
                        var statusCode = Convert.ToInt32(battery["BatteryStatus"] ?? 0);
                        info.Status = GetBatteryStatusText(statusCode);
                        info.IsCharging = statusCode == 2 || statusCode == 6 || statusCode == 7 || statusCode == 8 || statusCode == 9;

                        var runTime = battery["EstimatedRunTime"];
                        if (runTime != null)
                        {
                            var minutes = Convert.ToInt32(runTime);
                            if (minutes > 0 && minutes < 71582788) // Not unlimited
                            {
                                info.EstimatedRuntime = TimeSpan.FromMinutes(minutes);
                            }
                        }

                        info.DesignCapacity = Convert.ToInt32(battery["DesignCapacity"] ?? 0);
                        info.FullChargeCapacity = Convert.ToInt32(battery["FullChargeCapacity"] ?? 0);

                        // Calculate health
                        if (info.DesignCapacity > 0 && info.FullChargeCapacity > 0)
                        {
                            var healthPercent = (info.FullChargeCapacity * 100) / info.DesignCapacity;
                            info.BatteryHealth = healthPercent switch
                            {
                                >= 80 => $"Good ({healthPercent}%)",
                                >= 50 => $"Fair ({healthPercent}%)",
                                _ => $"Poor ({healthPercent}%)"
                            };
                        }

                        break; // Usually only one battery
                    }

                    // Try to get cycle count from WMI (may not be available on all systems)
                    try
                    {
                        using var cycleSearcher = new ManagementObjectSearcher(
                            @"root\WMI",
                            "SELECT * FROM BatteryCycleCount");
                        
                        foreach (ManagementObject obj in cycleSearcher.Get())
                        {
                            info.CycleCount = Convert.ToInt32(obj["CycleCount"] ?? 0);
                            break;
                        }
                    }
                    catch { /* Cycle count not available */ }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"PowerManagementService: Error getting battery info: {ex.Message}");
                }

                return info;
            });
        }

        /// <summary>
        /// Gets the current active power plan.
        /// </summary>
        public async Task<PowerPlanInfo?> GetActivePowerPlanAsync()
        {
            var plans = await GetPowerPlansAsync();
            return plans.Find(p => p.IsActive);
        }

        /// <summary>
        /// Puts the computer to sleep.
        /// </summary>
        public async Task<bool> SleepAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "rundll32.exe",
                        Arguments = "powrprof.dll,SetSuspendState 0,1,0",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    Process.Start(psi);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"PowerManagementService: Sleep failed: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Hibernates the computer.
        /// </summary>
        public async Task<bool> HibernateAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "shutdown",
                        Arguments = "/h",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    Process.Start(psi);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"PowerManagementService: Hibernate failed: {ex.Message}");
                    return false;
                }
            });
        }

        private static string GetBatteryStatusText(int code) => code switch
        {
            1 => "Discharging",
            2 => "AC Power",
            3 => "Fully Charged",
            4 => "Low",
            5 => "Critical",
            6 => "Charging",
            7 => "Charging High",
            8 => "Charging Low",
            9 => "Charging Critical",
            10 => "Undefined",
            11 => "Partially Charged",
            _ => "Unknown"
        };

        private static string GetPlanDescription(string name)
        {
            var lowerName = name.ToLower();
            if (lowerName.Contains("balanced"))
                return "Automatically balances performance with energy consumption.";
            if (lowerName.Contains("high performance") || lowerName.Contains("ultimate"))
                return "Favors performance over energy consumption.";
            if (lowerName.Contains("power saver"))
                return "Conserves energy by reducing performance.";
            return "Custom power plan.";
        }
    }
}
