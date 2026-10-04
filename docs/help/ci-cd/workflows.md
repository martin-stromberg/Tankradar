← [Zurück zur Übersicht](index.md)

# Workflows und gemeinsame Bausteine

Alle Dateien liegen unter [`.github/`](../../../.github/). Grundlage ist die Vorlage
„CI Workflows for a Staging-Based .NET Release Pipeline“, angepasst an die MAUI-App.

## Die sieben Workflows

| Datei | Anzeigename | Trigger | Aufgabe |
|---|---|---|---|
| `verify-pr-source.yml` | Verify PR Source | PR → `main` | Lehnt PRs nach `main` ab, die nicht von `staging` kommen. |
| `pr-staging-ci.yml` | PR CI for Staging | PR → `staging` | Qualitätsprüfungen (`static checks`, `build & test`, `ios build`). Reine Rückführungs-PRs (`main` → `staging`) überspringen die Prüfungen. |
| `staging-ci.yml` | Pre-Release | Push → `staging` | Dieselben Prüfungen, dann `version`, `prerelease` (Windows-ZIP als GitHub-Pre-Release) und `ios-prerelease`. |
| `staging-to-main-promotion.yml` | Staging to Main Promotion | nach erfolgreichem `Pre-Release` | Eröffnet den Entwurfs-PR `staging` → `main` (Label `automated-promotion`). |
| `sync-staging-with-main.yml` | Backmerge Main to Staging | Push → `main` | Eröffnet den PR `main` → `staging` (Label `automated-backmerge`). |
| `release.yml` | Release | Push → `main` oder Tag `v*.*.*` | Ermittelt die Version, prüft/repariert vorhandene Releases, veröffentlicht `release-win-x64.zip` + `update.json`, baut iOS. |
| `security-scan.yml` | Security Scan | wöchentlich (Mo 04:00 UTC), manuell | Schwachstellenprüfung unabhängig von Codeänderungen. |

Wichtig: `staging-to-main-promotion.yml` referenziert den **Anzeigenamen** `Pre-Release`. Wer den Namen
in `staging-ci.yml` ändert, muss den Eintrag im selben Commit anpassen (`validate-workflows.py` prüft das).

## Gemeinsame Bausteine

| Baustein | Zweck |
|---|---|
| `.github/actions/security-scan` | `dotnet list package --vulnerable --include-transitive`; lässt den Schritt bei Funden **fehlschlagen**. Von `static checks` und vom wöchentlichen Scan genutzt. |
| `.github/actions/build-and-package` | Veröffentlicht die Windows-App und erzeugt ZIP und `update.json` (ruft `scripts/package-windows.ps1`). |
| `.github/actions/package-ios` | Baut iOS (iOS 16). Pinnt SDK-Band (`10.0.1xx`) und iOS-Workload auf eine zu Release-Xcode passende Version, setzt `MauiXamlInflator=XamlC`, die Buildnummer (`ApplicationVersion` = Commit-Anzahl + `run_attempt` − 1) und die Anzeigeversion (Pre-Release-Suffix gekürzt). Mit `IOS_SIGNING_ENABLED=true` und vollständigen Secrets: Import von Zertifikat und Profil in eine temporäre Keychain, signierte `release-ios.ipa`, optional TestFlight-Upload per iTMSTransporter, Aufräumen der Keychain. Sonst unsignierter Simulator-Build als Compile-Prüfung (kein Fehlschlag). Blendet die übrigen Zielframeworks per Eigenschaft aus (`IncludeAndroidTarget=false`, `IncludeMacCatalystTarget=false`), weil der Restore alle Zielframeworks der `.csproj` auswertet und bei fehlender Workload mit `NETSDK1147` scheitern würde; die Windows-Jobs blenden umgekehrt die Apple-Ziele aus (`IncludeIosTarget=false`, `IncludeMacCatalystTarget=false`). Der lokale Prüflauf (`scripts/local-ci.ps1`) simuliert die Windows-Jobs mit diesen `false`-Werten, stellt die Variablen danach wieder her und prüft iOS in einem eigenen Schritt „iOS-Compile-Prüfung“ (Apple-Ziel aktiv, Android aus; ohne iOS-Workload übersprungen). Einziger iOS-Pfad der Pipeline; der frühere Baustein `build-ios` entfällt. |

## Skripte

