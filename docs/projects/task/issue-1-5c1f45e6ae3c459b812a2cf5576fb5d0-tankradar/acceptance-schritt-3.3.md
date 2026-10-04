# Abnahmeprüfung – Entwicklungsschritt 3

## Ergebnis

**Status:** Abweichungen gefunden

## Abweichungen

- [ ] **Der Pflichtjob `static checks` scheitert auf GitHub an der neuen Selbstprüfung in `scripts/test-ios-deployment.ps1`.** Betroffen sind `pr-staging-ci.yml` und `staging-ci.yml`.

  Die Ursache ist die Kombination aus Job-Umgebung und Prüfskript:
  - Die Nachbesserung setzt in den Windows-Jobs `IncludeIosTarget: 'false'` und `IncludeMacCatalystTarget: 'false'` als Job-Umgebungsvariablen. MSBuild liest Umgebungsvariablen als Eigenschaften.
  - Derselbe Job ruft im Schritt `iOS deployment script checks` das Skript `pwsh -NoProfile -File scripts/test-ios-deployment.ps1` auf.
  - Der neue Prüfblock „Zielframeworks per Eigenschaft ausblendbar“ (ca. Z. 147–155) erwartet ohne explizite `-p:`-Werte, dass `net10.0-ios` enthalten ist. Wegen der geerbten Umgebung trifft das im Job nicht zu.
  - Die Prüfung „nur net10.0-ios“ setzt `IncludeIosTarget` ebenfalls nicht explizit auf `true`. Sie liefert im Job deshalb eine leere Liste.

  Praktisch nachgestellt unter Windows mit `IncludeIosTarget=false IncludeMacCatalystTarget=false pwsh -NoProfile -File scripts/test-ios-deployment.ps1`:
  - `FEHLER  Standard: iOS-Zielframework enthalten net10.0-android;net10.0-windows10.0.19041.0`
  - `FEHLER  Ausgeblendet: nur net10.0-ios (macOS-Job)`
  - `iOS-Deployment-Pruefung FEHLGESCHLAGEN (2)`, Exit-Code 1.

  Ohne diese Umgebungsvariablen, also lokal und in `local-ci.ps1`, läuft das Skript grün. Deshalb ist der Fehler lokal nicht aufgefallen.

  Folgen:
  - Jeder PR nach `staging` wird rot, weil `static checks` eine Pflicht-Statusprüfung ist.
  - Auf `staging` hängt `version` per `needs` von `static-checks` ab. Damit entstehen keine Pre-Releases mehr: Weder `prerelease` noch `ios prerelease` laufen.

  Die Pipeline erfüllt damit die Anforderung „Pushes auf `staging` erzeugen automatisch Pre-Release-Versionen“ auf GitHub nicht. Der Prüfblock muss die Eigenschaften explizit setzen (`-p:IncludeIosTarget=true -p:IncludeMacCatalystTarget=true` bzw. für den iOS-Fall `-p:IncludeIosTarget=true`) oder die Umgebungsvariablen für die Aufrufe entfernen. Alternativ werden die Eigenschaften im Workflow nur in den dotnet-Schritten gesetzt.

## Hinweise

**Behebung der Abweichungen aus `acceptance-schritt-3.2.md`:**

- **NETSDK1147 im iOS-Build: behoben.**
  - Die `Tankradar.MAUI.csproj` koppelt alle vier Zielframeworks an `IncludeAndroidTarget`, `IncludeIosTarget`, `IncludeMacCatalystTarget` und `IncludeWindowsTarget` (Standard `true`).
  - `package-ios` setzt `IncludeAndroidTarget=false` und `IncludeMacCatalystTarget=false` bei der Workload-Installation und beim Build/Publish. Der Restore erfolgt implizit im selben Schritt. Der falsche Kommentar ist korrigiert.
  - Alle Windows-Jobs, die dotnet aufrufen, setzen `IncludeIosTarget=false` und `IncludeMacCatalystTarget=false` auf Job-Ebene. Das gilt damit auch für Workload-Restore, Restore, Format, Security-Scan, Build, Tests und `build-and-package`. Betroffen sind:
    - `static checks` und `build & test` (PR und staging)
    - `prerelease`
    - `release`
    - `security-scan`

  Die Eigenschaftswerte sind über alle Schritte konsistent.
- **PrivacyInfo-Prüfung: behoben.**
  - `Invoke-IpaValidation` bricht bei fehlendem oder ungültigem `PrivacyInfo.xcprivacy` mit `FEHLER … exit 1` ab, identisch zur Vorlage.
  - `ITSAppUsesNonExemptEncryption` ist wieder Pflicht und in der `Info.plist` auf `false` gesetzt. Damit ist auch die Empfehlung aus 3.2 umgesetzt.
  - Die Doku in `ios-deployment.md` ist korrigiert.
  - Der doppelte Block in `einrichtung.md` ist entfernt.

