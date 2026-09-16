namespace LogsViewer.Models
{
    public class AdvancedSearchCriteria
    {
        public string? Query { get; set; }

        public List<string> Levels { get; set; } = new();

        public List<string> Sources { get; set; } = new();

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 50;
    }
}
