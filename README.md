# Tankatlas

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)

Tankatlas ist eine .NET-MAUI-App für Kraftstoffpreise, Favoriten und ein offline nutzbares
Tankbuch. Zielplattformen sind iOS (Mindestversion iOS 16, primäre Zielplattform) und Windows
(Entwicklung, Debugging, automatisierte Tests).

Der für Anwender sichtbare App-Name lautet „Tankatlas“ (ursprünglich „Tankradar“). Technische Bezeichner
bleiben unverändert: Bundle-ID `de.martinstromberg.tankradar`, Projekt-, Solution-, Namespace- und
Assembly-Namen (`Tankradar.*`), Repository sowie die Umgebungsvariablen `TANKRADAR_*`.

Korrektur (Version 0.1.27): Der Seitenname steht nur noch in der Navigationsleiste, nicht mehr zusätzlich als Überschrift im Inhalt (Favoriten, Karte, Tankbuch, Optionen, Tank- und Ladevorgänge). Stations- und Gruppenname bleiben als Überschrift der Detailansicht bzw. Favoritengruppe.

Korrektur (Version 0.1.26): „Foto aufnehmen“ im Tankbuch-Formular endete auf dem iPhone sofort mit einer Fehlermeldung (.NET MAUI 10.0.0 im iOS-Build der CI forderte zusätzlich die Berechtigung „Fotos hinzufügen“ an). Die App nutzt jetzt .NET MAUI 10.0.120 (bisher je nach Umgebung 10.0.0 bis 10.0.110) (`MauiVersion` in der csproj), die Info.plist enthält nur noch die Kamera- und die Standortbeschreibung, die Aufnahme wird als JPEG übernommen, und fehlende Berechtigungen werden mit einem Hinweis auf die iOS-Einstellungen gemeldet ([ADR 0016](docs/adr/0016-maui-version-and-photo-permissions.md)).

Schritt 17a (Version 0.1.23): Die App hat ein eigenes Icon (weiße Zapfsäule mit gelbem Blitz auf Teal-Grün-Verlauf) für Windows und iOS sowie einen passenden Startbildschirm.

