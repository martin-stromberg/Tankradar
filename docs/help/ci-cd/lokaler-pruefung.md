← [Zurück zur Übersicht](index.md)

# Lokaler Prüflauf

Das Repository ist bis Version 1.0 privat, und GitHub Actions kann am Billing-Limit scheitern. Das Skript
[`scripts/local-ci.ps1`](../../../scripts/local-ci.ps1) führt deshalb dieselben Prüfungen wie die
Pipeline auf dem Entwicklungsrechner aus.

## Voraussetzungen

- .NET SDK 10.0.401 oder neuer mit den MAUI-Workloads (`dotnet workload restore Tankradar.sln`)
- Node.js 22 oder neuer (Skripttests, Abdeckungsprüfung)
- Python 3 mit PyYAML (Workflow-Validierung); optional `actionlint` im `PATH` für tiefergehende Prüfung
- PowerShell 7 (`pwsh`) oder Windows PowerShell
- Für die E2E-Tests eine interaktive Windows-Sitzung

## Aufruf

```powershell
.\scripts\local-ci.ps1                                  # alles
.\scripts\local-ci.ps1 -SkipE2E -SkipSecurityScan       # schneller bzw. offline
.\scripts\local-ci.ps1 -Package -PackageVersion 0.1.0   # zusätzlich Windows-Paket unter artifacts\
```

## Was geprüft wird

| Schritt | Entspricht in der Pipeline |
|---|---|
| Node-Tests der Pipeline-Skripte, Workflow-Validierung | `Pipeline script tests` / Dateiprüfung |
| Formatprüfung (`dotnet format --verify-no-changes --severity error`) | `Format check` |
| Sicherheitsprüfung der Abhängigkeiten (JSON-Auswertung über `scripts/check-vulnerabilities.mjs`, unabhängig von der Sprache des .NET SDK; gleiche Logik wie in der CI) | `Security scan` |
| Statische Analyse (Windows-Job-Simulation: nur Windows-Ziel, Warnungen als Fehler) | `Static analysis` |
| Unit- und Integrationstests mit Coverage, Mindestabdeckung 70 % | `Test …` / `Enforce coverage threshold` |
| FlaUI-E2E-Tests (best-effort, Fehlschlag = Warnung) | `Test E2E with FlaUI (best-effort)` |
| Windows-Paket (`-Package`) | `Build and package` |
| iOS-Compile-Prüfung (`net10.0-ios`, Simulator, Warnungen als Fehler, ohne Signierung; ohne lokale iOS-Workload übersprungen) | `iOS build` (unsignierter Simulator-Build) |

Am Ende zeigt eine Tabelle den Status jedes Schritts. Exit-Code 0 bedeutet: alle blockierenden Prüfungen
bestanden. Berichte liegen unter `TestResults/` und `coverage-report/` (nicht versioniert). Schlägt ein E2E-Test fehl,
weist der Lauf auf das Diagnoseverzeichnis `e2e-diagnostics/` hin (Screenshot, UI-Baum, Fehlertext je Test;
nicht versioniert, siehe [E2E-Diagnosedaten](workflows.md#e2e-diagnosedaten)).

Die Schritte von Restore bis Paketierung simulieren die Windows-Jobs der Pipeline: Das Skript setzt dafür
`IncludeAndroidTarget`, `IncludeIosTarget` und `IncludeMacCatalystTarget` auf `false` und `IncludeWindowsTarget` auf
`true` (nur das Windows-Ziel wird gebaut, auch bei vorbelegter Sitzung) und stellt die ursprünglichen Werte am Ende in jedem Fall wieder her (auch bei Fehler oder Abbruch), sodass
eine aufrufende PowerShell-Sitzung unverändert bleibt. Der iOS-Teil läuft als eigener Schritt
„iOS-Compile-Prüfung“ (Apple-Ziel aktiv, Android und Windows aus); danach stellt der Schritt den Windows-Restore
wieder her, damit spätere `--no-restore`-Builds nicht auf dem iOS-Restore-Zustand aufsetzen. Ist die iOS-Workload lokal nicht installiert, wird er mit
Hinweis übersprungen (kein Fehlschlag; das Skript installiert keine Workloads). Die iOS-Pakete selbst entstehen nur
auf einem Mac bzw. im macOS-Runner. Zusätzlich prüft der Schritt
„iOS-Deployment-Skript“ (`scripts/test-ios-deployment.ps1`) Syntax, Hilfe und das saubere Abbrechen von
[`scripts/iOS-Deployment.ps1`](ios-deployment.md) ohne Mac.

## Hinweise

- Zum Abschluss eines Schritts empfiehlt sich der Lauf vor jedem PR nach `staging`.
- Die [Git-Hooks](../git-hooks/index.md) laufen zusätzlich bei Commit und Push; der lokale Prüflauf ersetzt
  sie nicht.
