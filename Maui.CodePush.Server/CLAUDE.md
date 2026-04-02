# Maui.CodePush.Server

API REST para gerenciamento de releases CodePush. ASP.NET Core Minimal API + MongoDB.

## Modelo de Seguranca

### Autenticacao de Desenvolvedores (CLI -> Server)
- **Registro**: POST `/api/auth/register` com email/password/name. Senha hasheada com BCrypt.
- **Login**: POST `/api/auth/login` retorna JWT (expira em 7 dias). Claims: sub (accountId), email, name.
- **API Key**: Alternativa ao JWT para CI/CD. Header `X-Api-Key`. Gerada no registro (32 bytes hex).
- **Multi-scheme**: O servidor aceita JWT Bearer OU API Key, selecionado automaticamente pelo header presente.

### Autenticacao de Apps (Mobile -> Server)
- **App Token**: Header `X-CodePush-Token`. Gerado na criacao do app (32 bytes hex).
- **Modelo**: O token eh embedado no app mobile. Nao eh segredo absoluto (decompilavel) mas permite revogacao e rate limiting.
- **Validacao**: Endpoints `/api/updates/*` verificam que o token corresponde ao appId solicitado.

### Ownership de Apps
- **First-come-first-served**: Package name eh unico (constraint no DB). Quem registrar primeiro eh o dono.
- **Apenas o dono publica**: Endpoints de release verificam que o accountId autenticado corresponde ao accountId do app.

### Subscription (Mock)
- Todo registro cria subscription com status Active e plano "pro"
- `SubscriptionService.IsSubscriptionActiveAsync()` verifica o status
- Pronto para integrar Stripe via webhook `POST /api/webhook/stripe`

## Endpoints

### Auth (`/api/auth`)
| Metodo | Path | Auth | Descricao |
|--------|------|------|-----------|
| POST | `/register` | - | Registra conta. Retorna accountId + apiKey |
| POST | `/login` | - | Login. Retorna JWT token |
| GET | `/me` | JWT/ApiKey | Info da conta + subscription |

### Apps (`/api/apps`)
| Metodo | Path | Auth | Descricao |
|--------|------|------|-----------|
| POST | `/` | JWT/ApiKey | Cria app. Retorna appId + appToken |
| GET | `/` | JWT/ApiKey | Lista apps do usuario |
| GET | `/{appId}` | JWT/ApiKey | Detalhes do app |
| DELETE | `/{appId}` | JWT/ApiKey | Deleta app + releases |

### Releases (`/api/apps/{appId}/releases`)
| Metodo | Path | Auth | Descricao |
|--------|------|------|-----------|
| POST | `/` | JWT/ApiKey | Upload DLL (multipart form: file, moduleName, version, platform, channel) |
| GET | `/` | JWT/ApiKey | Lista releases |
| DELETE | `/{releaseId}` | JWT/ApiKey | Deleta release + arquivo |

### Updates (`/api/updates`) — Para apps mobile
| Metodo | Path | Auth | Descricao |
|--------|------|------|-----------|
| GET | `/check?app=X&module=Y&version=Z&platform=P&channel=C` | AppToken | Verifica se ha update |
| GET | `/download/{releaseId}` | AppToken | Baixa DLL do release |

## Estrutura

```
Program.cs                       — Setup: MongoDB, Auth (JWT+ApiKey+MultiScheme), CORS, endpoints
appsettings.json                 — JWT secret, MongoDB connection, uploads path
Data/
  MongoDbContext.cs              — MongoDB collections + indexes (unique Email, ApiKey, PackageName, AppToken)
  Entities/
    Account.cs                   — Email, PasswordHash (BCrypt), Name, ApiKey. BsonId com Guid
    Subscription.cs              — Status enum (Active/Inactive/Trial), Plan, ExpiresAt
    App.cs                       — PackageName (unique), DisplayName, AppToken, AccountId
    Release.cs                   — ModuleName, Version, Platform, Channel, DllHash, DllSize, FileName
Endpoints/
  AuthEndpoints.cs               — Register, Login, Me
  AppEndpoints.cs                — CRUD de apps
  ReleaseEndpoints.cs            — Upload/List/Delete releases. SHA-256 hash. Salva em uploads/{appId}/
  UpdateEndpoints.cs             — Check + Download para apps mobile (valida AppToken)
Services/
  TokenService.cs                — Gera JWT e tokens aleatorios (RandomNumberGenerator)
  SubscriptionService.cs         — Mock ativo. Placeholder para Stripe
Auth/
  ApiKeyAuthHandler.cs           — Custom AuthenticationHandler para header X-Api-Key
```

## Storage
- **DB**: MongoDB (`codepush` database, collections: accounts, subscriptions, apps, releases)
- **Driver**: MongoDB.Driver 3.4.0
- **DLLs**: `uploads/{appId}/{releaseId}.dll` no filesystem
- **JWT Secret**: Via appsettings ou env `CODEPUSH_JWT_SECRET`
- **Indexes**: Criados automaticamente no startup via `EnsureIndexesAsync()` (unique em email, apiKey, packageName, appToken; compound em releases)

## Como Rodar

### Local
```bash
dotnet run --project Maui.CodePush.Server
# Servidor em http://localhost:5000
# Precisa de appsettings.Development.json com MongoDB connection string
```

### Docker (producao)
```bash
# Na VPS:
# 1. Criar .env com as secrets (baseado em .env.example)
# 2. docker compose pull && docker compose up -d
```

### GitHub Actions
- Workflow: `.github/workflows/server-deploy.yml`
- Trigger: push em `main` que muda `Maui.CodePush.Server/**`
- Builda imagem Docker e pusha para `ghcr.io/felipebaltazar/codepush-server:latest`
- Secrets usadas: `MONGODB_CONNECTION_STRING`, `MONGODB_DATABASE_NAME`, `CODEPUSH_JWT_SECRET`

### Environment Variables (prioridade sobre appsettings)
| Variavel | Descricao |
|----------|-----------|
| `MONGODB_CONNECTION_STRING` | Connection string do MongoDB Atlas |
| `MONGODB_DATABASE_NAME` | Nome do database (default: codepush) |
| `CODEPUSH_JWT_SECRET` | Chave secreta para JWT tokens |