Stand: Entwicklungsschritt 16 umgesetzt (Version 0.1.21): Export des Tankbuchs auf der Seite „Tank- und Ladevorgänge“ – Bereich „Export“ mit Format
(CSV oder PDF) und Zeitraum (alle Vorgänge, dieses Jahr, letztes Jahr, letzte 12 Monate, eigener Zeitraum); der Export entsteht nur auf Veranlassung und wird über
Teilen/Speichern des Systems übergeben (im Testmodus Kopie nach `test-shared/`). CSV für deutschsprachige Tabellenkalkulationen (UTF-8 mit BOM, Semikolon,
Dezimalkomma, Einheiten Liter bzw. kWh, Schutz vor Formelinjektion), PDF-Bericht mit Kennzahlen, Monatskosten und Vorgangstabelle durch einen eigenen
PDF-Schreiber ohne neue Abhängigkeit ([ADR 0013](docs/adr/0013-tankbook-export.md)); geöffnete Vorgangs- und Fahrzeugseiten laden nach einer Wiederherstellung
neu. Details unter [`docs/help/Tankbuch/`](docs/help/Tankbuch/index.md). Zuvor Schritt 15a (Version 0.1.20): Tankstellen zeigen überall die Marke vorn und den Namen der Quelle klein
darunter (bei manchen Tankstellen ist er der Inhaber); in der Detailansicht lässt sich ein eigener Anzeigename vergeben, ändern und
entfernen (lokal, überall bevorzugt, Teil der Sicherung; [ADR 0012](docs/adr/0012-station-display-name.md)). Die erste Volltankung
eines Fahrzeugs liefert einen Verbrauch ab dem Start-Kilometerstand (Annahme: Tank beim Start voll), gekennzeichnet „ab Startstand“
mit Hinweis; die Auswertung zeigt ihn schon mit einem vollen Vorgang (siehe [ADR 0009](docs/adr/0009-tankbook-entries.md),
[ADR 0010](docs/adr/0010-tankbook-statistics.md)). Zuvor Schritt 15 (Version 0.1.19): lokale Sicherung und Wiederherstellung in den Optionen (Karte
„Sicherung“) – Sicherung auf Wunsch als ZIP (Datenbank-Schnappschuss, Belegfotos, Manifest mit Prüfsummen), Liste mit Datum und
Größe, Wiederherstellen nach Rückfrage (vollständige Prüfung, atomarer Austausch mit Rollback und Wiederaufnahme nach Absturz),
Löschen nach Rückfrage, Teilen/Speichern über die Systemfunktion und Einspielen einer externen Datei; ältere Sicherungen werden
migriert, zu neue, ungültige und manipulierte Dateien abgelehnt; Hinweis zur Sicherung in der Löschrückfrage von Fahrzeugen mit
Vorgängen; Kachelzahlen der Auswertung ohne Umbruch. Details unter [`docs/help/Sicherung/`](docs/help/Sicherung/index.md),
[ADR 0011](docs/adr/0011-local-backup-restore.md). Zuvor Schritt 14: Verbrauchs- und Kostenauswertung je Fahrzeug auf der Seite
„Tank- und Ladevorgänge“ – Kacheln Ø Verbrauch (nach Strecke gewichtet) und Kosten / 100 km, Monatsanalyse mit Kosten und Menge
des laufenden Monats, Verbrauchsverlauf (Balkendiagramm der letzten 6 Vorgänge), Kostenverlauf (Monatskosten) und Monatsliste,
je Art (Kraftstoff/Strom) getrennt, mit Startverbrauch als gekennzeichnetem Ersatzwert; selbst gezeichnete Diagramme (MAUI
Graphics, keine Zusatzbibliothek), lokal berechnet und offline; Details unter [`docs/help/Tankbuch/`](docs/help/Tankbuch/index.md),
[ADR 0010](docs/adr/0010-tankbook-statistics.md). Zuvor Schritt 13: Tank- und Ladevorgänge je Fahrzeug erfassen, korrigieren und
löschen (Datum, Tankstelle, Menge/Preis/Gesamtbetrag – zwei aus drei –, Kilometerstand, Teilbetankung, Notiz, Belegfoto), Verbrauch
pro 100 km je Vollvorgang, lokal und offline ([ADR 0009](docs/adr/0009-tankbook-entries.md)). Zuvor Schritt 12: Fahrzeugverwaltung
im Tankbuch (Name, Kraftstoffart, Start-Kilometerstand, optional Startverbrauch; [ADR 0008](docs/adr/0008-vehicle-management.md)).
Zuvor Schritt 11: Startseite (Bereich „Favoriten“) mit allen Favoritengruppen und ihren Tankstellen
(Name, Entfernung nur bei erlaubter Standortnutzung, Preise der gewählten Sorten mit Alter „vor X Min.“ und Amber ab
60 Minuten, Hinweise „Preis unbestätigt“/„Automatentankstelle“), Preisabruf nach Kennungen beim Öffnen und auf Wunsch,
Offline-Stand mit Hinweis und automatischer Aktualisierung nach Wiederverbindung, optionalem Bereich „In der Nähe“
(Schalter in den Optionen, Standard aus; Standort nur im Arbeitsspeicher) sowie Öffnen der Detailansicht aus Startseite und
Gruppenansicht; Details unter [`docs/help/Startseite/`](docs/help/Startseite/index.md), [ADR 0007](docs/adr/0007-home-page-design-deviations.md),
[ADR 0006](docs/adr/0006-favorite-groups.md). Außerdem: Kachel-Cache berücksichtigt `Age` und die Lebensdauer bei `304`,
gleichzeitiges Zuordnen meldet „bereits zugeordnet“. Zuvor Schritt 10: Favoritengruppen (Zuordnung von Tankstellen in der Detailansicht, Mehrfachzuordnung, Gruppen anlegen/umbenennen/beschreiben/nach Rückfrage löschen, Notiz und Priorität je Tankstelle, lokal und offline; Details unter [`docs/help/Favoriten/`](docs/help/Favoriten/index.md), [ADR 0006](docs/adr/0006-favorite-groups.md)) sowie Kachel-Cache nach HTTP-Caching-Angaben (Cache im Cache-Verzeichnis, Kennung mit App-Version, Quellenlink, Mausbedienung der Karte unter Windows). Zuvor Schritt 9: Kartenansicht der Suchergebnisse (OpenStreetMap-Kacheln, Preisniveau-Markierungen; [ADR 0005](docs/adr/0005-map-view-decisions.md)). Zuvor Schritt 8: Tankstellen-Detailansicht aus der Ergebnisliste (Adresse, Entfernung, Preise mit Alter,
Hinweise, Öffnungszeiten mit Stand und Altersgrenze von 24 Stunden, Offline-Betrieb mit automatischer Aktualisierung nach
Wiederverbindung; Details unter [`docs/help/Tankstellendetails/`](docs/help/Tankstellendetails/index.md)). Zuvor Schritt 7:
Suche nach Adresse, Ort oder PLZ. Schritt 6: Umkreissuche am aktuellen Standort (Radius als Chip-Auswahl 1/2/5/10/15/25 km, Standard 5 km)
mit Ergebnisliste, Filter nach Spritsorte und Sortierung. Zuvor abgeschlossen: Preisdaten über die
Tankerkönig-API (Schritt 5) sowie lokale Datenhaltung (SQLite, EF Core) und Einstellungen (Optionen,
Schritt 4). Enthalten sind außerdem Projektgrundgerüst, MVVM-Infrastruktur, Hauptnavigation mit vier
Bereichen (Favoriten, Karte, Tankbuch, Optionen), zentrales Design-System, Testinfrastruktur und CI/CD.

