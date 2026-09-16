using LogsViewer.Services.Contracts;

namespace LogsViewer.Services.Implementation.Raven.Models
{
    public class ApiKeySettingsModel
    {
        public bool IsEnabled
        {
            get;
            set;
        }

        public UserObjectMetadata Metadata
        {
            get;
            set;
        }

        public ApiKeySettingsModel()
        {
            this.Metadata = default!;
        }
    }
}
