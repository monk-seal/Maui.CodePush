using Maui.CodePush.Server.Data.Entities;
using MongoDB.Driver;

namespace Maui.CodePush.Server.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration["MongoDB:ConnectionString"]
            ?? "mongodb://localhost:27017";
        var databaseName = configuration["MongoDB:DatabaseName"]
            ?? "codepush";

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<Account> Accounts => _database.GetCollection<Account>("accounts");
    public IMongoCollection<Subscription> Subscriptions => _database.GetCollection<Subscription>("subscriptions");
    public IMongoCollection<App> Apps => _database.GetCollection<App>("apps");
    public IMongoCollection<Release> Releases => _database.GetCollection<Release>("releases");

    public async Task EnsureIndexesAsync()
    {
        await Accounts.Indexes.CreateManyAsync([
            new CreateIndexModel<Account>(
                Builders<Account>.IndexKeys.Ascending(a => a.Email),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<Account>(
                Builders<Account>.IndexKeys.Ascending(a => a.ApiKey),
                new CreateIndexOptions { Unique = true })
        ]);

        await Apps.Indexes.CreateManyAsync([
            new CreateIndexModel<App>(
                Builders<App>.IndexKeys.Ascending(a => a.PackageName),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<App>(
                Builders<App>.IndexKeys.Ascending(a => a.AppToken),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<App>(
                Builders<App>.IndexKeys.Ascending(a => a.AccountId))
        ]);

        await Releases.Indexes.CreateManyAsync([
            new CreateIndexModel<Release>(
                Builders<Release>.IndexKeys.Ascending(r => r.AppId)),
            new CreateIndexModel<Release>(
                Builders<Release>.IndexKeys
                    .Ascending(r => r.AppId)
                    .Ascending(r => r.ModuleName)
                    .Ascending(r => r.Platform)
                    .Ascending(r => r.Channel)
                    .Descending(r => r.CreatedAt))
        ]);

        await Subscriptions.Indexes.CreateOneAsync(
            new CreateIndexModel<Subscription>(
                Builders<Subscription>.IndexKeys.Ascending(s => s.AccountId)));
    }
}
