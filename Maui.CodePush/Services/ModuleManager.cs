using System.Security.Cryptography;
using System.Text.Json;

namespace Maui.CodePush;

public class ModuleManager
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _manifestPath;
    private ModuleManifest _manifest;

    internal ModuleManager(string basePath)
    {
        _manifestPath = Path.Combine(basePath, "codepush-manifest.json");
        _manifest = LoadManifest();
    }

    public ModuleManifest Manifest => _manifest;

    public ModuleInfo? GetModuleInfo(string moduleName)
    {
        _manifest.Modules.TryGetValue(moduleName, out var info);
        return info;
    }

    public void RegisterModule(string moduleName, string dllPath)
    {
        var hash = ComputeHash(dllPath);

        if (!_manifest.Modules.TryGetValue(moduleName, out var info))
        {
            info = new ModuleInfo();
            _manifest.Modules[moduleName] = info;
        }

        info.Hash = hash;
        info.Status = ModuleStatus.Embedded;
        SaveManifest();
    }

    public void MarkUpdated(string moduleName, string version, string dllPath)
    {
        var hash = ComputeHash(dllPath);

        if (!_manifest.Modules.TryGetValue(moduleName, out var info))
        {
            info = new ModuleInfo();
            _manifest.Modules[moduleName] = info;
        }

        info.PreviousHash = info.Hash;
        info.Hash = hash;
        info.Version = version;
        info.DownloadedAt = DateTime.UtcNow;
        info.Status = ModuleStatus.Pending;
        SaveManifest();
    }

    public void MarkApplied(string moduleName)
    {
        if (_manifest.Modules.TryGetValue(moduleName, out var info))
        {
            info.AppliedAt = DateTime.UtcNow;
            info.Status = ModuleStatus.Active;
            SaveManifest();
        }
    }

    public void MarkRolledBack(string moduleName)
    {
        if (_manifest.Modules.TryGetValue(moduleName, out var info))
        {
            info.Status = ModuleStatus.RolledBack;
            info.Hash = info.PreviousHash ?? string.Empty;
            info.PreviousHash = null;
            SaveManifest();
        }
    }

    public void UpdateLastChecked()
    {
        _manifest.LastChecked = DateTime.UtcNow;
        SaveManifest();
    }

    public void Reset()
    {
        _manifest = new ModuleManifest();
        SaveManifest();
    }

    public static string ComputeHash(string filePath)
    {
        if (!File.Exists(filePath))
            return string.Empty;

        using var stream = File.OpenRead(filePath);
        var hashBytes = SHA256.HashData(stream);
        return $"sha256:{Convert.ToHexString(hashBytes).ToLowerInvariant()}";
    }

    public static bool VerifyHash(string filePath, string expectedHash)
    {
        if (string.IsNullOrEmpty(expectedHash))
            return true;

        var actualHash = ComputeHash(filePath);
        return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    private ModuleManifest LoadManifest()
    {
        if (!File.Exists(_manifestPath))
            return new ModuleManifest();

        try
        {
            var json = File.ReadAllText(_manifestPath);
            return JsonSerializer.Deserialize<ModuleManifest>(json, _jsonOptions) ?? new ModuleManifest();
        }
        catch
        {
            return new ModuleManifest();
        }
    }

    private void SaveManifest()
    {
        try
        {
            var json = JsonSerializer.Serialize(_manifest, _jsonOptions);
            File.WriteAllText(_manifestPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CodePush] Failed to save manifest: {ex.Message}");
        }
    }
}
