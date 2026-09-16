namespace LogsViewer.Services.Contracts;

public interface ILogDistributionServices
{
    Task<LogsDistribution> GetLogsDistribution(string query);
}
