using LogsViewer.Models.Filters;

namespace LogsViewer.Models;

public class LogsPageViewModel
{
    public List<LogViewModel> Logs { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public string? SearchQuery { get; set; }

    // 👇 antes eran string / List<string>
    public List<Filters.LogLevel> SelectedLevels { get; set; } = new();
    public TimeRange TimeRange { get; set; } = TimeRange.Last24Hours;

    public string? Application { get; set; }
    public List<string> AvailableApplications { get; set; } = new();
}
