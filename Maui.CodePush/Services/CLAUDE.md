# Services/

| Arquivo | Papel |
|---------|-------|
| `ModuleManager.cs` | Gerencia manifesto JSON, SHA-256 hash, transicoes de estado, rollback |
| `UpdateClient.cs` | HTTP client com `X-CodePush-Token`. `CheckForUpdatesAsync()` envia `releaseVersion` (single call), recebe patches para todos os modulos. `DownloadModuleAsync()` baixa e verifica hash |
