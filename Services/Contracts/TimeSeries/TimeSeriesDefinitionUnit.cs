namespace LogsViewer.Services.Contracts.TimeSeries;

public record TimeSeriesDefinitionUnit(string Id, string Name, string Query, string? ValueFieldName, string? TagFieldName, DateTime? LastExecuted);
