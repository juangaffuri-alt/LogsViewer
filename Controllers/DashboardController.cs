using Microsoft.AspNetCore.Mvc;
using LogsViewer.Models;
using LogsViewer.Services.Contracts;

namespace LogsViewer.Controllers;

public class DashboardController : Controller
{
    private readonly ILogService _logService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ILogService logService, ILogger<DashboardController> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    /// <summary>
    /// Dashboard principal con gráficos y estadísticas
    /// </summary>
    public async Task<IActionResult> Index([FromQuery] string timeRange = "24h")
    {
        try
        {
            var stats = await _logService.GetStatisticsAsync();
            var recentLogs = await _logService.GetRecentLogsAsync(20);
            var timeSeriesData = await GetTimeSeriesDataAsync(timeRange);
            var topErrors = await GetTopErrorsAsync();

            var model = new DashboardViewModel
            {
                Statistics = MapToStatisticsViewModel(stats),
                RecentLogs = recentLogs.Select(MapToLogViewModel).ToList(),
                TimeSeriesData = timeSeriesData,
                TopErrors = topErrors
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            return View(new DashboardViewModel());
        }
    }

    /// <summary>
    /// API: Datos de línea de tiempo para gráficos
    /// </summary>
    [HttpGet("api/dashboard/timeline")]
    public async Task<IActionResult> GetTimeline([FromQuery] string timeRange = "24h")
    {
        try
        {
            var data = await GetTimeSeriesDataAsync(timeRange);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timeline");
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// API: Top errores
    /// </summary>
    [HttpGet("api/dashboard/top-errors")]
    public async Task<IActionResult> GetTopErrors([FromQuery] int limit = 10)
    {
        try
        {
            var errors = await GetTopErrorsAsync(limit);
            return Json(new { success = true, errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top errors");
            return Json(new { success = false, error = ex.Message });
        }
    }

    private async Task<TimeSeriesDataViewModel> GetTimeSeriesDataAsync(string timeRange)
    {
        // TODO: Implementar en el servicio
        return await Task.FromResult(new TimeSeriesDataViewModel());
    }

    private async Task<List<TopErrorViewModel>> GetTopErrorsAsync(int limit = 10)
    {
        // TODO: Implementar en el servicio
        return await Task.FromResult(new List<TopErrorViewModel>());
    }

    private LogViewModel MapToLogViewModel(object log) => new();
    private LogStatisticsViewModel MapToStatisticsViewModel(object stats) => new();
}
