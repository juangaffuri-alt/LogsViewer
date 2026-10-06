using System.Text;
using Microsoft.AspNetCore.Html;

namespace LogsViewer.Helpers;

/// <summary>
/// Resalta un JSON ya formateado (WriteIndented) con spans de colores para
/// claves, strings, números y booleanos. Todo el texto de origen se escapa
/// HTML-encodeando carácter por carácter, salvo los tags que se emiten
/// desde una whitelist fija -> no hay riesgo de XSS aunque el JSON contenga
/// markup malicioso en sus valores.
/// </summary>
public static class JsonHighlighter
{
    public static IHtmlContent Highlight(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new HtmlString(string.Empty);

        var sb = new StringBuilder(json.Length + 64);
        var inString = false;
        var isKey = false;                // la string actual es clave (va antes del ':')
        var token = new StringBuilder();  // número o literal "en curso" (sin emitir todavía)

        void CloseToken(string cssClass)
        {
            if (token.Length == 0) return;
            sb.Append("<span class=\"").Append(cssClass).Append("\">")
              .Append(System.Net.WebUtility.HtmlEncode(token.ToString()))
              .Append("</span>");
            token.Clear();
        }

        for (int i = 0; i < json.Length; i++)
        {
            var c = json[i];

            if (inString)
            {
                // Cierre de string: puede venir escapada (\")
                if (c == '"' && i > 0 && json[i - 1] != '\\')
                {
                    sb.Append('"');
                    inString = false;
                    sb.Append("</span>");
                }
                else
                {
                    sb.Append(System.Net.WebUtility.HtmlEncode(c.ToString()));
                }
                continue;
            }

            switch (c)
            {
                case '"':
                    CloseToken("json-num");
                    CloseToken("json-lit");
                    // ¿es clave? Si el último carácter relevante del JSON es
                    // '{' o ',' (o estamos al principio), lo que sigue es una clave.
                    isKey = LastSignificant(sb) is '{' or ',' or '[' or '\0';
                    inString = true;
                    sb.Append(isKey ? "<span class=\"json-key\">" : "<span class=\"json-str\">");
                    break;

                case >= '0' and <= '9' or '-' or '+' or '.' or 'e' or 'E':
                    if (token.Length == 0 && !IsPrevNumberChar(LastSignificant(sb)))
                        CloseToken("json-lit");   // un dígito suelto no pertenece a un literal
                    if (token.Length > 0 || char.IsDigit(c) || c == '-')
                        token.Append(c);
                    else
                        sb.Append(c);            // '.'/exponente huérfanos fuera de número
                    break;

                case 't' or 'f' or 'n':
                    if (token.Length == 0 && !IsPrevNumberChar(LastSignificant(sb)))
                        token.Append(c);
                    else
                        sb.Append(c);
                    break;

                default:
                    CloseToken("json-num");
                    CloseToken("json-lit");
                    sb.Append(System.Net.WebUtility.HtmlEncode(c.ToString()));
                    break;
            }
        }

        CloseToken("json-num");
        CloseToken("json-lit");
        return new HtmlString(sb.ToString());
    }

    // Último carácter "relevante": recorre hacia atrás solo dentro del span
    // recién cerrado (\"</span>\"); si hay más markup, devuelve '\"' (fin de string).
    private static char LastSignificant(StringBuilder sb)
    {
        var n = sb.Length;
        if (n >= 7)
        {
            // "</span>" ocupa las 7 últimas posiciones -> mirar antes de él
            var endsWithClose = true;
            for (int k = 0; k < 7; k++)
                if (sb[n - 1 - k] != "</span>"[6 - k]) { endsWithClose = false; break; }
            if (endsWithClose)
                return sb[n - 8];
        }
        return n == 0 ? '\0' : sb[n - 1];
    }

    private static bool IsPrevNumberChar(char ch) =>
        char.IsDigit(ch) || ch is '.' or 'e' or 'E' or '+' or '-';
}
