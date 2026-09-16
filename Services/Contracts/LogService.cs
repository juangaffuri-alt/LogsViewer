using LogsViewer.Models;
using LogsViewer.Services.Contracts;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using System.Diagnostics;

namespace LogsViewer.Services.Implementation;

/// <summary>
/// Implementación de ILogService usando RavenDB
/// </summary>
public class LogService : ILogService
{
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<LogService> _logger;

    public LogService(IDocumentStore documentStore, ILogger<LogService> logger)
    {
        _documentStore = documentStore;
        _logger = logger;
    }

    public async Task<List<LogViewModel>> GetLogsAsync(int page = 1, int pageSize = 50)
    {
        try
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                var logs = await session.Query<LogViewModel>()
                    .OrderByDescending(x => x.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return logs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting logs");
            return new List<LogViewModel>();
        }
    }

    public async Task<int> GetLogsCountAsync()
    {
        try
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                var count = await session.Query<LogViewModel>()
                    .CountAsync();

                return count;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting logs");
            return 0;
        }
    }

    public async Task<List<LogViewModel>> GetRecentLogsAsync(int count = 10)
    {
        try
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                var logs = await session.Query<LogViewModel>()
                    .OrderByDescending(x => x.Timestamp)
                    .Take(count)
                    .ToListAsync();

                return logs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent logs");
            return new List<LogViewModel>();
        }
    }

    public async Task<List<LogViewModel>> SearchLogsAsync(string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<LogViewModel>();

            using (var session = _documentStore.OpenAsyncSession())
            {
                var logs = await session.Query<LogViewModel>()
                    .Where(x =>
                        x.Message.Contains(query) ||
                        x.Source.Contains(query) ||
                        (x.Exception != null && x.Exception.Contains(query)))
                    .OrderByDescending(x => x.Timestamp)
                    .Take(1000)
                    .ToListAsync();

                return logs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching logs with query: {query}", query);
            return new List<LogViewModel>();
        }
    }

    public async Task<LogStatisticsViewModel> GetStatisticsAsync()
    {
        try
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                var logs = await session.Query<LogViewModel>()
                    .OrderByDescending(x => x.Timestamp)
                    .Take(10000)
                    .ToListAsync();

                var stats = new LogStatisticsViewModel
                {
                    TotalLogs = logs.Count,
                    ErrorCount = logs.Count(x => x.Level == "ERROR"),
                    WarningCount = logs.Count(x => x.Level == "WARNING"),
                    InfoCount = logs.Count(x => x.Level == "INFO"),
                    DebugCount = logs.Count(x => x.Level == "DEBUG"),
                    TraceCount = logs.Count(x => x.Level == "TRACE"),
                    LogsBySource = logs
                        .GroupBy(x => x.Source)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    LogsByHour = logs
                        .GroupBy(x => x.Timestamp.ToString("yyyy-MM-dd HH:00"))
                        .OrderByDescending(g => g.Key)
                        .Take(24)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return stats;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics");
            return new LogStatisticsViewModel();
        }
    }

    public async Task<TimeSeriesDataViewModel> GetTimeSeriesDataAsync(string timeRange = "24h")
    {
        try
        {
            var startDate = GetStartDateFromTimeRange(timeRange);

            using (var session = _documentStore.OpenAsyncSession())
            {
                var logs = await session.Query<LogViewModel>()
                    .Where(x => x.Timestamp >= startDate)
                    .OrderBy(x => x.Timestamp)
                    .ToListAsync();

                var groupedData = logs
                    .GroupBy(x => x.Timestamp.ToString("yyyy-MM-dd HH:00"))
                    .OrderBy(g => g.Key)
                    .ToList();

                var model = new TimeSeriesDataViewModel
                {
                    Timestamps = groupedData.Select(g => g.Key).ToList(),
                    ErrorCounts = groupedData.Select(g => g.Count(x => x.Level == "ERROR")).ToList(),
                    WarningCounts = groupedData.Select(g => g.Count(x => x.Level == "WARNING")).ToList(),
                    InfoCounts = groupedData.Select(g => g.Count(x => x.Level == "INFO")).ToList(),
                    DebugCounts = groupedData.Select(g => g.Count(x => x.Level == "DEBUG")).ToList()
                };

                return model;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time series data");
            return new TimeSeriesDataViewModel();
        }
    }

    public async Task<List<TopErrorViewModel>> GetTopErrorsAsync(int limit = 10)
    {
        try
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                var errors = await session.Query<LogViewModel>()
                    .Where(x => x.Level == "ERROR")
                    .OrderByDescending(x => x.Timestamp)
                    .Take(1000)
                    .ToListAsync();

                var topErrors = errors
                    .GroupBy(x => x.Message)
                    .OrderByDescending(g => g.Count())
                    .Take(limit)
                    .Select(g => new TopErrorViewModel
                    {
                        Message = g.Key,
                        Count = g.Count(),
                        Source = g.FirstOrDefault()?.Source ?? "Unknown"
                    })
                    .ToList();

                return topErrors;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top errors");
            return new List<TopErrorViewModel>();
        }
    }

    public async Task<List<LogViewModel>> GetLogsByLevelAsync(string level, int limit = 100)
    {
        try
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                var logs = await session.Query<LogViewModel>()
                    .Where(x => x.Level == level)
                    .OrderByDescending(x => x.Timestamp)
                    .Take(limit)
                    .ToListAsync();

                return logs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting logs by level: {level}", level);
            return new List<LogViewModel>();
        }
    }

    public async Task<List<LogViewModel>> GetLogsBySourceAsync(string source, int limit = 100)
    {
        try
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                var logs = await session.Query<LogViewModel>()
                    .Where(x => x.Source == source)
                    .OrderByDescending(x => x.Timestamp)
                    .Take(limit)
                    .ToListAsync();

                return logs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting logs by source: {source}", source);
            return new List<LogViewModel>();
        }
    }

    public async Task<List<LogViewModel>> AdvancedSearchAsync(AdvancedSearchCriteria criteria)
    {
        try
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                // Iniciar la consulta con IRavenQueryable
                IRavenQueryable<LogViewModel> query = session.Query<LogViewModel>();

                // Filtro de texto
                if (!string.IsNullOrWhiteSpace(criteria.Query))
                {
                    query = query.Where(x =>
                        x.Message.Contains(criteria.Query) ||
                        x.Source.Contains(criteria.Query));
                }

                // Filtro por niveles
                if (criteria.Levels.Any())
                {
                    query = query.Where(x => criteria.Levels.Contains(x.Level));
                }

                // Filtro por fuentes
                if (criteria.Sources.Any())
                {
                    query = query.Where(x => criteria.Sources.Contains(x.Source));
                }

                // Filtro por rango de fechas
                if (criteria.StartDate.HasValue)
                {
                    query = query.Where(x => x.Timestamp >= criteria.StartDate);
                }

                if (criteria.EndDate.HasValue)
                {
                    query = query.Where(x => x.Timestamp <= criteria.EndDate);
                }

                var logs = await query
                    .OrderByDescending(x => x.Timestamp)
                    .Skip((criteria.Page - 1) * criteria.PageSize)
                    .Take(criteria.PageSize)
                    .ToListAsync();

                return logs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in advanced search");
            return new List<LogViewModel>();
        }
    }

    /// <summary>
    /// Convierte rango de tiempo a fecha de inicio
    /// </summary>
    private DateTime GetStartDateFromTimeRange(string timeRange)
    {
        return timeRange switch
        {
            "15m" => DateTime.UtcNow.AddMinutes(-15),
            "1h" => DateTime.UtcNow.AddHours(-1),
            "24h" => DateTime.UtcNow.AddHours(-24),
            "7d" => DateTime.UtcNow.AddDays(-7),
            "30d" => DateTime.UtcNow.AddDays(-30),
            _ => DateTime.UtcNow.AddHours(-24)
        };
    }
}
