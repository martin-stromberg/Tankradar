# Test-Ergebnisse

## Ergebnis

**Status:** Ausführungsfehler

Die Unit- und Integrationstests laufen vollständig durch. Die E2E-Suite (FlaUI) konnte die Anwendung nicht starten; alle E2E-Tests scheitern im Setup. Der Lauf war zum Zeitpunkt dieses Berichts noch aktiv (kein Abschluss). Die vollständige Ausgabe liegt im Scratchpad unter `C:\Users\Martin\AppData\Local\Temp\claude\D--Repositories-softwareschmiede-159935ab-daf8-42be-95a4-4b122012e75c\193abe06-cc86-41f8-b9a0-a855350ff68c\scratchpad\e2e\console.log`.

Zusätzlich gab es beim ersten Anlauf einen Build-Fehler (siehe unten), der nach einer Wartezeit von ca. 60 s verschwand.

## Fehlgeschlagene Tests

### Tankradar.Tests.E2E (FlaUI)

- **Alle E2E-Tests (Setup, `E2ETestBase.LaunchApplication`)** — `hostpolicy.dll` nicht gefunden: `Tankradar.MAUI\bin\Debug\net10.0-windows10.0.19041.0\win-x64\Tankradar.MAUI.runtimeconfig.json` fehlt im Build-Ausgabeordner, daher startet die App nicht (Build-Artefakt-Problem, keine Testlogik).

### Build (Vorlauf)

- **`dotnet build Tankradar.sln` (Erstlauf und Zweitlauf)** — Datei durch „.NET Host" gesperrt (`XACRF7024`, `libarc.bin.so` in `Tankradar.MAUI\obj\...\net10.0-android`). Nach einer Wartezeit von 60 s war der Build fehlerfrei; der Build-Lock dürfte mit dem Runtimeconfig-Problem zusammenhängen. Ein Testprozess (`testhost.exe`) lief zu diesem Zeitpunkt noch. Die Prozessliste konnte nicht abgefragt werden (Berechtigung verweigert).

## E2E-Abdeckung

