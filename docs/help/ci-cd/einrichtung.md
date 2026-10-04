← [Zurück zur Übersicht](index.md)

# Einmalige Einrichtung auf GitHub (Checkliste)

Diese Einstellungen können nicht im Repository abgelegt werden und müssen einmalig von einem
Repository-Administrator vorgenommen werden. Bis dahin sind die Workflows nur als Dateien vorhanden und
wurden noch nicht auf GitHub ausgeführt.

## 1. Branches

- [ ] `main` existiert (Standardbranch).
- [ ] `staging` existiert (z. B. `git push origin main:staging` durch den Administrator).

## 2. Branch-Schutz (Branch-Regeln)

- [ ] **`staging`:** Pull Request erforderlich; Statusprüfungen `static checks`, `build & test` und
      `ios build` erforderlich; Branch muss aktuell sein; direkte Pushes gesperrt.
- [ ] **`main`:** Pull Request erforderlich (nur von `staging`; zusätzlich Statusprüfung `verify-source`
      aus `Verify PR Source`); direkte Pushes gesperrt, Bypass nur bewusst für Administratoren.
- [ ] Merge-Methode für den Backmerge-PR `main` → `staging`: **„Create a merge commit“**.
- [ ] Actions-Berechtigung: *Workflow permissions* auf „Read and write“ und „Allow GitHub Actions to
      create and approve pull requests“ aktivieren (nötig für Promotion-/Backmerge-PRs und Releases).
- [ ] Kontrolle: Branch-Schutz mit `gh api repos/<owner>/<repo>/branches/<branch>/protection` prüfen,
      nicht aus einem erfolgreichen Push schließen.

## 3. Labels

- [ ] `automated-promotion` (Farbe `0E8A16`) und `automated-backmerge` (Farbe `1D76DB`). Die Workflows legen
      sie beim ersten Lauf selbst an; manuell: `gh label create automated-promotion --color 0E8A16`.

## 4. Secrets (Settings → Secrets and variables → Actions → Secrets)

| Name | Inhalt | Pflicht |
|---|---|---|
| `FUEL_PRICE_API_KEY` | API-Schlüssel des Kraftstoffpreis-Dienstes | optional, bis die App ihn nutzt |
| `ROUTING_API_KEY` | API-Schlüssel des Routing-Dienstes | optional, bis die App ihn nutzt |
| `IOS_CERTIFICATE_P12_BASE64` | Signierungszertifikat (`.p12`), Base64-kodiert | für signierten iOS-Build |
| `IOS_CERTIFICATE_PASSWORD` | Passwort des Zertifikats | für signierten iOS-Build |
| `IOS_PROVISIONING_PROFILE_BASE64` | Provisioning-Profil (`.mobileprovision`), Base64-kodiert | für signierten iOS-Build |

Die Schlüssel stehen den Build-Schritten als Umgebungsvariablen `TANKRADAR_FUEL_PRICE_API_KEY` und
`TANKRADAR_ROUTING_API_KEY` zur Verfügung. Die App liest sie derzeit noch nicht.

Base64-Kodierung unter Windows: `[Convert]::ToBase64String([IO.File]::ReadAllBytes("datei.p12"))`.

## 5. Repository-Variablen (… → Variables)

| Name | Inhalt | Pflicht |
|---|---|---|
| `APP_BUNDLE_ID` | Bundle-ID der App (ersetzt den Platzhalter `com.softwareschmiede.tankradar.dev`) | für signierten iOS-Build |
| `APPLE_TEAM_ID` | Apple-Team-ID | für signierten iOS-Build |
| `IOS_CODESIGN_KEY` | Name der Signierungsidentität, z. B. `Apple Distribution: Firma (TEAMID)` | für signierten iOS-Build |
| `IOS_PROVISIONING_PROFILE` | Name des Provisioning-Profils | für signierten iOS-Build |
| `IOS_XCODE_VERSION` | Gewünschte Xcode-Version (z. B. `26.0`); leer = Runner-Standard | optional |

Sind **nicht alle** iOS-Werte aus den beiden Tabellen vorhanden, baut die Pipeline iOS unsigniert
(Simulator-Build) und scheitert deswegen nicht.

## 6. Erste Beförderung nach `main` (Kaltstart)

Ein `workflow_run`-Trigger nutzt nur die Workflow-Datei auf dem Standardbranch. Solange
`staging-to-main-promotion.yml` nur auf `staging` existiert, entsteht kein automatischer Promotion-PR.

- [ ] Nach dem ersten Merge der Pipeline nach `staging` den ersten PR von Hand anlegen:
      `gh pr create --base main --head staging --draft --label automated-promotion --title "Automated promotion from staging to main" --body "Erste Beförderung (Kaltstart)."`
- [ ] Nach dem Merge dieses PR arbeitet die Automatik; den Backmerge-PR `main` → `staging` mit
      „Create a merge commit“ mergen.

## 7. Prüfung

- [ ] Lokaler Prüflauf erfolgreich: [`scripts/local-ci.ps1`](lokaler-pruefung.md).
- [ ] Erster PR nach `staging`: alle Statusprüfungen grün.
- [ ] Erster Push auf `staging`: Pre-Release `v0.1.0-rc.1` mit `release-win-x64.zip` und `update.json`.
- [ ] Erster Release auf `main`: `v0.1.0`.
