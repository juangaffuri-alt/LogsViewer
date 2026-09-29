namespace LogsViewer.Models.Filters;

public static class LogLevelExtensions
{
    /// <summary>Texto para mostrar en checkboxes y tablas.</summary>
    public static string ToDisplay(this LogLevel level) => level switch
    {
        LogLevel.Verbose => "Verbose",
        LogLevel.Debug => "Debug",
        LogLevel.Information => "Info",
        LogLevel.Warning => "Warning",
        LogLevel.Error => "Error",
        LogLevel.Fatal => "Fatal",
        _ => level.ToString()
    };

    /// <summary>Valor para querystring / cookie (coincide con lo que manda Serilog).</summary>
    public static string ToQueryString(this LogLevel level) => level switch
    {
        LogLevel.Verbose => "VERBOSE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Information => "INFO",
        LogLevel.Warning => "WARNING",
        LogLevel.Error => "ERROR",
        LogLevel.Fatal => "FATAL",
        _ => "INFO"
    };

    /// <summary>Parseo tolerante: acepta "informational", "INFO", "Error", etc.</summary>
    public static LogLevel FromQueryString(string? value)
    {
        var v = value?.Trim().ToLowerInvariant();
        return v switch
        {
            "verbose" => LogLevel.Verbose,
            "debug" => LogLevel.Debug,
            "info" => LogLevel.Information,
            "information" => LogLevel.Information,
            "informational" => LogLevel.Information,
            "warn" => LogLevel.Warning,
            "warning" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "fatal" => LogLevel.Fatal,
            "critical" => LogLevel.Fatal,
            _ => LogLevel.Information
        };
    }

    public static IReadOnlyList<LogLevel> All => Enum.GetValues<LogLevel>();
}
