// Services/LogFilterPreferencesService.cs
using LogsViewer.Models;
using LogsViewer.Services.Contracts;
using System.Text.Json;

namespace LogsViewer.Services;

public class LogFilterPreferencesService
{
    private const string CookieName = "logsviewer.filters";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceScopeFactory _scopeFactory;

    // 👇 Cache de aplicaciones
    private static List<string> _cachedApps = new();
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private static bool _isWarmedUp = false;

    public LogFilterPreferencesService(
        IHttpContextAccessor httpContextAccessor,
        IServiceScopeFactory scopeFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory = scopeFactory;
    }

    public LogFilterPreferences Get()
    {
        var raw = _httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
        if (string.IsNullOrEmpty(raw))
            return new LogFilterPreferences();

        try
        {
            return JsonSerializer.Deserialize<LogFilterPreferences>(raw)
                   ?? new LogFilterPreferences();
        }
        catch
        {
            return new LogFilterPreferences();
        }
    }

    public void Set(LogFilterPreferences prefs)
    {
        var json = JsonSerializer.Serialize(prefs);
        _httpContextAccessor.HttpContext?.Response.Cookies.Append(
            CookieName,
            json,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });
    }


    /// <summary>
    /// Devuelve la lista cacheada sin bloquear. Si está vencida, dispara
    /// un refresco en background y devuelve el valor actual (aunque sea viejo).
    /// </summary>

    public List<string> GetApplicationsCached()
    {
        // Primera vez: esperamos el refresco (bloqueante)
        if (!_isWarmedUp)
        {
            RefreshApplicationsAsync().GetAwaiter().GetResult();
            _isWarmedUp = true;
            return _cachedApps;
        }

        // Siguientes veces: disparo en background si expiró
        if (DateTime.UtcNow >= _cacheExpiry && _refreshLock.CurrentCount > 0)
            _ = RefreshApplicationsAsync();

        return _cachedApps;
    }

    private async Task RefreshApplicationsAsync()
    {
        if (!await _refreshLock.WaitAsync(0))
            return; // ya hay un refresco en curso

        try
        {
            // ⚠️ No podemos inyectar ILogService directamente (scoped),
            // así que creamos un scope nuevo
            using var scope = _scopeFactory.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<ILogService>();

            var apps = await logService.GetDistinctApplicationsAsync();

            _cachedApps = apps;
            _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
        }
        catch
        {
            // Si falla, extendemos la expiración un poco para no martillar
            _cacheExpiry = DateTime.UtcNow.AddMinutes(1);
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}