namespace LogsViewer.Models
{
    public class LogViewModel
    {
        public string Id { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public string Level { get; set; } = string.Empty; // ERROR, WARNING, INFO, DEBUG

        public string Message { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public Dictionary<string, object>? Properties { get; set; }

        public string? Exception { get; set; }

        public string? StackTrace { get; set; }
    }
}
