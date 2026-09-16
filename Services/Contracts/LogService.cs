using LogsViewer.Models;
using LogsViewer.Services.Contracts;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace LogsViewer.Services.Implementation;

/// <summary>
/// Implementación de ILogService usando RavenDB.
/// Lee de la colección LogEntity, que es donde EventMiddleware/LogWriter
/// realmente persisten los eventos que llegan por /api/events/raw.
/// </summary>
public class LogService : ILogService
{
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<LogService> _logger;

    // Ventana de trabajo para operaciones que agregan/agrupan en memoria
    // (RavenDB no puede agrupar cómodamente por campos derivados como "Source").
    private const int WorkingSetSize = 10000;

    public LogService(IDocumentStore documentStore, ILogger<LogService> logger)
    {
        _documentStore = documentStore;
        _logger = logger;
    }

    public async Task<List<LogViewModel>> GetLogsAsync(int page = 1, int pageSize = 50)
    {
        try
        {
            using var session = _documentStore.OpenAsyncSession();
            var entities = await session.Query<LogEntity>()
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return entities.Select(e => MapToViewModel(e, session)).ToList();
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
            using var session = _documentStore.OpenAsyncSession();
            return await session.Query<LogEntity>().CountAsync();
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
            using var session = _documentStore.OpenAsyncSession();
            var entities = await session.Query<LogEntity>()
                .OrderByDescending(x => x.Timestamp)
                .Take(count)
                .ToListAsync();

            return entities.Select(e => MapToViewModel(e, session)).ToList();
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

            using var session = _documentStore.OpenAsyncSession();
            var entities = await session.Query<LogEntity>()
                .Where(x =>
                    x.Message.Contains(query) ||
                    (x.Exception != null && x.Exception.Contains(query)))
                .OrderByDescending(x => x.Timestamp)
                .Take(1000)
                .ToListAsync();

            return entities.Select(e => MapToViewModel(e, session)).ToList();
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
            using var session = _documentStore.OpenAsyncSession();
            var entities = await session.Query<LogEntity>()
                .OrderByDescending(x => x.Timestamp)
                .Take(WorkingSetSize)
                .ToListAsync();

            var logs = entities.Select(e => MapToViewModel(e, session)).ToList();

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

            using var session = _documentStore.OpenAsyncSession();
            var entities = await session.Query<LogEntity>()
                .Where(x => x.Timestamp >= startDate)
                .OrderBy(x => x.Timestamp)
                .ToListAsync();

            var logs = entities.Select(e => MapToViewModel(e, session)).ToList();

            var groupedData = logs
                .GroupBy(x => x.Timestamp.ToString("yyyy-MM-dd HH:00"))
                .OrderBy(g => g.Key)
                .ToList();

            return new TimeSeriesDataViewModel
            {
                Timestamps = groupedData.Select(g => g.Key).ToList(),
                ErrorCounts = groupedData.Select(g => g.Count(x => x.Level == "ERROR")).ToList(),
                WarningCounts = groupedData.Select(g => g.Count(x => x.Level == "WARNING")).ToList(),
                InfoCounts = groupedData.Select(g => g.Count(x => x.Level == "INFO")).ToList(),
                DebugCounts = groupedData.Select(g => g.Count(x => x.Level == "DEBUG")).ToList()
            };
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
            using var session = _documentStore.OpenAsyncSession();
            var entities = await session.Query<LogEntity>()
                .Where(x => x.LevelNumeric == (int)LogLevel.Error || x.LevelNumeric == (int)LogLevel.Critical)
                .OrderByDescending(x => x.Timestamp)
                .Take(1000)
                .ToListAsync();

            var errors = entities.Select(e => MapToViewModel(e, session)).ToList();

            return errors
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
            using var session = _documentStore.OpenAsyncSession();
            var entities = await session.Query<LogEntity>()
                .OrderByDescending(x => x.Timestamp)
                .Take(WorkingSetSize)
                .ToListAsync();

            return entities
                .Select(e => MapToViewModel(e, session))
                .Where(x => string.Equals(x.Level, level, StringComparison.OrdinalIgnoreCase))
                .Take(limit)
                .ToList();
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
            using var session = _documentStore.OpenAsyncSession();
            var entities = await session.Query<LogEntity>()
                .OrderByDescending(x => x.Timestamp)
                .Take(WorkingSetSize)
                .ToListAsync();

            return entities
                .Select(e => MapToViewModel(e, session))
                .Where(x => string.Equals(x.Source, source, StringComparison.OrdinalIgnoreCase))
                .Take(limit)
                .ToList();
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
            var logs = await GetFilteredLogsAsync(criteria);

            return logs
                .OrderByDescending(x => x.Timestamp)
                .Skip((criteria.Page - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in advanced search");
            return new List<LogViewModel>();
        }
    }

    /// <summary>
    /// Convierte una LogEntity (lo que realmente guarda LogWriter) al LogViewModel
    /// que consumen las vistas.
    /// </summary>
    private static LogViewModel MapToViewModel(LogEntity entity, IAsyncDocumentSession session)
    {
        string source = entity.Properties
            .Where(p => p.Name is "Application" or "SourceContext" or "MachineName")
            .Select(p => p.GetValueString())
            .FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? "Unknown";

        return new LogViewModel
        {
            Id = session.Advanced.GetDocumentId(entity) ?? string.Empty,
            Timestamp = entity.Timestamp.UtcDateTime,
            Level = MapLevelBucket(entity.LevelNumeric),
            Message = entity.Message,
            Source = source,
            Exception = entity.Exception,
            StackTrace = entity.Exception,
            Properties = entity.Properties.ToDictionary(p => p.Name, p => (object)p.GetValueString())
        };
    }

    /// <summary>
    /// Mapea el LogLevel numérico (seteado por ClefParser) a los buckets
    /// ERROR/WARNING/INFO/DEBUG/TRACE que usan las estadísticas y filtros.
    /// </summary>
    private static string MapLevelBucket(int levelNumeric) => (LogLevel)levelNumeric switch
    {
        LogLevel.Trace => "TRACE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Information => "INFO",
        LogLevel.Warning => "WARNING",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "ERROR",
        _ => "INFO"
    };

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
    public async Task<int> AdvancedSearchCountAsync(AdvancedSearchCriteria criteria)
    {
        try
        {
            var logs = await GetFilteredLogsAsync(criteria);
            return logs.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting advanced search results");
            return 0;
        }
    }

    /// <summary>
    /// Aplica todos los filtros de AdvancedSearchCriteria (menos paginado) y devuelve
    /// la lista completa resultante. Compartido entre AdvancedSearchAsync y AdvancedSearchCountAsync
    /// para que la cuenta y los resultados paginados siempre sean consistentes entre sí.
    /// </summary>
    private async Task<List<LogViewModel>> GetFilteredLogsAsync(AdvancedSearchCriteria criteria)
    {
        using var session = _documentStore.OpenAsyncSession();
        IRavenQueryable<LogEntity> query = session.Query<LogEntity>();

        if (criteria.StartDate.HasValue)
            query = query.Where(x => x.Timestamp >= criteria.StartDate);

        if (criteria.EndDate.HasValue)
            query = query.Where(x => x.Timestamp <= criteria.EndDate);

        var entities = await query
            .OrderByDescending(x => x.Timestamp)
            .Take(WorkingSetSize)
            .ToListAsync();

        var logs = entities.Select(e => MapToViewModel(e, session)).AsEnumerable();

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            logs = logs.Where(x =>
                x.Message.Contains(criteria.Query, StringComparison.OrdinalIgnoreCase) ||
                x.Source.Contains(criteria.Query, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.Levels.Any())
            logs = logs.Where(x => criteria.Levels.Contains(x.Level));

        if (criteria.Sources.Any())
            logs = logs.Where(x => criteria.Sources.Contains(x.Source));

        return logs.ToList();
    }
}