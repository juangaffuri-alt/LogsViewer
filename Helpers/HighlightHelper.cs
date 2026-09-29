using System.Text.RegularExpressions;
using System.Web;

namespace LogsViewer.Helpers;

public static class HighlightHelper
{
    /// <summary>
    /// Escapa el HTML y envuelve las coincidencias del término buscado
    /// en un &lt;mark class="hl"&gt;. Devuelve un IHtmlContent seguro.
    /// </summary>
    public static Microsoft.AspNetCore.Html.IHtmlContent Highlight(string? text, string? term)
    {
        if (string.IsNullOrEmpty(text))
            return Microsoft.AspNetCore.Html.HtmlString.Empty;

        // 1. Escapar HTML para evitar XSS
        var safe = HttpUtility.HtmlEncode(text);

        // 2. Si no hay término, devolver el texto escapado
        if (string.IsNullOrWhiteSpace(term))
            return new Microsoft.AspNetCore.Html.HtmlString(safe);

        // 3. Escapar el término para que sea literal en la regex
        var pattern = Regex.Escape(HttpUtility.HtmlEncode(term.Trim().Trim('"')));

        // 4. Reemplazar todas las coincidencias, ignorando mayúsculas
        var highlighted = Regex.Replace(
            safe,
            pattern,
            match => $"<mark class=\"hl\">{match.Value}</mark>",
            RegexOptions.IgnoreCase);

        return new Microsoft.AspNetCore.Html.HtmlString(highlighted);
    }
}