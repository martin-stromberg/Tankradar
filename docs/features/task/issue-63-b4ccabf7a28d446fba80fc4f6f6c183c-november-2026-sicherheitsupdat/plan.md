# Umsetzungsplan: November-2026-Sicherheitsupdates für .NET-/NuGet-Pakete (Issue #63)

## Übersicht

Sechs direkte Paketreferenzen werden auf den November-2026-Patchstand (oder einen neueren kompatiblen Stand) angehoben — innerhalb der .NET-10-Linie bzw. des SQLitePCLRaw-2.x-Zweigs. Alle Änderungen liegen in genau zwei `.csproj`-Dateien; hinzu kommen zwei veraltende Versionsnennungen in `docs/help/` und ein `changes.log`-Eintrag (Projektkonvention). Da das Windows-Paket self-contained ausgeliefert wird, ist nach dem Bump der normale Release-Weg (staging-ci → Pre-Release, Promotion `staging` → `main` → `release.yml`) zu durchlaufen, damit die ausgelieferten Artefakte die aktualisierten Pakete enthalten.

Anforderung: [requirement.md](requirement.md) · Bestandsaufnahme: [inventory.md](inventory.md) · Basisbranch für Diffs und PR: `staging`.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Geänderte Dateien | `src/Tankradar.MAUI/Tankradar.MAUI.csproj` (5 Referenzen), `src/Tankradar.Tests.E2E/Tankradar.Tests.E2E.csproj` (2 Referenzen), `docs/help/Einstellungen/ablauf-technisch.md`, `docs/help/Navigation/ablauf-technisch.md`, `changes.log` |
| Paketupdates | 6 Pakete, siehe Tabelle unten — `SQLitePCLRaw.bundle_e_sqlite3` kommt in beiden Projekten vor (7 geänderte Versionsangaben in 2 csproj-Dateien, davon eine indirekt über das `MauiVersion`-Property) |
| Nicht betroffen | Kein `Directory.Packages.props`/Lock-Files/`nuget.config`/`global.json` vorhanden — Versionen stehen ausschließlich inline; keine weiteren Versionsträger zu pflegen |
| Release-Weg | Kein Code- oder Workflow-Eingriff nötig: Push auf `staging` → `staging-ci.yml` baut bei Versionsänderung automatisch ein Windows-Pre-Release (`scripts/package-windows.ps1`, self-contained `win-x64`); Merge des Promotion-PRs `staging` → `main` löst `release.yml` aus (finales `release-win-x64.zip` + `update.json`) |

### Zu ändernde Referenzen (Ist → Soll, mit Fundstelle)

**`src/Tankradar.MAUI/Tankradar.MAUI.csproj`:**

| Zeile | Eintrag | Ist | Soll |
|---|---|---|---|
| 31 | `<MauiVersion>`-Property (steuert `Microsoft.Maui.Controls` Z. 104 und implizit Core/Essentials/Resizetizer/Build-Tasks) | `10.0.120` | `10.0.130` |
| 105 | `Microsoft.Extensions.Logging.Debug` | `10.0.0` | `10.0.15` |
| 106 | `Microsoft.EntityFrameworkCore.Sqlite` | `10.0.10` | `10.0.15` |
| 108 | `SQLitePCLRaw.bundle_e_sqlite3` | `2.1.12` | `2.1.14` |
| 109 | `Microsoft.EntityFrameworkCore.Design` (`PrivateAssets=all`, nur Design-Time) | `10.0.10` | `10.0.15` |

**`src/Tankradar.Tests.E2E/Tankradar.Tests.E2E.csproj`:**

| Zeile | Eintrag | Ist | Soll |
|---|---|---|---|
| 20 | `Microsoft.Data.Sqlite` (direkter DB-Zugriff der E2E-Tests; Kommentar Z. 19 fordert gleiche SQLite-Version wie Hauptprojekt) | `10.0.10` | `10.0.15` |
| 21 | `SQLitePCLRaw.bundle_e_sqlite3` (transitives Pinning gegen GHSA-2m69-gcr7-jv3q) | `2.1.12` | `2.1.14` |

