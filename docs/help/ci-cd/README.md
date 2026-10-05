← [Zurück zur Übersicht](index.md)

# CI/CD-Pipeline — Übersicht

## Branch-Modell

| Branch | Zweck | Wer darf was |
|---|---|---|
| `main` | Stabile Releases. Enthält nur veröffentlichten, getaggten Code. | Nur Pull Requests, und zwar ausschließlich von `staging` (erzwingt `verify-pr-source.yml`). |
| `staging` | Integrationszweig; jeder Push erzeugt ein Pre-Release (RC). | Nur Pull Requests aus Entwicklungszweigen. |
| Entwicklungszweige | Arbeit an Features und Korrekturen (z. B. `task/…`). | Pull Requests nach `staging`. |

## Ablauf

1. **Pull Request nach `staging`** — `PR CI for Staging` läuft: Formatprüfung, Sicherheitsprüfung der
   Abhängigkeiten, statische Analyse (Warnungen als Fehler, nur Windows-Ziel; die Apple-Ziele prüft der iOS-Build), Build, Unit- und
   Integrationstests mit Mindest-Testabdeckung (70 %), blockierende FlaUI-E2E-Tests (Windows) und ein
   unsignierter iOS-Build (Compile-Prüfung).
2. **Merge nach `staging`** — `Pre-Release` wiederholt die Prüfungen, ermittelt die nächste Version
   und veröffentlicht ein GitHub-Pre-Release `vX.Y.Z-rc.N` mit der Windows-App. Zusätzlich wird iOS
   gebaut (signiert und nach TestFlight hochgeladen, sobald `IOS_SIGNING_ENABLED` und die Secrets hinterlegt sind).
3. **Automatischer Promotion-Pull-Request** — Nach erfolgreichem `Pre-Release` eröffnet
   `Staging to Main Promotion` einen Entwurfs-PR `staging` → `main`. Ein Maintainer prüft und mergt.
4. **Merge nach `main`** — `Release` veröffentlicht `vX.Y.Z` als finales Release; `Backmerge Main to
   Staging` eröffnet einen PR `main` → `staging`, der **mit „Create a merge commit“** gemergt werden
   muss (nicht Squash/Rebase), damit der Release-Tag von `staging` aus erreichbar bleibt.
5. **Wöchentlich** — `Security Scan` prüft die Abhängigkeiten auch ohne Codeänderung auf neu bekannt
   gewordene Schwachstellen.

## Release-Artefakte

| Artefakt | Inhalt |
|---|---|
| `release-win-x64.zip` | Windows-App, self-contained (.NET-Laufzeit enthalten). Nach dem Entpacken `Tankradar.MAUI.exe` starten — keine Installation. Ein Installer (MSIX/Setup) wird bewusst nicht erstellt. |
| `update.json` | Manifest mit Version, Download-URL, SHA-256 und Größe des ZIPs. |
| iOS-`.ipa` | Nur als Workflow-Artefakt (14 Tage), nur bei `IOS_SIGNING_ENABLED=true` und vollständiger Signierung. Mit den API-Key-Secrets wird sie zusätzlich nach TestFlight hochgeladen. Sonst wird iOS unsigniert (Simulator-Build) gebaut; dieser Build dient als Compile-Prüfung und erzeugt kein Release-Asset. Lokal: [`scripts/iOS-Deployment.ps1`](ios-deployment.md). |

## Geheimnisse und Konfiguration

Geheimnisse und umgebungsspezifische Werte kommen **ausschließlich** über GitHub Secrets und
Repository-Variablen in die Pipeline, nie aus dem Repository. Welche Namen erwartet werden, steht in
der [Einrichtungs-Checkliste](einrichtung.md). Ist `IOS_SIGNING_ENABLED` nicht `true` oder fehlen Signierungsdaten, schlägt die
Pipeline deswegen nicht fehl, sondern baut iOS unsigniert und schreibt eine Notiz bzw. Warnung mit den fehlenden
Werten in das Job-Protokoll.

## Wenn GitHub Actions nicht läuft

Das Repository ist bis Version 1.0 privat; das Billing-Limit kann Actions-Läufe verhindern. Dann
führt [`scripts/local-ci.ps1`](lokaler-pruefung.md) dieselben Prüfungen lokal aus.

## Bekannte Einschränkungen

- Die FlaUI-E2E-Tests sind **blockierend** (bewusste Abweichung von der CI-Vorlage, dort best-effort): Eine
  Auslieferung (PR nach `staging`, Pre-Release auf `staging`) erfolgt nur mit vollständig grünen Tests.
  Sie zählen nicht zur Testabdeckung. Bei fehlgeschlagenen E2E-Tests erzeugt die Testbasis
  Diagnosedaten (siehe [E2E-Diagnosedaten](workflows.md#e2e-diagnosedaten)). Details und Betriebsart
  (Off-Screen): [E2E-Tests als Auslieferungs-Gate](workflows.md#e2e-tests-als-auslieferungs-gate).
- Die Mindest-Testabdeckung (70 %) bezieht sich auf die per `coverlet.runsettings` eingegrenzte
  Logik (ohne generierten Code, Plattformcode und XAML-Code-Behind).
- Die Workflows wurden syntaktisch geprüft (`scripts/validate-workflows.py`, `actionlint`); ein echter
  Lauf auf GitHub steht bis zur [Einrichtung](einrichtung.md) aus. Der iOS-Teil läuft nur auf
  macOS-Runnern und wurde in der Entwicklungsumgebung (Windows) nicht ausgeführt.
