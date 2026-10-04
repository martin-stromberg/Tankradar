# Umsetzungsplan

## Entscheidungen
- Versionsermittlung per eigenem, getestetem Node-Skript (`scripts/versioning.mjs`) statt semantic-release: 0.x-Regeln (erste Version 0.1.0; feat/breaking -> minor, fix/perf -> patch; nie automatisch >= 1.0.0). Release-Erstellung per `gh release create` (Vorlage 11.3). `resolve-release-version.mjs` bleibt wie in der Vorlage (Klassifizierung, Reparatur, Prerelease-Guard) inkl. Tests.
- Runner: `windows-latest` für Format/Analyse/Build/Test/Windows-Paket (MAUI-Windows, FlaUI), `macos-latest` für iOS (unsigniert, signiert nur bei vollständigen Secrets/Variablen), `ubuntu-latest` für Leichtgewichtiges.
- E2E best-effort (Vorlage „best-effort test category“), aus Coverage ausgeschlossen, Fehlschlag als Warnung + TRX-Artefakt.
- Coverage: `coverlet.runsettings` schließt generierten Code, Plattformcode, XAML-Code-Behind aus; Prüfung per `scripts/check-coverage.mjs`; zusätzliche Unit-Tests heben Abdeckung auf > 70 %.
- Windows-Paket: `scripts/package-windows.ps1` (publish, ZIP, update.json), von Composite-Action und lokalem Prüflauf gemeinsam genutzt.
- Lokaler Prüflauf: `scripts/local-ci.ps1`.

## Arbeitspakete
1. Restpunkte Schritt 2 (chmod, Docstring, checks.md, README, changes.log).
2. Build-Hygiene: CS1574/CS0436 beheben, E2E-Basis konfigurationsunabhängig, neue Unit-Tests.
3. `.github/actions/{security-scan,build-and-package,build-ios}`, sieben Workflows.
4. Skripte: versioning, resolve-release-version, determine-next-version, check-coverage (+Tests), package-windows.ps1, local-ci.ps1; `package.json`; `.gitignore`.
5. Dokumentation: `docs/help/ci-cd/` (Pipeline, Versionierung, lokaler Prüflauf, Einrichtungs-Checkliste), Index, README, changes.log.
6. Verifikation: Workflow-YAML parsen, Node-Tests, lokaler Prüflauf.

## Offene Punkte
(keine)
