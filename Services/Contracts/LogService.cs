using LogsViewer.Models;
using LogsViewer.Models.Filters;
using LogsViewer.Services.Contracts;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using LogLevel = LogsViewer.Models.Filters.LogLevel;

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
    private readonly IMemoryCache _memoryCache;

    // Serialización de las propiedades de cada log hacia PropertiesJson.
    private static readonly JsonSerializerOptions PropertiesJsonOptions = new() { WriteIndented = true };

    // Ventana de trabajo para operaciones que agregan/agrupan en memoria
    // (RavenDB no puede agrupar cómodamente por campos derivados como "Source").
    private const int WorkingSetSize = 10000;

    // Límites de seguridad: nunca pedir páginas gigantes ni contar working sets completos.
    private const int MaxPageSize = 200;
    private const int CountCacheTtlSeconds = 30;

    public LogService(IDocumentStore documentStore, ILogger<LogService> logger, IMemoryCache memoryCache)
    {
        _documentStore = documentStore;
        _logger = logger;
        _memoryCache = memoryCache;
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

    public async Task<List<LogViewModel>> GetRecentLogsAsync(int count, AdvancedSearchCriteria criteria)
    {
        // Solo la página 1 con los primeros "count": el stream hace Take en RavenDB
        // y deja de leer en cuanto se completan (antes traía 10.000 documentos).
        criteria.Page = 1;
        criteria.PageSize = Math.Clamp(count, 1, MaxPageSize);

        var logs = new List<LogViewModel>(criteria.PageSize);
        await foreach (var log in StreamFilteredLogsAsync(criteria))
            logs.Add(log);
        return logs;
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

    public async Task<LogStatisticsViewModel> GetStatisticsAsync(AdvancedSearchCriteria criteria)
    {
        // El dashboard pide stats + timeSeries + topErrors en cada request: cacheamos
        // brevemente por criterios para no escanear el working set 3 veces por golpe.
        return await _memoryCache.GetOrCreateAsync(
            BuildCountCacheKey(criteria) + "|stats",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CountCacheTtlSeconds);
                return await ComputeStatisticsAsync(criteria);
            }) ?? new LogStatisticsViewModel();
    }

    private async Task<LogStatisticsViewModel> ComputeStatisticsAsync(AdvancedSearchCriteria criteria)
    {
        // Contamos por niveles con una sola pasada (antes se recorría la lista 5 veces)
        // y solo extraemos el Level: no mapeamos el view model ni serializamos propiedades.
        var errorCount = 0;
        var warningCount = 0;
        var infoCount = 0;
        var debugCount = 0;
        var traceCount = 0;
        var total = 0;

        await foreach (var e in StreamMatchingEntitiesAsync(criteria))
        {
            total++;
            switch (LogLevelExtensions.FromQueryString(e.Level))
            {
                case LogLevel.Error:
                case LogLevel.Fatal: errorCount++; break;
                case LogLevel.Warning: warningCount++; break;
                case LogLevel.Information: infoCount++; break;
                case LogLevel.Debug: debugCount++; break;
                case LogLevel.Verbose: traceCount++; break;
            }
        }

        return new LogStatisticsViewModel
        {
            TotalLogs = total,
            ErrorCount = errorCount,
            WarningCount = warningCount,
            InfoCount = infoCount,
            DebugCount = debugCount,
            TraceCount = traceCount
        };
    }

    public async Task<List<TopErrorViewModel>> GetTopErrorsAsync(int limit, AdvancedSearchCriteria criteria)
    {
        // Solo nos interesan los errores: agrupamos en un diccionario durante una
        // sola pasada, sin materializar listas intermedias de 10.000 view models.
        var groups = new Dictionary<(string Message, string Source), int>();

        await foreach (var e in StreamMatchingEntitiesAsync(criteria))
        {
            var level = LogLevelExtensions.FromQueryString(e.Level);
            if (level != LogLevel.Error && level != LogLevel.Fatal)
                continue;

            var source = DeriveSource(e);
            var key = (e.Message ?? string.Empty, source);
            groups[key] = groups.TryGetValue(key, out var count) ? count + 1 : 1;
        }

        return groups
            .OrderByDescending(kv => kv.Value)
            .Take(limit)
            .Select(kv => new TopErrorViewModel
            {
                Message = kv.Key.Message,
                Source = kv.Key.Source,
                Count = kv.Value
            })
            .ToList();
    }

    public async Task<TimeSeriesDataViewModel> GetTimeSeriesDataAsync(AdvancedSearchCriteria criteria)
    {
        // Elegimos la granularidad según el rango
        var range = criteria.StartDate.HasValue
            ? DateTime.UtcNow - criteria.StartDate.Value
            : TimeSpan.FromHours(24);

        // Agrupamos en un diccionario durante una sola pasada (antes se recorria
        // la lista completa varias veces entre GroupBy y los 4 Count()).
        var buckets = new SortedDictionary<DateTime, int[]>(); // [error, warning, info, debug]

        await foreach (var e in StreamMatchingEntitiesAsync(criteria))
        {
            var local = e.Timestamp.LocalDateTime;
            var key = new DateTime(local.Year, local.Month, local.Day,
                local.Hour, local.Minute, 0, DateTimeKind.Local);

            if (!buckets.TryGetValue(key, out var counts))
                buckets[key] = counts = new int[4];

            switch (LogLevelExtensions.FromQueryString(e.Level))
            {
                case LogLevel.Error:
                case LogLevel.Fatal: counts[0]++; break;
                case LogLevel.Warning: counts[1]++; break;
                case LogLevel.Information: counts[2]++; break;
                default: counts[3]++; break; // Debug + Verbose
            }
        }

        return new TimeSeriesDataViewModel
        {
            Timestamps = buckets.Keys.Select(k => k.ToString("yyyy-MM-dd HH:mm")).ToList(),
            ErrorCounts = buckets.Values.Select(v => v[0]).ToList(),
            WarningCounts = buckets.Values.Select(v => v[1]).ToList(),
            InfoCounts = buckets.Values.Select(v => v[2]).ToList(),
            DebugCounts = buckets.Values.Select(v => v[3]).ToList()
        };
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
                .Where(x => string.Equals(x.Level.ToString(), level, StringComparison.OrdinalIgnoreCase))
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
            // Normalizamos la paginación: nunca páginas gigantes (el pageSize llegaba
            // directo desde el querystring y se usaba en Skip/Take sin validar).
            var page = Math.Max(1, criteria.Page);
            var pageSize = Math.Clamp(criteria.PageSize <= 0 ? 50 : criteria.PageSize, 1, MaxPageSize);
            criteria.Page = page;
            criteria.PageSize = pageSize;

            // Paginado ANTES de mapear: solo construimos los view models de la página
            // actual (y por tanto solo serializamos esas PropertiesJson), no los
            // ~10.000 del working set.
            return await StreamFilteredLogsAsync(criteria)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in advanced search");
            return new List<LogViewModel>();
        }
    }

    /// <summary>
    /// Deriva el "Source" de la entidad: Application por defecto; si no hay
    /// application, usa SourceContext o MachineName.
    /// </summary>
    private static string DeriveSource(LogEntity e)
    {
        var app = GetApplication(e);
        return app != "Unknown"
            ? app
            : (GetProperty(e, "SourceContext") ?? GetProperty(e, "MachineName") ?? "Unknown");
    }

    /// <summary>
    /// Convierte una LogEntity (lo que realmente guarda LogWriter) al LogViewModel
    /// que consumen las vistas.
    /// </summary>
    private static LogViewModel MapToViewModel(LogEntity entity, IAsyncDocumentSession session)
    {
        var application = GetApplication(entity);
        var sourceContext = GetProperty(entity, "SourceContext");

        // Source = Application por defecto; si querés la clase, cambiá esta línea.
        var source = DeriveSource(entity);

        // Serializamos las propiedades directo a PropertiesJson de forma perezosa:
        // el JSON se genera solo si alguien lo lee (la vista paginada o una API),
        // no para los ~10.000 logs del working set que nunca se muestran.
        LazyString? propertiesJson = null;
        if (entity.Properties != null && entity.Properties.Length > 0)
        {
            propertiesJson = new LazyString(() =>
            {
                var properties = entity.Properties.ToDictionary(p => p.Name, p => (object?)p.GetValueString());
                return JsonSerializer.Serialize(properties, PropertiesJsonOptions);
            });
        }

        return new LogViewModel
        {
            Id = session.Advanced.GetDocumentId(entity) ?? string.Empty,
            Application = application,
            SourceContext = sourceContext ?? "",
            Timestamp = entity.Timestamp.LocalDateTime,
            Level = LogLevelExtensions.FromQueryString(entity.Level),
            Message = entity.Message,
            Source = source,
            Exception = entity.Exception,
            StackTrace = entity.Exception,
            PropertiesJson = propertiesJson
        };
    }

    public async Task<int> AdvancedSearchCountAsync(AdvancedSearchCriteria criteria)
    {
        try
        {
            // Contamos sobre el stream filtrado sin materializar los view models de los
            // 10.000 logs; además cacheamos brevemente porque Index/SearchLogs pedían
            // la cuenta en cada request (y en el mismo request, junto a los resultados).
            return await _memoryCache.GetOrCreateAsync(
                BuildCountCacheKey(criteria),
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CountCacheTtlSeconds);
                    return (int?)await CountFilteredLogsAsync(criteria);
                }) ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting advanced search results");
            return 0;
        }
    }

    private string BuildCountCacheKey(AdvancedSearchCriteria criteria) =>
        $"logcount|{criteria.Application}|{criteria.Query}|{string.Join(",", criteria.Levels.OrderBy(l => l.ToString()))}" +
        $"|{string.Join(",", criteria.Sources.OrderBy(s => s))}|{criteria.StartDate:O}|{criteria.EndDate:O}";

    /// <summary>
    /// Cuenta los logs que matchean los criterios sobre las entidades crudas
    /// (sin construir LogViewModel ni PropertiesJson), con cache breve de 30s.
    /// </summary>
    private async ValueTask<int> CountFilteredLogsAsync(AdvancedSearchCriteria criteria)
    {
        var count = 0;
        await foreach (var _ in StreamMatchingEntitiesAsync(criteria))
            count++;
        return count;
    }

    /// <summary>
    /// Stream perezoso de los logs filtrados (ordenados por Timestamp desc.).
    /// No materializa la lista completa: quien consume puede Stop early (paginado,
    /// Take, etc.) y el mapeo/serialización solo ocurre para los elementos leídos.
    /// </summary>
    private async IAsyncEnumerable<LogViewModel> StreamFilteredLogsAsync(AdvancedSearchCriteria criteria)
    {
        using var session = _documentStore.OpenAsyncSession();
        var query = BuildEntityQuery(session, criteria);

        var selectedLevels = criteria.Levels.Any() ? criteria.Levels.ToHashSet() : null;
        var sources = criteria.Sources.Any() ? criteria.Sources.ToHashSet(StringComparer.Ordinal) : null;
        var application = string.IsNullOrWhiteSpace(criteria.Application) ? null : criteria.Application.Trim();
        var text = string.IsNullOrWhiteSpace(criteria.Query) ? null : criteria.Query.Trim();

        // Paginamos en el servidor: para requests de página pedimos solo hasta el
        // final de esa página, en vez de traer 10.000 documentos para descartarlos.
        // Para streams de conteo/agrupación (PageSize <= 0) usamos el working set completo.
        var take = criteria.PageSize > 0
            ? Math.Min(WorkingSetSize, Math.Max(1, criteria.Page) * Math.Clamp(criteria.PageSize, 1, MaxPageSize))
            : WorkingSetSize;

        await using var enumerator = ((IAsyncEnumerable<LogEntity>)query.Take(take)).GetAsyncEnumerator();

        while (await enumerator.MoveNextAsync())
        {
            var e = enumerator.Current;
            var vm = MapToViewModel(e, session);

            if (selectedLevels != null && !selectedLevels.Contains(vm.Level))
                continue;

            if (application != null && !string.Equals(vm.Application, application, StringComparison.OrdinalIgnoreCase))
                continue;

            if (sources != null && !sources.Contains(vm.Source))
                continue;

            if (text != null &&
                !vm.Message.Contains(text, StringComparison.OrdinalIgnoreCase) &&
                !vm.Source.Contains(text, StringComparison.OrdinalIgnoreCase))
                continue;

            yield return vm;
        }
    }

    /// <summary>
    /// Aplica al query de RavenDB los filtros indexables (rango de fechas) y deja
    /// el resto (texto, niveles, application, sources) para el filtrado fino en memoria.
    /// </summary>
    private static IRavenQueryable<LogEntity> BuildEntityQuery(IAsyncDocumentSession session, AdvancedSearchCriteria criteria)
    {
        IRavenQueryable<LogEntity> query = session.Query<LogEntity>();

        if (criteria.StartDate.HasValue)
            query = query.Where(x => x.Timestamp >= criteria.StartDate);
        if (criteria.EndDate.HasValue)
            query = query.Where(x => x.Timestamp <= criteria.EndDate);

        return query.OrderByDescending(x => x.Timestamp);
    }

    private static string? GetProperty(LogEntity entity, string name) => entity.Properties
        .Where(p => p.Name == name)
        .Select(p => p.GetValueString())
        .FirstOrDefault(v => !string.IsNullOrEmpty(v));

    private static string GetApplication(LogEntity entity) => GetProperty(entity, "Application") ?? "Unknown";

    /// <summary>
    /// Filtro común: decide si una entidad cruda matchea los niveles/application/sources
    /// pedidos. Devuelve el Source calculado o null si no matchea.
    /// </summary>
    private static string? MatchesFilters(
        LogEntity e,
        HashSet<LogLevel>? selectedLevels,
        HashSet<string>? sources,
        string? application,
        out LogLevel level)
    {
        level = LogLevelExtensions.FromQueryString(e.Level);
        if (selectedLevels != null && !selectedLevels.Contains(level))
            return null;

        var app = GetApplication(e);
        if (application != null && !string.Equals(app, application, StringComparison.OrdinalIgnoreCase))
            return null;

        var source = app != "Unknown" ? app : (GetProperty(e, "SourceContext") ?? GetProperty(e, "MachineName") ?? "Unknown");
        if (sources != null && !sources.Contains(source))
            return null;

        return source;
    }

    /// <summary>
    /// Stream perezoso de las entidades que matchean los criterios (incluido el filtro
    /// de texto sobre Message/Source). Lo usan conteos y agrupaciones: cero view models,
    /// cero JSON de propiedades.
    /// </summary>
    private async IAsyncEnumerable<LogEntity> StreamMatchingEntitiesAsync(AdvancedSearchCriteria criteria)
    {
        using var session = _documentStore.OpenAsyncSession();
        var query = BuildEntityQuery(session, criteria);

        var selectedLevels = criteria.Levels.Any() ? criteria.Levels.ToHashSet() : null;
        var sources = criteria.Sources.Any() ? criteria.Sources.ToHashSet(StringComparer.Ordinal) : null;
        var application = string.IsNullOrWhiteSpace(criteria.Application) ? null : criteria.Application.Trim();
        var text = string.IsNullOrWhiteSpace(criteria.Query) ? null : criteria.Query.Trim();

        await using var enumerator = ((IAsyncEnumerable<LogEntity>)query.Take(WorkingSetSize)).GetAsyncEnumerator();

        while (await enumerator.MoveNextAsync())
        {
            var e = enumerator.Current;
            var source = MatchesFilters(e, selectedLevels, sources, application, out _);
            if (source is null)
                continue;

            if (text != null)
            {
                var message = e.Message ?? string.Empty;
                if (!message.Contains(text, StringComparison.OrdinalIgnoreCase) &&
                    !source.Contains(text, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            yield return e;
        }
    }


    public async Task<List<string>> GetDistinctApplicationsAsync()
    {
        try
        {
            using var session = _documentStore.OpenAsyncSession();
            var entities = await session.Query<LogEntity>()
                .OrderByDescending(x => x.Timestamp)
                .Take(WorkingSetSize)
                .ToListAsync();

            // Solo necesitamos el Application: no mapeamos el view model completo
            // ni construimos el JSON de propiedades de cada log.
            return entities
                .Select(GetApplication)
                .Where(a => !string.IsNullOrEmpty(a) && a != "Unknown")
                .Distinct()
                .OrderBy(a => a)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting distinct applications");
            return new List<string>();
        }
    }

}