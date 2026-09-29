# Tankradar

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)

Tankradar ist eine .NET-MAUI-App für Kraftstoffpreise, Favoriten und ein offline nutzbares
Tankbuch. Zielplattformen sind iOS (Mindestversion iOS 16, primäre Zielplattform) und Windows
(Entwicklung, Debugging, automatisierte Tests).

Dieses Repository befindet sich in Entwicklungsschritt 1 ("App-Grundgerüst, Navigation und
Design-System"): Projektgrundgerüst, MVVM-Infrastruktur, Hauptnavigation mit vier Bereichen
(Favoriten, Karte, Tankbuch, Optionen), zentrales Design-System und Testinfrastruktur.

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
    Models/                          Datenmodelle (aktuell leer)
    ViewModels/                      MVVM-ViewModels (BaseViewModel + vier Seiten-ViewModels)
    Views/                           XAML-Seiten der vier Hauptbereiche
    Services/                        AppConfiguration u. a. Dienste
    Resources/DesignSystem.xaml      Zentrales Design-System (Farben, Typografie, Spacing, ...)
    Resources/Raw/appsettings.json   Konfigurationsdatei (Bundle-ID)
    Platforms/                       Plattformspezifischer Code (iOS, Windows, Android, MacCatalyst)
  Tankradar.Tests.Unit/              Unit-Tests (xUnit)
  Tankradar.Tests.Integration/       Integrationstests (xUnit)
  Tankradar.Tests.E2E/               End-to-End-Tests gegen die Windows-App (xUnit + FlaUI)
scripts/
  create-review-version.ps1          Erzeugt startfähige Windows-Zwischenstände
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
(Secrets, Zertifikate, DB-Dumps), das Commit-Nachrichten-Format sowie die Testausführung.
Details: [`docs/help/git-hooks/`](docs/help/git-hooks/README.md).

## Tests ausführen

```powershell
dotnet test Tankradar.sln
```

Dies führt Unit-, Integrations- und E2E-Tests aus. Der E2E-Test (`NavigationE2ETests`) startet die
kompilierte Windows-App (`Tankradar.MAUI.exe`) über FlaUI und navigiert durch alle vier
Navigationsbereiche; dafür muss `src/Tankradar.MAUI` zuvor für `net10.0-windows10.0.19041.0`
gebaut worden sein (geschieht automatisch, wenn die gesamte Solution gebaut/getestet wird).

E2E-Tests verwenden ein eigenes, temporäres Testdatenverzeichnis (Umgebungsvariable
`TEST_DATA_PATH`, vom Testlauf automatisch gesetzt und danach wieder gelöscht), damit sie nicht mit
Entwicklungs- oder Echtbetriebsdaten kollidieren. Die App selbst löst ihr Datenverzeichnis zentral
über `IAppDataPathProvider` (`src/Tankradar.MAUI/Services/AppDataPathProvider.cs`) auf: Ist
`TEST_DATA_PATH` gesetzt, wird dieses Verzeichnis verwendet, andernfalls das reguläre
Plattform-Datenverzeichnis (`FileSystem.AppDataDirectory`).

## Windows-Zwischenstände (Review-Versionen)

Ein startfähiger Windows-Zwischenstand lässt sich jederzeit lokal erzeugen:

```powershell
.\scripts\create-review-version.ps1 -Version 0.1.0 -ChangelogFile "Kurze Beschreibung der Änderungen"
```

Das Skript baut die Windows-App als Release (`dotnet publish`), kopiert die startfähige,
ungepackte Ausgabe nach `review-versions/<Version>_<JJJJ-MM-TT>/bin/` und legt dort eine
`CHANGELOG.md` an. `-ChangelogFile` akzeptiert wahlweise einen Dateipfad mit dem Changelog-Text
oder den Changelog-Text direkt als Zeichenkette. Der Aufruf ist idempotent: ein erneuter Aufruf mit
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
- **Android/MacCatalyst:** Das MAUI-Standardtemplate legt zusätzlich zu iOS und Windows auch
  Android- und MacCatalyst-Ziele an. Diese sind für Schritt 1 nicht erforderlich, aber unschädlich
  und bauen erfolgreich mit.

## Bundle-ID

Die Bundle-ID ist als Konfigurationswert vorgesehen (`Services/AppConfiguration.cs`), mit dem
Platzhalterwert `com.softwareschmiede.tankradar.dev`, der die Entwicklung nicht blockiert. Sie kann
über die Umgebungsvariable `TANKRADAR_BUNDLE_ID` oder über
`src/Tankradar.MAUI/Resources/Raw/appsettings.json` überschrieben werden.
