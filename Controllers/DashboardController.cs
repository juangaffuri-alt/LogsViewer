using Microsoft.AspNetCore.Mvc;
using LogsViewer.Models;
using LogsViewer.Models.Filters;
using LogsViewer.Services;
using LogsViewer.Services.Contracts;

namespace LogsViewer.Controllers;

public class DashboardController : Controller
{
    private readonly ILogService _logService;
    private readonly ILogger<DashboardController> _logger;
    private readonly LogFilterPreferencesService _filterService;

    public DashboardController(
        ILogService logService,
        ILogger<DashboardController> logger,
        LogFilterPreferencesService filterService)
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

            var criteria = new AdvancedSearchCriteria
            {
                Application = prefs.Application,
                Levels = prefs.Levels.ToList(),
                StartDate = prefs.TimeRange.ToStartDate(),
                EndDate = DateTime.UtcNow
            };

            var stats = await _logService.GetStatisticsAsync(criteria);
            var recentLogs = await _logService.GetRecentLogsAsync(20, criteria);
            var timeSeriesData = await _logService.GetTimeSeriesDataAsync(criteria);
            var topErrors = await _logService.GetTopErrorsAsync(10, criteria);

            var model = new DashboardViewModel
            {
                Statistics = stats,
                RecentLogs = recentLogs,
                TimeSeriesData = timeSeriesData,
                TopErrors = topErrors,
                CurrentTimeRange = prefs.TimeRange
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
    public async Task<IActionResult> GetTimeline()
    {
        var prefs = _filterService.Get();
        var criteria = new AdvancedSearchCriteria
        {
            Application = prefs.Application,
            Levels = prefs.Levels.ToList(),
            StartDate = prefs.TimeRange.ToStartDate(),
            EndDate = DateTime.UtcNow
        };

        var data = await _logService.GetTimeSeriesDataAsync(criteria);
        return Json(new { success = true, data });
    }

    [HttpGet("api/dashboard/top-errors")]
    public async Task<IActionResult> GetTopErrors([FromQuery] int limit = 10)
    {
        var prefs = _filterService.Get();
        var criteria = new AdvancedSearchCriteria
        {
            Application = prefs.Application,
            Levels = prefs.Levels.ToList(),
            StartDate = prefs.TimeRange.ToStartDate(),
            EndDate = DateTime.UtcNow
        };

        var errors = await _logService.GetTopErrorsAsync(limit, criteria);
        return Json(new { success = true, errors });
    }
}