# Projektstruktur, Zielframeworks, SDK/Runtime

## Solution-Mitglieder (`Tankradar.sln`)

4 Projekte, alle unter dem Solution-Ordner `src` (sln Z. 6–15, 79–83):

| Projekt | Pfad | Rolle |
|---|---|---|
| `Tankradar.MAUI` | `src/Tankradar.MAUI/Tankradar.MAUI.csproj` | MAUI-App („Tankatlas"), `OutputType=Exe`, `UseMaui=true`, `SingleProject=true` |
| `Tankradar.Tests.Unit` | `src/Tankradar.Tests.Unit/` | xUnit-Unit-Tests, `ProjectReference` → MAUI |
| `Tankradar.Tests.Integration` | `src/Tankradar.Tests.Integration/` | xUnit-Integrationstests (reale SQLite-DB), `ProjectReference` → MAUI |
| `Tankradar.Tests.E2E` | `src/Tankradar.Tests.E2E/` | xUnit + FlaUI-E2E-Tests der Windows-App, **keine** `ProjectReference` (startet die gebaute Exe) |

**Nicht** in der Solution: `src/PagesScreenshots/PagesScreenshots.csproj`
(Hilfsprogramm für Screenshots, referenziert `Tankradar.Tests.E2E`).

`src/TestSupport/` ist **kein eigenes Projekt** — 14 `.cs`-Hilfsdateien
(Mock-Server, `TestDataPaths`, `TransientRetry`, `E2EStartupPolicy` u. a.), die per
`<Compile Include="..\TestSupport\*.cs" Link=…>` in die Testprojekte und
(`TestDataPaths.cs`) in das Hauptprojekt eingebunden werden.

## Zielframeworks und `Include*Target`-Schalter

`Tankradar.MAUI.csproj` Z. 8–16 — bedingte TFMs, alle Schalter default `true`:

| Property | TFM | Zusatzbedingung |
|---|---|---|
| `IncludeAndroidTarget` | `net10.0-android` | — |
| `IncludeIosTarget` | `net10.0-ios` | nicht auf Linux |
| `IncludeMacCatalystTarget` | `net10.0-maccatalyst` | nicht auf Linux |
| `IncludeWindowsTarget` | `net10.0-windows10.0.19041.0` | nur auf Windows |

- CI-Windows-Jobs setzen `IncludeAndroidTarget/IosTarget/MacCatalystTarget=false`
  (Env), der macOS-Job blendet umgekehrt aus — verhindert `NETSDK1147` bei
  fehlenden Workloads.
- Testprojekte + PagesScreenshots: fest `net10.0-windows10.0.19041.0`.
- Mindestversionen (csproj Z. 59–64): iOS 16.0, MacCatalyst 15.0, Android 21.0,
  Windows 10.0.19041.
- Windows: `WindowsPackageType=None` (unpackaged), `WindowsAppSDKSelfContained=true`.
- iOS-Release: `MtouchInterpreter=all` + `TrimmerRootAssembly` für App, EF Core,
  EFCore.Relational, EFCore.Sqlite, `Microsoft.Data.Sqlite` (Z. 73–84).
- Weitere Imports: `MSBuild/TankerkoenigApiKey.targets` (immer),
  `MSBuild/WindowsAppIcon.targets` (nur Windows-TFM) — beide ohne Paketbezug.

## SDK / Workloads (Ist-Stand des Rechners, verifiziert)

- `dotnet --version`: **10.0.403** (kein `global.json` im Repo → SDK frei, README
  fordert „10.0.401 oder neuer", Workflows pinnen `dotnet-version: '10.0.x'`).
- `dotnet workload list`: Workload-Set **10.0.401**; installiert:
  `maui-windows` 10.0.20/10.0.100, `android`, `ios`, `maccatalyst`
  (je …/10.0.100, Quelle SDK 10.0.400 / VS 18.11).
- CI: `dotnet workload restore Tankradar.sln`; der iOS-Baustein
  (`.github/actions/package-ios`) pinnt das iOS-Manifest zusätzlich per
  Rollback-Datei (`--skip-manifest-update`).
- `MauiVersion` (NuGet-Pakete) ist **entkoppelt** von der Workload: die Workload
  liefert nur `Microsoft.Maui.Sdk` (ADR 0016). MAUI-Paket-Updates erfordern daher
  **kein** Workload-Update.

## Versions-/App-Metadaten (für Release relevant)

- `Tankradar.MAUI.csproj` Z. 49–52: `Version`/`InformationalVersion`/`ApplicationDisplayVersion`
  `0.1.0`, `ApplicationVersion` `1` — werden im Release-Build per `-p:Version=…`
  überschrieben (`scripts/package-windows.ps1` Z. 63–65).
- `ApplicationId`: `de.martinstromberg.tankradar`; Anzeigename „Tankatlas".
