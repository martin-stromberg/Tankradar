# Tankatlas

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)

Tankatlas ist eine .NET-MAUI-App für Kraftstoffpreise, Favoriten und ein offline nutzbares
Tankbuch. Primäre Zielplattform ist iOS (ab iOS 16); Windows dient der Entwicklung, dem
Debugging und den automatisierten Tests.

Der für Anwender sichtbare App-Name lautet „Tankatlas“ (ursprünglich „Tankradar“). Technische
Bezeichner bleiben unverändert: Bundle-ID `de.martinstromberg.tankradar`, Projekt-, Solution-,
Namespace- und Assembly-Namen (`Tankradar.*`) sowie die Umgebungsvariablen `TANKRADAR_*`.

## Funktionen

- **Karte und Suche** – Tankstellen am aktuellen Standort, rund um eine Adresse, einen Ort
  oder eine PLZ sowie entlang einer Route finden; Kartenansicht (OpenStreetMap) mit
  Preisniveau-Markierungen → [Hilfe](docs/help/Suche/index.md)
- **Favoriten** – Tankstellen in selbst benannten Gruppen sammeln (Notiz, Priorität, eigener
  Anzeigename); die Startseite zeigt alle Gruppen mit aktuellen Preisen →
  [Favoriten](docs/help/Favoriten/index.md), [Startseite](docs/help/Startseite/index.md)
- **Tankstellen-Details** – Adresse, Entfernung, Preise mit Alter und Öffnungszeiten →
  [Hilfe](docs/help/Tankstellendetails/index.md)
- **Tankbuch** – Fahrzeuge und Tank-/Ladevorgänge erfassen, Verbrauchs- und
  Kostenauswertung mit Diagrammen, Export als CSV oder PDF →
  [Hilfe](docs/help/Tankbuch/index.md)
- **Sicherung** – lokale Sicherung und Wiederherstellung als ZIP-Archiv →
  [Hilfe](docs/help/Sicherung/index.md)
- **Offline-first** – Preise werden lokal mit Zeitstempel gespeichert; ohne Verbindung gilt
  der zuletzt bekannte Stand → [Hilfe](docs/help/Preisdaten/index.md)

## Schnellstart

### Voraussetzungen

- .NET SDK 10.0.401 oder neuer
- .NET MAUI Workloads für iOS und Windows:

  ```powershell
  dotnet workload install maui-windows ios
  ```

- Windows 10 (Build 19041) oder neuer für das Windows-Ziel
- Für echte iOS-Builds ein Mac mit Xcode (siehe „Bekannte Einschränkungen“)

### Bauen und starten

```powershell
dotnet restore
dotnet build Tankradar.sln
dotnet run --project src/Tankradar.MAUI -f net10.0-windows10.0.19041.0
```

### Git-Hooks installieren

Nach dem Klonen einmalig ausführen:

```powershell
.\.githooks\install-hooks.cmd
```

```bash
./.githooks/install-hooks.sh
```

Die Hooks prüfen bei jedem Commit und Push u. a. Formatierung, Übersetzungen, verbotene
Muster (Secrets, Zertifikate), das Commit-Nachrichten-Format und die Tests. Details:
[docs/help/git-hooks/](docs/help/git-hooks/index.md).

### Tests und Prüflauf

```powershell
dotnet test Tankradar.sln   # Unit-, Integrations- und E2E-Tests (xUnit, E2E via FlaUI)
.\scripts\local-ci.ps1      # vollständiger Prüflauf wie die CI (Format, Sicherheit,
                            # statische Analyse, Tests, Mindest-Coverage 70 %)
```

Die E2E-Tests starten die kompilierte Windows-App im Testmodus außerhalb des sichtbaren
Bildschirms und bedienen sie ausschließlich über UI-Automation-Muster.

## Projektstruktur

```
src/
  Tankradar.MAUI/              Hauptprojekt (Single Project, iOS + Windows)
  Tankradar.Tests.Unit/        Unit-Tests (xUnit)
  Tankradar.Tests.Integration/ Integrationstests (xUnit)
  Tankradar.Tests.E2E/         End-to-End-Tests der Windows-App (xUnit + FlaUI)
  TestSupport/                 Mock-Server und gemeinsame Testhilfen
scripts/                       Build-, Paket- und CI-Hilfsskripte
docs/                          Anwender- und Entwicklerdokumentation
.github/                       CI/CD-Pipeline (GitHub Actions)
```

## Dokumentation

- **[docs/help/index.md](docs/help/index.md)** – Übersicht aller Funktions- und
  Entwicklerthemen (Einstellungen, Suche, Favoriten, Tankbuch, Sicherung, Preisdaten, ...)
- **[docs/adr/](docs/adr/)** – Architekturentscheidungen (ADRs)
- **[docs/design-system.md](docs/design-system.md)** – Design-Token, Typografie, Komponenten
- **[docs/help/ci-cd/](docs/help/ci-cd/index.md)** – Pipeline, Versionierung, Releases,
  iOS-Deployment und lokaler Prüflauf (inkl. Windows-Zwischenständen über
  `scripts/create-review-version.ps1`)
- **[changes.log](changes.log)** – Änderungsprotokoll je Entwicklungsschritt

## API-Schlüssel und Testmodus

Die Preise kommen von der Tankerkönig-API („Daten: Tankerkönig / MTS-K“, CC BY 4.0). Der
API-Schlüssel steht nie im Quellcode: lokal über die Umgebungsvariable
`TANKRADAR_FUEL_PRICE_API_KEY` (oder `tankerkoenig.local.props`), in der CI über das Secret
`FUEL_PRICE_API_KEY`; zur Laufzeit liegt er in Keychain (iOS) bzw. Credential Locker
(Windows). Ohne Schlüssel bauen und laufen alle Tests – sie nutzen lokale Mock-Server.

Für die manuelle Abnahme unter Windows (ohne GPS) und für automatisierte Tests gibt es einen
Testmodus, der ausschließlich über Umgebungsvariablen (`TANKATLAS_TEST_DATA_PATH`,
`TANKATLAS_TEST_LOCATION`, `TANKRADAR_*_URL` u. a.) gesteuert wird. Details:
[docs/help/Preisdaten/](docs/help/Preisdaten/index.md) und
[docs/help/Suche/ablauf-technisch.md](docs/help/Suche/ablauf-technisch.md).

## Bekannte Einschränkungen

- **iOS-Build:** Erfordert einen Mac mit Xcode bzw. die CI (`net10.0-ios`, iOS 16 ist
  konfiguriert); Anleitung unter
  [docs/help/ci-cd/ios-deployment.md](docs/help/ci-cd/ios-deployment.md), Datenschutzangaben
  unter [docs/help/Datenschutz/](docs/help/Datenschutz/index.md)
- **Sicherungen sind unverschlüsselt** –
  [ADR 0011](docs/adr/0011-local-backup-restore.md)
- **Keine Strompreise in Version 1.0** (keine frei nutzbare Strompreis-API) –
  [ADR 0001](docs/adr/0001-no-electricity-prices-in-v1.md)
- **Android/MacCatalyst-Ziele** des MAUI-Templates sind vorhanden, aber nicht im Fokus
