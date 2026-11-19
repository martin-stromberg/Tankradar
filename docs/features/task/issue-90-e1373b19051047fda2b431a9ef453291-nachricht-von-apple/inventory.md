# Bestandsaufnahme – .NET-10-Stand vor dem .NET-11-Upgrade

## Ausgangslage

- `global.json`: .NET SDK `10.0.400`, `rollForward: latestPatch`.
- Zielframeworks (`src/Tankradar.MAUI/Tankradar.MAUI.csproj`):
  `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`,
  `net10.0-windows10.0.19041.0` — per `Include*Target`-Eigenschaften
  ausblendbar. iOS-Mindestversion 16.0 (Stakeholder-Entscheidung),
  MacCatalyst 15.0, Android 21.0, Windows 10.0.19041.0.
- Test-/Hilfsprojekte (`Tests.Unit`, `Tests.Integration`, `Tests.E2E`,
  `PagesScreenshots`): `net10.0-windows10.0.19041.0`.
- Zentrale Paketverwaltung (`Directory.Packages.props`):
  `MauiVersion` 10.0.130; `Microsoft.Extensions.Logging.Debug`,
  `Microsoft.EntityFrameworkCore.Sqlite`/`.Design`,
  `Microsoft.Data.Sqlite` je 10.0.15; unabhängige Pakete
  (xunit, coverlet, FlaUI, SQLitePCLRaw 2.1.14).

## iOS-Buildkette (CI)

- Composite Action `.github/actions/package-ios` (einziger iOS-Pfad):
  - wählt Xcode über `xcode-version` (= Repo-Variable `IOS_XCODE_VERSION`,
    aktuell **nicht gesetzt** → Runner-Standard),
  - pinnt SDK-Band per `dotnet-version: '10.0.1xx'` + CI-`global.json`
    `10.0.100` (der Runner bringt ein neueres SDK mit; ohne Pin würde der
    Resolver das höchste Band nehmen),
  - pinnt die iOS-Workload per Rollback-File auf `26.5.10318`
    (`Microsoft.iOS.Sdk.net10.0_26.5` = Xcode-26.5-Bindungen),
  - entfernt vorinstallierte `Microsoft.iOS.Sdk.net10.0_*`-Packs anderer
    Versionen,
  - baut `net10.0-ios` (unsignierter Compile-Check bzw. signierte
    `release-ios.ipa` mit optionalem TestFlight-Upload).
- Aufrufer: `pr-staging-ci.yml` (Job `ios-build`), `staging-ci.yml`
  (`ios-prerelease`), `release.yml` (`ios`) — alle `runs-on: macos-latest`.

## Relevante Fakten (Recherche, 2026-11-19)

- `macos-latest` = `macos-26`-Image: nur Xcode 26.x (Default 26.6),
  **kein Xcode 27**.
- GitHub-Runner-Label `xcode-27` (macOS 27, arm64, GA seit 2026-10-15):
  Xcode 27.0 / 27.1 (Default) / **27.2** mit iOS-27.2-SDK
  (`/Applications/Xcode_27.2.app`).
- NuGet: `Microsoft.NET.Sdk.iOS.Manifest-11.0.100` → `27.2.11039`;
  Pack `Microsoft.iOS.Sdk.net11.0_27.2` → `27.2.11039` (GA, Xcode 27.2).
  Für net11.0_27.0 existiert nur eine RC-Version; GA-Band ist 27.2.
- NuGet (stabil): `Microsoft.Maui.Controls` 11.0.10; EF Core /
  `Microsoft.Extensions.Logging.Debug` / `Microsoft.Data.Sqlite` je 11.0.0;
  Android-Manifest 11.0.100 → 37.2.31.
- Lokal installiert: .NET SDK 10.0.404 und **11.0.100**.

## Sonstige net10-Referenzen

- Workflows: `dotnet-version: '10.0.4xx'` in `pr-staging-ci.yml` (2×),
  `release.yml`, `security-scan.yml`, `staging-ci.yml` (2×);
  `build-and-package/action.yml` `10.0.x`.
- Skripte: `scripts/iOS-Deployment.ps1`, `local-ci.ps1`,
  `package-windows.ps1`, `create-review-version.ps1`,
  `test-ios-deployment.ps1` (TFM-Strings `net10.0-*`).
- Doku: `README.md` (Badge, SDK 10.0.401, TFM-Beispiele),
  `pages/README.md`, `docs/help/ci-cd/*`, vereinzelte
  `docs/help/*/ablauf-technisch.md`, `docs/RELEASE_NOTES.md`.
- EF Core: reguläre Migrations unter `src/Tankradar.MAUI/Data/Migrations/`
  (kein kompiliertes Modell). EF-11-Upgrade benötigt keine neue Migration.

## Risiken / Punkte für den Plan

- `xcode-27`-Runner sind arm64-only (für diese App unkritisch).
- Workload-Pin: `workload update --from-rollback-file` mit
  `{"microsoft.net.sdk.ios": "27.2.11039"}` im 11.0.100-Band.
- Pack-Cleanup-Glob muss `net10.0_*` → `net11.0_*` folgen.
- iOS-Mindestversion 16.0 bleibt; ob .NET 11/Xcode 27 dabei warnt, zeigt
  der CI-iOS-Job (lokal nicht prüfbar, kein Mac).
- `IOS_XCODE_VERSION` muss auf `27.2` gesetzt werden (Repo-Variable,
  bisher ungesetzt), damit die Action `Xcode_27.2.app` wählt — passend zum
  Workload-Pin.
