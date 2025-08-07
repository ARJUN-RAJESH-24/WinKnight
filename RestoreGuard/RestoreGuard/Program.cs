using System;
using System.Linq;
using System.Management;

namespace RestoreGuard
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Create a restore point
                CreateRestorePoint("Manual Restore Point", 0, 100);
                Console.WriteLine("\nRestore point creation requested.\n");

                // List existing restore points
                ListRestorePoints();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Console.WriteLine("StackTrace: " + ex.StackTrace);
            }

            Console.WriteLine("\nPress Enter to exit.");
            Console.ReadLine();
        }

        public static void CreateRestorePoint(string description, int restoreType, int eventType)
        {
            var scope = new ManagementScope(@"\\localhost\root\default");
            scope.Connect();

            var path = new ManagementPath("SystemRestore");
            var restoreClass = new ManagementClass(scope, path, null);

            var result = restoreClass.InvokeMethod("CreateRestorePoint", new object[] { description, restoreType, eventType });

            if (result == null || Convert.ToInt32(result) != 0)
                throw new Exception("Failed to create restore point. Return code: " + result);
        }

        public static void ListRestorePoints()
        {
            var scope = new ManagementScope(@"\\localhost\root\default");
            scope.Connect();

            var query = new ObjectQuery("SELECT * FROM SystemRestore");
            var searcher = new ManagementObjectSearcher(scope, query);

            var results = searcher.Get();

            if (!results.Cast<ManagementObject>().Any())
            {
                Console.WriteLine("No restore points found or System Restore may be disabled.");
                return;
            }

            Console.WriteLine("Existing Restore Points:\n");

            foreach (ManagementObject rp in results)
            {
                Console.WriteLine($"Description: {rp["Description"]}");
                Console.WriteLine($"Creation Time: {rp["CreationTime"]}");
                Console.WriteLine($"Event Type: {rp["EventType"]}");
                Console.WriteLine($"Restore Point Type: {rp["RestorePointType"]}");
                Console.WriteLine(new string('-', 40));
            }
        }
    }
}
