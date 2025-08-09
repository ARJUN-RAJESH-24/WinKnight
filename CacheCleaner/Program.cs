using System;
using System.IO;
using System.Security.Principal;
using System.Diagnostics;
using System.Threading.Tasks;

public class SystemCleaner
{
    public static async Task Main(string[] args)
    {
        // Check for administrator privileges, which are required for cleaning system files.
        if (!IsAdministrator())
        {
            Console.WriteLine("This program must be run with administrator privileges to perform a full system cleanup.");
            return;
        }

        Console.WriteLine("Starting Windows System Cleanup...");
        Console.WriteLine("----------------------------------");

        // List of directories to clean. Environment.GetFolderPath is used for user-specific paths.
        string[] directoriesToClean = {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
            @"C:\Windows\Temp",
            @"C:\Windows\Prefetch"
        };

        foreach (var dir in directoriesToClean)
        {
            await ClearDirectoryAsync(dir);
        }

        Console.WriteLine("\nCleanup complete.");
    }

    /// <summary>
    /// Checks if the current process is running with administrator privileges.
    /// This is necessary for deleting files from system directories.
    /// </summary>
    /// <returns>True if running as administrator, otherwise false.</returns>
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

    /// <summary>
    /// Deletes all files and subdirectories from a given directory.
    /// Handles exceptions for files in use or permission denied.
    /// </summary>
    /// <param name="directoryPath">The full path of the directory to clean.</param>
    private static async Task ClearDirectoryAsync(string directoryPath)
    {
        // Use Task.Run to offload the I/O operation to a background thread, preventing the UI from freezing.
        await Task.Run(() =>
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Directory not found: {directoryPath}. Skipping.");
                return;
            }

            Console.WriteLine($"\nCleaning directory: {directoryPath}");
            int filesDeleted = 0;
            int directoriesDeleted = 0;

            // Delete all files in the directory
            try
            {
                var files = Directory.GetFiles(directoryPath);
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        filesDeleted++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Failed to delete file '{Path.GetFileName(file)}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error accessing directory files: {ex.Message}");
            }

            // Delete all subdirectories
            try
            {
                var subdirectories = Directory.GetDirectories(directoryPath);
                foreach (string subdir in subdirectories)
                {
                    try
                    {
                        Directory.Delete(subdir, true); // The 'true' parameter deletes recursively
                        directoriesDeleted++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Failed to delete subdirectory '{Path.GetFileName(subdir)}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error accessing subdirectories: {ex.Message}");
            }

            Console.WriteLine($"  Cleanup of '{directoryPath}' finished. Deleted {filesDeleted} files and {directoriesDeleted} directories.");
        });
    }
}
