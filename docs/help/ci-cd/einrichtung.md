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

## 4. Apple-seitige Vorbereitung (für signierte iOS-Builds und TestFlight)

Diese Schritte erfolgen im Apple-Developer-Konto (Mitgliedschaft im Apple Developer Program nötig). Ohne sie
baut die Pipeline iOS weiterhin unsigniert als Compile-Prüfung.

- [ ] **App-ID / Bundle-ID** unter *Certificates, Identifiers & Profiles → Identifiers* anlegen (Typ *App IDs → App*).
      Die Bundle-ID von Tankatlas lautet `de.martinstromberg.tankradar` (steht als `ApplicationId` in der `.csproj`; keine
      Geheiminformation).
- [ ] **App-Eintrag in App Store Connect** (*Apps → Neue App*) mit genau dieser Bundle-ID anlegen; ohne
      Eintrag lehnt der Upload den Build ab.
- [ ] **Distribution-Zertifikat** (*Apple Distribution*) erzeugen. Auf einem Mac in der Schlüsselbundverwaltung
      mit privatem Schlüssel als **`.p12`** exportieren und ein Passwort vergeben (wird zu
      `IOS_CERTIFICATE_P12_BASE64` und `IOS_CERTIFICATE_P12_PASSWORD`). Der Identitätsname (z. B.
      `Apple Distribution: Firma (TEAMID)`) wird zu `IOS_CODESIGN_KEY`.
- [ ] **Provisioning-Profil** vom Typ *App Store Connect* (Distribution) für die App-ID und das Zertifikat
      erzeugen und als `.mobileprovision` herunterladen. Der **Profilname** wird zu `IOS_PROVISIONING_PROFILE`,
      die Datei Base64-kodiert zu `IOS_PROVISIONING_PROFILE_BASE64`.
- [ ] **App-Store-Connect-API-Key** (*Benutzer und Zugriff → Integrationen → App Store Connect API*, Rolle
      *App Manager* oder höher) erzeugen: **Key-ID** (`IOS_API_KEY_ID`), **Issuer-ID** (`IOS_API_ISSUER_ID`)
      und die einmalig herunterladbare `.p8`-Datei (Inhalt wird zu `IOS_API_KEY_P8`).
- [ ] Die Dateien (`.p12`, `.mobileprovision`, `.p8`) **nie** ins Repository legen; der Pre-Commit-Hook blockiert
      sie. Nach dem Eintragen als Secret lokal sicher aufbewahren oder löschen.

## 5. Secrets (Settings → Secrets and variables → Actions → Secrets)

| Name | Inhalt | Pflicht |
|---|---|---|
| `FUEL_PRICE_API_KEY` | API-Schlüssel des Kraftstoffpreis-Dienstes; nur für den iOS-Build (TestFlight) | optional (ohne Schlüssel zeigt die App nur zuletzt bekannte Preise) |
| `ROUTING_API_KEY` | API-Schlüssel des Routing-Dienstes | optional, bis die App ihn nutzt |
| `IOS_CODESIGN_KEY` | Name der Signierungsidentität, z. B. `Apple Distribution: Firma (TEAMID)` | für signierten iOS-Build |
| `IOS_PROVISIONING_PROFILE` | Name des Provisioning-Profils (wie im Developer-Portal) | für signierten iOS-Build |
| `IOS_PROVISIONING_PROFILE_BASE64` | `.mobileprovision`-Datei, Base64-kodiert | für signierten iOS-Build |
| `IOS_CERTIFICATE_P12_BASE64` | Distribution-Zertifikat inkl. privatem Schlüssel (`.p12`), Base64-kodiert | für signierten iOS-Build |
| `IOS_CERTIFICATE_P12_PASSWORD` | Passwort der `.p12`-Datei | für signierten iOS-Build |
| `IOS_API_KEY_ID` | Key-ID des App-Store-Connect-API-Keys | für TestFlight-Upload |
| `IOS_API_ISSUER_ID` | Issuer-ID des App-Store-Connect-API-Keys | für TestFlight-Upload |
| `IOS_API_KEY_P8` | Inhalt der `.p8`-Datei des API-Keys (Klartext, mehrzeilig) | für TestFlight-Upload |

