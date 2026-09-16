using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogsViewer.Services.Configuration;

namespace LogsViewer.Infrastructure.App;

internal static class BackendConfiguratorFactory
{
    public static IBackendConfigurator Create(IConfiguration configuration, IFeatureManagement featureManagement)
    {
        LogStorageType storageType = configuration.GetValue<LogStorageType>("StorageType");

        return storageType switch
        {
            LogStorageType.RavenDb => new LogsViewer.Services.Implementation.Raven.Infrastructure.BackendConfigurator(featureManagement),
            LogStorageType.MongoDb => new LogsViewer.Services.Implementation.Mongo.Infrastructure.BackendConfigurator(featureManagement),
            _ => throw new InvalidProgramException($"Enum value {storageType} is not supported.")
        };
    }
}
