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
        var isKey = false;          // la string actual es clave (va antes del ':')
        var numberStart = -1;
        var literalStart = -1;      // true / false / null fuera de comas

        void CloseNumber()
        {
            if (numberStart < 0) return;
            sb.Append("<span class=\"json-num\">")
              .Append(System.Net.WebUtility.HtmlEncode(json.AsSpan(numberStart)))
              .Append("</span>");
            numberStart = -1;
        }

        void CloseLiteral()
        {
            if (literalStart < 0) return;
            sb.Append("<span class=\"json-lit\">")
              .Append(System.Net.WebUtility.HtmlEncode(json.AsSpan(literalStart)))
              .Append("</span>");
            literalStart = -1;
        }

        for (int i = 0; i < json.Length; i++)
        {
            var c = json[i];

            if (inString)
            {
                // Cierre de string: puede venir escapada (\")
                if (c == '"' && i > 0 && json[i - 1] != '\\')
                {
                    sb.Append(System.Net.WebUtility.HtmlEncode(json.AsSpan(i, 1)));
                    inString = false;
                    sb.Append("</span>");
                }
                else
                {
                    sb.Append(System.Net.WebUtility.HtmlEncode(json.AsSpan(i, 1)));
                }
                continue;
            }

            switch (c)
            {
                case '"':
                    CloseNumber();
                    CloseLiteral();
                    // ¿es clave? Miramos hacia atrás: si el último token relevante
                    // es '{' o ',' entonces lo que sigue es una clave.
                    isKey = LastSignificant(sb.ToString()) is '{' or ',' or "";
                    inString = true;
                    sb.Append(isKey ? "<span class=\"json-key\">\"" : "<span class=\"json-str\">\"");
                    break;

                case >= '0' and <= '9' or '-' or '+':
                    if (numberStart < 0 && !IsInsidePrevToken(json, i))
                        numberStart = i;
                    CloseLiteral();
                    sb.Append(c);
                    break;

                case 't' or 'f' or 'n':
                    if (literalStart < 0) literalStart = i;
                    CloseNumber();
                    sb.Append(c);
                    break;

                default:
                    CloseNumber();
                    CloseLiteral();
                    sb.Append(System.Net.WebUtility.HtmlEncode(json.AsSpan(i, 1)));
                    break;
            }
        }

        CloseNumber();
        CloseLiteral();
        return new HtmlString(sb.ToString());
    }

    private static char? LastSignificant(string s)
    {
        for (int i = s.Length - 1; i >= 0; i--)
        {
            var ch = s[i];
            if (!char.IsWhiteSpace(ch)) return ch;
        }
        return null;
    }

    private static bool IsInsidePrevToken(string json, int i) =>
        i > 0 && (char.IsDigit(json[i - 1]) || json[i - 1] == '.' || json[i - 1] == 'e' || json[i - 1] == 'E');
}
