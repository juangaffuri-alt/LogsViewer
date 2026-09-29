namespace LogsViewer.Models.Filters;

public class LogFilterPreferences
{
    public TimeRange TimeRange { get; set; } = TimeRange.Last24Hours;

    public HashSet<LogLevel> Levels { get; set; } = new()
    {
        LogLevel.Error,
        LogLevel.Warning,
        LogLevel.Information,
        LogLevel.Debug
    };

    public string? Application { get; set; } // null = todas
}
