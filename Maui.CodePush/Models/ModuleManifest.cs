using System.Text.Json.Serialization;

namespace Maui.CodePush;

public class ModuleManifest
{
    [JsonPropertyName("appVersion")]
    public string AppVersion { get; set; } = "1.0.0";

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;

    [JsonPropertyName("lastChecked")]
    public DateTime? LastChecked { get; set; }

    [JsonPropertyName("modules")]
    public Dictionary<string, ModuleInfo> Modules { get; set; } = new();
}
