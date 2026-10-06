namespace LogsViewer.Models
{
    public class LogViewModel
    {
        public string Id { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public LogsViewer.Models.Filters.LogLevel Level { get; set; }

        public string Message { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// JSON con las propiedades del log. Es "perezoso": solo se serializa
        /// cuando alguien lo lee (la vista o el Json() de una API), no para
        /// los ~10.000 logs del working set que nunca se muestran.
        /// </summary>
        public LazyString? PropertiesJson { get; set; }

        public string? Exception { get; set; }

        public string? StackTrace { get; set; }
        public string Application { get; set; } = "Unknown";
        public string SourceContext { get; set; } = "";
    }
}
