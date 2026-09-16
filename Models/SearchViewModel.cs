namespace LogsViewer.Models;

public class SearchViewModel
{
    public string Query { get; set; } = string.Empty;

    public List<LogViewModel> Results { get; set; } = new();

    public int ResultCount { get; set; }

    public double ExecutionTimeMs { get; set; }

    public List<string> Suggestions { get; set; } = new();

    public bool HasError { get; set; }

    public string? ErrorMessage { get; set; }
}
