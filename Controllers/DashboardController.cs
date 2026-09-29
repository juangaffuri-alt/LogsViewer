using Microsoft.AspNetCore.Mvc;
using LogsViewer.Models;
using LogsViewer.Services.Contracts;
using LogsViewer.Services;
using LogsViewer.Models.Filters;

namespace LogsViewer.Controllers;

public class DashboardController : Controller
{
    private readonly ILogService _logService;
    private readonly ILogger<DashboardController> _logger;
    private readonly LogFilterPreferencesService _filterService;

    public DashboardController(ILogService logService, ILogger<DashboardController> logger, LogFilterPreferencesService filterService)
    {
        _logService = logService;
        _logger = logger;
        _filterService = filterService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var prefs = _filterService.Get();
            var from = prefs.TimeRange.ToStartDate();
            var to = DateTime.UtcNow;

            var stats = await _logService.GetStatisticsAsync(from ?? DateTime.MinValue, to);
            var recentLogs = await _logService.GetRecentLogsAsync(20);
            var timeSeriesData = await _logService.GetTimeSeriesDataAsync(prefs.TimeRange.ToQueryString());
            var topErrors = await _logService.GetTopErrorsAsync(10, from ?? DateTime.MinValue, to);

            var model = new DashboardViewModel
            {
                Statistics = stats,
                RecentLogs = recentLogs,
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

    [HttpGet("api/dashboard/timeline")]
    public async Task<IActionResult> GetTimeline([FromQuery] string timeRange = "24h")
    {
        var range = TimeRangeExtensions.FromQueryString(timeRange);
        var data = await _logService.GetTimeSeriesDataAsync(range.ToQueryString());
        return Json(new { success = true, data });
    }

    [HttpGet("api/dashboard/top-errors")]
    public async Task<IActionResult> GetTopErrors([FromQuery] int limit = 10, [FromQuery] string timeRange = "24h")
    {
        var (from, to) = ParseTimeRange(timeRange);
        var errors = await _logService.GetTopErrorsAsync(limit, from, to);
        return Json(new { success = true, errors });
    }

    private static (DateTime from, DateTime to) ParseTimeRange(string timeRange)
    {
        var to = DateTime.UtcNow;
        var from = timeRange switch
        {
            "15m" => to.AddMinutes(-15),
            "1h" => to.AddHours(-1),
            "24h" => to.AddHours(-24),
            "7d" => to.AddDays(-7),
            "30d" => to.AddDays(-30),
            _ => to.AddHours(-24)
        };
        return (from, to);
    }
}