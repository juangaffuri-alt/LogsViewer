using LogsViewer.Services.Contracts;

namespace LogsViewer.Services.Implementation.Raven.Models
{
    public class ApiKeyModel
    {
        public string Id
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string Key
        {
            get;
            set;
        }

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

        public ApiKeyModel()
        {
            this.Id = default!;
            this.Name = default!;
            this.Key = default!;
            this.Metadata = default!;
        }
    }
}
