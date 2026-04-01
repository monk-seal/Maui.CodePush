using System.Text.Json;

namespace Maui.CodePush;

public class UpdateClient
{
    private readonly HttpClient _httpClient;
    private readonly CodePushOptions _options;
    private readonly string _tempPath;

    internal UpdateClient(CodePushOptions options, string tempPath)
    {
        _options = options;
        _tempPath = tempPath;
        _httpClient = new HttpClient();

        if (!string.IsNullOrEmpty(options.ServerUrl))
            _httpClient.BaseAddress = new Uri(options.ServerUrl.TrimEnd('/') + "/");
    }

    public async Task<UpdateCheckResult?> CheckForUpdatesAsync(
        string moduleName,
        string currentVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ServerUrl))
            return null;

        try
        {
            var platform = GetPlatform();
            var url = $"api/updates/check?app={Uri.EscapeDataString(_options.AppKey ?? "")}" +
                      $"&module={Uri.EscapeDataString(moduleName)}" +
                      $"&version={Uri.EscapeDataString(currentVersion)}" +
                      $"&platform={Uri.EscapeDataString(platform)}" +
                      $"&channel={Uri.EscapeDataString(_options.Channel)}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<UpdateCheckResult>(json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CodePush] Update check failed: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> DownloadModuleAsync(
        ModuleUpdateInfo updateInfo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tempFilePath = Path.Combine(_tempPath, $"{updateInfo.Name}.dll");

            using var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = File.Create(tempFilePath);
            await contentStream.CopyToAsync(fileStream, cancellationToken);
            fileStream.Close();

            // Verify hash
            if (!string.IsNullOrEmpty(updateInfo.Hash))
            {
                if (!ModuleManager.VerifyHash(tempFilePath, updateInfo.Hash))
                {
                    System.Diagnostics.Debug.WriteLine($"[CodePush] Hash mismatch for {updateInfo.Name}. Rejecting update.");
                    File.Delete(tempFilePath);
                    return null;
                }
            }

            return tempFilePath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CodePush] Download failed for {updateInfo.Name}: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> DownloadModuleFromUrlAsync(
        string moduleName,
        string downloadUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tempFilePath = Path.Combine(_tempPath, $"{moduleName}.dll");

            using var response = await _httpClient.GetAsync(downloadUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = File.Create(tempFilePath);
            await contentStream.CopyToAsync(fileStream, cancellationToken);

            return tempFilePath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CodePush] Download failed for {moduleName}: {ex.Message}");
            return null;
        }
    }

    private static string GetPlatform()
    {
#if ANDROID
        return "android";
#elif IOS
        return "ios";
#else
        return "unknown";
#endif
    }
}
