using LogsViewer.Models;

namespace LogsViewer.Services
{
    // Services/TimeRangeService.cs
    public class TimeRangeService
    {
        private const string CookieName = "logsviewer.timerange";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TimeRangeService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public TimeRange Get()
        {
            var value = _httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
            return Enum.TryParse<TimeRange>(value, out var range) ? range : TimeRange.Last7Days;
        }

        public void Set(TimeRange range)
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Append(
                CookieName,
                range.ToString(),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax
                });
        }
    }
}
