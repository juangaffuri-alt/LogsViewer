namespace LogsViewer.Services.Contracts;

public record LogsDistribution(IReadOnlyList<LogsDistributionItem> Items, TimeSpan SliceInterval);
