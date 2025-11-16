using System;
using System.Collections.Generic;
using System.IO;

namespace WinKnightUI
{
    public static class ActivityLogger
    {
        private static readonly string LogFilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "WinKnightReports", "activity_log.txt");

        public static void Log(string message)
        {
            string folder = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            File.AppendAllText(LogFilePath, $"{DateTime.Now}: {message}\n");
        }

        // ✔ FIXED: Now accepts ONLY 1 parameter (count)
        public static List<string> ReadLastEntries(int count)
        {
            if (!File.Exists(LogFilePath))
                return new List<string>();

            List<string> allLines = File.ReadAllLines(LogFilePath).ToList();

            return allLines.Count <= count
                ? allLines
                : allLines.Skip(allLines.Count - count).ToList();
        }
    }
}
