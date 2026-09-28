// Controllers/PreferencesController.cs
using LogsViewer.Models;
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
        // Si desmarcaste todos los levels, el binder deja la lista vacía o null.
        // Decidimos: "sin niveles seleccionados" = mostrar todos (o ninguno).
        // Aquí lo interpretamos como "todos" para que la UI no quede vacía.
        if (prefs.Levels == null || prefs.Levels.Count == 0)
            prefs.Levels = new List<string> { "VERBOSE", "DEBUG", "INFO", "WARNING", "ERROR", "FATAL" };

        _filterService.Set(prefs);
        return LocalRedirect(returnUrl ?? "/");
    }
}