using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Maui.CodePush.Server.Data.Entities;

public enum SubscriptionStatus
{
    Active,
    Inactive,
    Trial
}

public class Subscription
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonElement("accountId")]
    [BsonRepresentation(BsonType.String)]
    public Guid AccountId { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public SubscriptionStatus Status { get; set; }

    [BsonElement("plan")]
    public string Plan { get; set; } = string.Empty;

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
}
