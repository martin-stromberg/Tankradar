← [Zurück zur Übersicht](index.md)

# iOS-Deployment (`scripts/iOS-Deployment.ps1`)

Das Skript stammt aus einem bewährten anderen Projekt und wurde für Tankradar angepasst (Projekt
`src/Tankradar.MAUI/Tankradar.MAUI.csproj`, Umgebungsvariablen mit Präfix `TANKRADAR_IOS_*`). Es baut die
iOS-App, startet sie im Simulator oder auf einem Gerät und lädt signierte Builds nach TestFlight hoch. Unter
Windows läuft der Build über *Pair to Mac*, alles andere per SSH auf dem Mac. Die automatische Variante in
GitHub Actions ist der Baustein `.github/actions/package-ios` (siehe [Workflows](workflows.md)); beide nutzen
dieselbe iTMSTransporter-Suche.

## Aktionen

| `-Action` | Wirkung |
|---|---|
| `build` | iOS-App bauen; mit Signierungsdaten eine `.ipa`, sonst ohne Signierung. |
| `simulator` | Bauen, im Simulator starten, Screenshot ablegen (`-Video` zusätzlich App-Vorschau). |
| `device` | Bauen und auf einem angeschlossenen Gerät installieren und starten (`-Console` streamt die Ausgabe). |
| `store` | Signierten Release-Build erzeugen, validieren und nach App Store Connect/TestFlight hochladen; erhöht die Buildnummer (`-NoBumpBuildNumber` verhindert das, `-Version X.Y.Z` setzt eine neue Marketing-Version und die Buildnummer auf 1). |
| `upload` | Vorhandene `.ipa` (`-IpaPath`, sonst die neueste im Build-Verzeichnis) validieren und hochladen. |
| `list` | Simulatoren und Geräte anzeigen. |
| `menu` | Interaktives Menü (Standard). |

Hilfe: `Get-Help ./scripts/iOS-Deployment.ps1 -Full`.

## Voraussetzungen und Umgebungsvariablen

| Variable | Bedeutung |
|---|---|
| `TANKRADAR_IOS_MAC_SERVER_ADDRESS` | Adresse des Macs (Pair to Mac / SSH) |
| `TANKRADAR_IOS_MAC_SERVER_USER` | macOS-**Kurzname** des Benutzers (z. B. `martin`), nicht der volle Name |
| `TANKRADAR_IOS_MAC_SERVER_PASSWORD` | Passwort für Pair to Mac (nur der Build; wird nie im Klartext ausgegeben) |
| `TANKRADAR_IOS_MAC_DOTNET_ROOT` | .NET-Pfad auf dem Mac; Standard `/Users/<user>/Library/Caches/maui/PairToMac/SDKs/dotnet/` (Visual Studio 2026), bei VS 2022 `/Users/<user>/Library/Caches/Xamarin/XMA/SDKs/dotnet/` |
| `TANKRADAR_IOS_CODESIGN_KEY` | Signierungsidentität, z. B. `Apple Distribution: Firma (TEAMID)` |
| `TANKRADAR_IOS_PROVISIONING_PROFILE` | Name des Provisioning-Profils |
| `TANKRADAR_IOS_API_KEY_PATH` | Pfad zur `.p8`-Datei des API-Keys, **außerhalb** des Repositories |
| `TANKRADAR_IOS_API_KEY_ID` / `TANKRADAR_IOS_API_ISSUER_ID` | Key- und Issuer-ID aus App Store Connect |
| `TANKRADAR_IOS_TRANSPORTER_PATH` | Pfad zu iTMSTransporter auf dem Mac (überschreibt die automatische Suche) |
| `TANKRADAR_IOS_BUNDLE_ID` | Bundle-ID (wird als `ApplicationId` übergeben); ohne Angabe gilt der Wert `de.martinstromberg.tankradar` aus der `.csproj` |

Alle Werte lassen sich auch als Parameter übergeben (`-ServerAddress`, `-ServerUser`, `-CodesignKey`, …).
Die Apple-seitigen Voraussetzungen (App-ID, App-Eintrag, Zertifikat, Profil, API-Key) stehen in der
[Einrichtungs-Checkliste](einrichtung.md).

### Pair to Mac (Windows)

1. Auf dem Mac *Entfernte Anmeldung* (SSH) aktivieren und Xcode installieren.
2. In Visual Studio den Mac einmalig koppeln (*Tools → iOS → Pair to Mac*), damit die .NET-Umgebung auf dem
   Mac bereitgestellt wird; ihr Pfad ist `TANKRADAR_IOS_MAC_DOTNET_ROOT`.
