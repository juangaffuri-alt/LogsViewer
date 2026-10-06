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
        /// Propiedades del log ya serializadas como JSON.
        /// Se genera en LogService (MapToViewModel), por lo que la vista/JS
        /// no necesitan volver a serializar ni conocer el modelo de propiedades.
        /// </summary>
        public string? PropertiesJson { get; set; }

        public bool HasProperties => !string.IsNullOrWhiteSpace(PropertiesJson)
                                     && PropertiesJson != "{}";

        public string? Exception { get; set; }

        public string? StackTrace { get; set; }
        public string Application { get; set; } = "Unknown";
        public string SourceContext { get; set; } = "";
    }
}
