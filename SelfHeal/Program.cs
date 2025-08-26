using System;
using System.Collections.Generic;
using System.Diagnostics; // This is the corrected 'using' directive for the EventLog class
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
// Note: This using statement is now redundant and has been removed from the code.

public class SelfHeal
{
    // A constant to target the System event log, a good place for critical errors.
    private const string LogName = "System";
    // Event ID for unexpected shutdowns, often a sign of a BSOD.
    private const int CriticalEventId = 41;
    // An array of other common critical Event IDs to look for
    private static readonly int[] OtherCriticalEvents = { 1000, 1001, 7000, 7001 };

    public static async Task Main(string[] args)
    {
        // Must be run as administrator to execute SFC and DISM.
        if (!IsAdministrator())
        {
            Console.WriteLine("This program must be run with administrator privileges to perform a full system scan.");
            return;
        }

        Console.WriteLine("Starting Windows Self-Heal Scan and Repair...");
        Console.WriteLine("---------------------------------------------");

        var report = new StringBuilder();
        report.AppendLine("Windows Self-Heal Report");
        report.AppendLine("------------------------");

        // --- 1. Scan for Critical Events (e.g., unexpected shutdowns/BSODs) ---
        report.AppendLine("\n1. Critical Event Log Scan:");
        await ScanEventLogAsync(report);

        // --- 2. Check for Driver Issues ---
        report.AppendLine("\n2. Driver Integrity Check:");
        await RunDriverQueryAsync(report);

        // --- 3. Check and Repair System File Integrity (SFC Scan) ---
        report.AppendLine("\n3. System File Checker (SFC) Scan:");
        await RunSFCScanAsync(report);

        // --- 4. Check and Repair Windows Component Store Health (DISM Scan) ---
        report.AppendLine("\n4. Deployment Image Servicing and Management (DISM) Scan:");
        await RunDISMScanAsync(report);

        Console.WriteLine("\nScan and repair complete. Writing report to SelfHeal_report.txt");
        // FIX: Use a specific encoding (e.g., UTF-8) to prevent file corruption
        File.WriteAllText("SelfHeal_report.txt", report.ToString(), Encoding.UTF8);
    }

    private static bool IsAdministrator()
    {
#if WINDOWS
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
#else
        return false;
#endif
    }

    private static async Task ScanEventLogAsync(StringBuilder report)
    {
        try
        {
            await Task.Run(() =>
            {
                var eventLog = new EventLog(LogName);
                var criticalEvents = 0;
                var daysToCheck = 7;

                var criticalEventDictionary = new Dictionary<long, List<EventLogEntry>>();

                foreach (EventLogEntry entry in eventLog.Entries)
                {
                    if (entry.TimeGenerated > DateTime.Now.AddDays(-daysToCheck))
                    {
                        if (entry.EntryType == EventLogEntryType.Error && (entry.InstanceId == CriticalEventId || Array.Exists(OtherCriticalEvents, id => id == entry.InstanceId)))
                        {
                            if (!criticalEventDictionary.ContainsKey(entry.InstanceId))
                            {
                                criticalEventDictionary[entry.InstanceId] = new List<EventLogEntry>();
                            }
                            criticalEventDictionary[entry.InstanceId].Add(entry);
                            criticalEvents++;
                        }
                    }
                }

                if (criticalEvents > 0)
                {
                    report.AppendLine($"  Found a total of {criticalEvents} critical errors in the last {daysToCheck} days.");
                    foreach (var kvp in criticalEventDictionary)
                    {
                        report.AppendLine($"  - Event ID {kvp.Key}: {kvp.Value.Count} occurrences.");
                        if (kvp.Value.Count > 0)
                        {
                            report.AppendLine($"    First occurrence: {kvp.Value[0].TimeGenerated}");
                            report.AppendLine($"    Most recent: {kvp.Value[kvp.Value.Count - 1].TimeGenerated}");
                        }
                        report.AppendLine("    Action: These events may indicate an underlying hardware or software issue that requires further investigation.");
                    }
                }
                else
                {
                    report.AppendLine($"  No critical errors found in the last {daysToCheck} days. The system appears stable.");
                }
            });
        }
        catch (Exception ex)
        {
            report.AppendLine($"  Error scanning event log: {ex.Message}");
        }
    }

