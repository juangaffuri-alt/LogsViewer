using Microsoft.AspNetCore.Mvc;
using LogsViewer.Models;
using LogsViewer.Services.Contracts;

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

    /// <summary>
    /// Página de Discover - Vista principal con estadísticas y logs recientes
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            var stats = await _logService.GetStatisticsAsync();
            var recentLogs = await _logService.GetRecentLogsAsync(10);

            var model = new DashboardViewModel
            {
                Statistics = MapToStatisticsViewModel(stats),
                RecentLogs = recentLogs.Select(MapToLogViewModel).ToList()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading home page");
            return View(new DashboardViewModel());
        }
    }

    private LogViewModel MapToLogViewModel(object log) => new();
    private LogStatisticsViewModel MapToStatisticsViewModel(object stats) => new();
}
