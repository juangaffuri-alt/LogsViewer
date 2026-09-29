namespace LogsViewer.Models.Filters;

public static class LogLevelInfo
{
    public record LevelStyle(string Icon, string Color, string DisplayName);

    public static IReadOnlyCollection<LogLevel> All => Enum.GetValues<LogLevel>();

    private static readonly Dictionary<LogLevel, LevelStyle> Styles = new()
    {
        [LogLevel.Verbose] = new("bi-three-dots", "#6c757d", "Verbose"),
        [LogLevel.Debug] = new("bi-bug", "#8e44ad", "Debug"),
        [LogLevel.Information] = new("bi-info-circle", "#0d6efd", "Info"),
        [LogLevel.Warning] = new("bi-exclamation-triangle", "#fd7e14", "Warning"),
        [LogLevel.Error] = new("bi-x-circle", "#dc3545", "Error"),
        [LogLevel.Fatal] = new("bi-skull", "#7f1d1d", "Fatal"),
    };

    public static LevelStyle Get(LogLevel level)
        => Styles.TryGetValue(level, out var s)
            ? s
            : new("bi-circle", "#6c757d", level.ToString());
}
