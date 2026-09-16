namespace LogsViewer.Models
{
    public class LogStatisticsViewModel
    {
        public int TotalLogs { get; set; }

        public int ErrorCount { get; set; }

        public int WarningCount { get; set; }

        public int InfoCount { get; set; }

        public int DebugCount { get; set; }

        public int TraceCount { get; set; }

        public Dictionary<string, int> LogsBySource { get; set; } = new();

        public Dictionary<string, int> LogsByHour { get; set; } = new();

        public double ErrorPercentage => TotalLogs > 0 ? (ErrorCount * 100.0) / TotalLogs : 0;

        public double WarningPercentage => TotalLogs > 0 ? (WarningCount * 100.0) / TotalLogs : 0;
    }
}
