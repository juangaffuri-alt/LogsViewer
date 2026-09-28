using LogsViewer.Models;
using LogsViewer.Services;
using LogsViewer.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace LogsViewer.Controllers;

public class HomeController : Controller
{
    private readonly ILogService _logService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogService logService, ILogger<HomeController> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var to = DateTime.UtcNow;
            var from = to.AddHours(-24);

            var stats = await _logService.GetStatisticsAsync(from, to);
            var recentLogs = await _logService.GetRecentLogsAsync(10);
            var timeSeriesData = await _logService.GetTimeSeriesDataAsync("24h");

            var model = new DashboardViewModel
            {
                Statistics = stats,
                RecentLogs = recentLogs,
                TimeSeriesData = timeSeriesData
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading home page");
            return View(new DashboardViewModel());
        }
    }
    
}