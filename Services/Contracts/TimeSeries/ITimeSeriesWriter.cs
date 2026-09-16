namespace LogsViewer.Services.Contracts.TimeSeries;

public interface ITimeSeriesWriter : IAsyncDisposable
{
    ValueTask Write(DateTimeOffset timestamp, double value, string? tag);
}
