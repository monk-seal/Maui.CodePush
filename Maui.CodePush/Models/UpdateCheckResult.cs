using System.Text.Json.Serialization;

namespace Maui.CodePush;

public class UpdateCheckResult
{
    [JsonPropertyName("updateAvailable")]
    public bool UpdateAvailable { get; set; }

    [JsonPropertyName("modules")]
    public List<ModuleUpdateInfo> Modules { get; set; } = new();
}

public class ModuleUpdateInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("isMandatory")]
    public bool IsMandatory { get; set; }
}
