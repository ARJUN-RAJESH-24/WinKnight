// 
// WinKnightCore.cs
// This file provides the core logic for the WinKnight application.
// It is designed to be integrated into a C# Windows application.
//
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Principal;

namespace WinKnightUI
{
    // A simple class to hold the results of a diagnostic scan.
    public class ScanReport
    {
        public bool IsSuccessful { get; set; }
        public string Message { get; set; }
        public List<string> LogEntries { get; set; } = new List<string>();
    }

    public class WinKnightCore
    {
        // Helper method to check for administrator privileges.
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        #region RestoreGuard Module
        /// <summary>
        /// Creates a system restore point with a specified description.
        /// This method requires administrator privileges.
        /// </summary>
        /// <param name="description">The description for the restore point.</param>
        /// <returns>A ScanReport indicating the success or failure of the operation.</returns>
        public async Task<ScanReport> CreateSystemRestorePoint(string description)
        {
            if (!IsAdministrator())
            {
                return new ScanReport { IsSuccessful = false, Message = "Administrator privileges are required to create a system restore point." };
            }

            var report = new ScanReport();
            try
            {
                // This command line uses WMI to create a restore point.
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
        /// <summary>
        /// Runs the System File Checker (SFC) tool to scan for and repair corrupted system files.
        /// </summary>
        /// <returns>A ScanReport detailing the outcome of the SFC scan.</returns>
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

        /// <summary>
        /// Runs the DISM tool to check and repair the Windows Component Store.
        /// </summary>
        /// <returns>A ScanReport detailing the outcome of the DISM operation.</returns>
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
        /// <summary>
        /// Clears temporary files from specified directories.
        /// </summary>
        /// <returns>A ScanReport indicating the success of the cleanup.</returns>
        public async Task<ScanReport> CleanCache()
        {
            var report = new ScanReport { IsSuccessful = true, Message = "Cache cleanup completed." };
            List<string> tempDirectories = new List<string>
            {
                Path.GetTempPath(), // %TEMP%
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"), // C:\Windows\Temp
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch") // C:\Windows\Prefetch
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
        }
        #endregion
    }
}
