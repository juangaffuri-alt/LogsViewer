namespace LogsViewer.Models.Filters;

public static class TimeRangeExtensions
{
    /// <summary>Fecha de inicio (UTC) para consultas a RavenDB.</summary>
    public static DateTime? ToStartDate(this TimeRange range) => range switch
    {
        TimeRange.LastHour => DateTime.UtcNow.AddHours(-1),
        TimeRange.Last24Hours => DateTime.UtcNow.AddDays(-1),
        TimeRange.Last7Days => DateTime.UtcNow.AddDays(-7),
        TimeRange.Last30Days => DateTime.UtcNow.AddDays(-30),
        TimeRange.Last90Days => DateTime.UtcNow.AddDays(-90),
        TimeRange.All => null,
        _ => DateTime.UtcNow.AddDays(-1)
    };

    /// <summary>Texto para mostrar en el &lt;select&gt; del layout.</summary>
    public static string ToDisplay(this TimeRange range) => range switch
    {
        TimeRange.LastHour => "Última hora",
        TimeRange.Last24Hours => "Últimas 24 horas",
        TimeRange.Last7Days => "Últimos 7 días",
        TimeRange.Last30Days => "Últimos 30 días",
        TimeRange.Last90Days => "Últimos 90 días",
        TimeRange.All => "Todo",
        _ => "Últimas 24 horas"
    };

    /// <summary>Valor corto para querystring / cookie / RQL.</summary>
    public static string ToQueryString(this TimeRange range) => range switch
    {
        TimeRange.LastHour => "1h",
        TimeRange.Last24Hours => "24h",
        TimeRange.Last7Days => "7d",
        TimeRange.Last30Days => "30d",
        TimeRange.Last90Days => "90d",
        TimeRange.All => "all",
        _ => "24h"
    };

    /// <summary>Parseo tolerante desde querystring / cookie.</summary>
    public static TimeRange FromQueryString(string? value) => value?.ToLowerInvariant() switch
    {
        "1h" => TimeRange.LastHour,
        "24h" => TimeRange.Last24Hours,
        "7d" => TimeRange.Last7Days,
        "30d" => TimeRange.Last30Days,
        "90d" => TimeRange.Last90Days,
        "all" => TimeRange.All,
        _ => TimeRange.Last24Hours
    };

    /// <summary>Todos los valores para iterar en vistas.</summary>
    public static IReadOnlyList<TimeRange> All => Enum.GetValues<TimeRange>();
}
