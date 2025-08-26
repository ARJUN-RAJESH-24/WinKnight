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
                Console.WriteLine("RestoreGuard is now watching for Windows Update events...");
                WatchWindowsUpdateEvents();

                Console.WriteLine("Press Enter to stop watching.");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Console.WriteLine("StackTrace: " + ex.StackTrace);
            }
        }

        // Watches for Windows Update start events
        public static void WatchWindowsUpdateEvents()
        {
            string query = @"
             SELECT * FROM __InstanceCreationEvent 
             WITHIN 5 
             WHERE TargetInstance ISA 'Win32_NTLogEvent' 
             AND TargetInstance.Logfile = 'Application' 
             AND TargetInstance.EventCode = '1234'
";


            ManagementEventWatcher watcher = new ManagementEventWatcher(query);
            watcher.EventArrived += (sender, e) =>
            {
                Console.WriteLine("Windows Update detected! Creating restore point...");
                try
                {
                    CreateRestorePoint("Before Windows Update", 12, 100);
                    Console.WriteLine("Restore point creation requested.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error creating restore point: " + ex.Message);
                }
            };

            watcher.Start();
        }

        // Creates a restore point
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

        // Lists all existing restore points
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
