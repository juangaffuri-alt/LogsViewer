using LogsViewer.Services.Contracts;
using System.Text.Json.Serialization;

namespace LogsViewer.Services.Implementation.Raven.Models
{
    public class ApiKeySettingsModel
    {
        public string Id { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }

        public UserObjectMetadata Metadata { get; set; }

        public const string SingletonId = "ApiKeySettingsModels/global";

        public ApiKeySettingsModel()
        {
            this.Metadata = default!;
        }
    }
}
