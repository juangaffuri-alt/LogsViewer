using LogsViewer.Models.Filters;
using LogsViewer.Services;
using Microsoft.AspNetCore.Mvc;

public class PreferencesController : Controller
{
    private readonly LogFilterPreferencesService _filterService;

    public PreferencesController(LogFilterPreferencesService filterService)
    {
        _filterService = filterService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetFilters(LogFilterPreferences prefs, string? returnUrl)
    {
        if (prefs.Levels == null || prefs.Levels.Count == 0)
            prefs.Levels = LogLevelExtensions.All.ToHashSet();

        if (string.IsNullOrWhiteSpace(prefs.Application))
            prefs.Application = null;

        _filterService.Set(prefs);
        return LocalRedirect(returnUrl ?? "/");
    }
}