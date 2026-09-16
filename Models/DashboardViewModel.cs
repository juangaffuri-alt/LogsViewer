namespace LogsViewer.Models;

public class DashboardViewModel
{
    public LogStatisticsViewModel Statistics { get; set; } = new();

    public List<LogViewModel> RecentLogs { get; set; } = new();

    public TimeSeriesDataViewModel TimeSeriesData { get; set; } = new();

    public List<TopErrorViewModel> TopErrors { get; set; } = new();

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
