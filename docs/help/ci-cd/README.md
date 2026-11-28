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
   Integrationstests mit Mindest-Testabdeckung (70 %) und die übrigen deterministischen Tests —
   alles blockierend (`Category!=OsInterface`); die als `OsInterface` markierten Tests, darunter alle
   FlaUI-E2E-Tests (Windows), laufen parallel im Job `os-interface tests` als best-effort. Dazu ein
   unsignierter iOS-Build (Compile-Prüfung).
2. **Merge nach `staging`** — `Pre-Release` wiederholt die Prüfungen, ermittelt die nächste Version
   und pusht den RC-Tag `vX.Y.Z-rc.N`; der iOS-Job baut signiert und lädt verbindlich nach
   TestFlight hoch (kein GitHub-Pre-Release mehr, Issue #64).
3. **Automatischer Promotion-Pull-Request** — Nach erfolgreichem `Pre-Release` eröffnet
   `Staging to Main Promotion` einen Entwurfs-PR `staging` → `main`. Ein Maintainer prüft und mergt.
4. **Merge nach `main`** — `Release` ermittelt `vX.Y.Z`, baut iOS signiert, lädt nach TestFlight und
   pusht danach den Release-Tag `vX.Y.Z` (kein GitHub-Release, Issue #64); `Backmerge Main to
   Staging` eröffnet einen PR `main` → `staging`, der **mit „Create a merge commit“** gemergt werden
   muss (nicht Squash/Rebase), damit der Release-Tag von `staging` aus erreichbar bleibt.
5. **Wöchentlich** — `Security Scan` prüft die Abhängigkeiten auch ohne Codeänderung auf neu bekannt
   gewordene Schwachstellen.
6. **Zeitgesteuert — Review-Issues** — Drei Workflows erzeugen wiederkehrend ein GitHub-Issue mit
   fest vorgegebenem Prüfauftrag, dessen Body mit der Erwähnung `@softwarewerkstatt` beginnt:
   monatlich das Architektur- und Sicherheitsreview (1. des Monats), wöchentlich die
   NuGet-Paketprüfung (dienstags) und jährlich das Compliance- und Copyright-Review (1. Januar).
   Existiert noch ein offenes Issue desselben Prüftyps (Erkennung über das jeweilige Label), wird
   kein neues erzeugt. Die Cron-Trigger wirken erst, sobald die Workflows auf `main` liegen.

## Release-Artefakte

Einziges Auslieferungsergebnis ist die signierte iOS-`.ipa` in App Store Connect (TestFlight) —
die GitHub-Release-Ablage ist entfernt (Issue #64, ADR 0023): Das Windows-ZIP `release-win-x64.zip`
und das Manifest `update.json` entfallen ebenso wie der `gh release`-Pfad; Release- und RC-Tags
werden als reine Git-Tags gepusht.

| Artefakt | Inhalt |
|---|---|
| iOS-`.ipa` | Entsteht im signierten Build und wird **nicht** als Workflow-Artefakt veröffentlicht (sie enthält den Tankerkönig-Schlüssel, das Repository ist öffentlich). In `release.yml` und `staging-ci.yml` ist der Upload nach TestFlight per iTMSTransporter verbindlich (`require-upload: 'true'`): fehlende Signierung oder fehlende Secrets lassen den Job fehlschlagen. Nur die PR-CI baut weiterhin unsigniert (Simulator-Build) als Compile-Prüfung. Lokal: [`scripts/iOS-Deployment.ps1`](ios-deployment.md). |

## Geheimnisse und Konfiguration

Geheimnisse und umgebungsspezifische Werte kommen **ausschließlich** über GitHub Secrets und
Repository-Variablen in die Pipeline, nie aus dem Repository. Welche Namen erwartet werden, steht in
der [Einrichtungs-Checkliste](einrichtung.md). In den Release- und Staging-Läufen ist der TestFlight-Upload
verbindlich (`require-upload: 'true'`): Ist `IOS_SIGNING_ENABLED` nicht `true` oder fehlen Signierungs- bzw.
API-Key-Secrets, schlägt der Job mit einer Fehlermeldung fehl — ein „Release ohne Auslieferung" kann nicht
still entstehen. Lediglich die PR-CI (`pr-staging-ci.yml`) baut in diesem Fall weiterhin unsigniert und
schreibt eine Notiz bzw. Warnung mit den fehlenden Werten in das Job-Protokoll.

## Wenn GitHub Actions nicht läuft

Das Repository ist bis Version 1.0 privat; das Billing-Limit kann Actions-Läufe verhindern. Dann
führt [`scripts/local-ci.ps1`](lokaler-pruefung.md) dieselben Prüfungen lokal aus.

## Bekannte Einschränkungen

- Die FlaUI-E2E-Tests laufen in der **best-effort-Lane** `os-interface tests` (vorlagenkonform): Sie
  sind als `Category=OsInterface` markiert — ebenso echte Prozessstarts und Loopback-Listener-Tests —
  und bleiben sichtbar, blockieren die Auslieferung aber nicht. Blockierend ist nur die
  deterministische Lane (`Category!=OsInterface`); sie liefert auch die Testabdeckung. Bei
  fehlgeschlagenen E2E-Tests erzeugt die Testbasis Diagnosedaten (siehe
  [E2E-Diagnosedaten](workflows.md#e2e-diagnosedaten)). Details, Betriebsart (Off-Screen) und die
  Retry-/Quarantäne-Konvention: [E2E-Tests und Lane-Modell](workflows.md#e2e-tests-und-lane-modell)
  sowie [OS-Schnittstellen-Tests](../stabilitaet/os-interface-tests.md).
- Die Mindest-Testabdeckung (70 %) bezieht sich auf die per `coverlet.runsettings` eingegrenzte
  Logik (ohne generierten Code, Plattformcode und XAML-Code-Behind).
- Die Workflows wurden syntaktisch geprüft (`scripts/validate-workflows.py`, `actionlint`); ein echter
  Lauf auf GitHub steht bis zur [Einrichtung](einrichtung.md) aus. Der iOS-Teil läuft nur auf
  macOS-Runnern und wurde in der Entwicklungsumgebung (Windows) nicht ausgeführt.
