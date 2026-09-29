using LogsViewer.Models;
using LogsViewer.Models.Filters;
using LogsViewer.Services;
using LogsViewer.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
namespace LogsViewer.Controllers;

public class HomeController : Controller
{
    private readonly ILogService _logService;
    private readonly LogFilterPreferencesService _filterService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ILogService logService,
        LogFilterPreferencesService filterService,
        ILogger<HomeController> logger)
    {
        _logService = logService;
        _filterService = filterService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Desarrollo"))
            return RedirectToAction("Index", "Dashboard");

        return View(); // vista pública descriptiva
    }
}