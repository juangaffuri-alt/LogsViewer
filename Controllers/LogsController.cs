using Microsoft.AspNetCore.Mvc;
using LogsViewer.Models;
using LogsViewer.Services.Contracts;

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
        try
        {
            var model = new LogsPageViewModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                SearchQuery = query,
                TimeRange = timeRange,
                SelectedLevels = levels?.ToList() ?? new()
            };

            // TODO: Implementar búsqueda con filtros en el servicio
            model.Logs = await _logService.GetLogsAsync(page, pageSize);
            model.TotalCount = await _logService.GetLogsCountAsync();

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading logs");
            return View(new LogsPageViewModel());
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
            var logs = await _logService.GetLogsAsync(page, pageSize);
            var totalCount = await _logService.GetLogsCountAsync();

            return Json(new
            {
                success = true,
                logs = logs.Select(MapToLogViewModel),
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
            return Json(new
            {
                success = true,
                logs = logs.Select(MapToLogViewModel)
            });
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
            return Json(new
            {
                success = true,
                stats = MapToStatisticsViewModel(stats)
            });
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
            var logs = await _logService.GetLogsAsync(1, 10000); // Limitar exportación

            var csv = GenerateCsv(logs);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

            return File(bytes, "text/csv", $"logs-export-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting logs");
            return BadRequest(new { error = ex.Message });
        }
    }

    private string GenerateCsv(List<LogViewModel> logs)
    {
        // TODO: Implementar generación de CSV
        return "timestamp,level,source,message\n";
    }

    private LogViewModel MapToLogViewModel(object log) => new();
    private LogStatisticsViewModel MapToStatisticsViewModel(object stats) => new();
}

public class ExportLogsRequest
{
    public string? Query { get; set; }
    public string[]? Levels { get; set; }
    public string TimeRange { get; set; } = "24h";
}
