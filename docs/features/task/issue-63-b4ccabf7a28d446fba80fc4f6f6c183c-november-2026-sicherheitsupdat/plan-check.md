# Plan-Check: November-2026-Sicherheitsupdates (Issue #63)

Status: Plan vollständig

Geprüft am 2026-11-16 gegen [requirement.md](requirement.md), [inventory.md](inventory.md) (inkl. aller Detaildokumente) und dem Repo-Ist-Zustand. Alle im Plan genannten Fundstellen, Zeilennummern, Versionsangaben, Skript- und Workflow-Behauptungen wurden stichprobenartig (in diesem Fall: vollständig für die betroffenen Dateien) verifiziert.

## Geprüfte Kriterien

### 1. Abdeckung aller Paketänderungen der Anforderung — erfüllt

Alle sechs Pakete der Anforderung sind im Plan mit korrekter Fundstelle abgedeckt (7 Versionsangaben in 2 csproj-Dateien):

- `src/Tankradar.MAUI/Tankradar.MAUI.csproj` — verifiziert:
  - Z. 31 `<MauiVersion>10.0.120</MauiVersion>` — steuert `Microsoft.Maui.Controls` `Version="$(MauiVersion)"` an Z. 104 (indirekte Referenz korrekt erkannt).
  - Z. 105 `Microsoft.Extensions.Logging.Debug` `10.0.0`; Z. 106 `Microsoft.EntityFrameworkCore.Sqlite` `10.0.10`; Z. 108 `SQLitePCLRaw.bundle_e_sqlite3` `2.1.12` (Kommentar GHSA-2m69-gcr7-jv3q Z. 107); Z. 109 `Microsoft.EntityFrameworkCore.Design` `10.0.10` mit `PrivateAssets=all`.
