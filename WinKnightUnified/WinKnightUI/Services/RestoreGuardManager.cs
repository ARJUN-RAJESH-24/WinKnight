using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace WinKnightUI.Services
{
    public static class RestoreGuardManager
    {
        public static void CreateRestorePoint(string description, int restoreType, int eventType)
        {
            var scope = new ManagementScope(@"\\localhost\root\default");
            scope.Connect();

            var restoreClass = new ManagementClass(scope, new ManagementPath("SystemRestore"), null);

            var result = restoreClass.InvokeMethod("CreateRestorePoint",
                new object[] { description, restoreType, eventType });

            if (result == null || Convert.ToInt32(result) != 0)
                throw new Exception("Failed to create restore point. Return code: " + result);
        }

        public static List<RestorePointModel> ListRestorePoints()
        {
            var scope = new ManagementScope(@"\\localhost\root\default");
            scope.Connect();

            var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM SystemRestore"));
            var list = new List<RestorePointModel>();

            foreach (ManagementObject rp in searcher.Get())
            {
                string description = rp["Description"]?.ToString() ?? "";
                string sourceType = description.StartsWith("Manual") ? "Manual" : "Automatic";

                list.Add(new RestorePointModel
                {
                    Description = description,
                    EventType = Convert.ToInt32(rp["EventType"]),
                    RestorePointType = Convert.ToInt32(rp["RestorePointType"]),
                    CreationTime = ConvertTimestamp(rp["CreationTime"]?.ToString()),
                    SourceType = sourceType
                });
            }

            return list.OrderByDescending(x => x.CreationTime).ToList();
        }

        private static DateTime ConvertTimestamp(string ts)
        {
            if (string.IsNullOrEmpty(ts)) return DateTime.MinValue;
            return DateTime.ParseExact(ts.Substring(0, 14), "yyyyMMddHHmmss", null);
        }

        public static void DeleteAllRestorePoints()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/C vssadmin delete shadows /all /quiet",
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true
                };
                Process.Start(psi)?.WaitForExit();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to delete restore points: " + ex.Message);
            }
        }
    }
}
