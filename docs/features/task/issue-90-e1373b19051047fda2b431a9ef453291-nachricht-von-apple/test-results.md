# Testergebnisse – Issue #90 (.NET 11 / iOS 27 SDK)

Umgebung: Windows, .NET SDK 11.0.100 (userbezogen, `~/dotnet11`, da die
MSI-Workload-Installation ohne Elevation nicht möglich ist),
`IncludeAndroidTarget=false`, `IncludeIosTarget=false`,
`IncludeMacCatalystTarget=false` (Windows-Ziel wie in den CI-Jobs).

| Prüfung | Ergebnis |
|---|---|
| `dotnet workload restore` (Band 11.0.100) | OK — installiert `maui-tizen` (enthält `Microsoft.Maui.Sdk` 11.0.0 u. a.) |
| `dotnet restore Tankradar.sln -p:Configuration=Release` | OK |
| `dotnet build Tankradar.sln -c Release --no-restore -p:TreatWarningsAsErrors=true` | OK — 0 Warnungen, 0 Fehler |
| `dotnet test Tankradar.Tests.Unit -c Release --no-build` | 2164/2164 bestanden |
| `dotnet test Tankradar.Tests.Integration -c Release --no-build` | 311/311 bestanden |
| `pwsh scripts/test-ios-deployment.ps1` | OK (alle Assertions) |
| `npm test` (scripts/*.test.mjs) | 32/32 bestanden |
| `python scripts/validate-workflows.py` | Exit 1; exakt die 2 preexisting `e2e-dumps`-Befunde (`pr-staging-ci.yml`, `staging-ci.yml`) — unverändertes Vorhalten aus Issue #58 |
| `python scripts/test_validate_workflows.py` | 13/14 — 1 preexisting Failure (dieselbe `e2e-dumps`-Ursache) |
| `dotnet ef migrations script` (Probe) | bestätigt `COLLATE "NOCASE"` als EF-11-Emission |

## CI-Befund und Fix (PR #91)

Der erste `ios build`-Lauf auf `xcode-27` schlug mit `error MT7199` fehl:
Ab dem iOS-/MacCatalyst-27-SDK verlangt Apple den UIKit-Scene-Lifecycle
(`UIApplicationSceneManifest` in der Info.plist), sonst crasht die App auf
iOS 27 beim Start. Umsetzung nach dem offiziellen Migrationsleitfaden:

- `UIApplicationSceneManifest` (Single-Scene, `__MAUI_DEFAULT_SCENE_CONFIGURATION__`,
  `SceneDelegate`) in `Platforms/iOS/Info.plist` und
  `Platforms/MacCatalyst/Info.plist`.
- `SceneDelegate : MauiUISceneDelegate` in beiden Plattformordnern;
  `AppDelegate`/`Program.cs` bleiben unverändert (keine eigenen
  Lifecycle-Handler vorhanden — nur Windows-Events in `MauiProgram`).
- `test-ios-deployment.ps1` prüft Manifest und SceneDelegate-Datei ab.

Zweiter Lauf: `ios build` und `static checks` grün; `build & test` fiel im
Schritt `Test E2E with FlaUI`: `AppRelativePath` in `E2ETestBase` zeigte noch
auf `net10.0-windows10.0.19041.0`. Zwei übersehene TFM-Reste korrigiert
(`E2ETestBase.AppRelativePath`, `ApiKeyBuildRunner`-Probeprojekt `net10.0` →
`net11.0`, plus zwei Skript-Fixture-Strings).

Dritter Lauf: `build & test` fiel in `Test integration`:
`Restore_PrepareTimeout_FailsWithoutChangingLiveData` (erwartet `Failed`, kam
`Ok`). Ursache: `CancellationTokenSource.CancelAfter(TimeSpan.Zero)` bricht
unter .NET 11 nicht mehr synchron ab (lokal verifiziert: `IsCancellationRequested`
direkt nach dem Aufruf `false`) — damit war der Test racy. Fix:
`BackupDatabasePreparer.PrepareAsync` behandelt ein Budget ≤ 0 jetzt
deterministisch als sofort überschritten (`Failed`). 34/34 Schema-Tests lokal
erneut grün.

## Umgesetzte Fixes während der Verifikation

1. `SQLitePCLRaw.bundle_e_sqlite3` 2.1.14 → 3.0.5 (NU1605; EF Core 11
   verlangt transitiv >= 3.0.5).
2. CsWinRT1032 in App-Code: 11 Collection-Expressions als explizite
   `new T[]`-Initialisierer formuliert (Analyzer-Vorgabe und
   XML-Doc-Checker-konform, verhaltensgleich).
3. CsWinRT1032 in Tests.Unit/Tests.Integration: `NoWarn` (Testassemblys
   werden nicht als WinRT-Komponente ausgeliefert).
4. `BackupTestTampering.RemovedCollation`: Tamper entfernt jetzt beide
   Schreibweisen (`COLLATE "NOCASE"`/EF 11, `COLLATE NOCASE`/EF 10).

## Nicht lokal prüfbar (erfolgt in der CI)

- iOS-Compile-Prüfung `net11.0-ios` (kein Mac/kein iOS-Build unter
  Windows) → PR-Check `ios build` auf `xcode-27`.
- Android-/MacCatalyst-Targets (Workloads nicht installiert) → CI-Jobs.
- Reale E2E-/Geräteabnahme → nächster Staging-Pre-Release bzw. Release.
