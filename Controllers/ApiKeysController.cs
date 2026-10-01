using LogsViewer.Models;
using LogsViewer.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogsViewer.Controllers;

[Authorize(Roles = "Desarrollo,Admin")]
public class ApiKeysController : Controller
{
    private readonly IApiKeyServices _apiKeyServices;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(IApiKeyServices apiKeyServices, ILogger<ApiKeysController> logger)
    {
        _apiKeyServices = apiKeyServices;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var model = new ApiKeysPageViewModel
        {
            Settings = await _apiKeyServices.GetSettings(ct),
            Keys = await _apiKeyServices.ListApiKeys(ct)
        };

        var settings = await _apiKeyServices.GetSettings(ct);
        _logger.LogWarning("DEBUG Controller Index: IsEnabled={IsEnabled}", settings.IsEnabled);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(bool enable, CancellationToken ct)
    {
        // 👇 Validación: no se puede habilitar sin al menos una key
        if (enable)
        {
            var keys = await _apiKeyServices.ListApiKeys(ct);
            if (keys == null || keys.Count == 0)
            {
                TempData["Error"] = "No podés habilitar las API keys sin tener al menos una creada.";
                return RedirectToAction(nameof(Index));
            }
        }

        await _apiKeyServices.StoreSettings(new ApiKeySettings(enable), ct);
        TempData["Message"] = enable
            ? "API keys habilitadas. Cualquier request requiere header X-Seq-ApiKey."
            : "API keys deshabilitadas. Cualquier request sin header X-Seq-ApiKey.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "El nombre es obligatorio.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _apiKeyServices.Create(name.Trim(), string.IsNullOrWhiteSpace(key) ? null : key.Trim(), ct);
            TempData["Message"] = $"API key '{name}' creada correctamente.";
        }
        catch (Exception ex)
        {
            // El servicio tira Exception genérica cuando el name o key ya existen
            _logger.LogWarning(ex, "Error creando API key {Name}", name);
            TempData["Error"] = $"No se pudo crear la API key. Verificá que el nombre y la key no existan ya.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        try
        {
            await _apiKeyServices.Delete(id, ct);
            TempData["Message"] = "API key eliminada.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando API key {Id}", id);
            TempData["Error"] = "No se pudo eliminar la API key.";
        }

        return RedirectToAction(nameof(Index));
    }
}