3. `TANKRADAR_IOS_MAC_SERVER_ADDRESS`, `_USER` und `_PASSWORD` setzen.

### SSH-Schlüssel (für `simulator`, `device`, `store`, `upload`, `list` von Windows aus)

Diese Aktionen verwenden SSH/scp im `BatchMode` (kein Passwort-Prompt). Einmalig einrichten:

```powershell
ssh-keygen -t ed25519
type $env:USERPROFILE\.ssh\id_ed25519.pub | ssh <user>@<mac> "mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys"
ssh <user>@<mac> echo ok     # muss ohne Passwort-Abfrage antworten
```

### iTMSTransporter

Das Skript sucht das Werkzeug auf dem Mac in der Transporter-App (`/Applications`, `~/Applications`), unter
`/usr/local/itms`, im `PATH`, in Xcode und per Spotlight. Fehlt es, hilft die Transporter-App aus dem Mac App
Store oder das Paket aus dem Apple Transporter User Guide.

## Beispiele

```powershell
$env:TANKRADAR_IOS_MAC_SERVER_ADDRESS = "192.168.1.20"
$env:TANKRADAR_IOS_MAC_SERVER_USER    = "martin"
./scripts/iOS-Deployment.ps1 -Action list
./scripts/iOS-Deployment.ps1 -Action simulator
./scripts/iOS-Deployment.ps1 -Action store            # Build + Validierung + TestFlight-Upload
./scripts/iOS-Deployment.ps1 -Action upload -IpaPath C:\temp\Tankradar.ipa -NoPrompt
```

## Lokale Dateien

- `.ios-deploy.user.json` — gemerkte Auswahl von Zertifikat, Profil und Gerät je Aktion; nicht versioniert.
- `logs/ios-deploy-*.log` — Laufprotokolle (Transcript); nicht versioniert.
- Temporäre `.ios-deploy-*.rsp`-Dateien (Build-Parameter, enthalten ggf. das Pair-to-Mac-Passwort) werden nach dem
  Lauf gelöscht und sind ebenfalls ignoriert.
- `store` ändert `<ApplicationVersion>` in der `.csproj`; die Änderung kann committet werden.
- **Buildnummern nicht mischen:** Die CI verwendet Commit-Anzahl + `run_attempt` − 1 als `CFBundleVersion`, `store`
  zählt den Wert in der `.csproj` hoch. Ein lokaler Upload nach einem CI-Upload derselben Version wird von App Store
  Connect abgelehnt, wenn seine Nummer kleiner ist. Pro Version daher entweder nur CI oder nur lokal hochladen, oder
  die Nummer in der `.csproj` über die Commit-Anzahl (`git rev-list --count HEAD`) anheben.

## Abweichungen von der Vorlage

- Projekt, Umgebungsvariablen und Bundle-ID (`<ApplicationId>` der `.csproj`, optional `-BundleId`) für Tankradar.
- Die Store-Validierung der `.ipa` prüft Signatur, Distribution-Profil (`get-task-allow`), das Privacy-Manifest
  `PrivacyInfo.xcprivacy` im Bundle-Root (Pflicht, Fehler bei Fehlen; die Datei liegt unter
  `src/Tankradar.MAUI/Platforms/iOS/Resources/`), Gerätefamilie (iPhone muss enthalten sein),
  `ITSAppUsesNonExemptEncryption` = `false` (Pflicht; in der `Info.plist` gesetzt, die App nutzt nur Standard-HTTPS)
  und führt `iTMSTransporter -m verify` aus. Vorlagen-Invarianten, die für Tankradar nicht gelten (nur iPhone,
  `en`+`de`-Lokalisierung), sind abgeschwächt bzw. entfallen.
- Die Variablen `$isWindows`/`$isMacOS` heißen `$onWindows`/`$onMacOS`, weil PowerShell 7 gleichnamige
  Konstanten schreibgeschützt führt.
- Android-Anteile der Vorlage entfallen.

## Was unter Windows geprüft wird – und was nicht

`scripts/test-ios-deployment.ps1` (Teil von `scripts/local-ci.ps1` und des CI-Schritts *iOS deployment script
checks*) prüft ohne Mac: PowerShell-Syntax, Hilfe (`Get-Help`), das saubere Abbrechen mit verständlicher Meldung
bei fehlendem Mac, die Buildnummern-Logik und das Transporter-Suchskript.

**Nicht** prüfbar ohne Mac, daher bis zum ersten Lauf auf einem Mac bzw. auf GitHub offen: Pair to Mac, SSH/scp,
Simulator, Gerät, Signierung, Keychain, iTMSTransporter, Upload nach TestFlight.
