namespace LogsViewer.Services.Contracts;

public interface IDistributedLock : IAsyncDisposable
{
    bool Acquired
    {
        get;
    }
}