    private static async Task RunDriverQueryAsync(StringBuilder report)
    {
        Console.WriteLine("  Running 'DriverQuery' to check for unsigned drivers...");
        string output = await ExecuteProcessAsync("driverquery.exe", "/fo csv /v");
        report.AppendLine("  Driver Integrity Output:");
        report.AppendLine("--------------------------");

        var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        var unsignedDriversFound = new List<string>();

        // Skip header lines in CSV output
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            // Assuming the 10th column is for 'Signed' status
            if (parts.Length > 9 && parts[9].Trim().ToLower() == "\"false\"")
            {
                unsignedDriversFound.Add(parts[0]);
            }
        }

        if (unsignedDriversFound.Count > 0)
        {
            report.AppendLine($"  Warning: Found {unsignedDriversFound.Count} unsigned drivers, which could cause instability.");
            report.AppendLine("  Unsigned Drivers:");
            foreach (var driverName in unsignedDriversFound)
            {
                report.AppendLine($"  - {driverName.Trim('"')}");
            }
        }
        else
        {
            report.AppendLine("  Status: All active drivers are signed and appear to be valid.");
        }
    }

    private static async Task RunSFCScanAsync(StringBuilder report)
    {
        Console.WriteLine("  Running 'sfc /scannow' to check file integrity... This may take a while.");
        string output = await ExecuteProcessAsync("sfc.exe", "/scannow");
        report.AppendLine("  SFC Scan Output:");
        report.AppendLine("--------------------");
        report.AppendLine(output);

        if (output.Contains("found corrupt files and successfully repaired them"))
        {
            report.AppendLine("  Status: Corrupted files were found and fixed.");
            report.AppendLine("  Action: A reboot is recommended to complete the repairs.");
        }
        else if (output.Contains("Windows Resource Protection did not find any integrity violations"))
        {
            report.AppendLine("  Status: No integrity violations found. The file system is healthy.");
        }
        else
        {
            report.AppendLine("  Warning: SFC scan completed, but status could not be determined. Review the full output for details.");
        }
    }

    private static async Task RunDISMScanAsync(StringBuilder report)
    {
        Console.WriteLine("  Running 'DISM /Online /Cleanup-Image /ScanHealth' to check store health... This may take a while.");
        string scanOutput = await ExecuteProcessAsync("dism.exe", "/Online /Cleanup-Image /ScanHealth");
        report.AppendLine("  DISM ScanHealth Output:");
        report.AppendLine("-------------------------");
        report.AppendLine(scanOutput);

        if (scanOutput.Contains("No component store corruption detected"))
        {
            report.AppendLine("  Status: The Windows component store is healthy.");
        }
        else if (scanOutput.Contains("The component store is repairable"))
        {
            report.AppendLine("  Warning: The Windows component store is corrupted but repairable.");
            report.AppendLine("  Action: Attempting to run 'DISM /Online /Cleanup-Image /RestoreHealth' to repair...");

            string repairOutput = await ExecuteProcessAsync("dism.exe", "/Online /Cleanup-Image /RestoreHealth");
            report.AppendLine("  DISM RestoreHealth Output:");
            report.AppendLine("---------------------------");
            report.AppendLine(repairOutput);

            if (repairOutput.Contains("The operation completed successfully"))
            {
                report.AppendLine("  Status: Repair was successful.");
            }
            else
            {
                report.AppendLine("  Error: The repair failed. Manual intervention may be needed.");
            }
        }
        else
        {
            report.AppendLine("  Warning: Unable to determine DISM status. Please review the full output.");
        }
    }

    private static async Task<string> ExecuteProcessAsync(string fileName, string arguments)
    {
        try
        {
            var process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return output;
        }
        catch (Exception ex)
        {
            return $"Error: Failed to execute process '{fileName} {arguments}'. Reason: {ex.Message}";
        }
    }
}