**Praktisch verifiziert unter Windows (SDK 10.0.401, Workloads android/ios/maccatalyst/maui-windows):**

- **`dotnet msbuild -getProperty:TargetFrameworks`:**

  | Kombination | Ergebnis |
  |---|---|
  | Standard | `net10.0-android;net10.0-ios;net10.0-maccatalyst;net10.0-windows10.0.19041.0` |
  | Windows-Jobs (auch per Umgebungsvariable) | `net10.0-android;net10.0-windows10.0.19041.0` |
  | iOS-Job (Windows-Ziel zusätzlich aus, um macOS nachzubilden) | `;net10.0-ios` |

- **Führendes Semikolon unschädlich:** In einer Kopie im Scratchpad liefen `dotnet restore` (Assets nur für `net10.0-ios`) und `dotnet build -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64 -p:MauiXamlInflator=XamlC` ohne Fehler durch. Ergebnis: 0 Fehler, 1 Warnung MAUI1001 wegen XamlC, wie in der Vorlage. Eine leere `TargetFrameworks`-Liste entsteht in keiner Workflow-Kombination.
- **Solution mit den Eigenschaften der Windows-Jobs** (`IncludeIosTarget=false`, `IncludeMacCatalystTarget=false` als Umgebungsvariablen), im Repository ausgeführt:
  - Restore OK
  - `dotnet build Tankradar.sln -c Release --no-restore -p:TreatWarningsAsErrors=true`: 0 Warnungen, 0 Fehler. Die Testprojekte mit Projektreferenz auf das MAUI-Projekt (`net10.0-windows10.0.19041.0`) bauen.
  - `dotnet format --verify-no-changes --severity error` OK
  - `dotnet list package --vulnerable` ohne Funde
  - Unit 15/15, Integration 3/3
- **Weitere Läufe:**
  - `scripts/local-ci.ps1 -SkipE2E`: erfolgreich, Exit-Code 0. Alle Schritte OK, Zeilenabdeckung 73,2 % bei 70 % Schwelle.
  - `scripts/test-ios-deployment.ps1` ohne Umgebungsvariablen: alle Prüfungen OK. Mit Umgebung der Windows-Jobs: Exit-Code 1, siehe Abweichung.
  - `npm test` 24/24
  - `python scripts/validate-workflows.py`: OK für 7 Workflows. `actionlint` ist lokal nicht vorhanden.
- **Ungewollter Nebeneffekt:** Ein versuchsweises `dotnet workload restore Tankradar.sln` wollte Workload-Pakete installieren. Der Vorgang brach an der fehlenden Rechteerhöhung ab und wurde zurückgerollt; es blieb ohne Änderung.
- **Nicht ausführbar:** alle macOS- und GitHub-gebundenen Teile (Workload-Pinning, Keychain, Signierung, iTMSTransporter, Upload).

**Weitere Beobachtungen (keine Abweichung):**

- **Android-Ziel in den Windows-Jobs:** Die Windows-Jobs bauen und prüfen weiterhin `net10.0-android` und installieren dafür die Android-Workload. Die Vorlage blendet dort `IncludeAndroidTarget=false` aus, und laut Anforderung hat Tankradar kein Android-Ziel. Das ist funktional unschädlich (Android-SDK auf `windows-latest` vorhanden), kostet aber Laufzeit. Die Doku nennt es bewusst („Windows- und Android-Ziel“).
- **Entfernen fremder iOS-Packs:** Wie in 3.2 angemerkt, löscht Tankradar nur das Versionsverzeichnis. Die Vorlage löscht das übergeordnete Pack-Verzeichnis (`dirname`). Das ist unkommentiert und vermutlich unschädlich, ein Beleg auf macOS steht aber aus.
- **Veralteter Satz im Projektplan:** Die Beschreibung von Schritt 1 in `project-plan.md` (Z. 61) spricht noch von einem „Platzhalterwert“ für die Bundle-ID. Das ist historisch und ohne Wirkung auf Schritt 3.
- **Übrige Punkte unverändert sachgerecht wie in 3.2 bewertet:**
  - Übernahme von `scripts/iOS-Deployment.ps1` mit allen Aktionen und `TANKRADAR_IOS_*`
  - Gate `IOS_SIGNING_ENABLED` und `IOS_*`-Secrets
  - Einrichtungs-Checkliste
  - Bundle-ID an allen Stellen
  - `drafts/` nicht versioniert
