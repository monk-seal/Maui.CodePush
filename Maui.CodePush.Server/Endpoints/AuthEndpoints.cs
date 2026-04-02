using System.Security.Claims;
using Maui.CodePush.Server.Data;
using Maui.CodePush.Server.Data.Entities;
using Maui.CodePush.Server.Services;
using MongoDB.Driver;

namespace Maui.CodePush.Server.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", Register);
        group.MapPost("/login", Login);
        group.MapGet("/me", GetMe).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        MongoDbContext db,
        TokenService tokenService)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Email, password, and name are required." });
        }

        var exists = await db.Accounts.Find(a => a.Email == request.Email).AnyAsync();
        if (exists)
            return Results.Conflict(new { error = "An account with this email already exists." });

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Name = request.Name,
            ApiKey = TokenService.GenerateRandomToken(),
            CreatedAt = DateTime.UtcNow
        };

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Status = SubscriptionStatus.Active,
            Plan = "pro",
            ExpiresAt = null,
            CreatedAt = DateTime.UtcNow
        };

        await db.Accounts.InsertOneAsync(account);
        await db.Subscriptions.InsertOneAsync(subscription);

        return Results.Ok(new
        {
            accountId = account.Id,
            email = account.Email,
            name = account.Name,
            apiKey = account.ApiKey
        });
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        MongoDbContext db,
        TokenService tokenService)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new { error = "Email and password are required." });

        var account = await db.Accounts.Find(a => a.Email == request.Email).FirstOrDefaultAsync();
        if (account is null || !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
            return Results.Unauthorized();

        var token = tokenService.GenerateJwtToken(account.Id, account.Email, account.Name);
        var expiresAt = DateTime.UtcNow.AddDays(7);

        return Results.Ok(new { token, expiresAt });
    }

    private static async Task<IResult> GetMe(ClaimsPrincipal user, MongoDbContext db)
    {
        var accountId = GetAccountId(user);
        if (accountId is null) return Results.Unauthorized();

        var account = await db.Accounts.Find(a => a.Id == accountId.Value).FirstOrDefaultAsync();
        if (account is null) return Results.NotFound();

        var subscription = await db.Subscriptions.Find(s => s.AccountId == accountId.Value).FirstOrDefaultAsync();

        return Results.Ok(new
        {
            accountId = account.Id,
            email = account.Email,
            name = account.Name,
            apiKey = account.ApiKey,
            createdAt = account.CreatedAt,
            subscription = subscription is null ? null : new
            {
                status = subscription.Status.ToString(),
                plan = subscription.Plan,
                expiresAt = subscription.ExpiresAt
            }
        });
    }

    internal static Guid? GetAccountId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public record RegisterRequest(string Email, string Password, string Name);
public record LoginRequest(string Email, string Password);
