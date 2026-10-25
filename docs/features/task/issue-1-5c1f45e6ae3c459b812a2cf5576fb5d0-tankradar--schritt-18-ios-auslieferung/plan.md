# Umsetzungsplan: Schritt 18 – iOS-Build, Signierung und TestFlight-Auslieferung (Tankradar/Tankatlas)

## Übersicht

Das vorhandene Auslieferungsgerüst (Skript `scripts/iOS-Deployment.ps1`, Action `package-ios`, Privacy-Manifest, Dateischutz) wird geprüft, geschlossen und dokumentiert: Mindestversion iOS 16 und verständliche Meldungen im Skript, vollständigere Tests ohne Mac, Berechtigungstexte, Privacy-Manifest, IPA-Prüfung auch in der CI, Datenschutzangaben für den App Store in der Projektdokumentation. Dazu kommen der Befund zum Absturz 0xC0000005 beim Beenden der App, fehlende ViewModel-Sortiertests, die Icon-Härtungen (17a) und die Routensuche-Härtungen (17). Ohne Mac prüfbar ist nur: Parser, Hilfsfunktionen, Textmuster, Dateiinhalte, Kompilierbarkeit des iOS-Zweigs (soweit unter Windows möglich), Workflow-Validierung. Nicht prüfbar: Signierung, Upload, tatsächlicher Inhalt der IPA (Icon 1024 px, `MinimumOSVersion`, Linker-Ergebnis zu `UserDefaults`), Verhalten der Berechtigungsdialoge.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Mindestversion iOS 16 | Prüfung an drei Stellen: `test-ios-deployment.ps1` (csproj `SupportedOSPlatformVersion` = 16.0), `Invoke-IpaValidation` und neuer Prüfschritt in `package-ios` (`MinimumOSVersion` der gebauten `Info.plist` muss mit `16.0` beginnen) | Die Quelle ist die csproj (nicht ändern), geprüft wird das Ergebnis im Paket; das ist auf dem Mac/CI die einzige Stelle, an der Abweichungen sichtbar werden. |
| IPA-Prüfung in der CI | Gemeinsame Prüflogik als eigener Schritt "Validate iOS package" in `package-ios/action.yml` (entpacken, `plutil`-Prüfungen für Privacy-Manifest, `ITSAppUsesNonExemptEncryption`, `MinimumOSVersion`, `UIDeviceFamily`), vor dem Upload, mit denselben Meldungstexten wie im Skript; kein Aufruf des PowerShell-Skripts | Das Skript ist auf Pair-to-Mac/SSH ausgelegt; ein Bash-Schritt ist auf dem Runner direkt ausführbar. Doppelte Logik wird durch einen Test in `test-ios-deployment.ps1` abgesichert, der beide Textmuster vergleicht. |
| Fehlende Werte im Skript | Zentrale Hilfsfunktion `Get-MissingDeployValues -Action` (reine Funktion, liefert Liste aus Parametername, Umgebungsvariable und Herkunftshinweis, z. B. Team-ID/Zertifikatsname aus Apple Developer, Profil, API-Key aus App Store Connect); `Assert-StorePrerequisites` und `Assert-CodesigningForAction` nutzen sie und melden **alle** fehlenden Werte der jeweiligen Aktion gesammelt, nie die der anderen | Verständliche Meldung, testbar ohne Mac, und eine Aktion blockiert keine andere (jede Aktion prüft nur ihre Werte). Team-ID wird als Hinweis aus dem Zertifikatsnamen "(TEAMID)" erklärt, nicht als neue Pflicht. |
| Datenschutz-Dokumentation | Neuer Bereich `docs/help/Datenschutz/` (`index.md`, `beschreibung.md` mit App-Store-Angaben, `ablauf-technisch.md` mit Privacy-Manifest und Required-Reason-APIs), verlinkt aus `docs/help/index.md`, `README.md` und `docs/help/ci-cd/ios-deployment.md`; Entscheidungen in neuem ADR `docs/adr/0015-ios-delivery-and-privacy.md` | Folgt der bestehenden Bereichsstruktur; die ADR hält die Begründungen fest (Berechtigungen, Schutzklasse, Keychain-Zugriffsklasse, Manifest). |
| Keychain-Formulierung | Zugriffsklasse **nicht ändern**; Formulierung präzisieren: "in der Keychain des Geräts (iOS) bzw. im Credential Locker (Windows); nie in der App-Sicherung der App, nie im Build; iOS-Keychain-Einträge können in verschlüsselten Gerätesicherungen enthalten sein und sind nicht mit iCloud synchronisiert" | Änderung auf `ThisDeviceOnly` ist über `SecureStorage` nicht ohne Eigenbau möglich und nicht gefordert; die Anforderung verlangt korrekte Formulierung. "gerätelokal" wird in Code-Kommentaren/XML-Doku, Hilfe, ADR 0014/0015 und Datenschutztext ersetzt bzw. präzisiert. |
| Berechtigungstexte | `NSLocationWhenInUseUsageDescription` = "Zur Ermittlung von Tankstellen in deiner Nähe und – wenn du es wählst – als Startpunkt einer Route."; Kamera/Fotos bleiben auf Belegfotos begrenzt. `NSPhotoLibraryUsageDescription` bleibt zunächst erhalten (unschädlich, `MediaPicker` kann ihn beim Speichern verlangen); Begründung in ADR 0015, Prüfung auf dem Gerät als Folgepunkt. | Der Schlüssel kann ohne Gerät nicht als ungenutzt belegt werden; Entfernen birgt den Absturz bei einem fehlenden Schlüssel (Zugriff ohne Beschreibung beendet die App), Behalten höchstens eine Rückfrage des App-Review. Dem "keine ungenutzten Berechtigungen" wird durch Dokumentation und Test (nur drei Schlüssel erlaubt) entsprochen. |
| Privacy-Manifest | Ergänzen: `NSPrivacyAccessedAPICategoryUserDefaults` (CA92.1) einkommentieren (MAUI Essentials/`Preferences`-Code kann im Bundle verbleiben); `FileTimestamp` zusätzlich `3B52.1` (vom Anwender gewählte Sicherungsdatei, Zeitstempel); gesammelte Datentypen: `PreciseLocation` (AppFunctionality) bleibt, zusätzlich `NSPrivacyCollectedDataTypeOtherUserContent`? Nein – Adress-/Ortseingaben werden an Dienste weitergegeben, aber nicht vom Entwickler gespeichert; Einordnung im ADR 0015 begründen und im Datenschutztext als "an Drittanbieter übermittelt, nicht gespeichert" benennen. `DiskSpace`/`SystemBootTime` bleiben (Laufzeit). | Fehlende Deklaration verursacht ITMS-91053; Überdeklaration wird nicht abgelehnt. Datentypen werden so gewählt, dass sie mit den Angaben in App Store Connect übereinstimmen. |
| Data Protection | Keine Codeänderung: `DatabaseFileProtector` (Klasse `CompleteUntilFirstUserAuthentication`) bleibt; Dokumentation, ob Export-/Sicherungsdateien betroffen sind (sie erben die Standardklasse). Ergänzend `ProtectFile` für fertige Sicherungs-ZIPs (`BackupService`) und Exportdateien (`TankbookExportService`) | Klein, konsistent mit Belegfotos; Dateien enthalten personenbezogene Tankbuchdaten. |
| Windows-ICO mehrere Größen | Eigenes MSBuild-Ziel in `Tankradar.MAUI.csproj` für Windows (nach dem Resizetizer-Ziel), das aus den vom Resizetizer erzeugten `appiconLogo.targetsize-{16,24,32,48,256}.png` per `RoslynCodeTaskFactory`-Inline-Task (keine neue Abhängigkeit) ein ICO (PNG-Einträge) schreibt und `ApplicationIcon` darauf setzt. Fallback, falls der Resizetizer-Zeitpunkt ungeeignet ist: eingechecktes ICO unter `Platforms/Windows/` plus Test, der es gegen die SVG-Quelle (Zeitstempel/Größen) prüft. | Der Resizetizer liefert nur 64 px; 16–256 px sind Anforderung. 64 px ist als Größe nicht in den Targetsize-Dateien enthalten, daher Größen 16, 24, 32, 48, 256 (und das 64er des Resizetizers, falls vorhanden). |
| Icon-Prüfung gegen veraltete Dateien | `create-review-version.ps1`: Publish-Ordner vor `dotnet publish` löschen, danach `LastWriteTime` der Icon-Dateien >= Startzeit des Builds und ICO-Kopf auslesen (Anzahl/Größen mindestens 16, 32, 48, 256 vorhanden) | Beseitigt das Bestehen mit Altdateien; reine Dateisystemprüfung. |
| Icon-Unit-Tests | `AppIconResourceTests_*` per `System.Xml.Linq` (`XDocument`): Wurzel `svg`, `viewBox`, Hintergrund = ein vollflächiges `rect` mit `linearGradient`-Füllung, kein `rx`, kein `opacity`; Vordergrund ohne vollflächiges Rechteck | Robust gegenüber Formatierung; kein neues Paket (`System.Xml.Linq` ist Teil der Basisbibliothek). |
| iOS-1024-Icon | Prüfung der Größe/Deckung auf SVG-Ebene (Quelle quadratisch, vollflächig deckend, Endgröße 1024 px liegt in der Resizetizer-Logik, `BaseSize`) und im Skript: `Invoke-IpaValidation` entpackt `Assets.car` mit `assetutil`, prüft den Eintrag `appiconItunesArtwork` auf 1024x1024 (Fehler bei abweichender Größe, Warnung bei nicht lesbar) | Auf dem Mac ausführbar, unter Windows nur als Textmuster in `test-ios-deployment.ps1` prüfbar. |
| Abfragetakt (Pacer) | `RouteQueryPacer` reserviert weiter, erhält aber die Rückmeldung zum tatsächlichen Absenden: Erweiterung um `MarkSent()` (aufgerufen unmittelbar nach `RequestThrottle` über einen Rückruf des Preisdienstes) bzw. alternativ zusätzliche Reserve `RouteSearchOptions.QueryIntervalReserve` (Standard 250 ms) auf das Intervall. Gewählt: **Reserve plus Messung ab Absenden** nur dann, wenn `IFuelPriceService.SearchNearbyAsync` den Zeitpunkt melden kann; sonst nur die Reserve (einfachste Lösung, kein Eingriff in `IFuelPriceService`). Entscheidung der Umsetzung: nur Reserve (250 ms) | Die Reserve deckt die gemessene Abweichung (13 ms) und die 1-s-Drosselung nicht verkürzbar; eine Schnittstellenänderung am Preisdienst ist unverhältnismäßig. Die ADR 0014 hält fest, dass der Abstand "mindestens 10 s zuzüglich Reserve" ab Reservierung beträgt. |
| Grenzmeldung | `SearchTexts.FormatRouteLimit` rundet `MaxSearchLengthKm` mit `Math.Floor` ab; der Satz "oder einen kleineren Abstand" entfällt, wenn `DistanceKm <= 1` | Die Hilfe nennt 85 km; die App muss dasselbe Ergebnis zeigen. Bei 1 km gibt es keinen kleineren Abstand. |
| Absturz beim Beenden (0xC0000005) | Befund dokumentiert (ADR 0015, Abschnitt "Befund"): betroffen ist die App beim Beenden durch den Test (Finalizer-Thread, WinRT-Objekte nach Abbau des Dispatchers). Maßnahmen: (1) `E2ETestBase.Dispose`/`RestartApplication` warten nach `Application.Close()` auf das Prozessende (15 s) und beenden sonst den Prozessbaum (`Kill`); (2) App-Seite: `PasswordVault`-Zugriff in `CredentialLockerApiKeyStore` wird je Aufruf in kurzem Gültigkeitsbereich gehalten (nicht als Feld) und die Handler in `OffscreenWindow` werden beim `Closed`-Ereignis des Fensters abgemeldet; (3) E2E-Diagnose: App-Ausgabe wird abgefangen und mit PID im Testprotokoll gekennzeichnet, Exit-Code `0xC0000005` wird als eigener Hinweis ausgegeben. | Der Absturz tritt selten und erst nach getaner Arbeit auf; eine sichere Behebung des CsWinRT-Verhaltens ist nicht zu belegen, die genannten Maßnahmen verkleinern die Angriffsfläche und machen das Ereignis eindeutig zuordenbar. Ein vollständiger Beweis ist nicht möglich, deshalb zusätzlich als Befund dokumentiert. |
| Sortiertests | Neue Testklasse `MapViewModelTests_Sort` (Unit) statt Erweiterung der großen Klassen (Konvention `test-class-structure`) | Themenfokussiert, klein. |
| Abfragegrenze nach Tankerkönig-Antwort | Eine Antwort liegt nicht vor: keine Änderung an `RouteSearchOptions`; Stand wird im ADR 0015 und im Abschlussbericht gemeldet | Anforderung: "nur falls Antwort vorliegt, sonst melden". |