Hinweis: Die `Microsoft.Data.Sqlite`-Referenz im E2E-Projekt begründet sich über das Hauptprojekt — `EFCore.Sqlite` 10.0.15 zieht transitiv `Microsoft.Data.Sqlite` 10.0.15, daher muss die E2E-Referenz denselben Stand erhalten.

### Scope-Ausschlüsse

| # | Ausschluss | Begründung | Status |
|---|---|---|---|
| 1 | Kein Umstieg auf .NET 11 / SDK-11-Basis. | Explizites Nicht-Ziel der Anforderung („gesondert zu bewerten"). | bestätigt |
| 2 | Kein Umstieg auf SQLitePCLRaw 3.x; Update nur innerhalb des 2.x-Zweigs. | Explizites Nicht-Ziel der Anforderung. | bestätigt |
| 3 | Keine Updates weiterer Pakete: `coverlet.collector` 6.0.4, `Microsoft.NET.Test.Sdk` 17.14.1, `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4, `FlaUI.Core`/`FlaUI.UIA3` 5.0.0. | Anforderung nennt abschließend die sechs Pakete; diese Pakete sind nicht Teil des November-2026-Sicherheitslaufs. | bestätigt |
| 4 | Kein MAUI-Workload-Update (`dotnet workload` unverändert, weder lokal noch in der CI). | `MauiVersion` ist bewusst von der Workload entkoppelt (ADR 0016); die Workload liefert nur `Microsoft.Maui.Sdk`, die Pakete kommen aus NuGet. | bestätigt |
| 5 | Keine Funktions- oder Verhaltensänderung der Anwendung über die Paketaktualisierung hinaus; keine Schema-/Migrationsänderung. | Explizites Nicht-Ziel der Anforderung. | bestätigt |
| 6 | Kein neuer ADR und keine Änderung an `docs/adr/0016` (historisches Entscheidungsdokument zum Sprung auf 10.0.120). | Patch-Update innerhalb derselben Architekturentscheidung; Projektkonvention ist der `changes.log`-Eintrag (Vorbild: Eintrag 2026-10-25, Z. 97–102). | bestätigt |
| 7 | Keine Änderung an Build-/CI-Workflows, Skripten oder `.githooks`. | Der vorhandene Release-Weg erzeugt das neue Paket automatisch; keine Workflow-Versionen betroffen. | bestätigt |

## Akzeptanzszenarien

| # | Szenario | Nachweis |
|---|---|---|
| 1 | Alle sechs Pakete stehen auf den genannten oder neueren kompatiblen Patchständen | `Tankradar.MAUI.csproj`: `MauiVersion` = `10.0.130` (Z. 31, wirkt auf `Microsoft.Maui.Controls` Z. 104), `Microsoft.Extensions.Logging.Debug` ≥ `10.0.15`, `Microsoft.EntityFrameworkCore.Sqlite` ≥ `10.0.15`, `Microsoft.EntityFrameworkCore.Design` ≥ `10.0.15`, `SQLitePCLRaw.bundle_e_sqlite3` ≥ `2.1.14`; `Tankradar.Tests.E2E.csproj`: `Microsoft.Data.Sqlite` ≥ `10.0.15`, `SQLitePCLRaw.bundle_e_sqlite3` ≥ `2.1.14`. Prüfbar per `dotnet list Tankradar.sln package` bzw. Sichtung der csproj-Diffs gegen `staging`. |
| 2 | Vulnerability-Scan ohne Befund | `dotnet list Tankradar.sln package --vulnerable --include-transitive` meldet keine anfälligen Pakete (nach `dotnet restore`). Gleichwertig/blockierend in der CI: `node scripts/check-vulnerabilities.mjs --run Tankradar.sln` (Composite-Action `security-scan`, Exit ≠ 0 bei Funden). |
| 3 | Build erfolgreich | `dotnet build Tankradar.sln --configuration Release --no-restore` ohne Fehler/Warnungen (CI baut mit `-p:TreatWarningsAsErrors=true`); dabei keine NU1605-Auflösungskonflikte durch die neue `MauiVersion`. |
| 4 | Unit-, Integrations- und E2E-Tests grün | `dotnet test` für `src/Tankradar.Tests.Unit`, `src/Tankradar.Tests.Integration` und `src/Tankradar.Tests.E2E` (Release-Konfiguration) läuft fehlerfrei durch; Coverage-Gate 70 % unverändert erfüllt. |
| 5 | Neuer Build für ausgelieferte Artefakte | Nach Merge des Update-PRs auf `staging` erzeugt `staging-ci.yml` ein Pre-Release (`release-win-x64.zip`, self-contained — enthält die neuen Pakete); nach dem Promotion-Merge `staging` → `main` erzeugt `release.yml` das finale Release. Voraussetzung für `changed=true` in `determine-next-version.mjs`: Conventional-Commit mit `fix:`-Typ (s. Umsetzungsschritt 7 und Risiken). |

## Umsetzungsschritte

1. **Zielversionen auf nuget.org verifizieren**
   - `Microsoft.EntityFrameworkCore.Sqlite`/`.Design`, `Microsoft.Data.Sqlite`, `Microsoft.Extensions.Logging.Debug` ≥ 10.0.15; `Microsoft.Maui.Controls` ≥ 10.0.130; `SQLitePCLRaw.bundle_e_sqlite3` ≥ 2.1.14.
   - Existieren neuere kompatible Patchstände innerhalb der 10.0.x-/2.1.x-Linie, diese gemäß Akzeptanzkriterium „oder neuer" wählen und die Abweichung im PR dokumentieren (Details: Offener Punkt 1).

2. **`src/Tankradar.MAUI/Tankradar.MAUI.csproj` anpassen**
   - Z. 31: `<MauiVersion>10.0.120</MauiVersion>` → `10.0.130` (Kommentarblock Z. 28–30 bleibt — beschreibt den Mechanismus, keine Version; ggf. Versionsbezug „10.0.60" steht weiter für die iOS-Berechtigungsaussage und bleibt historisch korrekt).
   - Z. 105: `Version="10.0.0"` → `10.0.15` (`Microsoft.Extensions.Logging.Debug`).
   - Z. 106: `Version="10.0.10"` → `10.0.15` (`Microsoft.EntityFrameworkCore.Sqlite`).
   - Z. 108: `Version="2.1.12"` → `2.1.14` (`SQLitePCLRaw.bundle_e_sqlite3`); Kommentar Z. 107 (GHSA-2m69-gcr7-jv3q-Motiv) bleibt inhaltlich korrekt, keine Änderung.
   - Z. 109: `Version="10.0.10"` → `10.0.15` (`Microsoft.EntityFrameworkCore.Design`).

3. **`src/Tankradar.Tests.E2E/Tankradar.Tests.E2E.csproj` anpassen**
   - Z. 20: `Version="10.0.10"` → `10.0.15` (`Microsoft.Data.Sqlite`).
   - Z. 21: `Version="2.1.12"` → `2.1.14` (`SQLitePCLRaw.bundle_e_sqlite3`); Kommentar Z. 19 bleibt.

4. **Restore + Vulnerability-Scan**
   - `dotnet restore Tankradar.sln` (mit `IncludeAndroidTarget=false IncludeIosTarget=false IncludeMacCatalystTarget=false`, wie CI).
   - `dotnet list Tankradar.sln package --vulnerable --include-transitive` — ohne Befund; ergänzend `node scripts/check-vulnerabilities.mjs --run Tankradar.sln` (identisch zum CI-Gate).
   - Transitiv gezogene Nebenpakete der neuen `MauiVersion` sichten (`Microsoft.WindowsAppSDK`, `Microsoft.Graphics.Win2D`, `Microsoft.Web.WebView2`, `Microsoft.Windows.SDK.BuildTools` — Vorbild ADR 0016/changes.log Z. 101) und im `changes.log`-Eintrag dokumentieren.

5. **Build + Tests (lokal, vor Push — der pre-push-Hook erzwingt Build + Unit/Integration)**
   - `dotnet build Tankradar.sln --configuration Release --no-restore`.
   - `dotnet test` für die drei Testprojekte inkl. E2E (E2E braucht Windows-Desktop-Sitzung und die gebaute Exe; `TANKRADAR_E2E_WINDOW=offscreen` als Default).
   - Alternativ komplett über `scripts/local-ci.ps1` (voller CI-Spiegel: Format, Security-Scan, Warnungen-als-Fehler, Coverage 70 %, E2E blockierend).

6. **Dokumentation nachziehen**
   - `docs/help/Einstellungen/ablauf-technisch.md` Z. 7: „Entity Framework Core **10.0.10**" → neue EF-Version.
   - `docs/help/Navigation/ablauf-technisch.md` Z. 54: „`Microsoft.Maui.Controls` **10.0.120**" → neue MAUI-Version; vorher die fachliche Aussage (keine `Shell.TabBar*Shadow*`/`TabBar*Bold*`-Attached-Properties) gegen die neue Version verifizieren (Offener Punkt 2).
   - `changes.log`: neuer Eintrag oben im Muster `## <Datum> — <Titel> (Issue #63)`, Inhalt: angehobene Paketversionen, transitiv gezogene Änderungen, Scan-/Testergebnis, Abschluss „Keine Schemaänderung."; kein `RELEASE_NOTES.md`-Eintrag (keine nutzersichtbare App-Wirkung — Konvention analog zu CI-/Wartungsänderungen).
   - `docs/adr/0016` und `issue.md`/`requirement.md` bleiben unverändert (historisch bzw. Aufgabenbeschreibung).

7. **Commit, Push, PR gegen `staging`**
   - Conventional-Commit mit `fix:`-Typ (z. B. `fix(deps): NuGet-Pakete auf November-2026-Patchstand anheben, closes #63`) — nur `feat:`/`fix:` löst in `determine-next-version.mjs` `changed=true` aus; ohne Versionsänderung erzeugt `staging-ci` **kein** Pre-Release und Akzeptanzkriterium 5 wäre nicht erfüllbar.
   - Push erst nach lokalem grünen Lauf — `.githooks/pre-push` führt sonst `dotnet build` + `dotnet test` (Unit + Integration) aus und scheitert bei Fehlern; E2E lokal optional mit `PRE_PUSH_E2E=1` wiederholbar.
   - PR gegen `staging` (`verify-pr-source.yml` erzwingt `staging` als Ziel); PR-CI führt Scan, Build (Warnings als Fehler), Unit/Integration + Coverage, FlaUI-E2E und iOS-Compile selbst aus — Checks mit `--fail-fast` beobachten.
   - Nach Merge: staging-ci-Pre-Release abwarten; der automatische Promotion-PR `staging` → `main` (`staging-to-main-promotion.yml`) erzeugt nach dem Merge via `release.yml` das finale Release-Paket.

## Teststrategie

| Stufe | Kommando | Bemerkung |
|---|---|---|
| Restore | `dotnet restore Tankradar.sln` | Env: `IncludeAndroidTarget=false IncludeIosTarget=false IncludeMacCatalystTarget=false` (CI-Windows-Spiegel; verhindert NETSDK1147). |
| Build | `dotnet build Tankradar.sln --configuration Release --no-restore` | Vorbedingung für E2E (die Tests starten `src/Tankradar.MAUI/bin/Release/net10.0-windows10.0.19041.0/win-x64/Tankradar.MAUI.exe`). |
| Vulnerability-Scan | `dotnet list Tankradar.sln package --vulnerable --include-transitive` | Exakt das Akzeptanzkriterium; zusätzlich `node scripts/check-vulnerabilities.mjs --run Tankradar.sln` (CI-Identität). |
| Unit-Tests | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build` | ~350 Dateien; In-Memory-SQLite — betroffen vom EFCore/SQLitePCLRaw-Bump. |
| Integrationstests | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build` | ~68 Dateien; reale SQLite-Dateien, Migrations-Upgrade-Tests — prüfen den EFCore-Sprung gegen bestehende Datenbanken. |
| E2E-Tests | `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build` | ~113 Dateien; FlaUI/UIA3 startet die **gebaute Exe** — Realbedingungen: Windows-Desktop-Sitzung nötig, `TANKRADAR_E2E_WINDOW=offscreen` (Default; `foreground` möglich), `Microsoft.Data.Sqlite`-Bump betrifft direkte Zugriffe auf die isolierte Test-DB (`TankbookTestDatabase.cs`, `BackupTestTampering.cs`). |
| Komplettlauf | `scripts/local-ci.ps1` | Spiegelt die CI 1:1 inkl. Format-Check, Security-Scan, Coverage-Gate 70 % und iOS-Compile-Prüfung; `-SkipE2E` nur für Zwischenläufe. |
| Release-Nachweis | `staging-ci.yml` (Pre-Release) → Promotion → `release.yml` | Erfolgt auf GitHub nach Merge; `scripts/package-windows.ps1` baut self-contained `win-x64`. |

## Risiken

- **Transitive Verschiebungen durch `MauiVersion` 10.0.130:** MAUI 10.0.120 hob u. a. `Microsoft.WindowsAppSDK` auf ≥ 1.8.260529003, Win2D auf 1.4.0, WebView2 auf 1.0.4258.31 an (changes.log Z. 101). Für 10.0.130 ist die transitive Auflösung erneut zu prüfen — NU1605-Konflikte oder Paketgrößenwachstum sind möglich und würden im Restore/Build sichtbar.
- **Fehlende Versionsänderung im Pre-Release-Weg:** Ein `chore:`/`build:`-Commit ohne `feat:`/`fix:` erzeugt `changed=false` in `staging-ci` → kein RC-Build → Akzeptanzkriterium „neuer Build für ausgelieferte Artefakte" nicht erfüllt. Daher `fix:`-Commit (s. Schritt 7).
- **Sicherheitsfixes liegen in Runtime-kompatiblen Paketen, aber die eigentliche .NET-Runtime im self-contained Paket kommt aus dem SDK:** Die Runner verwenden `dotnet-version: '10.0.x'` — das aktuelle SDK (≥ 10.0.403) bringt die gepatchte Runtime mit; lokal ist 10.0.403 installiert. Kein zusätzlicher Schritt nötig, aber der Paket-Bump allein würde die Runtime-Sicherheitsfixes nicht liefern — der Rebuild via CI tut es.
- **pre-push-Hook:** `.githooks/pre-push` (Format, XML-Doku, `dotnet build`, `dotnet test` Unit + Integration) läuft vor jedem Push — ein Push ohne lokale Verifikation scheitert am Hook; E2E nur mit `PRE_PUSH_E2E=1`.
- **E2E-Flaky-Risiko:** Bekannter sporadischer Absturz 0xC0000005 im WinRT-Finalizer (Issue #22, ADR 0015) kann CI-E2E-Läufe unabhängig vom Update rot machen — bei Fehlschlag `e2e-dumps/`-Artefakte prüfen und nicht vorschnell dem Paket-Bump zuschreiben.
- **iOS-MTbf:** `EFCore.Design`-Bump ist design-time-only (`PrivateAssets=all`); iOS-`TrimmerRootAssembly`-Einträge sind versionsunabhängig. Der iOS-Compile-Check in der PR-CI deckt Restrisiken ab.

## Offene Punkte

Keine ungeklärten Punkte. Zwei ursprüngliche Klärungsfragen wurden vorab durch Recherche aufgelöst:

| # | Punkt | Auflösung |
|---|---|---|
| 1 | Verfügbarkeit/Zielstände | **Geklärt (2026-11-16, NuGet-v3-FlatContainer-Index geprüft):** Alle Zielversionen existieren und sind zugleich die neuesten Patchstände ihrer Linie — EF Core Sqlite/Design + Data.Sqlite + Logging.Debug: `10.0.15` (neuestes 10.0.x); Maui.Controls: `10.0.130` (neuestes 10.0.x); SQLitePCLRaw.bundle_e_sqlite3: `2.1.14` (neuestes 2.1.x). Es ist genau der im Issue genannte Stand zu setzen. |
| 2 | `Shell.TabBar*Shadow*`/`TabBar*Bold*` unter MAUI 10.0.130 | **Verifikationsaufgabe in Umsetzungsschritt 6 (kein Klärungsbedarf durch Anwender):** Nach dem Restore die XML-Dokumentation bzw. Assemblies von `microsoft.maui.controls/10.0.130` im NuGet-Cache auf `TabBarShadow`/`TabBar*Bold*` greppen; nur bei Neuexistenz den fachlichen Satz korrigieren, sonst lediglich die Versionsnummer im Hinweis aktualisieren. |
