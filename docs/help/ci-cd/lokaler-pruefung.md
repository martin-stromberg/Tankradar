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
| Sicherheitsprüfung der Abhängigkeiten | `Security scan` |
| Statische Analyse (Build aller Zielplattformen mit Warnungen als Fehler) | `Static analysis` |
| Unit- und Integrationstests mit Coverage, Mindestabdeckung 70 % | `Test …` / `Enforce coverage threshold` |
| FlaUI-E2E-Tests (best-effort, Fehlschlag = Warnung) | `Test E2E with FlaUI (best-effort)` |
| Windows-Paket (`-Package`) | `Build and package` |

Am Ende zeigt eine Tabelle den Status jedes Schritts. Exit-Code 0 bedeutet: alle blockierenden Prüfungen
bestanden. Berichte liegen unter `TestResults/` und `coverage-report/` (nicht versioniert). Schlägt ein E2E-Test fehl,
weist der Lauf auf das Diagnoseverzeichnis `e2e-diagnostics/` hin (Screenshot, UI-Baum, Fehlertext je Test;
nicht versioniert, siehe [E2E-Diagnosedaten](workflows.md#e2e-diagnosedaten)).

Der iOS-Teil wird lokal nur als Compile-Prüfung des iOS-Zielframeworks im Schritt „Statische Analyse“
abgedeckt; die iOS-Pakete selbst entstehen nur auf einem Mac bzw. im macOS-Runner. Zusätzlich prüft der Schritt
„iOS-Deployment-Skript“ (`scripts/test-ios-deployment.ps1`) Syntax, Hilfe und das saubere Abbrechen von
[`scripts/iOS-Deployment.ps1`](ios-deployment.md) ohne Mac.

## Hinweise

- Zum Abschluss eines Schritts empfiehlt sich der Lauf vor jedem PR nach `staging`.
- Die [Git-Hooks](../git-hooks/index.md) laufen zusätzlich bei Commit und Push; der lokale Prüflauf ersetzt
  sie nicht.
