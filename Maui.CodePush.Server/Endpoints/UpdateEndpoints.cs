using Maui.CodePush.Server.Data;
using MongoDB.Driver;

namespace Maui.CodePush.Server.Endpoints;

public static class UpdateEndpoints
{
    public static RouteGroupBuilder MapUpdateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/updates").WithTags("Updates");

        group.MapGet("/check", CheckForUpdate);
        group.MapGet("/download/{releaseId:guid}", DownloadRelease);

        return group;
    }

    private static async Task<IResult> CheckForUpdate(
        HttpRequest request,
        MongoDbContext db,
        string app,
        string module,
        string version,
        string platform,
        string? channel = "production")
    {
        var appToken = request.Headers["X-CodePush-Token"].ToString();
        if (string.IsNullOrWhiteSpace(appToken))
            return Results.Unauthorized();

        if (!Guid.TryParse(app, out var appId))
            return Results.BadRequest(new { error = "Invalid app ID." });

        var appEntity = await db.Apps.Find(a => a.Id == appId && a.AppToken == appToken).FirstOrDefaultAsync();
        if (appEntity is null)
            return Results.Unauthorized();

        channel ??= "production";

        var latestRelease = await db.Releases
            .Find(r => r.AppId == appId
                && r.ModuleName == module
                && r.Platform == platform
                && r.Channel == channel)
            .SortByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestRelease is null || latestRelease.Version == version)
        {
            return Results.Ok(new
            {
                updateAvailable = false,
                modules = Array.Empty<object>()
            });
        }

        var baseUrl = $"{request.Scheme}://{request.Host}";

        return Results.Ok(new
        {
            updateAvailable = true,
            modules = new[]
            {
                new
                {
                    name = latestRelease.ModuleName,
                    version = latestRelease.Version,
                    downloadUrl = $"{baseUrl}/api/updates/download/{latestRelease.Id}",
                    hash = latestRelease.DllHash,
                    size = latestRelease.DllSize,
                    isMandatory = latestRelease.IsMandatory
                }
            }
        });
    }

    private static async Task<IResult> DownloadRelease(
        Guid releaseId,
        HttpRequest request,
        MongoDbContext db,
        IConfiguration configuration)
    {
        var appToken = request.Headers["X-CodePush-Token"].ToString();
        if (string.IsNullOrWhiteSpace(appToken))
            return Results.Unauthorized();

        var release = await db.Releases.Find(r => r.Id == releaseId).FirstOrDefaultAsync();
        if (release is null)
            return Results.NotFound();

        var appEntity = await db.Apps.Find(a => a.Id == release.AppId && a.AppToken == appToken).FirstOrDefaultAsync();
        if (appEntity is null)
            return Results.Unauthorized();

        var uploadsPath = configuration["Uploads:Path"] ?? "uploads";
        var filePath = Path.Combine(uploadsPath, release.AppId.ToString(), $"{releaseId}.dll");

        if (!File.Exists(filePath))
            return Results.NotFound(new { error = "Release file not found on disk." });

        var fileBytes = await File.ReadAllBytesAsync(filePath);
        return Results.File(fileBytes, "application/octet-stream", release.FileName);
    }
}
