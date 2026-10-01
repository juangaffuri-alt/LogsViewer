// Models/ApiKeysPageViewModel.cs
using LogsViewer.Services.Contracts;

namespace LogsViewer.Models;

public class ApiKeysPageViewModel
{
    public ApiKeySettings Settings { get; set; } = new(false);
    public IReadOnlyList<ApiKeyData> Keys { get; set; } = Array.Empty<ApiKeyData>();
}