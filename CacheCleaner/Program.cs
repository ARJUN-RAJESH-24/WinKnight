using System;
using System.IO;
using System.Security.Principal;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

public class SystemCleaner
{
    /// <summary>
    /// The entry point of the application.
    /// This is the modern, asynchronous Main method supported by .NET 8.0.
    /// </summary>
    public static async Task Main(string[] args)
    {
        // The app.manifest file should be configured to require administrator privileges.
        if (!IsAdministrator())
        {
            Console.WriteLine("This program must be run with administrator privileges to perform a full system cleanup.");
            return;
        }

        Console.WriteLine("Starting Windows System Cleanup...");
        Console.WriteLine("----------------------------------");
        Console.WriteLine("Running with administrator privileges.");

        // List of directories to clean. This list has been expanded for a more comprehensive cleanup.
        string[] directoriesToClean = {
            // User-specific temporary directories
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
            Path.GetTempPath(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "INetCache"), // Internet Explorer/Edge Cache
            
            // System-wide temporary directories
            @"C:\Windows\Temp",
            @"C:\Windows\Prefetch"
        };

        foreach (var dir in directoriesToClean)
        {
            await ClearDirectoryAsync(dir);
        }

        // Clean browser caches specifically
        await ClearBrowserCachesAsync();

        Console.WriteLine("\nCleanup complete. Press any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Checks if the current process is running with administrator privileges.
    /// The `app.manifest` should be used to guarantee this.
    /// </summary>
    /// <returns>True if running as administrator, otherwise false.</returns>
    private static bool IsAdministrator()
    {
        // Check if the current user is in the Administrator role.
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// Clears the caches of popular web browsers.
    /// NOTE: This may clear some browsing history or cookies. Use with caution.
    /// </summary>
    private static async Task ClearBrowserCachesAsync()
    {
        Console.WriteLine("\nCleaning web browser caches...");
        // Path to the user's Local AppData folder
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Define paths to browser caches
        var browserCachePaths = new Dictionary<string, string>
        {
            {"Google Chrome", Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Cache")},
            {"Microsoft Edge", Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Cache")},
            {"Mozilla Firefox", Path.Combine(localAppData, "Mozilla", "Firefox", "Profiles")} // Firefox profiles are in a subfolder
        };

        foreach (var entry in browserCachePaths)
        {
            if (Directory.Exists(entry.Value))
            {
                await ClearDirectoryAsync(entry.Value);
            }
            else
            {
                Console.WriteLine($"  Cache directory for {entry.Key} not found: {entry.Value}. Skipping.");
            }
        }
    }

    /// <summary>
    /// Deletes all files and subdirectories from a given directory.
    /// Handles exceptions for files in use or permission denied.
    /// </summary>
    /// <param name="directoryPath">The full path of the directory to clean.</param>
    private static async Task ClearDirectoryAsync(string directoryPath)
    {
        await Task.Run(() =>
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Directory not found: {directoryPath}. Skipping.");
                return;
            }

            Console.WriteLine($"\nCleaning directory: {directoryPath}");
            int filesDeleted = 0, directoriesDeleted = 0;
            int filesSkipped = 0, directoriesSkipped = 0;

            // Delete all files in the directory
            try
            {
                var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                        filesDeleted++;
                    }
                    catch
                    {
                        filesSkipped++;
                        Console.WriteLine($"  Skipped file: {Path.GetFileName(file)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error accessing files in directory: {ex.Message}");
                return;
            }

            // Delete all subdirectories
            try
            {
                var subdirectories = Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly);
                foreach (string subdir in subdirectories)
                {
                    try
                    {
                        Directory.Delete(subdir, true);
                        directoriesDeleted++;
                    }
                    catch
                    {
                        directoriesSkipped++;
                        Console.WriteLine($"  Skipped directory: {Path.GetFileName(subdir)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error accessing subdirectories: {ex.Message}");
                return;
            }

            Console.WriteLine($"\n  Results for '{directoryPath}':");
            Console.WriteLine($"  - Files: {filesDeleted} deleted, {filesSkipped} skipped");
            Console.WriteLine($"  - Directories: {directoriesDeleted} deleted, {directoriesSkipped} skipped");
        });
    }
}
