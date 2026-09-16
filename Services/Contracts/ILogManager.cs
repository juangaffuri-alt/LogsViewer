namespace LogsViewer.Services.Contracts;

public interface ILogManager
{
    Task RemoveOldLogs(DateTimeOffset timeAtDeletedLogs, CancellationToken cancellationToken);
}
