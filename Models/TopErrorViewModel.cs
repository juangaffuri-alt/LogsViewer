namespace LogsViewer.Models
{
    public class TopErrorViewModel
    {
        public string Message { get; set; } = string.Empty;

        public int Count { get; set; }

        public string Source { get; set; } = string.Empty;
    }
}