- `src/Tankradar.Tests.E2E/Tankradar.Tests.E2E.csproj` — verifiziert:
  - Z. 20 `Microsoft.Data.Sqlite` `10.0.10` (Kommentar Z. 19 „gleiche … SQLite-Version wie das Hauptprojekt"); Z. 21 `SQLitePCLRaw.bundle_e_sqlite3` `2.1.12`.
- Repo-weite Suche bestätigt: **keine** weiteren Fundstellen (kein `Directory.Packages.props`, keine Lock-Files, keine weiteren csproj-Referenzen der betroffenen Pakete). `PagesScreenshots.csproj` ist kein Solution-Mitglied und referenziert nur das E2E-Projekt — korrekt aus dem Scope gelassen.

### 2. Akzeptanzszenarien ableitbar und überprüfbar — erfüllt

- **Szenario 1 (Versionsstände):** Nachweis über csproj-Diff gegen `staging` bzw. `dotnet list Tankradar.sln package` — überprüfbar.
- **Szenario 2 (Vulnerability-Scan):** Exakt das Akzeptanzkriterium `dotnet list Tankradar.sln package --vulnerable --include-transitive` genannt; `node scripts/check-vulnerabilities.mjs --run Tankradar.sln` verifiziert (Skript ruft intern `dotnet list … --vulnerable --include-transitive --no-restore --format json`, Exit ≠ 0 bei Funden). Restore-Reihenfolge (Schritt 4: restore → scan) ist korrekt, da das Skript `--no-restore` nutzt.
- **Szenario 4 (Tests):** Alle drei Stufen mit exakten Kommandos (Release-Konfiguration, `--no-build`); Coverage-Gate 70 % erwähnt.
- **Szenario 5 (Release-Build):** Weg `staging-ci.yml` (Pre-Release, gated auf `changed == 'true'` via `scripts/determine-next-version.mjs --rc`) → Promotion-PR → `release.yml` verifiziert an den Workflow-Dateien; `verify-pr-source.yml` erzwingt tatsächlich nur `staging` → `main`.

### 3. Fehlende Schritte, falsche Annahmen, Risiken — keine falschen Annahmen gefunden

- **`fix:`-Commit-Typ:** Korrekt und verifiziert. `scripts/versioning.mjs` bildet `feat` → minor, `fix`/`perf` → patch ab; andere Typen (`chore`, `build`, `docs`, …) erzeugen `changed=false` → kein Pre-Release → Akzeptanzkriterium 5 wäre nicht erfüllbar. Der Plan nennt das Risiko explizit.
- **Closing-Keyword:** Beispiel-Commit `fix(deps): …, closes #63` enthält das Closing-Keyword; Issue schließt erst beim Erreichen von `main` — passt zum Staging-Workflow.
- **pre-push-Hook:** Verifiziert — `.githooks/pre-push` führt Branch-Schutz (keine Direktpushes auf `main`/`staging`), Conventional-Commits-Check, Strict-Checks (forbidden-patterns, Format, XML-Doku, Übersetzungen, Enum-Coverage, no-notimplemented) und `test-execution-check.py` aus (`dotnet build` + `dotnet test --no-build` für Unit+Integration; E2E nur mit `PRE_PUSH_E2E=1`). Plan-Schritt 7 deckt das korrekt ab.
- **E2E-Vorbedingung:** `E2ETestBase.cs:22` verifiziert — Tests starten `Tankradar.MAUI.exe` aus `bin/<Config>/net10.0-windows10.0.19041.0/win-x64/`; `TANKRADAR_E2E_WINDOW=offscreen` ist CI-Default (pr-staging-ci.yml Z. 179). Windows-Desktop-Sitzung ist Voraussetzung — Plan nennt dies explizit (Schritt 5, Teststrategie).
- **Transitives Risiko `MauiVersion` 10.0.130:** NU1605-/Paketgrößenrisiko korrekt benannt; Verifikation transitiver Nebenpakete ist expliziter Teil von Schritt 4 (Vorbild changes.log Z. 101, verifiziert: Eintrag 2026-10-25, Z. 97–105 dokumentiert den 10.0.120-Sprung inkl. WindowsAppSDK/Win2D/WebView2-Verschiebungen).
- **CI-Beobachtung:** „Checks mit `--fail-fast` beobachten" — korrekt.

### 4. Offene Punkte und Scope-Ausschlüsse — plausibel

- Beide ursprünglichen Klärungsfragen sind aufgelöst (NuGet-Verfügbarkeit der Zielstände; `Shell.TabBar*Shadow*`/`TabBar*Bold*`-Verifikation als Implementierungsaufgabe in Schritt 6 statt Anwender-Klärung — korrekt, da im NuGet-Cache prüfbar).
- Scope-Ausschlüsse 1–7 sind begründet und decken sich mit den Nicht-Zielen der Anforderung bzw. verifizierten Fakten (ADR 0016: `MauiVersion` entkoppelt von Workload; `dotnet-ef` nicht per `dotnet-tools.json` gepinnt; keine Workflow-Änderung nötig, da Release-Weg automatisch läuft).
- `docs/adr/0016` bleibt als historisches Entscheidungsdokument unverändert, `changes.log`-Eintrag folgt der verifizierten Konvention — plausibel.

### 5. Testbedarf / E2E-Abnahme — realistisch eingeplant

- Alle drei Teststufen sind mit exakten Kommandos eingeplant; E2E läuft lokal auf Windows (Entwicklungsmaschine ist Windows, SDK 10.0.403 + maui-windows-Workload verifiziert) und blockierend in der PR-CI (`pr-staging-ci.yml` Z. 169 ff., Off-Screen-Default, Crash-Dumps `e2e-dumps/`).
- `scripts/local-ci.ps1` als CI-Spiegel inkl. E2E ist als Komplettweg genannt; `-SkipE2E` nur für Zwischenläufe — realistisch.
- Flaky-Risiko (0xC0000005 WinRT-Finalizer, Issue #22) ist als bekanntes Risiko mit Diagnosehinweis (`e2e-dumps/`) dokumentiert — verhindert Fehlattribuierung an das Paket-Update.

## Hinweise (keine Lücken, zur Kenntnis)

1. **Squash-Merge-Nuance:** Schritt 7 verlangt einen `fix:`-Commit. Bei Squash-Merge des PRs nach `staging` bestimmt GitHub den Committext aus dem PR-Titel — der PR-Titel muss dann ebenfalls `fix:`-konform sein, damit `determine-next-version.mjs` `changed=true` liefert. Der Plan formuliert das am Commit, nicht am PR-Titel; in der Umsetzung ist beides konventionell zu halten (der pre-push-Hook prüft ohnehin alle Commit-Nachrichten).
2. **Ungenauigkeit „nur feat:/fix:"**: `versioning.mjs` löst auch `perf:` (patch) und Breaking Changes (major) aus. Für diese Aufgabe ist `fix:` die richtige Wahl; die Formulierung ist dadurch nicht falsch, nur unvollständig.
3. **pre-push-Hook-Umfang:** Der Plan nennt Format, XML-Doku, Build und Tests; der Hook führt zusätzlich forbidden-patterns, Übersetzungs- und Enum-Coverage-Checks aus. Da die Änderung nur Versionsstrings und Markdown betrifft, sind diese Checks voraussichtlich unkritisch — die verkürzte Nennung ändert nichts am Ablauf.

## Fazit

Der Plan deckt alle sechs Paketänderungen vollständig ab (inkl. `MauiVersion`-Indirektion und doppelter `SQLitePCLRaw`-Referenz), alle Akzeptanzkriterien sind in überprüfbare Szenarien übersetzt, die kritischste Falle (`fix:`-Commit für den Pre-Release-Trigger) ist verifiziert korrekt erkannt, und der E2E-/Release-Weg ist realistisch beschrieben. Keine inhaltlichen Lücken festgestellt; die drei Hinweise oben sind Feinschliff, keine Mängel.
