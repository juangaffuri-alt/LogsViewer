// Services/LogFilterPreferencesService.cs
using LogsViewer.Models;
using System.Text.Json;

namespace LogsViewer.Services;

public class LogFilterPreferencesService
{
    private const string CookieName = "logsviewer.filters";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LogFilterPreferencesService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
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
}