| Skript | Zweck |
|---|---|
| `scripts/versioning.mjs` | Reine Versionslogik (0.x-Regeln, RC-Nummer). |
| `scripts/determine-next-version.mjs` | Liest die Git-Historie und liefert `changed`, `version`, `rc_tag`, `rc_version`. |
| `scripts/resolve-release-version.mjs` | Entscheidet im Release-Workflow: neues Release, vorhandenes Release reparieren oder nichts tun. Pre-Releases werden nie repariert. |
| `scripts/check-coverage.mjs` | Prüft die Zeilenabdeckung gegen die Schwelle. |
| `scripts/package-windows.ps1` | Windows-Publish, ZIP und `update.json` (auch lokal nutzbar). |
| `scripts/validate-workflows.py` | Syntaktische und strukturelle Prüfung der Workflows. |
| `scripts/local-ci.ps1` | [Lokaler Prüflauf](lokaler-pruefung.md). |
| `scripts/iOS-Deployment.ps1` | [iOS-Build, Simulator, Gerät und TestFlight-Upload](ios-deployment.md) (lokal, mit Mac). |
| `scripts/test-ios-deployment.ps1` | Prüfung des iOS-Skripts ohne Mac (Syntax, Hilfe, Abbruchverhalten, Hilfsfunktionen). |

Tests der Node-Skripte: `npm test` (`scripts/*.test.mjs`, keine Abhängigkeiten nötig).

## Runner

Windows-spezifische Schritte (Format, Analyse, Build, Tests, Paket) laufen auf `windows-latest`, weil die
MAUI-Windows-App und die FlaUI-Tests Windows benötigen. iOS läuft auf `macos-latest`. Leichtgewichtige
Schritte (PR-Quellenprüfung, Versionsermittlung, Promotion) laufen auf `ubuntu-latest`.

## Abweichungen von der Vorlage

- **Versionsermittlung:** `semantic-release` entfällt. Es würde die erste Version als 1.0.0 vergeben und
  bei Breaking Changes automatisch auf 1.0 anheben, was der Projektvorgabe (0.1.0, keine automatische 1.0)
  widerspricht. Stattdessen ermittelt `determine-next-version.mjs` die Version nach denselben
  Conventional-Commits-Regeln. Folglich entfallen `release.config.js` und die npm-Abhängigkeiten.
- **Release-Erstellung:** Auch der automatische Weg nutzt die `gh` CLI (Vorlage, Abschnitt 11.3).
- **Linux-Variante und `release-metadata.json`** entfallen: Die App ist Windows/iOS-only und nutzt
  `msTools.Updater` nicht.
- **Plattformen:** `windows-latest`/`macos-latest` statt `ubuntu-latest` für Build und Tests.
- **Testprojekte:** Unit, Integration und E2E laufen getrennt; nur Unit und Integration fließen in die
  Coverage ein, E2E ist best-effort (Vorlage: „best-effort test category“).
- **Restore:** `dotnet restore` läuft mit `-p:Configuration=Release`, weil die MacCatalyst-Laufzeiten
  konfigurationsabhängig sind; `dotnet list package` läuft deshalb mit `--no-restore`.

## E2E-Diagnosedaten

Die FlaUI-E2E-Tests sind best-effort; damit ein Fehlschlag trotzdem nachvollziehbar bleibt, erfasst die
Testbasis (`E2ETestBase.RunWithDiagnostics`, `E2EDiagnostics`) bei einem fehlgeschlagenen Test pro Test:

- `<Testklasse>.<Test>.png` – Screenshot des App-Fensters (bei Startfehlern des gesamten Bildschirms),
- `<Testklasse>.<Test>.uitree.txt` – Dump des UI-Automation-Baums (Typ, Name, AutomationId, Klasse, Position),
- `<Testklasse>.<Test>.error.txt` – Fehlermeldung samt Stacktrace und Zustand des App-Prozesses.

Die Dateien liegen im Verzeichnis `e2e-diagnostics/` im Repository-Root (überschreibbar über die
Umgebungsvariable `TANKRADAR_E2E_DIAGNOSTICS_DIR`); es ist nicht versioniert. Beide CI-Workflows
(`pr-staging-ci.yml`, `staging-ci.yml`) laden es im Schritt `Upload E2E diagnostics` mit `if: always()` als
Artefakt `e2e-diagnostics-pr` bzw. `e2e-diagnostics-staging` hoch, also auch bei best-effort-Fehlschlägen.
Neue E2E-Tests kapseln ihren Testkörper dafür in `RunWithDiagnostics(...)`.
