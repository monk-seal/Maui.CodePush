# Maui.CodePush.Cli

CLI tool para gerenciar releases e patches CodePush.
NuGet: `dotnet-codepush`. Comando: `codepush`. TFM: `net9.0`.

## Comandos

| Comando | Descricao |
|---------|-----------|
| `codepush login` | Autentica no servidor, salva token em .codepush.json |
| `codepush init` | Cria .codepush.json auto-detectando csproj |
| `codepush apps list` | Lista apps no servidor |
| `codepush apps add` | Registra app (gera AppToken) |
| `codepush release create` | Cria release (versao da loja): publish app + captura deps + upload + git tag |
| `codepush release list` | Lista releases no servidor |
| `codepush release [paths]` | Deploy rapido via servidor ou adb (legacy) |
| `codepush patch` | Cria patch: build modulo + check deps + upload + git tag |
| `codepush devices` | Lista devices Android conectados |
| `codepush rollback` | Remove updates do device (adb) |
| `codepush update` | Self-update da CLI (`--pre` para pre-releases) |

## Opcoes Globais Importantes

| Opcao | Disponivel em | Descricao |
|-------|---------------|-----------|
| `--dotnet-args` | release create, release, patch | Args extras para `dotnet publish/build` (ex: `/p:AndroidSigningKeyPass=x`) |
| `--no-git-tag` | release create, patch | Pula criacao de git tag |
| `--local` | release | Forca deploy via adb em vez de servidor |
| `--platform` | todos | android ou ios |
| `--configuration` | release create, release, patch | Debug ou Release |

## Estrutura

```
Program.cs                       — Entry point, banner + 8 subcommands
appsettings.json                 — ServerUrl placeholder (injetado no CI)
Commands/
  LoginCommand.cs                — Auth no servidor, salva token/apiKey
  InitCommand.cs                 — Auto-detect de csproj, cria .codepush.json
  AppsCommand.cs                 — Subcommands: list, add (--set-default)
  ReleaseCommand.cs              — Deploy (server/adb) + subcommands: create, list
  PatchCommand.cs                — Build + check deps + upload patch + git tag
  DevicesCommand.cs              — Lista devices via AdbService
  RollbackCommand.cs             — Remove updates via adb
  UpdateCommand.cs               — Self-update via dotnet tool update
Services/
  ServerClient.cs                — HTTP client: auth, apps, releases v2, patches, legacy releases
  AdbService.cs                  — Encontra adb, push/remove files, restart app
  ProjectBuilder.cs              — Wrapper dotnet build com --dotnet-args
  ConfigManager.cs               — Leitura/escrita .codepush.json, auto-detect csproj
  DependencyAnalyzer.cs          — MetadataLoadContext: le assembly refs, check compatibilidade
  GitService.cs                  — Cria e pusha tags (release + patch)
  ConsoleUI.cs                   — Banner, cores ANSI, spinners, tabelas, icons
  CliSettings.cs                 — Le ServerUrl do appsettings.json embutido
Models/
  CodePushConfig.cs              — .codepush.json: packageName, serverUrl, appId, token, apiKey, modules
```

## Fluxo de Release

```
codepush release create --version 1.0.0 --app-project App.csproj
  1. dotnet publish do app (com --dotnet-args extras)
  2. Build de cada modulo
  3. DependencyAnalyzer extrai assembly refs de cada DLL
  4. Upload DLLs + snapshot para servidor
  5. Git tag: v1.0.0
```

## Fluxo de Patch

```
codepush patch --release 1.0.0
  1. Build do modulo
  2. DependencyAnalyzer extrai refs do novo DLL
  3. Busca snapshot da release no servidor
  4. Compara deps (incompativel = erro com detalhes)
  5. Upload como patch N
  6. Git tag: patch-v1.0.0-N
```

## Algoritmo de Compatibilidade (DependencyAnalyzer)

Para cada referencia do patch:
- **Nova** (nao existia na release) → INCOMPATIVEL
- **Versao aumentou** → INCOMPATIVEL
- **Versao igual ou menor** → OK
- Assemblies runtime .NET (System.Runtime, etc.) sao ignorados
