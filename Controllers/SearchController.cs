using Microsoft.AspNetCore.Mvc;
using LogsViewer.Models;
using LogsViewer.Services.Contracts;

namespace LogsViewer.Controllers;

public class SearchController : Controller
{
    private readonly ILogService _logService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ILogService logService, ILogger<SearchController> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    /// <summary>
    /// Página de búsqueda avanzada
    /// </summary>
    public async Task<IActionResult> Index([FromQuery] string? q = null)
    {
        try
        {
            var model = new SearchViewModel
            {
                Query = q ?? string.Empty
            };

            if (!string.IsNullOrEmpty(q))
            {
                var startTime = DateTime.UtcNow;
                model.Results = await _logService.SearchLogsAsync(q);
                model.ExecutionTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                model.ResultCount = model.Results.Count;
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in search");
            return View(new SearchViewModel { HasError = true, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// API: Búsqueda avanzada con filtros
    /// </summary>
    [HttpPost("api/search")]
    public async Task<IActionResult> AdvancedSearch([FromBody] AdvancedSearchRequest request)
    {
        try
        {
            var startTime = DateTime.UtcNow;

            // TODO: Implementar búsqueda avanzada en el servicio
            var results = new List<LogViewModel>();

            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return Json(new
            {
                success = true,
                results,
                resultCount = results.Count,
                executionTimeMs = executionTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in advanced search");
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// API: Sugerencias de búsqueda (autocompletar)
    /// </summary>
    [HttpGet("api/search/suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] string? q = null, [FromQuery] int limit = 5)
    {
        try
        {
            var suggestions = new List<string>();

            if (!string.IsNullOrEmpty(q))
            {
                // TODO: Implementar sugerencias basadas en consultas previas
                suggestions.Add($"source:{q}*");
                suggestions.Add($"message:{q}*");
                suggestions.Add($"level:ERROR AND {q}");
            }

            return Json(new
            {
                success = true,
                suggestions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions");
            return Json(new { success = false, error = ex.Message });
        }
    }
}

public class AdvancedSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public string[]? Levels { get; set; }
    public string[]? Sources { get; set; }
    public string TimeRange { get; set; } = "24h";
    public int? Page { get; set; } = 1;
    public int? PageSize { get; set; } = 50;
}
