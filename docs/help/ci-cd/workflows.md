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
| `.github/actions/build-ios` | Baut iOS (iOS 16); signiert nur bei vollständigen Secrets/Variablen, sonst unsigniert. |

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
