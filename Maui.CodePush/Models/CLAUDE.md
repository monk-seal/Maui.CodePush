# Models/

Data classes para configuracao, estado e comunicacao com servidor.

| Arquivo | Papel |
|---------|-------|
| `CodePushOptions.cs` | Config do consumidor: `ServerUrl`, `AppId`, `AppToken`, `ReleaseVersion`, `Channel`, `UpdatePolicy`, `AddModule()` |
| `ModuleManifest.cs` | Raiz do manifesto JSON persistido em `Modules/codepush-manifest.json` |
| `ModuleInfo.cs` | Estado por modulo: Version, Hash, PatchNumber, ReleaseVersion, Status. Ciclo: Embedded → Pending → Active → RolledBack |
| `UpdateCheckResult.cs` | Resposta do servidor. `Patches[]` (v2, por releaseVersion) + `Modules[]` (legacy). `ModuleUpdateInfo`: name, version, patchNumber, downloadUrl, hash, size, isMandatory |
