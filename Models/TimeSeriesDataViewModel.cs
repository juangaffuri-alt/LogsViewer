namespace LogsViewer.Models
{
    public class TimeSeriesDataViewModel
    {
        public List<string> Timestamps { get; set; } = new();

        public List<int> ErrorCounts { get; set; } = new();

        public List<int> WarningCounts { get; set; } = new();

        public List<int> InfoCounts { get; set; } = new();

        public List<int> DebugCounts { get; set; } = new();
    }
}