## Programmabläufe

### Store-Lauf von Windows (unverändert, erweitert um Prüfungen)

1. `Assert-StorePrerequisites` sammelt alle fehlenden Werte über `Get-MissingDeployValues` und bricht mit einer Meldung samt Herkunftshinweis ab.
2. `Update-BuildNumber` erhöht `ApplicationVersion` (außer `-NoBumpBuildNumber`).
3. `Invoke-Build` baut und signiert; `Get-LatestIpa` findet die IPA.
4. `Invoke-IpaValidation` prüft zusätzlich `MinimumOSVersion` (>= 16.0, Fehler bei kleinerer oder fehlender Angabe) und die Größe des Marketing-Icons.
5. `Invoke-StoreUpload` lädt hoch. `upload` führt Schritte 3 bis 5 mit `-IpaPath` aus.

Beteiligte Klassen/Komponenten: `iOS-Deployment.ps1` (`Get-MissingDeployValues`, `Assert-StorePrerequisites`, `Invoke-IpaValidation`).

### CI-Paketierung

1. `package-ios` baut die IPA (unverändert).
2. Neuer Schritt "Validate iOS package": IPA entpacken, `PrivacyInfo.xcprivacy` vorhanden und gültig, `ITSAppUsesNonExemptEncryption=false`, `MinimumOSVersion` beginnt mit `16.`, `UIDeviceFamily` enthält 1; Fehler beendet den Schritt vor dem Upload.
3. Upload nach TestFlight (unverändert).

### Routensuche – Takt und Grenzmeldung

1. `RouteStationSearchService.SearchAsync` ruft `RouteQueryPacer.WaitAsync`; der Abstand beträgt `MinQueryInterval` plus `QueryIntervalReserve` ab der letzten Reservierung.
2. Überschreitet die Route die Grenze, formatiert `SearchTexts.FormatRouteLimit` die Länge abgerundet und lässt bei 1 km Abstand den Vorschlag eines kleineren Abstands weg.

### Routensuche abbrechen (E2E)

1. Mock-Tankerkönig antwortet verzögert (`ResponseDelay`), die Suche startet, die Fortschrittsanzeige erscheint.
2. Test löst `Search.Route.Cancel` aus; die App zeigt die Rückkehr in den Eingabezustand ohne Ergebnis und ohne Fehlermeldung; weitere Preisabfragen kommen nicht mehr an (Zähler am Mock bleibt stabil).

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `MapViewModelTests_Sort` | Testklasse | ViewModel-Tests für Name/Preis/Entfernung (siehe Tests). |
| `RouteE2ETests_Cancel` | Testklasse | E2E "Suche abbrechen". |
| `AppIconResourceTests_Xml` | Testklasse | XML-Auswertung der Icon-Quellen und der csproj-Einbindung (ersetzt Textsuche in `AppIconResourceTests_Motif`, dort verbleibt nur Motiv). |
| `IosProjectFilesTests` | Testklasse (Unit) | Info.plist (erlaubte Schlüssel, Texte) und PrivacyInfo.xcprivacy per XML-Auswertung. |
| `Docs/Datenschutz` (Dateien) | Dokumentation | `docs/help/Datenschutz/{index,beschreibung,ablauf-technisch}.md`, `docs/adr/0015-ios-delivery-and-privacy.md`. |

## Änderungen an bestehenden Klassen

### `iOS-Deployment.ps1` (Skript)

- **Neue Methoden:** `Get-MissingDeployValues` (reine Funktion), `Test-MinimumOsVersion` (Hilfsfunktion zur Prüfung eines Versionstexts gegen 16.0, testbar), `Get-IpaCandidates` falls `Get-LatestIpa` zerlegt wird.
- **Geänderte Methoden:** `Assert-StorePrerequisites`, `Assert-CodesigningForAction` (gesammelte Meldung mit Herkunft/Team-ID-Hinweis), `Invoke-IpaValidation` (MinimumOSVersion, Icon 1024), Kommentarkopf und Hilfe (Mindestversion, Datenschutzhinweis).

### `test-ios-deployment.ps1` (Skript)

- Neue Prüfgruppen: Mindestversion (csproj 16.0, Validierungsmuster, `Test-MinimumOsVersion` mit 15.0/16.0/17.2/leer), `Get-MissingDeployValues` je Aktion (alle fehlend, einzeln fehlend, keine Querwirkung zwischen Aktionen), Meldungen mit Herkunft, `Update-BuildNumber` Randfälle (ungültige `-Version` Exit 1, gleiche Version erhöht nur Buildnummer, fehlendes Tag nur Warnung), `Get-LatestIpa` (`-IpaPath` fehlt, mehrere verschiedene IPAs, bevorzugt `publish`), Info.plist (nur Schlüssel Standort/Kamera/Fotos, deutsche Texte, keine weiteren `NS*UsageDescription`), Privacy-Manifest (Tracking false, UserDefaults CA92.1, Reason-Codes), CI-Schritt "Validate iOS package" enthält dieselben Fehlertexte wie das Skript.

### `create-review-version.ps1` (Skript)

- Publish-Ordner vor dem Build leeren; Icon-Prüfung mit Zeitstempel und ICO-Größen; Fehlertext nennt veraltete/fehlende Datei.

### `Info.plist` / `PrivacyInfo.xcprivacy` (iOS-Ressourcen)

- Standorttext erweitern; `UserDefaults` (CA92.1) und `3B52.1` ergänzen; Kommentar im Manifest anpassen.

### `package-ios/action.yml`

- Neuer Schritt "Validate iOS package" vor "Upload to TestFlight" (nur bei signiertem Build). `validate-workflows.py` / `test_validate_workflows.py` anpassen, falls Regeln zur Schrittreihenfolge bestehen.

### `Tankradar.MAUI.csproj` (nur Windows-Zweig)

- Neues Ziel für das mehrgrößige ICO (`MtouchInterpreter` und `TrimmerRootAssembly` bleiben unverändert).

### `RouteQueryPacer` / `RouteSearchOptions`

- **Neue Eigenschaften:** `RouteSearchOptions.QueryIntervalReserve` (`TimeSpan`, Standard 250 ms) — Reserve auf den Abstand.
- **Geänderte Methoden:** `RouteQueryPacer.WaitAsync` — Slotlänge = `MinQueryInterval + QueryIntervalReserve` (Reservierung und Freigabe bei Abbruch konsistent).

### `SearchTexts.FormatRouteLimit`

- Abrunden (`Math.Floor`), Satzende abhängig von `limit.DistanceKm`.

### `E2ETestBase`

- `Dispose` und `RestartApplication`: Warten auf Prozessende, danach `Kill`; Diagnoseausgabe des Exit-Codes.

### `CredentialLockerApiKeyStore`, `OffscreenWindow`

- Kein `PasswordVault`-Feld halten; Handler beim `Closed` abmelden (klein, risikoarm). Wirkung nur durch mehrere CI-Läufe belegbar.

### `BackupService`, `TankbookExportService`

- Nach Fertigstellung der Datei `IDatabaseFileProtector.ProtectFile` aufrufen (Konstruktor erhält den Protector; bestehende Tests mit `FakeDatabaseFileProtector` anpassen).

### Texte/Doku "gerätelokal"

- Kommentare in `MauiProgram.cs`, `SecureStorageApiKeyStore`, `CredentialLockerApiKeyStore`, `RouteSearchAccess` präzisieren; Hilfe `docs/help/Suche/*`, `docs/help/Einstellungen/*` (Routing-Schlüssel), ADR 0014 (Absatz Schlüsselablage) und neuer Datenschutztext verwenden die präzise Formulierung.

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `-Version` (Skript) | `X.Y` oder `X.Y.Z` (unverändert) | Meldung, Exit 1 |
| `MinimumOSVersion` der IPA | Major >= 16 | Fehler vor dem Upload |
| Store-Werte (Zertifikat, Profil, API-Key, Key-ID, Issuer-ID) | alle vorhanden, Distribution-Zertifikat, `.p8` außerhalb des Repos | gesammelte Meldung mit Herkunftshinweis |
| Grenzmeldung Routenlänge | abgerundete Kilometer | — |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `RouteSearchOptions.QueryIntervalReserve` | `TimeSpan` | 250 ms | Reserve auf den Abstand der Routenabfragen |
| `NSLocationWhenInUseUsageDescription` | Info.plist | erweiterter Text | Routenstart ergänzt |
| `NSPrivacyAccessedAPICategoryUserDefaults` (CA92.1), `FileTimestamp` 3B52.1 | Privacy-Manifest | — | Required-Reason-APIs |

## Seiteneffekte und Risiken

