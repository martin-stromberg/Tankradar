# Paketreferenzen — alle Fundstellen

Durchsucht: alle `*.csproj`, `*.props`, `*.targets`, `*.sln`, `*.md`, Workflows und Skripte.
Es gibt **keine** `Directory.Packages.props`, `packages.lock.json`, `nuget.config`,
`global.json` oder `Directory.Build.*` — alle Versionen stehen inline in den `.csproj`.

## 1. Direkte Fundstellen der betroffenen Pakete

### `src/Tankradar.MAUI/Tankradar.MAUI.csproj` (Hauptprojekt)

| Zeile | Referenz | Ist-Version | Soll (lt. Requirement) | Bemerkung |
|---|---|---|---|---|
| 31 | `<MauiVersion>` (MSBuild-Property) | `10.0.120` | `10.0.130` | Steuert `Microsoft.Maui.*` (Controls, Core, Essentials, Resizetizer, Build-Tasks/XamlC); Kommentar Zeilen 28–30 verweist auf ADR 0016 |
| 104 | `Microsoft.Maui.Controls` | `$(MauiVersion)` → `10.0.120` | `10.0.130` | Kein Literal — Änderung nur am Property in Z. 31 |
| 105 | `Microsoft.Extensions.Logging.Debug` | `10.0.0` | `10.0.15` | |
| 106 | `Microsoft.EntityFrameworkCore.Sqlite` | `10.0.10` | `10.0.15` | |
| 108 | `SQLitePCLRaw.bundle_e_sqlite3` | `2.1.12` | `2.1.14` | Kommentar Z. 107: hebt bewusst die transitiv gezogene, anfällige 2.1.11 (GHSA-2m69-gcr7-jv3q) an |
| 109–112 | `Microsoft.EntityFrameworkCore.Design` | `10.0.10` | `10.0.15` | `PrivateAssets=all`, `IncludeAssets=runtime; build; native; contentfiles; analyzers; buildtransitive` — nur Design-Time (`dotnet ef`) |

Weitere MAUI-bezogene Stellen in derselben Datei (keine Versionen, aber relevant):
- Z. 82–83: `TrimmerRootAssembly` für `Microsoft.EntityFrameworkCore.Sqlite` und
  `Microsoft.Data.Sqlite` (iOS-Linker-Ausnahmen, versionsunabhängig).

### `src/Tankradar.Tests.E2E/Tankradar.Tests.E2E.csproj`

| Zeile | Referenz | Ist-Version | Soll | Bemerkung |
|---|---|---|---|---|
| 20 | `Microsoft.Data.Sqlite` | `10.0.10` | `10.0.15` | Kommentar Z. 19: direkter DB-Zugriff im E2E-Test, „gleiche, nicht anfällige SQLite-Version wie das Hauptprojekt" |
| 21 | `SQLitePCLRaw.bundle_e_sqlite3` | `2.1.12` | `2.1.14` | gleiches Pinning-Motiv wie im Hauptprojekt |

## 2. Transitives Pinning / verwandte Pakete

- `SQLitePCLRaw.bundle_e_sqlite3` ist in **beiden** Projekten direkt referenziert,
  um transitive Auflösung auf die verwundbare 2.1.11 zu verhindern. Beim Update auf
  `2.1.14` müssen beide Stellen angehoben werden (Nicht-Ziel: kein Umstieg auf 3.x).
- **Kein** explizites Pinning von `SQLitePCLRaw.core`, `Microsoft.EntityFrameworkCore`
  (Basispaket), `Microsoft.EntityFrameworkCore.Relational` o. ä. — diese kommen
  ausschließlich transitiv über `EFCore.Sqlite`/`EFCore.Design` bzw. `Microsoft.Data.Sqlite`.
- `Microsoft.Data.Sqlite` im Hauptprojekt kommt transitiv über
  `Microsoft.EntityFrameworkCore.Sqlite` (direkt nur im E2E-Projekt referenziert).
- `Microsoft.Maui.Core`, `Microsoft.Maui.Essentials`, `Microsoft.Maui.Resizetizer`
  folgen `MauiVersion` implizit (Single-Project/MAUI-SDK); nicht separat gelistet.
- Transitive Nebenwirkungen des MAUI-Sprungs auf `10.0.120` (aus ADR 0016):
  `Microsoft.WindowsAppSDK` → ≥ `1.8.260529003`, `Microsoft.Graphics.Win2D` → `1.4.0`,
  `Microsoft.Web.WebView2` → `1.0.4258.31`, `Microsoft.Windows.SDK.BuildTools` →
  `10.0.26100.8249`. Für `10.0.130` erneut prüfen (Paketgröße/NU1605-Risiko).

## 3. Nicht betroffene Paketreferenzen (Vollständigkeit)

Alle übrigen direkten Referenzen, die **nicht** Teil des Updates sind:

| Projekt | Paket | Version |
|---|---|---|
| Tests.Unit / Tests.Integration / Tests.E2E | `coverlet.collector` | 6.0.4 |
| dto. | `Microsoft.NET.Test.Sdk` | 17.14.1 |
| dto. | `xunit` | 2.9.3 |
| dto. | `xunit.runner.visualstudio` | 3.1.4 |
| Tests.E2E | `FlaUI.Core`, `FlaUI.UIA3` | 5.0.0 |

## 4. Weitere Artefakte mit Paket-/Versionsbezug

- `issue.md` (Repo-Wurzel) und `requirement.md` — Aufgabentext, keine Änderung nötig.
- `changes.log` Z. 97–102 — historischer Eintrag zum MAUI-10.0.120-Sprung (kein Edit).
- `src/Tankradar.MAUI/MSBuild/TankerkoenigApiKey.targets` und
  `WindowsAppIcon.targets` — geprüft, **keine** Paketreferenzen/Versionsangaben.
- `scripts/package-windows.ps1`, Workflows — referenzieren keine Paketversionen
  (nur `dotnet-version: '10.0.x'` und Workload-Restore).
- `src/PagesScreenshots/PagesScreenshots.csproj` — **nicht** Solution-Mitglied,
  referenziert nur `Tankradar.Tests.E2E.csproj` (erbt dessen Pakete transitiv).
