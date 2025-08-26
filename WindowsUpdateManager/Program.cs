using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;

public class WindowsUpdateManager
{
    private const string ServiceName = "wuauserv";

    public static void Main(string[] args)
    {
        Console.WriteLine("--- Windows Update Manager ---");
        Console.WriteLine("1. Disable Windows Update");
        Console.WriteLine("2. Enable Windows Update");
        Console.WriteLine("3. Exit");
        Console.WriteLine("------------------------------");

        while (true)
        {
            Console.Write("Enter your choice: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    DisableWindowsUpdate();
                    break;
                case "2":
                    EnableWindowsUpdate();
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please enter 1, 2, or 3.");
                    break;
            }
        }
    }

    /// <summary>
    /// Disables the Windows Update service.
    /// This method stops the service and sets its startup type to 'Disabled'.
    /// </summary>
    public static void DisableWindowsUpdate()
    {
        try
        {
            ServiceController sc = new ServiceController(ServiceName);
            if (sc.Status == ServiceControllerStatus.Running)
            {
                Console.WriteLine("Stopping Windows Update service...");
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                Console.WriteLine("Windows Update service stopped successfully.");
            }
            else
            {
                Console.WriteLine("Windows Update service is already stopped.");
            }

            // Set the startup type to Disabled
            SetServiceStartupType(ServiceName, ServiceStartMode.Disabled);
            Console.WriteLine("Windows Update service startup type set to 'Disabled'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while disabling Windows Update: {ex.Message}");
        }
    }

    /// <summary>
    /// Enables the Windows Update service.
    /// This method sets the startup type to 'Automatic' and starts the service.
    /// </summary>
    public static void EnableWindowsUpdate()
    {
        try
        {
            // Set the startup type to Automatic
            SetServiceStartupType(ServiceName, ServiceStartMode.Automatic);
            Console.WriteLine("Windows Update service startup type set to 'Automatic'.");

            ServiceController sc = new ServiceController(ServiceName);
            if (sc.Status != ServiceControllerStatus.Running)
            {
                Console.WriteLine("Starting Windows Update service...");
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                Console.WriteLine("Windows Update service started successfully.");
            }
            else
            {
                Console.WriteLine("Windows Update service is already running.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while enabling Windows Update: {ex.Message}");
        }
    }

    /// <summary>
    /// Uses a command-line process to set the service startup type.
    /// This is a robust way to interact with the Service Control Manager.
    /// </summary>
    /// <param name="serviceName">The name of the service (e.g., "wuauserv").</param>
    /// <param name="startupMode">The desired startup mode.</param>
    private static void SetServiceStartupType(string serviceName, ServiceStartMode startupMode)
    {
        string mode = "";
        switch (startupMode)
        {
            case ServiceStartMode.Automatic:
                mode = "auto";
                break;
            case ServiceStartMode.Disabled:
                mode = "disabled";
                break;
            default:
                throw new ArgumentException("Unsupported startup mode.");
        }

        Process process = new Process();
        ProcessStartInfo startInfo = new ProcessStartInfo("sc.exe")
        {
            Arguments = $"config \"{serviceName}\" start= {mode}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        process.StartInfo = startInfo;
        process.Start();
        process.WaitForExit();
    }
}