Der Schlüssel `FUEL_PRICE_API_KEY` steht nur dem iOS-Build als Umgebungsvariable `TANKRADAR_FUEL_PRICE_API_KEY` zur Verfügung (das Repository ist öffentlich: das Windows-Paket wird ohne Schlüssel gebaut, die `.ipa` nicht als Artefakt hochgeladen). `ROUTING_API_KEY` steht den Build-Schritten als `TANKRADAR_ROUTING_API_KEY` zur Verfügung. Die App übernimmt den Schlüssel beim Build als Tankerkönig-Schlüssel (er gelangt nie ins Build-Log; empfohlen ist eine regelmäßige Rotation, da er in der iOS-App selbst zwangsläufig enthalten ist) (siehe [Preisdaten](../Preisdaten/index.md)); der Routing-Schlüssel wird noch nicht gelesen.

Base64-Kodierung unter Windows: `[Convert]::ToBase64String([IO.File]::ReadAllBytes("datei.p12"))`
(auf dem Mac: `base64 -i datei.p12 | pbcopy`).

## 6. Repository-Variablen (… → Variables)

| Name | Inhalt | Pflicht |
|---|---|---|
| `IOS_SIGNING_ENABLED` | `true` schaltet signierten iOS-Build und TestFlight-Upload ein; jeder andere Wert oder fehlende Variable = unsignierter Compile-Check | zum Aktivieren |
| `APP_BUNDLE_ID` | Bundle-ID der App; nur nötig, um den Standardwert `de.martinstromberg.tankradar` aus der `.csproj` zu überschreiben | optional |
| `IOS_XCODE_VERSION` | Gewünschte Xcode-Version (z. B. `26.0`); leer = Runner-Standard | optional |

Verhalten der Pipeline:

- `IOS_SIGNING_ENABLED` nicht `true` → iOS wird unsigniert gebaut, **kein Fehlschlag**.
- `IOS_SIGNING_ENABLED=true`, aber ein Signierungs-Secret fehlt → Warnung mit den fehlenden Namen im Job-Protokoll,
  iOS wird unsigniert gebaut, kein Fehlschlag.
- Vollständige Signierungs-Secrets → signierte `release-ios.ipa` (nur im Runner, **kein** Workflow-Artefakt: Sie enthält den Tankerkönig-Schlüssel und das Repository ist öffentlich).
- Zusätzlich `IOS_API_KEY_ID`, `IOS_API_ISSUER_ID`, `IOS_API_KEY_P8` → Upload nach TestFlight per iTMSTransporter.

Die Team-ID wird nicht separat konfiguriert; sie steckt in Zertifikat und Provisioning-Profil
(`APPLE_TEAM_ID` der früheren Pipeline-Fassung entfällt).

## 7. Erste Beförderung nach `main` (Kaltstart)

Ein `workflow_run`-Trigger nutzt nur die Workflow-Datei auf dem Standardbranch. Solange
`staging-to-main-promotion.yml` nur auf `staging` existiert, entsteht kein automatischer Promotion-PR.

- [ ] Nach dem ersten Merge der Pipeline nach `staging` den ersten PR von Hand anlegen:
      `gh pr create --base main --head staging --draft --label automated-promotion --title "Automated promotion from staging to main" --body "Erste Beförderung (Kaltstart)."`
- [ ] Nach dem Merge dieses PR arbeitet die Automatik; den Backmerge-PR `main` → `staging` mit
      „Create a merge commit“ mergen.

## 8. Prüfung

- [ ] Lokaler Prüflauf erfolgreich: [`scripts/local-ci.ps1`](lokaler-pruefung.md).
- [ ] Erster PR nach `staging`: alle Statusprüfungen grün.
- [ ] Erster Push auf `staging`: Pre-Release `v0.1.0-rc.1` mit `release-win-x64.zip` und `update.json`.
- [ ] Erster Release auf `main`: `v0.1.0`.
- [ ] Nach dem Setzen von `IOS_SIGNING_ENABLED=true` und der Secrets: ein Pre-Release-Lauf erzeugt die
      signierte `.ipa`; bei gesetztem API-Key erscheint der Build nach der Verarbeitung in TestFlight.
      Die Workload-Version (`ios-workload-version` in `.github/actions/package-ios`) muss zum Xcode des
      Runners passen (der App Store lehnt Builds aus Beta-Xcode ab).

Was **nur** auf GitHub/macOS geprüft werden kann (unter Windows nicht ausführbar): Workload-Pinning,
signierter Build, Keychain-Import, iTMSTransporter-Suche/-Installation, TestFlight-Upload sowie alle
Aktionen von [`scripts/iOS-Deployment.ps1`](ios-deployment.md), die einen Mac benötigen.
