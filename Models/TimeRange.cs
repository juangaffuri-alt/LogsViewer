namespace LogsViewer.Models
{
    // Models/TimeRange.cs
    public enum TimeRange
    {
        LastHour,
        Last24Hours,
        Last7Days,
        Last30Days,
        Last90Days,
        All
    }

    public static class TimeRangeExtensions
    {
        public static DateTime? GetFrom(this TimeRange range) => range switch
        {
            TimeRange.LastHour => DateTime.UtcNow.AddHours(-1),
            TimeRange.Last24Hours => DateTime.UtcNow.AddDays(-1),
            TimeRange.Last7Days => DateTime.UtcNow.AddDays(-7),
            TimeRange.Last30Days => DateTime.UtcNow.AddDays(-30),
            TimeRange.Last90Days => DateTime.UtcNow.AddDays(-90),
            TimeRange.All => null,
            _ => DateTime.UtcNow.AddDays(-7)
        };

        public static string ToDisplay(this TimeRange range) => range switch
        {
            TimeRange.LastHour => "Última hora",
            TimeRange.Last24Hours => "Últimas 24 horas",
            TimeRange.Last7Days => "Últimos 7 días",
            TimeRange.Last30Days => "Últimos 30 días",
            TimeRange.Last90Days => "Últimos 90 días",
            TimeRange.All => "Todo",
            _ => "Últimos 7 días"
        };
    }
    // Models/LogFilterPreferences.cs
    public class LogFilterPreferences
    {
        public string TimeRange { get; set; } = "24h";
        public List<string> Levels { get; set; } = new() { "ERROR", "WARNING", "INFO", "DEBUG" };
        public string? Application { get; set; } = null; // null = todas
    }

    public enum LogLevel
    {
        Verbose,
        Debug,
        Information,
        Warning,
        Error,
        Fatal
    }

    // Models/LogLevelInfo.cs
    public static class LogLevelInfo
    {
        public record LevelStyle(string Icon, string Color, string DisplayName);

        private static readonly Dictionary<string, LevelStyle> Styles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["VERBOSE"] = new("bi-three-dots", "#6c757d", "Verbose"),
            ["DEBUG"] = new("bi-bug", "#8e44ad", "Debug"),
            ["INFO"] = new("bi-info-circle", "#0d6efd", "Info"),
            ["WARNING"] = new("bi-exclamation-triangle", "#fd7e14", "Warning"),
            ["ERROR"] = new("bi-x-circle", "#dc3545", "Error"),
            ["FATAL"] = new("bi-skull", "#7f1d1d", "Fatal"),
        };

        public static LevelStyle Get(string level)
            => Styles.TryGetValue(level, out var s) ? s : new("bi-circle", "#6c757d", level);

        public static IReadOnlyCollection<string> All => Styles.Keys;
    }
}
