using System;

namespace WinKnightUI
{
    public class RestorePointModel
    {
        public string Description { get; set; } = string.Empty;
        public int EventType { get; set; }
        public int RestorePointType { get; set; }
        public DateTime CreationTime { get; set; }
        public string SourceType { get; set; } = string.Empty;
    }
}
