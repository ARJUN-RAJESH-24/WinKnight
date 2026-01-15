using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WinKnightUI.Services
{
    public static class UpdateMonitorService
    {
        private static EventLog? updateLog;

        public static void StartMonitoring()
        {
            updateLog = new EventLog("Microsoft-Windows-WindowsUpdateClient/Operational");
            updateLog.EntryWritten += UpdateLog_EntryWritten;
            updateLog.EnableRaisingEvents = true;
        }

        private static void UpdateLog_EntryWritten(object sender, EntryWrittenEventArgs e)
        {
            // EventID 19 = Successful Windows Update install
            if (e.Entry.InstanceId == 19)
            {
                Task.Run(() =>
                {
                    try
                    {
                        RestoreGuardManager.CreateRestorePoint(
                            "Automatic Update Restore Point", 12, 100
                        );
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Failed automatic restore point: " + ex.Message);
                    }
                });
            }
        }
    }
}
