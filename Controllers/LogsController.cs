using Microsoft.AspNetCore.Mvc;
using LogsViewer.Models;
using LogsViewer.Services.Contracts;
using System.Text;

namespace LogsViewer.Controllers;

public class LogsController : Controller
{
    private readonly ILogService _logService;
    private readonly ILogger<LogsController> _logger;

    public LogsController(ILogService logService, ILogger<LogsController> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    /// <summary>
    /// Página principal de visor de logs
    /// </summary>
    public async Task<IActionResult> Index(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? query = null,
        [FromQuery] string timeRange = "24h",
        [FromQuery] string[]? levels = null)
    {
        // La sidebar (ver _Layout.cshtml) muestra ERROR/WARNING/INFO/DEBUG tildados
        // por defecto. Si todavía no se mandó ningún "levels" en el querystring
        // (primera visita a /logs), aplicamos ese mismo default acá; si el
        // parámetro está presente (aunque venga vacío tras destildar todo), se
        // respeta lo que mandó el usuario.
        var selectedLevels = Request.Query.ContainsKey("levels")
            ? (levels?.ToList() ?? new())
            : new List<string> { "ERROR", "WARNING", "INFO", "DEBUG" };

        var model = new LogsPageViewModel
        {
            CurrentPage = page < 1 ? 1 : page,
            PageSize = pageSize <= 0 ? 50 : pageSize,
            SearchQuery = query,
            TimeRange = timeRange,
            SelectedLevels = selectedLevels
        };

        try
        {
            var criteria = BuildCriteria(model);

            model.Logs = await _logService.AdvancedSearchAsync(criteria);
            model.TotalCount = await _logService.AdvancedSearchCountAsync(criteria);

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading logs");
            return View(model);
        }
    }

    /// <summary>
    /// API: Obtener logs en JSON (AJAX)
    /// </summary>
    [HttpGet("api/logs/search")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> SearchLogs(
        [FromQuery] string? query = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string[]? levels = null,
        [FromQuery] string timeRange = "24h")
    {
        try
        {
            var tempModel = new LogsPageViewModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                SearchQuery = query,
                TimeRange = timeRange,
                SelectedLevels = levels?.ToList() ?? new()
            };
            var criteria = BuildCriteria(tempModel);

            var logs = await _logService.AdvancedSearchAsync(criteria);
            var totalCount = await _logService.AdvancedSearchCountAsync(criteria);

            return Json(new
            {
                success = true,
                logs,
                totalCount,
                totalPages = (totalCount + pageSize - 1) / pageSize,
                currentPage = page
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching logs");
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// API: Obtener logs recientes
    /// </summary>
    [HttpGet("api/logs/recent")]
    public async Task<IActionResult> RecentLogs([FromQuery] int count = 10)
    {
        try
        {
            var logs = await _logService.GetRecentLogsAsync(count);
            return Json(new { success = true, logs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent logs");
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// API: Obtener estadísticas de logs
    /// </summary>
    [HttpGet("api/logs/stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var stats = await _logService.GetStatisticsAsync();
            return Json(new { success = true, stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats");
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// API: Exportar logs en CSV
    /// </summary>
    [HttpPost("api/logs/export")]
    public async Task<IActionResult> ExportLogs([FromBody] ExportLogsRequest request)
    {
        try
        {
            var model = new LogsPageViewModel
            {
                CurrentPage = 1,
                PageSize = 10000, // Limitar exportación
                SearchQuery = request.Query,
                TimeRange = request.TimeRange,
                SelectedLevels = request.Levels?.ToList() ?? new()
            };
            var criteria = BuildCriteria(model);

            var logs = await _logService.AdvancedSearchAsync(criteria);

            var csv = GenerateCsv(logs);
            var bytes = Encoding.UTF8.GetBytes(csv);

            return File(bytes, "text/csv", $"logs-export-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting logs");
            return BadRequest(new { error = ex.Message });
        }
    }

    private static AdvancedSearchCriteria BuildCriteria(LogsPageViewModel model) => new()
    {
        Query = model.SearchQuery,
        Levels = model.SelectedLevels,
        StartDate = GetStartDateFromTimeRange(model.TimeRange),
        Page = model.CurrentPage,
        PageSize = model.PageSize
    };

    private static DateTime? GetStartDateFromTimeRange(string timeRange) => timeRange switch
    {
        "15m" => DateTime.UtcNow.AddMinutes(-15),
        "1h" => DateTime.UtcNow.AddHours(-1),
        "24h" => DateTime.UtcNow.AddHours(-24),
        "7d" => DateTime.UtcNow.AddDays(-7),
        "30d" => DateTime.UtcNow.AddDays(-30),
        _ => null // "custom" u otros valores no reconocidos: sin límite inferior
    };

    private static string GenerateCsv(List<LogViewModel> logs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("timestamp,level,source,message");

        foreach (var log in logs)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsvField(log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")),
                EscapeCsvField(log.Level),
                EscapeCsvField(log.Source),
                EscapeCsvField(log.Message)));
        }

        return sb.ToString();
    }

    private static string EscapeCsvField(string? field)
    {
        field ??= string.Empty;
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        return field;
    }
}

public class ExportLogsRequest
{
    public string? Query { get; set; }
    public string[]? Levels { get; set; }
    public string TimeRange { get; set; } = "24h";
}