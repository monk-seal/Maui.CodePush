using Maui.CodePush.Server.Data;
using Maui.CodePush.Server.Data.Entities;
using MongoDB.Driver;

namespace Maui.CodePush.Server.Services;

public class SubscriptionService
{
    private readonly MongoDbContext _db;

    public SubscriptionService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsSubscriptionActiveAsync(Guid accountId)
    {
        var sub = await _db.Subscriptions
            .Find(s => s.AccountId == accountId)
            .FirstOrDefaultAsync();

        return sub?.Status is SubscriptionStatus.Active or SubscriptionStatus.Trial;
    }

    public Task UpdateSubscriptionFromWebhookAsync(string stripeEventJson)
    {
        // TODO: Parse Stripe event, update subscription status
        throw new NotImplementedException("Stripe integration pending");
    }
}
