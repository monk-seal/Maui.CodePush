# Maui.CodePush.Server

API REST para gerenciamento de releases e patches CodePush. ASP.NET Core Minimal API + MongoDB.

## Modelo de Dados

### Entidades

| Entidade | Colecao | Descricao |
|----------|---------|-----------|
| `Account` | accounts | Email, PasswordHash (BCrypt), Name, ApiKey |
| `Subscription` | subscriptions | Status (Active/Inactive/Trial), Plan, ExpiresAt. Mock ativo |
| `App` | apps | PackageName (unique), DisplayName, AppToken, AccountId FK |
| `AppRelease` | appReleases | **Versao da loja**. Version, Platform, Channel, DependencySnapshot, GitTag |
| `Patch` | patches | **Code push update**. ReleaseId FK, PatchNumber, ModuleName, DllHash, IsActive, GitTag |
| `Release` | releases | **Legacy** (flat model antigo). Mantido para backward compat |

### Relacionamentos
```
Account 1──N App 1──N AppRelease 1──N Patch
Account 1──N Subscription
```

### DependencySnapshot (em AppRelease)
Lista de `ModuleDependencySnapshot`, cada um com:
- `ModuleName`, `DllHash`, `DllSize`
- `AssemblyReferences[]` — (Name, Version) extraidas do DLL via reflection na CLI

## Autenticacao

| Metodo | Header | Uso |
|--------|--------|-----|
| JWT Bearer | `Authorization: Bearer {token}` | CLI login (7 dias) |
| API Key | `X-Api-Key: {key}` | CLI CI/CD |
| App Token | `X-CodePush-Token: {token}` | Mobile app (check/download) |
| Multi-scheme | Automatico | Seleciona JWT ou ApiKey pelo header presente |

## Endpoints

### Auth (`/api/auth`)
- `POST /register` — Cria conta + subscription Active (mock)
- `POST /login` — Retorna JWT
- `GET /me` — Info da conta + subscription

### Apps (`/api/apps`) [JWT/ApiKey]
- `POST /` — Cria app (package name unico, gera AppToken)
- `GET /` — Lista apps do usuario
- `GET /{appId}` — Detalhes
- `DELETE /{appId}` — Deleta app + releases + patches

### Releases (`/api/apps/{appId}/releases/v2`) [JWT/ApiKey]
- `POST /` — Cria release com DLLs + dependency snapshot (multipart)
- `GET /` — Lista releases
- `GET /{releaseId}` — Detalhes com snapshot completo
- `DELETE /{releaseId}` — Deleta release + patches + arquivos

### Patches (`/api/apps/{appId}/releases/{releaseId}/patches`) [JWT/ApiKey]
- `POST /` — Cria patch (auto-increment PatchNumber, desativa anteriores)
- `GET /` — Lista patches
- `DELETE /{patchId}` — Deleta patch + arquivo

### Legacy Releases (`/api/apps/{appId}/releases`) [JWT/ApiKey]
- CRUD do modelo flat antigo (mantido para backward compat)

### Updates (`/api/updates`) [AppToken]
- `GET /check?app=X&releaseVersion=V&platform=P&channel=C` — **Novo**: retorna patches ativos
- `GET /check?app=X&module=M&version=V&platform=P` — **Legacy**: retorna release mais recente
- `GET /download/{id}` — Baixa DLL (tenta Patches primeiro, fallback Releases)

## Estrutura

```
Program.cs                           — Setup: MongoDB, Auth multi-scheme, CORS, endpoints
appsettings.json                     — JWT placeholder, MongoDB localhost, uploads path
Data/
  MongoDbContext.cs                   — Collections + indexes (unique em email, apiKey, packageName, appToken, release version)
  Entities/
    Account.cs                       — BsonId Guid, Email, PasswordHash, ApiKey
    Subscription.cs                  — Status enum, Plan, ExpiresAt
    App.cs                           — PackageName unique, AppToken, AccountId
    AppRelease.cs                    — Version, Platform, DependencySnapshot[], GitTag
    Patch.cs                         — ReleaseId FK, PatchNumber, ModuleName, DllHash, IsActive, GitTag
    Release.cs                       — Legacy flat model
Endpoints/
  AuthEndpoints.cs                   — Register, Login, Me
  AppEndpoints.cs                    — CRUD apps (deleta cascata em AppReleases + Patches + Releases)
  AppReleaseEndpoints.cs             — CRUD releases com multipart upload + snapshot
  PatchEndpoints.cs                  — CRUD patches com auto-increment e desativacao
  ReleaseEndpoints.cs                — Legacy CRUD
  UpdateEndpoints.cs                 — Check (v2 releaseVersion + legacy module) + Download
Services/
  TokenService.cs                    — JWT + RandomNumberGenerator tokens
  SubscriptionService.cs             — Mock ativo, placeholder Stripe
Auth/
  ApiKeyAuthHandler.cs               — Custom handler para X-Api-Key
```

## Storage
- **DB**: MongoDB (collections: accounts, subscriptions, apps, appReleases, patches, releases)
- **DLLs releases**: `uploads/{appId}/releases/{releaseId}/{moduleName}.dll`
- **DLLs patches**: `uploads/{appId}/patches/{patchId}.dll`
- **Env vars** (prioridade sobre appsettings): `MONGODB_CONNECTION_STRING`, `MONGODB_DATABASE_NAME`, `CODEPUSH_JWT_SECRET`

## Deploy
- Docker container em VPS, porta 8080
- CI/CD: push em main → GitHub Actions → Docker build → ghcr.io → deploy com aprovacao (environment Production)
- `docker compose pull && docker compose up -d` na VPS