## Voraussetzungen

- .NET SDK 10.0.401 oder neuer
- .NET MAUI Workloads für iOS und Windows:

  ```powershell
  dotnet workload install maui-windows ios
  ```

  Die Version der Pakete `Microsoft.Maui.*` (aktuell 10.0.120) legt `MauiVersion` in `src/Tankradar.MAUI/Tankradar.MAUI.csproj` fest; sie wird per NuGet geladen und muss nicht der installierten Workload entsprechen.

- Windows 10 (Build 19041) oder neuer für die Windows-Zielplattform
- Für einen tatsächlichen iOS-Build wird zusätzlich ein Mac mit Xcode benötigt (siehe Abschnitt
  „Bekannte Einschränkungen").

## Projektstruktur

```
Tankradar.sln                        Solution-Datei
src/
  Tankradar.MAUI/                    Hauptprojekt (Single Project, iOS + Windows)
    Models/                          Datenmodelle (Einstellungen, Enums)
    Data/                            EF-Core-Kontext, Entitäten und Migrationen (SQLite)
    ViewModels/                      MVVM-ViewModels (BaseViewModel + vier Seiten-ViewModels)
    Views/                           XAML-Seiten der vier Hauptbereiche
    Services/                        AppConfiguration u. a. Dienste
    Services/Pricing/                Tankerkönig-Client, Preis-Cache, Verbindungserkennung, API-Schlüssel
    Services/Backup/                 Lokale Sicherung und Wiederherstellung (Archiv, Validierung, Austausch mit Rollback)
    Resources/DesignSystem.xaml      Zentrales Design-System (Farben, Typografie, Spacing, ...)
    Resources/Raw/appsettings.json   Konfigurationsdatei (Bundle-ID)
    Platforms/                       Plattformspezifischer Code (iOS, Windows, Android, MacCatalyst)
  Tankradar.Tests.Unit/              Unit-Tests (xUnit)
  Tankradar.Tests.Integration/       Integrationstests (xUnit)
  Tankradar.Tests.E2E/               End-to-End-Tests gegen die Windows-App (xUnit + FlaUI)
scripts/
  create-review-version.ps1          Erzeugt startfähige Windows-Zwischenstände
  local-ci.ps1                       Lokaler Prüflauf (gleichwertig zur CI-Pipeline)
  package-windows.ps1                Windows-Release-Paket (ZIP + update.json)
  *.mjs, validate-workflows.py       Hilfsskripte der CI/CD-Pipeline (+ Tests, u. a. test_validate_workflows.py)
.github/
  workflows/, actions/               CI/CD-Pipeline (GitHub Actions)
coverlet.runsettings                 Coverage-Einstellungen für CI und lokalen Prüflauf
docs/
  adr/                                Architekturentscheidungen (z. B. Designabweichungen)
```

## Setup

```powershell
dotnet restore
dotnet build Tankradar.sln
```

Die App für Windows starten (Debug, unpackaged, ohne Installation):

```powershell
dotnet run --project src/Tankradar.MAUI -f net10.0-windows10.0.19041.0
```

## Git-Hooks zur Qualitätssicherung

Nach dem Klonen des Repositories müssen die lokalen Git-Hooks installiert werden. Rufe dazu aus
dem Repository-Verzeichnis auf:

```powershell
.\.githooks\install-hooks.cmd
```

```bash
./.githooks/install-hooks.sh
```

Die Hooks prüfen vor jedem Commit und Push automatisch Übersetzungen, XML-Dokumentation,
Platzhalter-Implementierungen, Enum-Testabdeckung, Code-Formatierung, verbotene Muster
(Secrets, Zertifikate, DB-Dumps, iOS-Signierungsdaten), das Commit-Nachrichten-Format sowie die Testausführung.
Details: [`docs/help/git-hooks/`](docs/help/git-hooks/README.md).

## CI/CD-Pipeline

GitHub Actions baut und prüft jeden Pull Request nach `staging`, erzeugt bei jedem Push auf `staging`
automatisch Pre-Releases (`vX.Y.Z-rc.N`) und bei jedem Push auf `main` finale Releases (`vX.Y.Z`). Die
Windows-App wird als ZIP veröffentlicht, das nach dem Entpacken ohne Installation startet. Die erste
Version ist 0.1.0; bis 1.0 gibt es keine automatische Anhebung auf 1.0. Pull Requests nach `main` sind
nur von `staging` aus zulässig.

Weil Actions im privaten Repository am Billing-Limit scheitern können, führt

```powershell
.\scripts\local-ci.ps1
```

dieselben Prüfungen (Format, Sicherheit, statische Analyse, Tests, Mindest-Testabdeckung 70 %) lokal aus.
Details, Versionierung und die einmalige Einrichtung auf GitHub (Branches, Branch-Schutz, Labels,
Secrets, Variablen): [`docs/help/ci-cd/`](docs/help/ci-cd/index.md).

## Tests ausführen

```powershell
dotnet test Tankradar.sln
```

Dies führt Unit-, Integrations- und E2E-Tests aus (der `pre-push`-Hook lässt die E2E-Tests standardmäßig aus,
`PRE_PUSH_E2E=1` schaltet sie ein; in der PR-CI nach `staging` sind sie blockierend). Die E2E-Tests starten die App
im Testmodus außerhalb des sichtbaren Bildschirms (Off-Screen), ohne den Vordergrund zu übernehmen; bedient wird nur über
UI-Automation-Muster, nie über Fokus, Tastatur oder Maus, damit Eingaben des Anwenders nicht im Testfenster landen; Rückfall auf
sichtbaren Betrieb per `TANKRADAR_E2E_WINDOW=foreground`. Der E2E-Test (`NavigationE2ETests`) startet die
kompilierte Windows-App (`Tankradar.MAUI.exe`) über FlaUI und navigiert durch alle vier
Navigationsbereiche; dafür muss `src/Tankradar.MAUI` zuvor für `net10.0-windows10.0.19041.0`
gebaut worden sein (geschieht automatisch, wenn die gesamte Solution gebaut/getestet wird).

E2E-Tests verwenden ein eigenes, temporäres Testdatenverzeichnis (Umgebungsvariable
`TANKATLAS_TEST_DATA_PATH`, vom Testlauf automatisch gesetzt und danach wieder gelöscht), damit sie nicht mit
Entwicklungs- oder Echtbetriebsdaten kollidieren. Die App selbst löst ihr Datenverzeichnis zentral
über `IAppDataPathProvider` (`src/Tankradar.MAUI/Services/AppDataPathProvider.cs`) auf: Ist
`TANKATLAS_TEST_DATA_PATH` gesetzt, wird dieses Verzeichnis verwendet, andernfalls das reguläre
Plattform-Datenverzeichnis (`FileSystem.AppDataDirectory`).

## Erststart und Einrichtung

Beim ersten Start nach einer Installation fragt Tankatlas einmalig nach den Spritsorten, die den Anwender interessieren
(Super E5, Super E10, Diesel; alle vorausgewählt). „Speichern“ übernimmt die Auswahl (mindestens eine Sorte), „Überspringen“
übernimmt alle drei Sorten. Die Auswahl ist dieselbe Einstellung wie der Bereich „Spritsorten“ in den **Optionen** und lässt
sich dort jederzeit ändern. Die Einrichtung erscheint danach nicht wieder. Die Einrichtung wird als Überlagerung innerhalb der
App umgesetzt, nicht als Systemdialog ([ADR 0017](docs/adr/0017-einrichtungsdialog.md)). Details:
[`docs/help/Einstellungen/`](docs/help/Einstellungen/index.md).

## Tankbuch

Der Tab „Tankbuch“ zeigt direkt die Tank- und Ladevorgänge eines Fahrzeugs. Das Fahrzeug wird im Kopf über seinen Namen
gewählt; angezeigt wird das zuletzt gewählte, sonst das erste Fahrzeug nach Name. Die Fahrzeugverwaltung (anlegen, bearbeiten,
löschen) ist über „Fahrzeuge verwalten“ im Kopf erreichbar. Ohne Fahrzeug führt ein Hinweis direkt zum Anlegen. Die Auswahl wird
nur gerätelokal gemerkt und ist nicht Teil der Sicherung ([ADR 0018](docs/adr/0018-tankbook-start-view.md)). Details:
[`docs/help/Tankbuch/`](docs/help/Tankbuch/index.md).

## Kraftstoffpreise und API-Schlüssel

Die App ruft Preise über die Tankerkönig-API ab (Quellenangabe „Daten: Tankerkönig / MTS-K“, CC BY 4.0,
sichtbar in den Optionen) und speichert jeden Preis mit Zeitstempel lokal (Offline-Betrieb, Alter „vor X Min.“,
ab 60 Minuten veraltet). Der API-Schlüssel steht nie im Quellcode: lokal über die Umgebungsvariable
`TANKRADAR_FUEL_PRICE_API_KEY`, dann `FUEL_PRICE_API_KEY`, dann die nicht versionierte Datei `tankerkoenig.local.props`, in der CI über das
Secret `FUEL_PRICE_API_KEY` (nur für den iOS-Build; das öffentlich herunterladbare Windows-Paket wird ohne Schlüssel gebaut); der Schlüssel gelangt nie in das MSBuild-Log; zur Laufzeit liegt er in Keychain (iOS) bzw. Credential Locker (Windows). Ohne
Schlüssel baut und testet alles (Tests nutzen einen lokalen Mock-Server, nie produktive Endpunkte).
Details: [`docs/help/Preisdaten/`](docs/help/Preisdaten/index.md).

## Umkreissuche und Testmodus unter Windows (ohne GPS)

Die Suche („Karte“) fragt den Standort nur auf Anforderung ab und speichert ihn nie; bei der Standortnutzung
„Nie“ in den Optionen erfolgt keine Abfrage. Windows-Rechner haben in der Regel kein GPS. Für die manuelle
Abnahme unter Windows (und für alle automatisierten Tests) gibt es den Testmodus: Er wird ausschließlich über
die Umgebungsvariable `TANKATLAS_TEST_DATA_PATH` (isoliertes Datenverzeichnis, der frühere Name `TEST_DATA_PATH`
wirkt nicht mehr) aktiviert; nur dann gelten zusätzlich

- `TANKATLAS_TEST_LOCATION` – fester Standort im Format `breite,länge` mit Dezimalpunkt (z. B. `52.5200,13.4050`);
  ohne gültigen Wert meldet die Suche im Testmodus, dass der Standort nicht ermittelt werden kann,
- `TANKATLAS_TEST_BACKUP_IMPORT` – Pfad einer Sicherungsdatei, die „Sicherung aus Datei einspielen“ ohne Systemdialog liefert
  (ohne Wert gilt die Auswahl als abgebrochen); „Teilen“ kopiert im Testmodus nach `<Datenverzeichnis>/test-shared/`,
- `TANKRADAR_ROUTING_URL` und `TANKRADAR_ROUTING_TEST_API_KEY` – Adresse und Schlüssel eines Routingdienstes (in Tests der lokale
  `MockOpenRouteServiceServer`; ohne Adresse wird im Testmodus keine Route berechnet; der Schlüssel belegt die Testablage im Speicher vor und schaltet die Routensuche ein),
- `TANKRADAR_ROUTE_QUERY_INTERVAL_MS` – Mindestabstand der Preisabfragen einer Routensuche im Testmodus in Millisekunden (ohne Wert 0),
- `TANKRADAR_PRICE_API_URL` und `TANKRADAR_PRICE_API_KEY` – Adresse und Schlüssel eines Preisdienstes
  (in Tests der lokale `MockTankerkoenigServer` aus `src/TestSupport`; ohne Adresse wird im Testmodus kein Abruf ausgeführt).

Die Suchart „Route“ (Schritt 17) zeigt Tankstellen entlang einer Strecke: Start (aktueller Standort oder Adresse) und Ziel werden über
OpenStreetMap-Nominatim aufgelöst, die Route für ein Auto berechnet OpenRouteService (Quellenangabe „Routen: OpenRouteService, Geodaten ©
OpenStreetMap-Mitwirkende“), der Abstand zur Route ist 1, 2, 5 oder 10 km (Standard 2 km). Die Tankerkönig-Abfragen entlang der Strecke sind auf
höchstens 3 je Suche mit mindestens 10 Sekunden Abstand begrenzt (zu lange Routen werden mit Hinweis
abgelehnt; [ADR 0014](docs/adr/0014-route-search.md)).
Den Schlüssel für OpenRouteService beantragt jeder Anwender selbst und trägt ihn in den **Optionen** unter **Routensuche** ein (verdecktes Feld, Prüfung per
Testanfrage, Schalter **Routensuche aktivieren**). Er liegt in der Keychain des Geräts (iOS) bzw. im Credential Locker (Windows), wird beim Build nie eingebunden
und gehört nicht zur Sicherung der App (iOS-Keychain-Einträge können in verschlüsselten Gerätesicherungen enthalten sein). Nur bei aktivierter Routensuche ist die Suchart „Route“ sichtbar und es gehen Aufrufe an OpenRouteService
(`https://api.heigit.org/openrouteservice/`); siehe [`docs/help/Preisdaten/api-schluessel.md`](docs/help/Preisdaten/api-schluessel.md). Routen, Adressen und Standort werden nicht gespeichert.

Neben der Standortsuche bietet „Karte“ die Suche nach Adresse, Ort oder Postleitzahl (Suchart „Adresse, Ort oder
PLZ“): Die Eingabe wird erst beim ausdrücklichen Absenden geprüft und über OpenStreetMap-Nominatim in eine
Position umgewandelt (höchstens eine Anfrage je Sekunde, identifizierende Kennung, Quellenangabe
„Geodaten © OpenStreetMap-Mitwirkende“); Adresse und Position werden nicht gespeichert, GPS ist nicht nötig. Im Testmodus
gibt `TANKRADAR_GEOCODING_URL` die Adresse eines Mock-Dienstes (`MockNominatimServer`) vor; ohne Adresse
wird die Auflösung verweigert.

Ohne Testmodus meldet die Suche unter Windows ohne Standortdienst, dass der Standort nicht ermittelt werden
kann; es gibt keinen stillen Ersatzstandort.

Die Suchergebnisse lassen sich alternativ auf einer Karte darstellen (Auswahl „Liste“/„Karte“, Vorbelegung aus der
Standardansicht der Optionen): eigene Kartenkomponente auf Basis von OpenStreetMap-Kacheln (Quellenangabe
„© OpenStreetMap-Mitwirkende“, Zoomen, Verschieben, Preismarkierungen in Grün/Teal/Rot/Grau, markierte
Suchposition, Tippen öffnet die Details). Kacheln werden mit identifizierender Kennung (App-Version, Projekt-URL) abgerufen und nach den HTTP-Caching-Angaben des Servers im Cache-Verzeichnis zwischengespeichert (Rückfall 7 Tage);
im Testmodus gibt `TANKRADAR_TILE_URL` die Adresse eines Mock-Kachelservers (`MockTileServer`) vor, ohne Adresse
werden keine Kacheln abgerufen. Entscheidungen: [ADR 0005](docs/adr/0005-map-view-decisions.md).

Details: [`docs/help/Suche/`](docs/help/Suche/index.md).

## Windows-Zwischenstände (Review-Versionen)

Ein startfähiger Windows-Zwischenstand lässt sich jederzeit lokal erzeugen:

```powershell
.\scripts\create-review-version.ps1 -Version 0.1.0 -ChangelogFile "Kurze Beschreibung der Änderungen"
```

Das Skript baut die Windows-App als Release (`dotnet publish`), kopiert die startfähige,
ungepackte Ausgabe nach `review-versions/<Version>_<JJJJ-MM-TT>/bin/` und legt dort eine
`CHANGELOG.md` an. `-ChangelogFile` ist Pflicht und akzeptiert wahlweise einen Dateipfad mit dem
Changelog-Text oder den Changelog-Text direkt als Zeichenkette; ein Aufruf ohne (oder mit leerem)
Changelog wird abgelehnt, bevor gebaut wird. Der Aufruf ist idempotent: ein erneuter Aufruf mit
derselben Version (am selben Tag) überschreibt das vorhandene Verzeichnis.

Die App im erzeugten Zwischenstand wird direkt über die `.exe` gestartet, ohne Installation:

```powershell
.\review-versions\0.1.0_2026-09-28\bin\Tankradar.MAUI.exe
```

Für Abnahmen wird der Zwischenstand nicht per `Start-Process` gestartet: Das Fenster käme in den Vordergrund und nähme dem Anwender Fokus und Tastatureingaben. Wer die
App von Hand oder skriptgesteuert prüfen will, startet die `.exe` selbst (Doppelklick) oder verwendet den Testmodus ohne sichtbares Fenster (`TANKATLAS_TEST_DATA_PATH`
mit `TANKATLAS_TEST_WINDOW=offscreen`, wie die E2E-Tests über `scripts/local-ci.ps1`).

`review-versions/` ist per `.gitignore` von Commits ausgeschlossen und wird niemals versioniert.

## Design-System

Alle Design-Token (Farben, Typografie, Spacing, Eckenradien, Schatten, Mindestgröße 44×44 px für
Bedienelemente) sind zentral in `src/Tankradar.MAUI/Resources/DesignSystem.xaml` definiert und
folgen dem verbindlichen Designentwurf. Details zur Farbpalette, Typografie und den Komponenten
sind im Design-System-Inventar dokumentiert:
[`docs/design-system.md`](docs/design-system.md).

Hell- und Dunkelmodus folgen dem System-Theme (`AppTheme.Unspecified`); Farben werden über
`AppThemeBinding` auf Light-/Dark-Ressourcen aufgelöst.

Als Schriften sind Inter (Fließtext, Headlines, `price-hero`) und JetBrains Mono (`label-code`)
vollständig als TTF-Dateien in `src/Tankradar.MAUI/Resources/Fonts/` eingebettet und über
`MauiProgram.ConfigureFonts(...)` registriert; OpenSans dient als zusätzliche Basisschrift.
Lizenzhinweise (SIL Open Font License) liegen als `OFL-Inter.txt` und `OFL-JetBrainsMono.txt` im
selben Verzeichnis.

### Bewusste Designabweichung

Der Umschalter „Kraftstoff/Laden" und die Ladestecker-Kennzeichnungen aus dem Designentwurf werden
in Version 1.0 **nicht** umgesetzt (keine offizielle, frei nutzbare Strompreis-API verfügbar).
Details und Begründung: [`docs/adr/0001-no-electricity-prices-in-v1.md`](docs/adr/0001-no-electricity-prices-in-v1.md).

## Bekannte Einschränkungen

- **iOS-Build:** Diese Entwicklungsumgebung ist ein Windows-Rechner ohne Mac/Xcode. Die
  `.csproj`-Konfiguration enthält iOS 16 korrekt als Ziel-Framework/Mindestversion
  (`net10.0-ios`, `SupportedOSPlatformVersion=16.0`); ein tatsächlicher iOS-Build (App-Bundle,
  Simulator/Gerät) ist hier jedoch nicht durchführbar und muss auf einem Mac bzw. über CI erfolgen.
  Für Build, Simulator, Gerät und TestFlight-Upload (auch von Windows per Pair to Mac) gibt es
  `scripts/iOS-Deployment.ps1`, siehe [`docs/help/ci-cd/ios-deployment.md`](docs/help/ci-cd/ios-deployment.md). Datenschutzangaben für den App Store: [`docs/help/Datenschutz/`](docs/help/Datenschutz/index.md).
- **Sicherungen sind unverschlüsselt:** Sie enthalten Fahrdaten und Belege; eine geteilte oder gespeicherte Kopie liegt außerhalb
  des Dateischutzes der App ([ADR 0011](docs/adr/0011-local-backup-restore.md)).
- **Sichtprüfung offen:** Das Aussehen der Auswertung (Kacheln, Diagramme) und der Karte „Sicherung“ sowie das Verhalten des
  Teilen-Blatts und der Dateiauswahl unter iOS sind ohne Gerät nur über Tests (Texte, Geometrie, Testdoppel) abgesichert und
  bleiben Teil der Abnahme.
- **Android/MacCatalyst:** Das MAUI-Standardtemplate legt zusätzlich zu iOS und Windows auch
  Android- und MacCatalyst-Ziele an. Diese sind für Schritt 1 nicht erforderlich, aber unschädlich
  und bauen erfolgreich mit.

## Bundle-ID

Die Bundle-ID ist als Konfigurationswert vorgesehen (`Services/AppConfiguration.cs`), mit dem
Standardwert `de.martinstromberg.tankradar`. Sie kann
über die Umgebungsvariable `TANKRADAR_BUNDLE_ID` oder über
`src/Tankradar.MAUI/Resources/Raw/appsettings.json` überschrieben werden.
