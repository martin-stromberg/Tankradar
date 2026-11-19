# Umsetzungsplan – .NET 11 / iOS-27-SDK (Issue #90)

## Ziel

App-Store-taugliche iOS-Builds mit iOS-27-SDK (Xcode 27) über das
.NET-11-Upgrade der Hauptlinie; `maintenance/net10` konserviert den
.NET-10-Stand (bereits angelegt, b778efd = `staging`-Stand).

## Akzeptanzszenarien

1. Happy Path CI: Ein PR-Check `ios build` läuft auf `xcode-27`, wählt
   `/Applications/Xcode_27.2.app`, installiert die gepinnte iOS-Workload
   `27.2.11039` und kompiliert `net11.0-ios` erfolgreich.
2. Windows-Pfad: `dotnet build`/`dotnet test` der Solution unter SDK
   11.0.100 ist grün (lokal und CI).
3. Release-Pfad: `release.yml`-Job `ios` baut auf `xcode-27` eine
   signierte `.ipa` mit iOS-27.2-SDK (ITMS-90725 entfällt ab dem
   nächsten Upload).

## Änderungen

### 1. SDK/TFM

- `global.json`: `10.0.400` → `11.0.100` (`rollForward: latestPatch`).
- `src/Tankradar.MAUI/Tankradar.MAUI.csproj`: `net10.0-android`,
  `net10.0-ios`, `net10.0-maccatalyst`,
  `net10.0-windows10.0.19041.0` → `net11.0-*`-Pendants;
  `SupportedOSPlatformVersion` unverändert (iOS 16.0).
- `src/Tankradar.Tests.{Unit,Integration,E2E}` und
  `src/PagesScreenshots`: `net10.0-windows10.0.19041.0` →
  `net11.0-windows10.0.19041.0`.

### 2. Pakete (`Directory.Packages.props`)

- `MauiVersion`: 10.0.130 → 11.0.10
- `Microsoft.Extensions.Logging.Debug`, `EntityFrameworkCore.Sqlite`,
  `EntityFrameworkCore.Design`, `Microsoft.Data.Sqlite`: 10.0.15 → 11.0.0
- Rest unverändert. Kommentar zur MAUI-Version anpassen.

### 3. CI / Workflows

- `dotnet-version: '10.0.4xx'` → `'11.0.1xx'` in `pr-staging-ci.yml`,
  `release.yml`, `security-scan.yml`, `staging-ci.yml`;
  `build-and-package/action.yml`: `'10.0.x'` → `'11.0.x'`.
- `package-ios/action.yml`:
  - `dotnet-version: '10.0.1xx'` → `'11.0.1xx'`
  - CI-`global.json`-Pin `10.0.100` → `11.0.100`
  - `ios-workload-version` Default `26.5.10318` → `27.2.11039`
  - Pack-Cleanup-Glob `Microsoft.iOS.Sdk.net10.0_*` → `net11.0_*`
  - Kommentare zum Band-Pinning auf die neue Lage bringen.
- iOS-Jobs in `pr-staging-ci.yml`, `staging-ci.yml`, `release.yml`:
  `runs-on: macos-latest` → `runs-on: xcode-27`.
- Repo-Variable `IOS_XCODE_VERSION` = `27.2` (per `gh variable set`;
  passt zur Workload 27.2.11039 und `/Applications/Xcode_27.2.app`).

### 4. Lokale Skripte

- `scripts/iOS-Deployment.ps1`: `net10.0-ios` → `net11.0-ios`
- `scripts/local-ci.ps1`: TFM-Strings/Hilfetexte `net10.0-ios` → `net11.0-ios`
- `scripts/package-windows.ps1`,
  `scripts/create-review-version.ps1`:
  `net10.0-windows10.0.19041.0` → `net11.0-windows10.0.19041.0`
- `scripts/test-ios-deployment.ps1`: `net10.0-ios`-Fixtures → `net11.0-ios`

### 5. Dokumentation

- `README.md`: .NET-Badge 10.0 → 11.0, SDK-Anforderung 10.0.401 → 11.0.100,
  TFM-Beispiele, iOS-Hinweis.
- `pages/README.md`: TFM-Referenz.
- `docs/help/ci-cd/lokaler-pruefung.md`: SDK-Version, `net11.0-ios`.
- `docs/help/ci-cd/workflows.md`: SDK-Band-Pin `11.0.1xx`, iOS-Workload-
  Pin und `xcode-27`-Runner erwähnen.
- `docs/help/ci-cd/einrichtung.md`: `IOS_XCODE_VERSION`-Beispiel `27.2`.
- `docs/help/{Einstellungen,Navigation,Preisdaten,Suche}/...`:
  `net10.0-*`-TFMs → `net11.0-*`; `.NET 10.0.401` → `.NET 11.0.100`.
- `docs/RELEASE_NOTES.md`: neuer Eintrag (EN+DE).
- `changes.log`: neuer Eintrag.
- ADRs 0016/0020 bleiben unverändert (historische Entscheidungsdokumente).

## Scope-Klärung

| Ausschluss | Begründung | Status |
|---|---|---|
| iOS-Mindestversion bleibt 16.0 | Stakeholder-Entscheidung; Xcode 27 unterstützt das Deployment-Ziel weiter. Falls der CI-Build eine Mindestanhebung erzwingt, wird sie separat gemeldet. | bestätigt (automatisierte Bearbeitung, naheliegende Annahme) |
| Kein Maintenance-CI-Setup für `maintenance/net10` | Der Branch ist Konservierungsanker; etwaige Wartungsläufe folgen bei Bedarf. | bestätigt (automatisierte Bearbeitung, naheliegende Annahme) |
| Kein Dependabot-Eintrag für `maintenance/net10` | Kann bei erster Wartung ergänzt werden; hält den Diff fokussiert. | bestätigt (automatisierte Bearbeitung, naheliegende Annahme) |

## Offene Punkte

Keine.

## Test-/Verifikationsplan

- Lokal (Windows, SDK 11.0.100): Restore + Build der Solution
  (`IncludeAndroidTarget=false`, `IncludeIosTarget=false`,
  `IncludeMacCatalystTarget=false`), `dotnet test` für Unit- und
  Integrationstests; `pwsh scripts/test-ios-deployment.ps1`;
  `python scripts/validate-workflows.py` + `test_validate_workflows.py`.
- Remote: PR-Checks (fail-fast überwacht), insbesondere `ios build`
  auf `xcode-27` — dort zeigt sich, ob Workload-Pin und Xcode-Auswahl
  greifen und iOS 16 als Mindestversion weiter akzeptiert wird.
- Reale End-to-End-Abnahme (Gerät/TestFlight): nicht in dieser Umgebung
  möglich; erfolgt mit dem nächsten Staging-Pre-Release bzw. Release.
