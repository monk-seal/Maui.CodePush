# Maui.CodePush (Biblioteca Principal)

NuGet: `CodePush.Maui` versao `0.1.0-pre`. TFM: `net9.0-android;net9.0-ios`.
Fornece o mecanismo de code push para apps .NET MAUI.

## Arquivos Raiz

| Arquivo | Papel |
|---------|-------|
| `CodePush.cs` | **Core engine**. `Initialize()` registra AssemblyResolve. `ResolveAssembly()`: temp -> base -> embedded. `CheckUpdatesAsync()` consulta servidor com `ReleaseVersion`, baixa patches. `Rollback()` reverte. |
| `AppBuilderExtensions.cs` | Extension `UseCodePush(Action<CodePushOptions>)`. Ponto de entrada. |
| `AssemblyRegister.cs` | HashSet interno de nomes de DLLs. |

## Pastas

### Models/
| Arquivo | Papel |
|---------|-------|
| `CodePushOptions.cs` | Config: `ServerUrl`, `AppId`, `AppToken`, `ReleaseVersion`, `Channel`, `UpdatePolicy`, `AddModule()` |
| `ModuleManifest.cs` | JSON persistido em `Modules/codepush-manifest.json` |
| `ModuleInfo.cs` | Estado por modulo: Version, Hash, PatchNumber, ReleaseVersion, Status (Embedded/Active/Pending/RolledBack) |
| `UpdateCheckResult.cs` | Resposta do servidor: `Patches[]` (novo) + `Modules[]` (legacy). Cada item: name, version, patchNumber, downloadUrl, hash |

### Services/
| Arquivo | Papel |
|---------|-------|
| `ModuleManager.cs` | Gerencia manifesto + SHA-256 + rollback |
| `UpdateClient.cs` | HTTP com `X-CodePush-Token`. Single call `CheckForUpdatesAsync()` envia `releaseVersion`, recebe patches para todos os modulos |

### Platforms/
- `Android/CodePushApplication.cs` — Base class para MainApplication
- `iOS/CodePushAppDelegate.cs` — Base class para AppDelegate (captura AssemblyName)

### Abstractions/
- `IAssemblyCollection.cs` — Interface interna

### build/
- `Maui.CodePush.props` — Implicit using
- `Maui.CodePush.targets` — Remove modulos do build (Android: AfterTargets _PrepareAssemblies, iOS: MtouchInterpreter + remove DLLs)

## API do Consumidor

```csharp
builder.UseCodePush(options =>
{
    options.AddModule("MyApp.Feature");
    options.ReleaseVersion = "1.0.0";    // versao da loja
    options.ServerUrl = "https://...";
    options.AppId = "guid";
    options.AppToken = "token";
});
```

## Regras
- Namespace: `Maui.CodePush`
- Logs: `[CodePush]` via `Debug.WriteLine`
- MSBuild targets: prefixo `CodePush_`
- Codigo platform-specific: `#if ANDROID` / `#elif IOS`
