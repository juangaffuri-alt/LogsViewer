namespace LogsViewer.Models
{
    public class LogsPageViewModel
    {
        public List<LogViewModel> Logs { get; set; } = new();

        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 50;

        public int TotalCount { get; set; }

        public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

        public string? SearchQuery { get; set; }

        public List<string> SelectedLevels { get; set; } = new();

        public string TimeRange { get; set; } = "24h";
    }
}
