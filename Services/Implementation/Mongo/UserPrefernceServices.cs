using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogsViewer.Services.Configuration;
using LogsViewer.Services.Contracts;
using LogsViewer.Services.Implementation.Mongo.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace LogsViewer.Services.Implementation.Mongo;

public class UserPrefernceServices : IUserPrefernceServices
{
    private readonly IMongoDatabase mongoDatabase;
    private readonly ILogger<UserPrefernceServices> logger;

    public UserPrefernceServices(IMongoDatabase mongoDatabase, ILogger<UserPrefernceServices> logger)
    {
        this.mongoDatabase = mongoDatabase;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<SturedQuery>> GetQueries(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetQueries.");

        IMongoCollection<MongoUserPrefernce> collection = this.mongoDatabase.GetCollection<MongoUserPrefernce>(CollectionNames.MongoUserPrefernce);
        MongoUserPrefernce? prefernce = await collection.AsQueryable().Where(t => t.Metadata.CreatedById == "System").SingleOrDefaultAsync(cancellationToken);

        if (prefernce == null)
        {
            return new List<SturedQuery>();
        }
        else
        {
            return prefernce.SturedQuerys;
        }
    }

    public async Task SaveQueries(List<SturedQuery> queries, CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to GetQueries.");

        IMongoCollection<MongoUserPrefernce> collection = this.mongoDatabase.GetCollection<MongoUserPrefernce>(CollectionNames.MongoUserPrefernce);

        MongoUserPrefernce? prefernce = await collection.AsQueryable().Where(t => t.Metadata.CreatedById == "System").SingleOrDefaultAsync(cancellationToken);

        if (prefernce == null)
        {
            prefernce = new MongoUserPrefernce()
            {
                Metadata = new UserObjectMetadata()
                {
                    Created = DateTime.UtcNow,
                    CreatedBy = "System",
                    CreatedById = "System"
                },
                SturedQuerys = queries
            };

            await collection.InsertOneAsync(prefernce, null, cancellationToken);
        }
        else
        {

            await collection.UpdateOneAsync(t => t.Id == prefernce.Id,
                  Builders<MongoUserPrefernce>.Update.Set(t => t.SturedQuerys, queries),
                  null,
                  cancellationToken);
        }

        this.logger.LogInformation("Save stored queries.");
    }
}
