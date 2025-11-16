using System;

namespace WinKnightUI
{
    public class RestorePointModel
    {
        public string Description { get; set; }
        public int EventType { get; set; }
        public int RestorePointType { get; set; }
        public DateTime CreationTime { get; set; }
        public string SourceType { get; set; }
    }
}