- **Privacy-Manifest/Info.plist:** Änderungen sind erst beim nächsten TestFlight-Upload (Verarbeitung durch Apple) bestätigt; ohne Mac nicht prüfbar. Die bestehenden Pre-Releases rc.7–rc.12 zeigen, dass das Paket grundsätzlich angenommen wird.
- **Windows-ICO-Ziel:** Fehler im Inline-Task könnten den Windows-Build brechen; Fallback ist das eingecheckte ICO. `local-ci.ps1` und `create-review-version.ps1` prüfen das Ergebnis.
- **Pacer-Reserve:** Die Suche mit drei Abfragen dauert 0,5 s länger; E2E-Tests mit `TANKRADAR_ROUTE_QUERY_INTERVAL_MS` und Unit-Tests mit festem Intervall prüfen die Zahlen (anpassen).
- **Grenzmeldung:** `RouteE2ETests_Errors` und `SearchTextsTests_Route` erwarten den bisherigen Text; Hilfe ("85 km") und App stimmen danach überein.
- **Dateischutz für Sicherung/Export:** Konstruktoränderung wirkt auf Tests mit `BackupService`/`TankbookExportService`.
- **0xC0000005:** Die Wirksamkeit der Gegenmaßnahmen ist nur statistisch (mehrere CI-Läufe) zu belegen; der Befund wird dokumentiert.
- **Kein Mac:** iOS-Zweige (`#if IOS`), Skript-Aktionen mit SSH, Signierung, Upload, Icon 1024, Linker-Verhalten nicht ausführbar; nur Kompilierbarkeit (CI-Simulator-Build im `package-ios` ohne Signierung) und Textprüfungen.
- Nicht anfassen: `MtouchInterpreter`, `TrimmerRootAssembly`, `steps.md`, `status.html`, `project-plan.md`; nichts pushen; gezieltes Staging.

## Umsetzungsreihenfolge

1. **Befund 0xC0000005 und Testabbau absichern**
   - Voraussetzungen: Keine
   - Beschreibung: `E2ETestBase` (Warten/Kill, Exit-Code-Hinweis), `E2EDiagnostics` (Kennzeichnung der App-Ausgabe); `CredentialLockerApiKeyStore` und `OffscreenWindow` anpassen; Befund in ADR 0015 vorbereiten.
2. **Sortiertests im ViewModel**
   - Voraussetzungen: Testbasis `MapViewModelTestBase` vorhanden
   - Beschreibung: `MapViewModelTests_Sort` anlegen (siehe Tests); bestehenden Routen-Test um unterscheidbare Preise erweitern.
3. **Routensuche-Härtungen**
   - Voraussetzungen: `RouteSearchOptions`, `RouteQueryPacer`, `SearchTexts`
   - Beschreibung: `QueryIntervalReserve`, Pacer-Anpassung, `FormatRouteLimit` (Abrunden, 1 km), Tests anpassen, Hilfe (`Suche/beschreibung.md`, `ablauf-technisch.md`) und ADR 0014 angleichen; "gerätelokal" präzisieren.
4. **E2E "Suche abbrechen"**
   - Voraussetzungen: Schritt 3 (Takt), Mock-Verzögerung (`ResponseDelay`), `RouteE2ETestBase`
   - Beschreibung: `RouteE2ETests_Cancel` nach dem Muster von `RouteE2ETests_Accessibility`.
5. **iOS-Projektdateien und Tests**
   - Voraussetzungen: Keine
   - Beschreibung: `Info.plist`, `PrivacyInfo.xcprivacy`; `IosProjectFilesTests`; Dateischutz für Sicherung/Export (`ProtectFile`) mit Testanpassungen.
6. **Skript `iOS-Deployment.ps1` und `test-ios-deployment.ps1`**
   - Voraussetzungen: Schritt 5 (Manifest-Inhalte für die Tests)
   - Beschreibung: `Get-MissingDeployValues`, `Test-MinimumOsVersion`, Icon-Größenprüfung, erweiterte Tests.
7. **CI-Schritt "Validate iOS package"**
   - Voraussetzungen: Schritt 6 (Fehlertexte)
   - Beschreibung: `package-ios/action.yml`, `validate-workflows.py`/Tests, Vergleichstest im Skripttest.
8. **Icon-Härtungen**
   - Voraussetzungen: Keine
   - Beschreibung: Windows-ICO-Ziel in der csproj, `create-review-version.ps1`, `AppIconResourceTests_Xml`, `changes.log` korrigieren (Windows-App unverpackt: ICO für Datei/Fenster/Taskleiste; keine Kacheln/Startmenü).
9. **Dokumentation**
   - Voraussetzungen: Schritte 1–8
   - Beschreibung: `docs/help/Datenschutz/*` (Standort, Weitergabe von Adress-/Routeneingaben an Nominatim und OpenRouteService, Kartenkacheln an OpenStreetMap, Preisabfragen an Tankerkönig, keine Weitergabe von Fahrzeug-/Tankbuchdaten, Schlüssel in der Keychain mit korrekter Formulierung, Dateischutz, Verschlüsselungsangabe); ADR 0015; `docs/help/ci-cd/ios-deployment.md` (Mindestversion, neue Meldungen, neue Prüfungen); `docs/help/index.md`; `README.md`; `changes.log`; Hinweis zur Tankerkönig-Antwort (nicht vorliegend).
