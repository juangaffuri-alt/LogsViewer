using LogsViewer.Services.Contracts;

namespace LogsViewer.Services.Implementation.Raven;

internal class FailedDistributedLock : IDistributedLock
{
    public bool Acquired
    {
        get => false;
    }

    public FailedDistributedLock()
    {

    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask();
    }
}