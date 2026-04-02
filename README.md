# Maui.CodePush

**Ship hotfixes to your .NET MAUI apps instantly. No app store review. No waiting.**

Maui.CodePush is an over-the-air (OTA) update system for .NET MAUI, bringing the same capabilities that [Shorebird](https://shorebird.dev/) offers for Flutter and [CodePush](https://microsoft.github.io/code-push/) offered for React Native — now for the .NET ecosystem.

> Push bug fixes, UI changes, and minor updates directly to your users' devices on **Android** and **iOS**, without going through app store review cycles.

---

## How It Works

```
  Developer                    Device
  ─────────                    ──────
  1. Build Feature module
  2. Push DLL update    ───>   3. App detects update
                               4. Downloads new DLL
                               5. Applies on next restart
                               6. Users see the fix instantly
```

At build time, CodePush **removes your feature modules from the app package** and embeds them as extractable resources. At runtime, modules are loaded dynamically via `Assembly.LoadFrom`. When an update is available, the new DLL replaces the current one — no reinstall needed.

## Quick Start

### 1. Install the NuGet package

```
dotnet add package CodePush.Maui --prerelease
dotnet tool install -g dotnet-codepush --prerelease
```

### 2. Create a Feature module

Create a standard .NET MAUI class library with your pages, ViewModels, and services:

```
dotnet new mauiclass -n MyApp.Feature
```

### 3. Configure your app

**MyApp.csproj:**
```xml
<ItemGroup>
  <!-- Declare which modules are managed by CodePush -->
  <CodePushModule Include="MyApp.Feature" />
</ItemGroup>

<ItemGroup>
  <!-- Embed the DLL as fallback for first launch -->
  <EmbeddedResource Include="MyApp.Feature.dll" />
</ItemGroup>
```

**MauiProgram.cs:**
```csharp
builder.UseCodePush(options =>
{
    options.AddModule("MyApp.Feature");
    options.ReleaseVersion = "1.0.0";     // matches your app store version
    options.ServerUrl = "https://your-codepush-server.com";
    options.AppId = "your-app-id";
    options.AppToken = "your-app-token";
});
```

**Platform entry points:**
```csharp
// Android: MainApplication.cs
public class MainApplication : CodePushApplication { ... }

// iOS: AppDelegate.cs
public class AppDelegate : CodePushAppDelegate { ... }
```

### 4. Create a release and push patches

```bash
# Login to CodePush server
codepush login --email you@email.com --password yourpass

# Register your app
codepush apps add --package-name com.myapp --name "My App" --set-default

# Create a release (captures dependency snapshot + git tag)
codepush release create --version 1.0.0 --app-project MyApp/MyApp.csproj

# Submit APK/IPA to app store...

# Later, push a patch (checks dependency compatibility)
codepush patch --release 1.0.0

# Or deploy directly to a connected device for testing
codepush release MyApp.Feature.csproj --local --restart
```

## Features

- **Android + iOS** support out of the box
- **Release/Patch model** — like Shorebird: create releases, push compatible patches
- **Dependency compatibility check** — prevents broken patches via assembly reference analysis
- **Zero config MSBuild integration** — automatically removes modules from the app package
- **3-tier assembly resolution** — pending updates > persisted > embedded fallback
- **SHA-256 hash verification** for download integrity
- **Git integration** — automatic tags for releases (`v1.0.0`) and patches (`patch-v1.0.0-1`)
- **Rollback** to any previous version or the original embedded module
- **CLI tool** — `dotnet-codepush` with login, apps, release, patch, devices, rollback
- **iOS hybrid AOT + Interpreter** — main app runs full AOT, only CodePush modules use the Mono interpreter (no performance penalty for the host app)

## How iOS Works

Apple restricts loading native code at runtime, but the Mono interpreter — which ships inside your app binary — can execute IL bytecode dynamically. CodePush configures `MtouchInterpreter` to enable interpretation **only for your modules**, keeping the rest of your app at full AOT performance.

This is the same approach used by React Native CodePush and Shorebird — interpreting managed bytecode through a built-in interpreter that ships with the app.

## Architecture

```
┌─────────────────────────────────────────────┐
│                Your MAUI App                │
│                                             │
│  MauiProgram.cs                             │
│    └── UseCodePush(options => {             │
│            options.AddModule("Feature");    │
│        })                                   │
│                                             │
│  AppShell.xaml                              │
│    └── References Feature.MainPage          │
│         (loaded dynamically by CodePush)    │
│                                             │
├─────────────────────────────────────────────┤
│             Maui.CodePush Library           │
│                                             │
│  Build Time:                                │
│    MSBuild targets remove Feature DLL       │
│    from assembly store, embed as resource   │
│                                             │
│  Runtime:                                   │
│    AssemblyResolve intercepts loading       │
│    → Check temp/ (pending updates)          │
│    → Check base/ (persisted version)        │
│    → Extract from embedded (first launch)   │
│    → Assembly.LoadFrom(path)                │
│                                             │
├─────────────────────────────────────────────┤
│           CodePush Server (Phase 2)         │
│    Release management, CDN, analytics       │
└─────────────────────────────────────────────┘
```

## Roadmap

| Phase | Status | Description |
|-------|--------|-------------|
| **1 - Local MVP** | **In Progress** | Core engine, MSBuild targets, version tracking, rollback. Android validated on physical device. |
| **2 - Server + CLI** | Planned | Backend API, `dotnet-codepush` CLI, code signing (RSA-2048), differential updates (bsdiff) |
| **3 - Production** | Planned | CDN distribution, staged rollouts, analytics dashboard, MonoVM interpreter optimizations |

## App Store Compliance

- **Apple iOS** (Section 3.3.2): The Mono interpreter is part of the app binary. Interpreting IL bytecode is analogous to JavaScript in WebKit — this approach is used by React Native CodePush and Shorebird
- **Google Play**: .NET assemblies run on the Mono runtime VM, which qualifies under Google's policy for virtual machine execution

CodePush is designed for **hotfixes and minor updates**, not for circumventing app store review for major features.

## Requirements

- .NET 9.0+
- .NET MAUI workloads for Android and/or iOS
- For Android device testing: `EmbedAssembliesIntoApk=true` in debug builds

## Contributing

See [CLAUDE.md](CLAUDE.md) for architecture documentation and [docs/PLAN.md](docs/PLAN.md) for the implementation plan and technical decisions history.

## License

See [LICENSE](LICENSE) for details.