10. **Abnahmeprüfungen**
    - Voraussetzungen: Alle vorherigen
    - Beschreibung: `scripts/local-ci.ps1`, `scripts/test-ios-deployment.ps1`, `scripts/validate-workflows.py`, Pre-Push-Prüfungen, Review-Version 0.1.25 mit `scripts/create-review-version.ps1`.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Sort_ThreeOrders_AreDistinctForSearchResult` | `MapViewModelTests_Sort` | Drei Stationen, deren Reihenfolge nach Preis, Name und Entfernung paarweise verschieden ist; Wechsel Preis -> Name -> Entfernung -> Preis, keine neuen Abfragen |
| `Sort_AfterMultiplePages_ShowsFirstPageInNewOrder` | `MapViewModelTests_Sort` | >25 Stationen, "Weitere anzeigen", Sortierwechsel: erste Seite (25 Einträge) in neuer Reihenfolge, Zähler "übrig" zurückgesetzt |
| `Sort_WithFilterAndMissingValues_PutsUnknownLast` | `MapViewModelTests_Sort` | Filter auf eine Sorte, Stationen ohne Preis bzw. Entfernung am Ende |
| `Sort_RouteResult_PriceDiffersFromName` | `MapViewModelTests_Route` (angepasst) oder `MapViewModelTests_Sort` | Routenergebnis mit Preisreihenfolge verschieden von Name |
| `WaitAsync_UsesIntervalPlusReserve` / Abbruch gibt Reserve frei | `RouteQueryPacerTests` | Abstand = Intervall + Reserve, Freigabe bei Abbruch in der Wartezeit |
| `FormatRouteLimit_RoundsDown` (z. B. 85,9 -> 85) und `FormatRouteLimit_AtOneKm_OmitsSmallerDistanceHint` | `SearchTextsTests_Route` | Abrunden und Vorschlag nur bei Abstand > 1 km |
| `Info_plist_*`, `Privacy_manifest_*` | `IosProjectFilesTests` | Erlaubte Schlüssel (Standort, Kamera, Fotos), deutsche Begründungen, Routenstart im Standorttext, `ITSAppUsesNonExemptEncryption=false`, Manifest-Einträge |
| `Icon_Background_IsFullBleedOpaqueSquare`, `Icon_Foreground_HasNoFullSizeRect`, `Project_DeclaresIcon` | `AppIconResourceTests_Xml` | XML-Auswertung statt Textsuche |
| `BackupService_ProtectsArchive`, `Export_ProtectsFile` | bestehende Backup-/Export-Testklassen | `ProtectFile` wird für fertige Dateien aufgerufen (Fake-Protector) |
| Skriptprüfungen (siehe oben) | `test-ios-deployment.ps1` | Mindestversion, fehlende Werte, Buildnummer-Randfälle, IPA-Auswahl, Plist/Manifest, CI-Schritt |
| ICO-Kopf-Prüfung | `create-review-version.ps1` (Laufzeit) und Unit-Test für die Auslese-Funktion falls als eigene Datei | Größen 16, 32, 48, 256 vorhanden |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `RouteQueryPacerTests`, `RouteStationSearchServiceTests_*` | Slotlänge um die Reserve |
| `SearchTextsTests_Route`, `RouteE2ETests_Errors` | Abrunden, Satz bei 1 km |
| `AppIconResourceTests_Motif` | Textsuche entfällt, XML-Variante ergänzt |
| `BackupService`-/`TankbookExportService`-Tests, `FakeDatabaseFileProtector` | neuer Konstruktorparameter / Aufruf |
| `MapViewModelTests_Route.SortAndFilter_...` | unterscheidbare Preise |
| `test-ios-deployment.ps1` | erweitert (siehe oben) |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Routensuche läuft (verzögerte Antwort), "Suche abbrechen" beendet sie, kein Ergebnis, keine weitere Abfrage | `RouteE2ETests_Cancel` | Härtung 17: E2E "Suche abbrechen" | Nur im echten Zusammenspiel von Pacer, Dienst und Oberfläche belegbar |
| Pflicht | Grenzmeldung mit abgerundeten km (und ohne "kleineren Abstand" bei 1 km) sichtbar | `RouteE2ETests_Errors` (angepasst) | Härtung 17: Grenzmeldung | Sichtbarer Anwendertext |
| Kein neuer E2E | Sortierung | — | Anforderung 6 verlangt die Tests ausdrücklich im ViewModel, nicht im E2E | `SearchE2ETests_LargeResult` bleibt unverändert |
| Kein neuer E2E | Berechtigungstexte, Privacy-Manifest, Skript, CI, Icon | — | Betrifft Paketierung/iOS und keinen unter Windows über die UI auslösbaren Ablauf; Nachweis über Datei-/Skripttests und Review-Version |

Betroffene bestehende E2E-Tests:

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `RouteE2ETests_Errors` | Erwarteter Text der Grenzmeldung |
| Alle E2E (`E2ETestBase`) | Teardown wartet auf Prozessende / beendet den Baum |

## Zusatzaufgabe: wackeliger Test Restore_WithPooledConnectionHoldingWal_Succeeds

Siehe requirement.md Punkt 9. Ursache per build & test	Test integration (blocking)	﻿2026-10-25T08:52:38.1827815Z ##[group]Run dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults --logger "trx;LogFileName=test-results-integration.trx" --logger "console;verbosity=normal"
build & test	Test integration (blocking)	2026-10-25T08:52:38.1829965Z ^[[36;1mdotnet test src/Tankradar.Tests.Integration --configuration Release --no-build --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults --logger "trx;LogFileName=test-results-integration.trx" --logger "console;verbosity=normal"^[[0m
build & test	Test integration (blocking)	2026-10-25T08:52:38.1848580Z shell: C:\Program Files\Gitinash.EXE --noprofile --norc -e -o pipefail {0}
build & test	Test integration (blocking)	2026-10-25T08:52:38.1849254Z env:
build & test	Test integration (blocking)	2026-10-25T08:52:38.1850174Z   IncludeAndroidTarget: false
build & test	Test integration (blocking)	2026-10-25T08:52:38.1850443Z   IncludeIosTarget: false
build & test	Test integration (blocking)	2026-10-25T08:52:38.1850674Z   IncludeMacCatalystTarget: false
build & test	Test integration (blocking)	2026-10-25T08:52:38.1850924Z   COVERAGE_THRESHOLD: 70
build & test	Test integration (blocking)	2026-10-25T08:52:38.1851146Z   DOTNET_ROOT: C:\Program Files\dotnet
build & test	Test integration (blocking)	2026-10-25T08:52:38.1851853Z ##[endgroup]
build & test	Test integration (blocking)	2026-10-25T08:52:39.1746203Z Test run for D:\Tankradar\Tankradar\src\Tankradar.Tests.Integrationin\Release
et10.0-windows10.0.19041.0\Tankradar.Tests.Integration.dll (.NETCoreApp,Version=v10.0)
build & test	Test integration (blocking)	2026-10-25T08:52:39.4076780Z A total of 1 test files matched the specified pattern.
build & test	Test integration (blocking)	2026-10-25T08:52:42.5996843Z [xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.4+50e68bbb8b (64-bit .NET 10.0.13)
build & test	Test integration (blocking)	2026-10-25T08:52:42.7189558Z [xUnit.net 00:00:00.13]   Discovering: Tankradar.Tests.Integration
build & test	Test integration (blocking)	2026-10-25T08:52:42.8755725Z [xUnit.net 00:00:00.29]   Discovered:  Tankradar.Tests.Integration
build & test	Test integration (blocking)	2026-10-25T08:52:42.9571807Z [xUnit.net 00:00:00.37]   Starting:    Tankradar.Tests.Integration
build & test	Test integration (blocking)	2026-10-25T08:52:45.5711481Z   Passed Tankradar.Tests.Integration.Integration.PriceCacheMigrationTests_Upgrade.Initialize_FromSchemaBeforePriceCache_KeepsSettingsAndAddsTables [1 s]
build & test	Test integration (blocking)	2026-10-25T08:52:45.5713614Z   Passed Tankradar.Tests.Integration.Integration.TestDatabaseTests_Pooling.CreateFactory_UsesConnectionsWithoutPooling [3 ms]
build & test	Test integration (blocking)	2026-10-25T08:52:45.5715134Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_KnownDetails.GetKnownDetailsAsync_HandlesVeryLongIdLists [2 s]
build & test	Test integration (blocking)	2026-10-25T08:52:45.5717191Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_KnownDetails.WithDetails_TakesDetailsAndKeepsLivePrices [9 ms]
build & test	Test integration (blocking)	2026-10-25T08:52:47.0638610Z   Passed Tankradar.Tests.Integration.Integration.TestDatabaseTests_Pooling.Dispose_AfterServiceUse_RemovesDirectoryWithoutClearingPools [780 ms]
build & test	Test integration (blocking)	2026-10-25T08:52:47.0641024Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_KnownDetails.GetKnownDetailsAsync_ReturnsOnlyStationsWithDetails [917 ms]
build & test	Test integration (blocking)	2026-10-25T08:52:47.0642972Z   Passed Tankradar.Tests.Integration.Integration.TankbookStatisticsServiceTests_RealDatabase.Statistics_OnRealDatabaseFile_SurvivesRestartAndMatchesEntries [3 s]
build & test	Test integration (blocking)	2026-10-25T08:52:47.0644401Z   Passed Tankradar.Tests.Integration.Integration.TankbookEntryServiceTests_RealDatabase.DeleteVehicle_WithEntries_OnRealFile_RemovesEntriesAndPhotos [3 s]
build & test	Test integration (blocking)	2026-10-25T08:52:47.0645677Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_Stations.GetStationsAsync_ReturnsLatestPricesAndPositions [951 ms]
build & test	Test integration (blocking)	2026-10-25T08:52:48.5631999Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_Stations.SavePricesAsync_AppendsOnlyForKnownStations [817 ms]
build & test	Test integration (blocking)	2026-10-25T08:52:48.5633355Z   Passed Tankradar.Tests.Integration.Integration.TankbookEntryServiceTests_RealDatabase.Entries_SurviveRestartOnRealDatabaseFile [1 s]
build & test	Test integration (blocking)	2026-10-25T08:52:48.5634464Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_Stations.SavePricesAsync_KeepsMasterData [754 ms]
build & test	Test integration (blocking)	2026-10-25T08:52:48.5635591Z   Passed Tankradar.Tests.Integration.Integration.HomePrivacyTests_Persistence.AfterHomeWithNearby_DatabaseDoesNotContainPosition [2 s]
build & test	Test integration (blocking)	2026-10-25T08:52:50.1638608Z   Passed Tankradar.Tests.Integration.Integration.TankbookStatisticsServiceTests_RealDatabase.Statistics_ExistingData_NoMigrationNeeded_Unchanged [2 s]
build & test	Test integration (blocking)	2026-10-25T08:52:50.1643986Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_OwnerName.CustomName_SurvivesLiveDetailAndSearch [1 s]
build & test	Test integration (blocking)	2026-10-25T08:52:51.8183167Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_OwnerName.Search_OwnerNamedStation_ShowsBrandWithSubName [732 ms]
build & test	Test integration (blocking)	2026-10-25T08:52:51.8185948Z   Passed Tankradar.Tests.Integration.Integration.VehicleServiceTests_RealDatabase.DeleteVehicle_WithEntries_OnRealFile_RemovesEntries [1 s]
build & test	Test integration (blocking)	2026-10-25T08:52:51.8188114Z   Passed Tankradar.Tests.Integration.Integration.HomePrivacyTests_Persistence.HomeWithNearby_LogsNoPositionOrRequestAddress [2 s]
build & test	Test integration (blocking)	2026-10-25T08:52:51.8190740Z   Passed Tankradar.Tests.Integration.Integration.VehicleServiceTests_RealDatabase.Vehicles_SurviveRestartOnRealDatabaseFile [912 ms]
build & test	Test integration (blocking)	2026-10-25T08:52:53.3223711Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Import_PreparedDatabase_IsAcceptedAndRestoredCleanly(tamper: MigrationLockRow) [3 s]
build & test	Test integration (blocking)	2026-10-25T08:52:53.3225267Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_RealFormat.Search_WithoutKnownDetails_ShowsNoAutomatedHint [533 ms]
build & test	Test integration (blocking)	2026-10-25T08:52:53.3239633Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Atomicity.Restore_FailureAtStep_RestoresOldStateCompletely(failingStep: "journal") [2 s]
build & test	Test integration (blocking)	2026-10-25T08:52:53.3241089Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_RealFormat.Search_AfterDetailRequest_ShowsAutomatedHintFromCache [1 s]
build & test	Test integration (blocking)	2026-10-25T08:52:54.8268601Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_RealFormat.Search_With300Stations_ShowsPagedListAndStaysFast [1 s]
build & test	Test integration (blocking)	2026-10-25T08:52:54.8270234Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Atomicity.Restore_FailureAtStep_RestoresOldStateCompletely(failingStep: "database-moved") [1 s]
build & test	Test integration (blocking)	2026-10-25T08:52:56.3327657Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Import_PreparedDatabase_IsAcceptedAndRestoredCleanly(tamper: Trigger) [3 s]
build & test	Test integration (blocking)	2026-10-25T08:52:56.3329178Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Atomicity.Restore_FailureAtStep_RestoresOldStateCompletely(failingStep: "receipts-moved") [1 s]
build & test	Test integration (blocking)	2026-10-25T08:52:57.8333724Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Atomicity.Restore_FailureAtStep_RestoresOldStateCompletely(failingStep: "receipts-placed") [1 s]
build & test	Test integration (blocking)	2026-10-25T08:52:57.8337064Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Import_PreparedDatabase_IsAcceptedAndRestoredCleanly(tamper: ManipulatedSequence) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:52:59.3340130Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Atomicity.Restore_FailureAtStep_RestoresOldStateCompletely(failingStep: "database-placed") [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:00.8356751Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Import_PreparedDatabase_IsAcceptedAndRestoredCleanly(tamper: ExtraTable) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:00.8358587Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Atomicity.Restore_FailureAtStep_RestoresOldStateCompletely(failingStep: "protected") [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:02.3399023Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_Offline.ViewModel_ServerGoneWithoutCache_ShowsFailureInsteadOfEmptyState [6 s]
build & test	Test integration (blocking)	2026-10-25T08:53:02.3400637Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Atomicity.Restore_FailureAtStep_RestoresOldStateCompletely(failingStep: "before-commit") [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:03.7386804Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Atomicity.Crash_DuringSwap_IsRecoveredAtNextStart [721 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:03.7389685Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Import_PreparedDatabase_IsAcceptedAndRestoredCleanly(tamper: RemovedCollation) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:03.7392554Z   Passed Tankradar.Tests.Integration.Integration.PriceServiceMockServerTests_Flow.Search_TransientServerErrors_AreRetried [497 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:03.7395488Z   Passed Tankradar.Tests.Integration.Integration.RoutePrivacyTests_Logging.RouteSearchSuccessAndFailures_LogNothingSensitive [13 s]
build & test	Test integration (blocking)	2026-10-25T08:53:03.7397485Z   Passed Tankradar.Tests.Integration.Integration.LoopbackHttpListenerTests_Retry.Start_PortAlwaysTaken_ThrowsAfterMaxAttempts [69 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:03.7399414Z   Passed Tankradar.Tests.Integration.Integration.LoopbackHttpListenerTests_Retry.Start_PortTaken_RetriesWithNewPort [69 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:03.7401382Z   Passed Tankradar.Tests.Integration.Integration.LoopbackHttpListenerTests_Retry.ParallelStarts_GetDistinctPorts [16 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:05.2382123Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Import_PreparedDatabase_IsAcceptedAndRestoredCleanly(tamper: PrefixedTrigger) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:05.2385180Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UnusableDatabase_IsRejected_WithoutLeftovers(tamper: OrphanEntry) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:06.7460311Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UnusableDatabase_IsRejected_WithoutLeftovers(tamper: HugeBrand) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:08.2519722Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Import_PreparedDatabase_IsAcceptedAndRestoredCleanly(tamper: View) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:08.2521783Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UnusableDatabase_IsRejected_WithoutLeftovers(tamper: HugeEntryNote) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:08.2523606Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_Offline.Search_ServerGone_ReturnsCachedStationsWithAge [6 s]
build & test	Test integration (blocking)	2026-10-25T08:53:09.7589439Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_Offline.Search_NoConnection_UsesCacheWithoutRequest [780 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:09.7591236Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UnusableDatabase_IsRejected_WithoutLeftovers(tamper: UnknownFuelKind) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:09.7592882Z   Passed Tankradar.Tests.Integration.Integration.PriceServiceMockServerTests_Flow.Search_ServerGone_ReturnsLastKnownPrices [6 s]
build & test	Test integration (blocking)	2026-10-25T08:53:11.2603730Z   Passed Tankradar.Tests.Integration.Integration.PriceServiceMockServerTests_Flow.Search_AgainstMock_ReturnsStationsAndPrices [640 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:11.2606298Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Import_PreparedDatabase_IsAcceptedAndRestoredCleanly(tamper: ExtraCheck) [3 s]
build & test	Test integration (blocking)	2026-10-25T08:53:11.2609222Z   Passed Tankradar.Tests.Integration.Integration.PriceServiceMockServerTests_Flow.Search_KeyProblems_AreReportedWithoutThrowing [643 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:11.2611805Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UnusableDatabase_IsRejected_WithoutLeftovers(tamper: HugeEntryStationName) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:12.7706863Z   Passed Tankradar.Tests.Integration.Integration.PriceServiceMockServerTests_Flow.Detail_AgainstMock_DerivesAutomatedStationFromOpeningTimes [733 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:14.2743783Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UnusableDatabase_IsRejected_WithoutLeftovers(tamper: OversizedCustomName) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:14.2745606Z   Passed Tankradar.Tests.Integration.Integration.PriceServiceMockServerTests_Flow.Search_SlowServer_TimesOutAndFallsBackToCache [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:14.2747163Z   Passed Tankradar.Tests.Integration.Integration.PriceServiceMockServerTests_Flow.Search_ConnectionRestored_FetchesLiveAgain [579 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:15.7804156Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Import_PreparedDatabase_IsAcceptedAndRestoredCleanly(tamper: ExtraColumn) [3 s]
build & test	Test integration (blocking)	2026-10-25T08:53:15.7806007Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UnusableDatabase_IsRejected_WithoutLeftovers(tamper: MissingTable) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:15.7807479Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Resilience.List_WithLockedFile_StillListsAllFiles [951 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:15.7808646Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_Offline.ViewModel_ServerGone_ShowsBannerStaleAgeAndUnconfirmedHint [6 s]
build & test	Test integration (blocking)	2026-10-25T08:53:17.2863872Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UnusableDatabase_IsRejected_WithoutLeftovers(tamper: ViewAsTable) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:17.2866310Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Resilience.Restore_WhenJournalWriteFails_KeepsLiveData [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:18.4172237Z   Passed Tankradar.Tests.Integration.Integration.StationCustomNameMigrationTests_Upgrade.Initialize_FromSchemaBeforeCustomName_KeepsDataWithoutCustomName [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:18.4175389Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Resilience.Undo_WithUnreadableJournal_ReturnsRollbackOnly [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:18.4177893Z   Passed Tankradar.Tests.Integration.Integration.DatabaseInitializerTests_FirstStart.Initialize_FirstStart_CreatesDatabaseFileAndSchema [539 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:18.4180073Z   Passed Tankradar.Tests.Integration.Integration.MigrationsTests_ModelSync.Model_HasNoPendingChanges [4 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:18.4182503Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UnusableDatabase_IsRejected_WithoutLeftovers(tamper: HugeGroupName) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:18.4183883Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Import_PreparedDatabase_IsAcceptedAndRestoredCleanly(tamper: ExtraIndex) [4 s]
build & test	Test integration (blocking)	2026-10-25T08:53:20.3021686Z   Passed Tankradar.Tests.Integration.Integration.AddressSearchMockServerTests_Flow.Berlin_Radius5_ShowsNearStationsAroundResolvedPosition [981 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:20.3032157Z   Passed Tankradar.Tests.Integration.Integration.AddressSearchMockServerTests_Flow.AddressSearch_DoesNotUseTestLocation [716 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:21.8010984Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UnusableDatabase_IsRejected_WithoutLeftovers(tamper: InvalidDate) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:21.8013600Z   Passed Tankradar.Tests.Integration.Integration.AddressSearchMockServerTests_Flow.InvalidInput_ReachesNeitherService [616 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:21.8015410Z   Passed Tankradar.Tests.Integration.Integration.AddressSearchMockServerTests_Flow.Offline_SendsNoRequests [490 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:21.8018055Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Restore_PreparedDatabase_IsRebuiltCleanly(tamper: PrefixedTrigger) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:21.8020321Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UnusableDatabase_IsRejected_WithoutLeftovers(tamper: DuplicateName) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:23.1614761Z [xUnit.net 00:00:40.57]     Tankradar.Tests.Integration.Integration.BackupServiceTests_Resilience.Restore_WithPooledConnectionHoldingWal_Succeeds [FAIL]
build & test	Test integration (blocking)	2026-10-25T08:53:23.1623018Z [xUnit.net 00:00:40.57]       Assert.True() Failure
build & test	Test integration (blocking)	2026-10-25T08:53:23.1623762Z [xUnit.net 00:00:40.57]       Expected: True
build & test	Test integration (blocking)	2026-10-25T08:53:23.1624433Z [xUnit.net 00:00:40.57]       Actual:   False
build & test	Test integration (blocking)	2026-10-25T08:53:23.1628308Z [xUnit.net 00:00:40.58]       Stack Trace:
build & test	Test integration (blocking)	2026-10-25T08:53:23.1638299Z [xUnit.net 00:00:40.58]         D:\Tankradar\Tankradar\src\Tankradar.Tests.Integration\Integration\BackupServiceTests_Resilience.cs(242,0): at Tankradar.Tests.Integration.Integration.BackupServiceTests_Resilience.Restore_WithPooledConnectionHoldingWal_Succeeds()
build & test	Test integration (blocking)	2026-10-25T08:53:23.1641008Z [xUnit.net 00:00:40.58]         --- End of stack trace from previous location ---
build & test	Test integration (blocking)	2026-10-25T08:53:23.4867660Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Resilience.Recovery_WhenUndoFails_ThrowsAndKeepsRollback [4 s]
build & test	Test integration (blocking)	2026-10-25T08:53:23.4870491Z   Failed Tankradar.Tests.Integration.Integration.BackupServiceTests_Resilience.Restore_WithPooledConnectionHoldingWal_Succeeds [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:23.4872067Z   Error Message:
build & test	Test integration (blocking)	2026-10-25T08:53:23.4872483Z    Assert.True() Failure
build & test	Test integration (blocking)	2026-10-25T08:53:23.4872865Z Expected: True
build & test	Test integration (blocking)	2026-10-25T08:53:23.4873174Z Actual:   False
build & test	Test integration (blocking)	2026-10-25T08:53:23.4873598Z   Stack Trace:
build & test	Test integration (blocking)	2026-10-25T08:53:23.4876160Z      at Tankradar.Tests.Integration.Integration.BackupServiceTests_Resilience.Restore_WithPooledConnectionHoldingWal_Succeeds() in D:\Tankradar\Tankradar\src\Tankradar.Tests.Integration\Integration\BackupServiceTests_Resilience.cs:line 242
build & test	Test integration (blocking)	2026-10-25T08:53:23.4878438Z --- End of stack trace from previous location ---
build & test	Test integration (blocking)	2026-10-25T08:53:24.9879511Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UnusableDatabase_IsRejected_WithoutLeftovers(tamper: HugeStationName) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:24.9881539Z   Passed Tankradar.Tests.Integration.Integration.AddressSearchMockServerTests_Flow.UnreachableGeocoding_ShowsServiceMessage [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:24.9882925Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Restore_PreparedDatabase_IsRebuiltCleanly(tamper: ExtraTable) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:24.9884404Z   Passed Tankradar.Tests.Integration.Integration.AddressSearchMockServerTests_Flow.Oranienburg_Radius1_ShowsOnlyEpsilonWithDistanceToAddress [952 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:24.9885783Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Resilience.Undo_WithUnreadableJournal_KeepsLiveData [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:26.4887579Z   Passed Tankradar.Tests.Integration.Integration.AddressSearchMockServerTests_Flow.Berlin_Radius25_CanBeSortedByNameAndFilteredByFuel [966 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:26.4890425Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UnusableDatabase_IsRejected_WithoutLeftovers(tamper: HugeCustomName) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:26.4893079Z   Passed Tankradar.Tests.Integration.Integration.AddressSearchMockServerTests_Flow.UnknownPlace_ShowsNotFoundAndSendsNoPriceRequest [518 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:26.4909286Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_CustomName.Model_LimitsCustomNameLength [1 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:28.0655099Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_CustomName.ReadPaths_ExposeCustomName [989 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:28.0659712Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Restore_PreparedDatabase_IsRebuiltCleanly(tamper: ExtraCheck) [3 s]
build & test	Test integration (blocking)	2026-10-25T08:53:28.0662047Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_CustomName.SetCustomName_SetsChangesAndRemoves [770 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:28.0664430Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UnusableDatabase_IsRejected_WithoutLeftovers(tamper: MissingColumn) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:29.7550557Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Resilience.Restore_DatabaseBrieflyLocked_SucceedsByRetry [3 s]
build & test	Test integration (blocking)	2026-10-25T08:53:29.7552225Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_CustomName.SetCustomName_RejectsTooLongAndUnknown [912 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:29.7553818Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_CustomName.SaveAsync_KeepsCustomName_WhileSourceDataChanges [974 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:31.2565834Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UnusableDatabase_IsRejected_WithoutLeftovers(tamper: HugeBlobCustomName) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:31.2567576Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_ListDeleteImport.Delete_RemovesFile_AndRejectsPaths [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:31.2569146Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Restore_PreparedDatabase_IsRebuiltCleanly(tamper: Trigger) [3 s]
build & test	Test integration (blocking)	2026-10-25T08:53:32.7684441Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UntamperedFile_IsAccepted [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:32.7687474Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_ListDeleteImport.List_OrdersNewestFirst_MarksUnreadable_IgnoresAndCleansTemporaryFiles [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:32.7690158Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_ListDeleteImport.List_WithoutBackupDirectory_IsEmpty [1 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:34.2747915Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Restore_PreparedDatabase_IsRebuiltCleanly(tamper: ManipulatedSequence) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:34.2750251Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Resilience.Restore_WhenUndoFails_KeepsRollbackAndJournal [5 s]
build & test	Test integration (blocking)	2026-10-25T08:53:34.2752576Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_UnusableDatabase_IsRejected_AndKeepsLiveData(tamper: HugeGroupName) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:35.7767070Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_ListDeleteImport.Import_ValidFile_IsAddedToList_WithCounterOnCollision [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:35.7770051Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Resilience.Restore_WithLeftoverJournal_UndoesFirstThenSucceeds [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:37.0423203Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_ListDeleteImport.Import_InvalidAndTooNewFiles_AreRejected_WithoutLeftovers [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:37.0425781Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Restore_PreparedDatabase_IsRebuiltCleanly(tamper: MigrationLockRow) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:37.0428571Z   Passed Tankradar.Tests.Integration.Integration.SettingsServiceTests_RestartPersistence.Save_ShowNearby_RoundTripsThroughFile(showNearby: True) [683 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:37.0430999Z   Passed Tankradar.Tests.Integration.Integration.GeocodingMockServerTests_Flow.Resolve_TwoQuickRequests_AreAtLeastOneSecondApart [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:37.0433139Z   Passed Tankradar.Tests.Integration.Integration.GeocodingMockServerTests_Flow.Resolve_UnknownPlace_IsNotFound [11 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:37.0435118Z   Passed Tankradar.Tests.Integration.Integration.GeocodingMockServerTests_Flow.Resolve_InvalidInput_NeverReachesServer(input: "Berlin<script>") [6 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:37.0437161Z   Passed Tankradar.Tests.Integration.Integration.GeocodingMockServerTests_Flow.Resolve_InvalidInput_NeverReachesServer(input: "") [5 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:37.6400311Z   Passed Tankradar.Tests.Integration.Integration.GeocodingMockServerTests_Flow.Resolve_InvalidInput_NeverReachesServer(input: "ab") [6 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:37.6418083Z   Passed Tankradar.Tests.Integration.Integration.GeocodingMockServerTests_Flow.Resolve_KnownPlace_ReturnsPosition(query: "10115", latitude: 52.520000000000003, longitude: 13.404999999999999) [7 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:37.6421308Z   Passed Tankradar.Tests.Integration.Integration.GeocodingMockServerTests_Flow.Resolve_KnownPlace_ReturnsPosition(query: "Berlin", latitude: 52.520000000000003, longitude: 13.404999999999999) [26 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:37.6424512Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_UnusableDatabase_IsRejected_AndKeepsLiveData(tamper: HugeCustomName) [3 s]
build & test	Test integration (blocking)	2026-10-25T08:53:37.6426076Z   Passed Tankradar.Tests.Integration.Integration.GeocodingMockServerTests_Flow.Resolve_KnownPlace_ReturnsPosition(query: "Eberswalde", latitude: 52.520000000000003, longitude: 13.73) [22 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:37.6427705Z   Passed Tankradar.Tests.Integration.Integration.GeocodingMockServerTests_Flow.Resolve_KnownPlace_ReturnsPosition(query: "Oranienburg", latitude: 52.880000000000003, longitude: 13.404999999999999) [7 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:37.6429047Z   Passed Tankradar.Tests.Integration.Integration.GeocodingMockServerTests_Flow.Mock_RequestWithoutUserAgent_IsForbidden [10 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:37.6430255Z   Passed Tankradar.Tests.Integration.Integration.SettingsServiceTests_RestartPersistence.Save_ShowNearby_RoundTripsThroughFile(showNearby: False) [690 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:39.1400695Z   Passed Tankradar.Tests.Integration.Integration.SettingsServiceTests_RestartPersistence.Save_GpsUsage_RoundTripsThroughFile(gps: WhileInUse) [722 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:39.1402841Z   Passed Tankradar.Tests.Integration.Integration.SettingsServiceTests_RestartPersistence.Save_GpsUsage_RoundTripsThroughFile(gps: Never) [717 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:39.1404620Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_UnusableDatabase_IsRejected_AndKeepsLiveData(tamper: InvalidDate) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:40.8346112Z   Passed Tankradar.Tests.Integration.Integration.GeocodingMockServerTests_Flow.Resolve_UnreachableServer_IsUnavailable [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:40.8348599Z   Passed Tankradar.Tests.Integration.Integration.GeocodingMockServerTests_Flow.Resolve_SendsIdentifyingUserAgentAndPolicyParameters [8 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:40.8350959Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Restore_PreparedDatabase_IsRebuiltCleanly(tamper: View) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:40.8353621Z   Passed Tankradar.Tests.Integration.Integration.SettingsServiceTests_RestartPersistence.Save_GpsUsage_RoundTripsThroughFile(gps: Always) [691 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:40.8357151Z   Passed Tankradar.Tests.Integration.Integration.SettingsServiceTests_RestartPersistence.Save_SingleSelectedFuelType_RoundTripsThroughFile(fuelType: SuperE10) [755 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:42.3513789Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_UnusableDatabase_IsRejected_AndKeepsLiveData(tamper: OrphanEntry) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:42.3516645Z   Passed Tankradar.Tests.Integration.Integration.SettingsServiceTests_RestartPersistence.Save_SingleSelectedFuelType_RoundTripsThroughFile(fuelType: SuperE5) [763 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:42.3519146Z   Passed Tankradar.Tests.Integration.Integration.GeocodingMockServerTests_Flow.Resolve_ServerFailures_AreMappedWithoutRetry [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:42.3522018Z   Passed Tankradar.Tests.Integration.Integration.SettingsServiceTests_RestartPersistence.Save_SingleSelectedFuelType_RoundTripsThroughFile(fuelType: Diesel) [913 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:43.8610588Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Restore_PreparedDatabase_IsRebuiltCleanly(tamper: ExtraIndex) [3 s]
build & test	Test integration (blocking)	2026-10-25T08:53:43.8612560Z   Passed Tankradar.Tests.Integration.Integration.AddressSearchPrivacyTests_Persistence.AfterAddressSearch_DatabaseFileDoesNotContainAddressOrPosition [905 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:43.8614426Z   Passed Tankradar.Tests.Integration.Integration.SettingsServiceTests_RestartPersistence.Save_ResultSortOrder_RoundTripsThroughFile(sort: Name) [981 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:43.8616469Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_UnusableDatabase_IsRejected_AndKeepsLiveData(tamper: HugeBlobCustomName) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:43.8617756Z   Passed Tankradar.Tests.Integration.Integration.AddressSearchPrivacyTests_Persistence.AddressSearch_NeverLogsAddressOrPosition [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:45.8847488Z   Passed Tankradar.Tests.Integration.Integration.SettingsServiceTests_RestartPersistence.Save_ResultSortOrder_RoundTripsThroughFile(sort: Distance) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:45.8850156Z   Passed Tankradar.Tests.Integration.Integration.AddressSearchPrivacyTests_Persistence.AfterAddressSearch_DatabaseHoldsNoAddressOrPosition [926 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:45.8853227Z   Passed Tankradar.Tests.Integration.Integration.SettingsServiceTests_RestartPersistence.Save_ResultSortOrder_RoundTripsThroughFile(sort: Price) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:45.8855882Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_UnusableDatabase_IsRejected_AndKeepsLiveData(tamper: DuplicateName) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:47.3971010Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Restore_PreparedDatabase_IsRebuiltCleanly(tamper: ExtraColumn) [3 s]
build & test	Test integration (blocking)	2026-10-25T08:53:47.3973735Z   Passed Tankradar.Tests.Integration.Integration.PriceRefreshMockServerTests_Flow.Refresh_ConnectionRestored_FetchesAgain [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:47.3976090Z   Passed Tankradar.Tests.Integration.Integration.SettingsServiceTests_RestartPersistence.Save_ResultView_RoundTripsThroughFile(view: Map) [882 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:47.3978913Z   Passed Tankradar.Tests.Integration.Integration.PriceRefreshMockServerTests_Flow.Refresh_WrongKey_ReportsFailureWithoutThrowing [982 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:47.3981334Z   Passed Tankradar.Tests.Integration.Integration.SettingsServiceTests_RestartPersistence.Save_ResultView_RoundTripsThroughFile(view: List) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:48.8987903Z   Passed Tankradar.Tests.Integration.Integration.SettingsServiceTests_RestartPersistence.Save_ThenNewServiceInstance_LoadsSameValues [893 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:48.8989793Z   Passed Tankradar.Tests.Integration.Integration.PriceRefreshMockServerTests_Flow.Refresh_MoreThanTenStations_UsesSeveralRequests [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:48.8991556Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_UnusableDatabase_IsRejected_AndKeepsLiveData(tamper: HugeEntryNote) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:50.4007231Z   Passed Tankradar.Tests.Integration.Integration.PriceRefreshMockServerTests_Flow.Refresh_AgainstMock_UpdatesPricesAndAges [871 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:50.4010365Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Restore_PreparedDatabase_IsRebuiltCleanly(tamper: RemovedCollation) [3 s]
build & test	Test integration (blocking)	2026-10-25T08:53:50.4013241Z   Passed Tankradar.Tests.Integration.Integration.VehiclesMigrationTests_Upgrade.Initialize_FromSchemaBeforeVehicles_KeepsExistingDataAndAddsTables [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:51.9000194Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.TamperedArchive_ReallyContainsTheManipulation(tamper: View) [978 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:51.9002129Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_UnusableDatabase_IsRejected_AndKeepsLiveData(tamper: HugeEntryStationName) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:51.9004044Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.TamperedArchive_ReallyContainsTheManipulation(tamper: MigrationLockRow) [894 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:51.9005667Z   Passed Tankradar.Tests.Integration.Integration.VehiclesMigrationTests_Upgrade.Database_CascadesVehicleDeleteToEntries [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:53.4045334Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.TamperedArchive_ReallyContainsTheManipulation(tamper: ExtraIndex) [932 ms]
build & test	Test integration (blocking)	2026-10-25T08:53:53.4048152Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_UnusableDatabase_IsRejected_AndKeepsLiveData(tamper: MissingColumn) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:54.9098394Z   Passed Tankradar.Tests.Integration.Integration.VehiclesMigrationTests_Upgrade.Database_RejectsDuplicateVehicleNamesCaseInsensitive [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:54.9100943Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.TamperedArchive_ReallyContainsTheManipulation(tamper: ManipulatedSequence) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:54.9103219Z   Passed Tankradar.Tests.Integration.Integration.DatabaseInitializerTests_Upgrade.Initialize_Twice_IsIdempotent [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:56.4135651Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_UnusableDatabase_IsRejected_AndKeepsLiveData(tamper: MissingTable) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:56.4137552Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.TamperedArchive_ReallyContainsTheManipulation(tamper: ExtraCheck) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:56.4139195Z   Passed Tankradar.Tests.Integration.Integration.DatabaseInitializerTests_Upgrade.Initialize_FromOlderSchemaVersion_KeepsData [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:56.4140741Z   Passed Tankradar.Tests.Integration.Integration.PriceRefreshMockServerTests_Flow.Refresh_ServerGone_ReturnsLastKnownPrices [7 s]
build & test	Test integration (blocking)	2026-10-25T08:53:57.9246310Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.TamperedArchive_ReallyContainsTheManipulation(tamper: ExtraTable) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:59.4253254Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Versions.Restore_TooNewBackup_IsRejected_AndKeepsLiveData(migration: "20261018094636_AddStationCustomName", schemaVersion: 99) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:53:59.4255201Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Flow.Search_DistanceSteps_ChangeStationSet(distanceKm: 1, expected: ["Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle", "Epsilon Tankstelle"]) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:53:59.4258627Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_UnusableDatabase_IsRejected_AndKeepsLiveData(tamper: OversizedCustomName) [4 s]
build & test	Test integration (blocking)	2026-10-25T08:53:59.4261299Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.TamperedArchive_ReallyContainsTheManipulation(tamper: ExtraColumn) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:00.9282149Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Versions.Restore_TooNewBackup_IsRejected_AndKeepsLiveData(migration: "29991231000000_Future", schemaVersion: 1) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:00.9284665Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.TamperedArchive_ReallyContainsTheManipulation(tamper: RemovedCollation) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:02.4339257Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Flow.Search_DistanceSteps_ChangeStationSet(distanceKm: 2, expected: ["Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle", "Route Nah", "Epsilon Tankstelle"]) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:06.3789504Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.TamperedArchive_ReallyContainsTheManipulation(tamper: PrefixedTrigger) [5 s]
build & test	Test integration (blocking)	2026-10-25T08:54:09.1506646Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_UnusableDatabase_IsRejected_AndKeepsLiveData(tamper: HugeStationName) [9 s]
build & test	Test integration (blocking)	2026-10-25T08:54:10.6506155Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Flow.Search_DistanceSteps_ChangeStationSet(distanceKm: 10, expected: ["Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle", "Route Nah", "Route Mittel", ···]) [9 s]
build & test	Test integration (blocking)	2026-10-25T08:54:10.6510192Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Versions.Restore_OlderMigration_IsMigratedAndKeepsData [11 s]
build & test	Test integration (blocking)	2026-10-25T08:54:12.1503321Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.TamperedArchive_ReallyContainsTheManipulation(tamper: Trigger) [4 s]
build & test	Test integration (blocking)	2026-10-25T08:54:12.1505276Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Versions.Restore_InvalidFiles_AreRejected_AndKeepLiveData [918 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:12.1507557Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_UnusableDatabase_IsRejected_AndKeepsLiveData(tamper: ViewAsTable) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:13.6526140Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Flow.Search_DistanceSteps_ChangeStationSet(distanceKm: 5, expected: ["Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle", "Route Nah", "Route Mittel", ···]) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:13.6529331Z   Passed Tankradar.Tests.Integration.Integration.TankbookExportServiceTests_RealDatabase.Export_Csv_Hybrid_ContainsLiterAndKwh [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:15.1675417Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Restore_Version0114Schema_WithLockRow_IsMigratedAndComplete [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:15.1677541Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_UnusableDatabase_IsRejected_AndKeepsLiveData(tamper: UnknownFuelKind) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:15.1678945Z   Passed Tankradar.Tests.Integration.Integration.TankbookExportServiceTests_RealDatabase.Export_Csv_WithRange_FiltersAndKeepsConsumptionFromPredecessor [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:16.6697099Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Inspect_CountsComeFromTheVerifiedDatabase_NotFromTheManifest [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:18.1799828Z   Passed Tankradar.Tests.Integration.Integration.TankbookExportServiceTests_RealDatabase.Export_Csv_FromRealDatabase_ContainsAllEntries [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:18.1803007Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_UnusableDatabase_IsRejected_AndKeepsLiveData(tamper: HugeBrand) [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:18.1806600Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Flow.Search_LongRoute_SpacesQueriesAndServesRepeatFromCache [5 s]
build & test	Test integration (blocking)	2026-10-25T08:54:19.6802392Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_PrepareTimeout_FailsWithoutChangingLiveData [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:19.6804545Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Rebuild.Inspect_UnusableDatabase_IsInvalid [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:19.6807051Z   Passed Tankradar.Tests.Integration.Integration.TankbookExportServiceTests_RealDatabase.Export_VehicleWithoutEntries_CreatesNoFile_AndDataStaysUnchanged [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:19.6809498Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Import_UnusableDatabaseBytes_AreRejectedBeforeEnteringTheList [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:21.1812890Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Flow.FilterAndSort_AfterSearch_MakeNoFurtherCalls [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:21.1816329Z   Passed Tankradar.Tests.Integration.Integration.FavoritesMigrationTests_Upgrade.Initialize_FromSchemaBeforeFavorites_KeepsExistingDataAndAddsTables [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:22.6837499Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Flow.Search_CurrentLocationStart_ResolvesOnlyDestination [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:22.6839009Z   Passed Tankradar.Tests.Integration.Integration.FavoritesMigrationTests_Upgrade.Database_RejectsDuplicateGroupNamesCaseInsensitive [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:22.6840440Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Schema.Restore_CancelledByCaller_KeepsLiveDataAndLeavesNoLeftovers [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:22.6841886Z   Passed Tankradar.Tests.Integration.Integration.TankbookExportServiceTests_RealDatabase.Export_Pdf_FromRealDatabase_IsValidPdfWithContent [3 s]
build & test	Test integration (blocking)	2026-10-25T08:54:24.1896035Z   Passed Tankradar.Tests.Integration.Integration.FavoritesMigrationTests_Upgrade.Database_CascadesGroupDeleteAndProtectsStations [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:24.1898684Z   Passed Tankradar.Tests.Integration.Integration.NearbySettingMigrationTests_Upgrade.Initialize_FromSchemaBeforeNearbySetting_KeepsExistingDataAndAddsColumn [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:24.1901017Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Flow.Search_TwoKilometers_ShowsStationsAlongRouteInOrder [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:24.1903895Z   Passed Tankradar.Tests.Integration.Integration.RoutePrivacyTests_Persistence.AfterRouteSearch_DatabaseFileDoesNotContainTextsOrRoute [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:25.6946745Z   Passed Tankradar.Tests.Integration.Integration.FavoritesMigrationTests_Upgrade.Favorites_SurviveRestartOnRealDatabaseFile [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:25.6949045Z   Passed Tankradar.Tests.Integration.Integration.NearbySettingMigrationTests_Upgrade.NearbySetting_SurvivesRestartOnRealDatabaseFile [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:25.6951233Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_Persistence.SaveAsync_KeepsHistoryAndReturnsLatestPrices [515 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:25.6953266Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Flow.Search_CallsEachServiceAsPlanned [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:26.5343862Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_Persistence.Schema_ContainsNoUserPositionColumns [656 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:26.5346249Z   Passed Tankradar.Tests.Integration.Integration.TankbookEntriesMigrationTests_Upgrade.Initialize_FromSchemaBeforeEntryDetails_KeepsDataAndFillsDefaults [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:26.5347538Z   Passed Tankradar.Tests.Integration.Integration.TestDataContextTests_Lifecycle.Constructor_CreatesIsolatedDataDirectory [< 1 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:26.5348670Z   Passed Tankradar.Tests.Integration.Integration.TestDataContextTests_Lifecycle.Constructor_CreatesUniqueDirectoryPerInstance [2 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:26.5349982Z   Passed Tankradar.Tests.Integration.Integration.TestDataContextTests_Lifecycle.Cleanup_RemovesDataDirectory [< 1 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:26.5351072Z   Passed Tankradar.Tests.Integration.Integration.RoutePrivacyTests_Persistence.NewViewModel_AfterRouteSearch_KnowsNoRouteOrInputs [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:26.5352918Z   Passed Tankradar.Tests.Integration.Integration.ForeignKeysTests_Connection.AppConnection_HasForeignKeysEnabled [434 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:28.0400054Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_Persistence.FindNearbyAsync_ReturnsStationsInRadiusSortedByDistance [505 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:28.0401830Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_Persistence.SaveAsync_ListUpdateKeepsStoredDetails [647 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:28.0403525Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Flow.Search_GeocodingStartAndDestination_AreSpacedByNominatimInterval [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:28.0405643Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Storage.Create_DoesNotContainDeletedContent [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:28.0407267Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_Persistence.SaveAsync_DuplicateIdsInBatch_AreStoredOnce [759 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:29.5478757Z   Passed Tankradar.Tests.Integration.Integration.RoutePrivacyTests_Persistence.AfterRouteSearch_DatabaseHoldsNoPlacesOrPositions [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:29.5486362Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Create.Create_DatabaseHasOnlyKnownTables [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:29.5493120Z   Passed Tankradar.Tests.Integration.Integration.PriceRepositoryTests_Persistence.GetStationAsync_Unknown_ReturnsNull [710 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:29.5496230Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Storage.Create_PhotoOverRestoreLimit_FailsWithoutLeavingFiles [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:31.0543116Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Create.Create_Twice_CreatesTwoFilesAndLeavesLiveDataUnchanged [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:31.0545398Z   Passed Tankradar.Tests.Integration.Integration.RoutePrivacyTests_Persistence.RouteSearch_DoesNotChangeSchemaOrSettings [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:31.0547836Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Create.Create_ContainsOnlyManifestDatabaseAndPhotos_WithMatchingManifest [825 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:32.5558342Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Storage.Restore_LeavesDatabaseInWalMode [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:32.5560250Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_RouteKey.Create_DoesNotContainRouteKey [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:32.5561950Z   Passed Tankradar.Tests.Integration.Integration.ApiKeyBuildTests_Log.Build_LocalProps_IsNotLogged [3 s]
build & test	Test integration (blocking)	2026-10-25T08:54:34.0695756Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Create.Completeness_AllEntitiesTimestampsAndPhotoBytes_SurviveRestore [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:34.0699240Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Limit.Search_CancelledWhileWaiting_SendsNoFurtherQueries [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:34.0702617Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_RouteKey.Restore_LeavesRouteSearchStateUntouched_AndNewDeviceHasItOff [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:35.5729972Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_SourceFile.Restore_TruncatedDatabase_IsInvalidFile_AndKeepsLiveData(keepBytes: 4196) [886 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:35.5732426Z   Passed Tankradar.Tests.Integration.Integration.ApiKeyBuildTests_Log.Build_PrimaryEnvironmentVariable_IsNotLogged [3 s]
build & test	Test integration (blocking)	2026-10-25T08:54:35.5734887Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Limit.Search_RouteNeedingMoreThanThreeQueries_IsRejectedBeforeFirstPriceQuery [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:35.5737343Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_CustomName.Restore_CustomNameAtLimit_IsAccepted [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:37.0875437Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_SourceFile.Restore_TruncatedDatabase_IsInvalidFile_AndKeepsLiveData(keepBytes: 12288) [991 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:37.0877365Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_SourceFile.Restore_TruncatedDatabase_IsInvalidFile_AndKeepsLiveData(keepBytes: 100) [764 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:38.5881507Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Restore.Restore_UnknownFile_IsNotFound_AndKeepsData [951 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:38.5883900Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Limit.Search_WithMinimumInterval_SpacesQueriesAcrossSearches [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:38.5885626Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_CustomName.Restore_BackupBeforeCustomName_IsMigratedWithoutCustomName [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:38.5887125Z   Passed Tankradar.Tests.Integration.Integration.ApiKeyBuildTests_Log.Build_SecretEnvironmentVariable_IsNotLogged [3 s]
build & test	Test integration (blocking)	2026-10-25T08:54:40.2042754Z   Passed Tankradar.Tests.Integration.Integration.SearchPrivacyTests_Persistence.AfterSearch_DatabaseHoldsStationsAndPricesButNoPositionOrDistance [991 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:40.2052170Z   Passed Tankradar.Tests.Integration.Integration.SearchPrivacyTests_Persistence.AfterSearch_DatabaseFileDoesNotContainSearchPosition [858 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:40.2054568Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_Restore.Restore_ReplacesChangedDataAndPhotos [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:41.7056795Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_CustomName.Restore_BringsBackCustomName [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:41.7059408Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_RoutingServerErrorOnce_IsRetriedAndSucceeds [2 s]
build & test	Test integration (blocking)	2026-10-25T08:54:41.7062042Z   Passed Tankradar.Tests.Integration.Integration.TileSourceMockServerTests_Flow.GetTile_FetchesOnceWithUserAgentAndCaches [70 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:41.7064988Z   Passed Tankradar.Tests.Integration.Integration.TileSourceMockServerTests_Flow.GetTile_ServerError_ReturnsNullWithoutRepeating [13 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:41.7086243Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_EmbeddedNul.Import_EmbeddedNul_IsRejectedOrListed_WithoutLeftovers(field: "TankbookEntries.Note", sql: "UPDATE TankbookEntries SET Note = 'a' || char(0) |"···) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:43.2114796Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_RoutingServerErrors_ReportsUnreachable [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:43.2118462Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_EmbeddedNul.Import_EmbeddedNul_IsRejectedOrListed_WithoutLeftovers(field: "Stations.CustomName", sql: "UPDATE Stations SET CustomName = 'a' || char(0) ||"···) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:43.2121276Z   Passed Tankradar.Tests.Integration.Integration.ApiKeyBuildTests_Sources.Build_OnlyLocalProps_UsesLocalProps [3 s]
build & test	Test integration (blocking)	2026-10-25T08:54:44.7141358Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_EmbeddedNul.Import_EmbeddedNul_IsRejectedOrListed_WithoutLeftovers(field: "Stations.Brand", sql: "UPDATE Stations SET Brand = 'a' || char(0) || repl"···) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:46.2294359Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_EmbeddedNul.Import_EmbeddedNul_IsRejectedOrListed_WithoutLeftovers(field: "Stations.Name", sql: "UPDATE Stations SET Name = 'a' || char(0) || repla"···) [1 s]
build & test	Test integration (blocking)	2026-10-25T08:54:46.2299325Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_EmbeddedNul.Restore_EmbeddedNul_IsRejectedOrTruncated_AndKeepsOtherData(field: "TankbookEntries.Note", sql: "UPDATE TankbookEntries SET Note = 'a' || char(0) |"···) [824 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:47.7298569Z   Passed Tankradar.Tests.Integration.Integration.ApiKeyBuildTests_Sources.Build_NoSource_EmbedsEmptyKey [3 s]
build & test	Test integration (blocking)	2026-10-25T08:54:49.7591181Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_RoutingServerStopped_ReportsUnreachable [7 s]
build & test	Test integration (blocking)	2026-10-25T08:54:52.9629423Z   Passed Tankradar.Tests.Integration.Integration.SearchPrivacyTests_Logging.SearchSuccessAndFailures_LogNoCoordinatesOrRequestAddress [12 s]
build & test	Test integration (blocking)	2026-10-25T08:54:54.4643293Z   Passed Tankradar.Tests.Integration.Integration.SearchPrivacyTests_Logging.LocationService_LogsNoCoordinates [7 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:54.4645536Z   Passed Tankradar.Tests.Integration.Integration.SearchPrivacyTests_Logging.ServiceExceptions_ContainNoCoordinatesOrRequestAddress [7 ms]
build & test	Test integration (blocking)	2026-10-25T08:54:54.4647775Z   Passed Tankradar.Tests.Integration.Integration.ApiKeyBuildTests_Sources.Build_NoPrimary_SecretEnvironmentVariableBeatsLocalProps [7 s]
build & test	Test integration (blocking)	2026-10-25T08:54:56.9458427Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_EmbeddedNul.Restore_EmbeddedNul_IsRejectedOrTruncated_AndKeepsOtherData(field: "Stations.Name", sql: "UPDATE Stations SET Name = 'a' || char(0) || repla"···) [10 s]
build & test	Test integration (blocking)	2026-10-25T08:54:58.9355914Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_Flow.Search_DieselFilter_SortsByDieselPriceAndDropsStationsWithoutDiesel [5 s]
build & test	Test integration (blocking)	2026-10-25T08:55:00.4365300Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_Flow.Search_InvalidRadius_SendsNoRequest(radius: 0) [9 ms]
build & test	Test integration (blocking)	2026-10-25T08:55:00.4368753Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_Flow.Search_InvalidRadius_SendsNoRequest(radius: 26) [10 ms]
build & test	Test integration (blocking)	2026-10-25T08:55:00.4371206Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_Flow.Search_InvalidRadius_SendsNoRequest(radius: 500) [10 ms]
build & test	Test integration (blocking)	2026-10-25T08:55:00.4373321Z   Passed Tankradar.Tests.Integration.Integration.ApiKeyBuildTests_Sources.Build_AllSourcesPresent_PrimaryEnvironmentVariableWins [5 s]
build & test	Test integration (blocking)	2026-10-25T08:55:00.4375377Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_SamePlace_ReportsWithoutRouting [9 s]
build & test	Test integration (blocking)	2026-10-25T08:55:07.2466415Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_Flow.ViewModel_InvalidRadius_SendsNoRequest_ValidRadius_SendsIt [8 s]
build & test	Test integration (blocking)	2026-10-25T08:55:08.7475485Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_UnknownDestination_ReportsNotFoundWithoutRouting [8 s]
build & test	Test integration (blocking)	2026-10-25T08:55:08.7478137Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_Flow.Search_Radius1_ReturnsOnlyAlpha [1 s]
build & test	Test integration (blocking)	2026-10-25T08:55:10.2598629Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_UnknownStart_ReportsNotFoundBeforeDestination [1 s]
build & test	Test integration (blocking)	2026-10-25T08:55:10.2602211Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_EmbeddedNul.Restore_EmbeddedNul_IsRejectedOrTruncated_AndKeepsOtherData(field: "Stations.Brand", sql: "UPDATE Stations SET Brand = 'a' || char(0) || repl"···) [13 s]
build & test	Test integration (blocking)	2026-10-25T08:55:11.7654796Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_Flow.Detail_NewStations_DeriveAutomatedHint [2 s]
build & test	Test integration (blocking)	2026-10-25T08:55:11.7656321Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_Flow.Mock_RadiusAbove25_AnswersWithError [10 ms]
build & test	Test integration (blocking)	2026-10-25T08:55:11.7657794Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_Offline_ReportsWithoutExternalCalls [1 s]
build & test	Test integration (blocking)	2026-10-25T08:55:15.6068405Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_RouteOfSixHundredKilometers_ReportsTooLongWithoutPriceQueries [4 s]
build & test	Test integration (blocking)	2026-10-25T08:55:17.1196207Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_Flow.Search_Radius5_ReturnsOnlyNearStationsAndSendsRadius [4 s]
build & test	Test integration (blocking)	2026-10-25T08:55:26.9929877Z   Passed Tankradar.Tests.Integration.Integration.SearchMockServerTests_Flow.Search_Radius25_ReturnsFourStationsSortedByDistance [11 s]
build & test	Test integration (blocking)	2026-10-25T08:55:26.9932359Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_RoutingTooManyRequests_RetriesThreeTimesAndReportsQuota [11 s]
build & test	Test integration (blocking)	2026-10-25T08:55:29.4480440Z   Passed Tankradar.Tests.Integration.Integration.BackupServiceTests_EmbeddedNul.Restore_EmbeddedNul_IsRejectedOrTruncated_AndKeepsOtherData(field: "Stations.CustomName", sql: "UPDATE Stations SET CustomName = 'a' || char(0) ||"···) [18 s]
build & test	Test integration (blocking)	2026-10-25T08:55:29.4483705Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_RouteNotFound_ReportsRouteNotCalculable [2 s]
build & test	Test integration (blocking)	2026-10-25T08:55:31.7368555Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_RoutingTimeout_IsRetriedAndReportsUnreachable [2 s]
build & test	Test integration (blocking)	2026-10-25T08:55:33.2385880Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_InvalidInput_MakesNoExternalCalls [314 ms]
build & test	Test integration (blocking)	2026-10-25T08:55:34.7395084Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_PriceServiceError_ReportsFailureWithoutPartialResults [1 s]
build & test	Test integration (blocking)	2026-10-25T08:55:36.2454313Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_WrongRoutingKey_ReportsKeyRejectedWithoutRetry [1 s]
build & test	Test integration (blocking)	2026-10-25T08:55:36.2456684Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_RoutingDistanceLimit_ReportsRouteTooLong [1 s]
build & test	Test integration (blocking)	2026-10-25T08:55:36.2459657Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_MissingRoutingKey_ReportsUnavailableWithoutAnyCallAndOtherModesStillWork [209 ms]
build & test	Test integration (blocking)	2026-10-25T08:55:36.3798501Z [xUnit.net 00:02:53.79]   Finished:    Tankradar.Tests.Integration
build & test	Test integration (blocking)	2026-10-25T08:55:37.8029631Z   Passed Tankradar.Tests.Integration.Integration.RouteSearchMockServerTests_Errors.Search_GeocodingServerError_ReportsGeocodingUnavailable [131 ms]
build & test	Test integration (blocking)	2026-10-25T08:55:37.8772674Z Results File: D:\Tankradar\Tankradar\TestResults	est-results-integration.trx
build & test	Test integration (blocking)	2026-10-25T08:55:37.8776716Z 
build & test	Test integration (blocking)	2026-10-25T08:55:37.8816395Z Test Run Failed.
build & test	Test integration (blocking)	2026-10-25T08:55:37.8817053Z Total tests: 291
build & test	Test integration (blocking)	2026-10-25T08:55:37.8817420Z      Passed: 290
build & test	Test integration (blocking)	2026-10-25T08:55:37.8817688Z      Failed: 1
build & test	Test integration (blocking)	2026-10-25T08:55:37.8819612Z  Total time: 2.9734 Minutes
build & test	Test integration (blocking)	2026-10-25T08:55:38.1256003Z 
build & test	Test integration (blocking)	2026-10-25T08:55:38.1262521Z Attachments:
build & test	Test integration (blocking)	2026-10-25T08:55:38.1265279Z   D:\Tankradar\Tankradar\TestResults\8126eb52-bde7-4257-b789-6268a1cbdde2\coverage.cobertura.xml
build & test	Test integration (blocking)	2026-10-25T08:55:38.2019171Z ##[error]Process completed with exit code 1. und Code von  (WAL, Pooling, Datei-Handles, Timing) klären; echten Fehler in der App beheben (mit Test) oder Zeit-/Reihenfolgeannahme im Test beseitigen, Prüfabsicht erhalten; mindestens 20 lokale Wiederholungen des Tests belegen; Ursache und Lösung berichten.

## Offene Punkte

Keine.

(Hinweis ohne Blocker: Eine Antwort von Tankerkönig zur Abfragegrenze liegt nicht vor; die Grenze bleibt unverändert und wird in ADR 0015 und im Abschlussbericht als offen gemeldet.)
