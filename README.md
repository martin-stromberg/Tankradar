# Tankatlas

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)

Tankatlas ist eine .NET-MAUI-App für Kraftstoffpreise, Favoriten und ein offline nutzbares
Tankbuch. Zielplattformen sind iOS (Mindestversion iOS 16, primäre Zielplattform) und Windows
(Entwicklung, Debugging, automatisierte Tests).

Der für Anwender sichtbare App-Name lautet „Tankatlas“ (ursprünglich „Tankradar“). Technische Bezeichner
bleiben unverändert: Bundle-ID `de.martinstromberg.tankradar`, Projekt-, Solution-, Namespace- und
Assembly-Namen (`Tankradar.*`), Repository sowie die Umgebungsvariablen `TANKRADAR_*`.

Entwicklungsschritt 6 umgesetzt: Umkreissuche am aktuellen Standort (Radius als Chip-Auswahl 1/2/5/10/15/25 km, Standard 5 km)
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
  *.mjs, validate-workflows.py       Hilfsskripte der CI/CD-Pipeline (+ Tests)
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

Dies führt Unit-, Integrations- und E2E-Tests aus. Der E2E-Test (`NavigationE2ETests`) startet die
kompilierte Windows-App (`Tankradar.MAUI.exe`) über FlaUI und navigiert durch alle vier
Navigationsbereiche; dafür muss `src/Tankradar.MAUI` zuvor für `net10.0-windows10.0.19041.0`
gebaut worden sein (geschieht automatisch, wenn die gesamte Solution gebaut/getestet wird).

E2E-Tests verwenden ein eigenes, temporäres Testdatenverzeichnis (Umgebungsvariable
`TANKATLAS_TEST_DATA_PATH`, vom Testlauf automatisch gesetzt und danach wieder gelöscht), damit sie nicht mit
Entwicklungs- oder Echtbetriebsdaten kollidieren. Die App selbst löst ihr Datenverzeichnis zentral
über `IAppDataPathProvider` (`src/Tankradar.MAUI/Services/AppDataPathProvider.cs`) auf: Ist
`TANKATLAS_TEST_DATA_PATH` gesetzt, wird dieses Verzeichnis verwendet, andernfalls das reguläre
Plattform-Datenverzeichnis (`FileSystem.AppDataDirectory`).

## Kraftstoffpreise und API-Schlüssel

Die App ruft Preise über die Tankerkönig-API ab (Quellenangabe „Daten: Tankerkönig / MTS-K“, CC BY 4.0,
sichtbar in den Optionen) und speichert jeden Preis mit Zeitstempel lokal (Offline-Betrieb, Alter „vor X Min.“,
ab 60 Minuten veraltet). Der API-Schlüssel steht nie im Quellcode: lokal über die Umgebungsvariable
`TANKRADAR_FUEL_PRICE_API_KEY` oder die nicht versionierte Datei `tankerkoenig.local.props`, in der CI über das
Secret `FUEL_PRICE_API_KEY`; zur Laufzeit liegt er in Keychain (iOS) bzw. Credential Locker (Windows). Ohne
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
- `TANKRADAR_PRICE_API_URL` und `TANKRADAR_PRICE_API_KEY` – Adresse und Schlüssel eines Preisdienstes
  (in Tests der lokale `MockTankerkoenigServer` aus `src/TestSupport`; ohne Adresse wird im Testmodus kein Abruf ausgeführt).

Ohne Testmodus meldet die Suche unter Windows ohne Standortdienst, dass der Standort nicht ermittelt werden
kann; es gibt keinen stillen Ersatzstandort.

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

`review-versions/` ist per `.gitignore` von Commits ausgeschlossen und wird niemals versioniert.

## Design-System

Alle Design-Token (Farben, Typografie, Spacing, Eckenradien, Schatten, Mindestgröße 44×44 px für
Bedienelemente) sind zentral in `src/Tankradar.MAUI/Resources/DesignSystem.xaml` definiert und
folgen dem verbindlichen Designentwurf. Details zur Farbpalette, Typografie und den Komponenten
sind im projektweiten Design-System-Inventar dokumentiert:
`docs/projects/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar/inventory/design-system.md`.

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
  `scripts/iOS-Deployment.ps1`, siehe [`docs/help/ci-cd/ios-deployment.md`](docs/help/ci-cd/ios-deployment.md).
- **Android/MacCatalyst:** Das MAUI-Standardtemplate legt zusätzlich zu iOS und Windows auch
  Android- und MacCatalyst-Ziele an. Diese sind für Schritt 1 nicht erforderlich, aber unschädlich
  und bauen erfolgreich mit.

## Bundle-ID

Die Bundle-ID ist als Konfigurationswert vorgesehen (`Services/AppConfiguration.cs`), mit dem
Standardwert `de.martinstromberg.tankradar`. Sie kann
über die Umgebungsvariable `TANKRADAR_BUNDLE_ID` oder über
`src/Tankradar.MAUI/Resources/Raw/appsettings.json` überschrieben werden.
