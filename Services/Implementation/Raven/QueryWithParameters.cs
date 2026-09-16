namespace LogsViewer.Services.Implementation.Raven;

public record QueryWithParameters(string Query, IReadOnlyDictionary<string, object> Parameters);