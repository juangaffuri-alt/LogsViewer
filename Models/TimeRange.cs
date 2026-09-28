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
}