Geplante Szenarien laut `plan.md` (Abschnitt „E2E-Tests"). Alle sind nicht bestanden, weil die App im Setup nicht startet.

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Karte füllt die Ansicht; Panel eingeklappt | `MapE2ETests_Layout` | Nicht ausgeführt (Setup-Fehler) |
| Panel ausklappen, Eintrag öffnet Detail | `MapE2ETests_Panel` | Nicht ausgeführt (Setup-Fehler) |
| Overlay öffnen, Adresse suchen | `MapE2ETests_SearchOverlay` | Nicht ausgeführt (Setup-Fehler) |
| Route über Overlay | `MapE2ETests_SearchOverlay` | Nicht ausgeführt (Setup-Fehler) |
| Schaltfläche nach Kartenbewegung | `MapE2ETests_AreaReload` | Nicht ausgeführt (Setup-Fehler) |
| Nachladen aktualisiert Anzahl, Ausschnitt bleibt | `MapE2ETests_AreaReload` | Nicht ausgeführt (Setup-Fehler) |
| Ohne Bewegung keine Schaltfläche | `MapE2ETests_AreaReload` | Nicht ausgeführt (Setup-Fehler) |
| Ausschnitt über 25 km: Hinweis, deaktiviert | `MapE2ETests_AreaReload` | Nicht ausgeführt (Setup-Fehler) |
| Bedienung ohne Geste | `MapE2ETests_Navigation` | Nicht ausgeführt (Setup-Fehler) |
| Quellenangabe bei ausgeklapptem Panel | `MapE2ETests_View` | Nicht ausgeführt (Setup-Fehler) |

## Zusammenfassung

Unit- und Integrationstests:

- Gesamt: 2286
- Bestanden: 2286
- Fehlgeschlagen: 0
- Übersprungen: 0

E2E (FlaUI): Lauf nicht abgeschlossen. Bis zum Berichtszeitpunkt sind 12 Tests bestanden und alle weiteren Setups mit `hostpolicy.dll`-Fehler gescheitert. Gesamtzahl unbekannt.

## Testabdeckung

**Abdeckung:** 88,8 % (Zeilen, Unit-Tests) | 54,0 % (Zeilen, Integrationstests). Die Werte stammen aus getrennten Cobertura-Berichten und sind keine Vereinigung. Die folgende Tabelle führt Dateien unter 80 % auf, bei denen Unit- und Integrationstests zusammen keine höhere Abdeckung ergaben.

| Datei | Abdeckung (Unit / Integration) |
|-------|-------------------------------|
| `Data\TankradarDbContextFactory.cs` | 0 % / 0 % |
| `Models\Routing\RouteQueryPoint.cs` | 0 % / 0 % |
| `Models\Tankbook\VehicleLimits.cs` | 0 % / 0 % |
| `Resources\ChartPalette.cs` | 0 % / 0 % |
| `Services\Backup\MauiBackupFilePicker.cs` | 0 % / 0 % |
| `Services\Backup\MauiBackupSharer.cs` | 0 % / 0 % |
| `Services\Navigation\ShellFavoriteGroupNavigator.cs` | 0 % / 0 % |
| `Services\Navigation\ShellStationNavigator.cs` | 0 % / 0 % |
| `Services\Navigation\ShellTankbookNavigator.cs` | 0 % / 0 % |
| `Services\Pricing\SecureStorageApiKeyStore.cs` | 0 % / 0 % |
| `Services\SemanticScreenReaderAnnouncer.cs` | 0 % / 0 % |
| `Views\Controls\MapRouteDrawable.cs` | 0 % / 0 % |
| `Views\Controls\StationMapView.Pointer.cs` | 0 % / 0 % |
| `Views\Controls\StationMapView.xaml.cs` | 0 % / 0 % |
| `Views\Controls\ViewportChangedEventArgs.cs` | 0 % / 0 % |
| `ViewModels\HomeStationViewModel.cs` | 38 % / 24 % |
| `Services\Location\MauiLocationService.cs` | 56 % / 56 % |
| `Services\Map\LegacyTileCache.cs` | 57 % / 0 % |
| `ViewModels\HomePriceLineViewModel.cs` | 60 % / 40 % |
| `Data\Migrations\20261006124841_AddNearbySetting.cs` | 62 % / 62 % |
| `Data\Migrations\20261018094636_AddStationCustomName.cs` | 62 % / 62 % |
| `Services\Tankbook\MauiReceiptPhotoPicker.cs` | 64 % / 0 % |
| `Data\Migrations\20261011170920_AddTankbookEntryDetails.cs` | 65 % / 65 % |
| `Services\Map\HttpTileSource.cs` | 70 % / 55 % |
| `Data\Migrations\20261004180847_AddFuelTypeSettings.cs` | 78 % / 78 % |
| `Data\Migrations\20261004180805_InitialCreate.cs` | 79 % / 79 % |

## Fehlende Tests

Quelle: `Coverage-Daten`

- `Data\TankradarDbContextFactory.cs` — „0 % Abdeckung"
- `Models\Routing\RouteQueryPoint.cs` — „0 % Abdeckung"
- `Models\Tankbook\VehicleLimits.cs` — „0 % Abdeckung"
- `Resources\ChartPalette.cs` — „0 % Abdeckung"
- `Services\Backup\MauiBackupFilePicker.cs` — „0 % Abdeckung"
- `Services\Backup\MauiBackupSharer.cs` — „0 % Abdeckung"
- `Services\Navigation\ShellFavoriteGroupNavigator.cs` — „0 % Abdeckung"
- `Services\Navigation\ShellStationNavigator.cs` — „0 % Abdeckung"
- `Services\Navigation\ShellTankbookNavigator.cs` — „0 % Abdeckung"
- `Services\Pricing\SecureStorageApiKeyStore.cs` — „0 % Abdeckung"
- `Services\SemanticScreenReaderAnnouncer.cs` — „0 % Abdeckung"
- `Views\Controls\MapRouteDrawable.cs` — „0 % Abdeckung"
- `Views\Controls\StationMapView.Pointer.cs` — „0 % Abdeckung"
- `Views\Controls\StationMapView.xaml.cs` — „0 % Abdeckung"
- `Views\Controls\ViewportChangedEventArgs.cs` — „0 % Abdeckung"
