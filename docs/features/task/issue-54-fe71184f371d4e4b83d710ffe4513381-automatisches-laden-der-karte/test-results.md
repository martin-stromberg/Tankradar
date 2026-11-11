# Test-Ergebnisse

Gemessen am 11.11.2026 (09:00–09:30 Uhr) auf Branch `task/issue-54-fe71184f371d4e4b83d710ffe4513381-automatisches-laden-der-karte`, Windows, .NET 10.0.403, Release-Build (Windows-Job-Simulation: `IncludeAndroidTarget=false`, `IncludeIosTarget=false`, `IncludeMacCatalystTarget=false`, `IncludeWindowsTarget=true`).

## Ergebnis

**Status:** Keine Fehler

## Zusammenfassung

- Gesamt: 2598
- Bestanden: 2598
- Fehlgeschlagen: 0
- Übersprungen: 0

| Suite | Befehl | Ergebnis |
|-------|--------|----------|
| Unit (`Tankradar.Tests.Unit`) | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build --settings coverlet.runsettings --collect:"XPlat Code Coverage"` | 2091/2091 bestanden (13 s) |
| Integration (`Tankradar.Tests.Integration`) | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build --settings coverlet.runsettings --collect:"XPlat Code Coverage"` | 298/298 bestanden (42 s) |
| E2E (`Tankradar.Tests.E2E`, FlaUI) | `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build` | 177/177 bestanden (26 min) |
| Pipeline-Skripte (Node) | `npm test` (`node --test scripts/*.test.mjs`) | 32/32 bestanden (1,6 s) |

Vorausgehender Build: `dotnet build Tankradar.sln --configuration Release` — erfolgreich, 0 Warnungen, 0 Fehler.

Die E2E-Tests liefen im Off-screen-Testmodus (App außerhalb des sichtbaren Bildschirms, kein `-E2EForeground` nötig); die interaktive Windows-Sitzung reichte aus.

## Issue-54-bezogene Tests

- `MapViewModelTests_AutoLoad` (Unit, neu): bestanden — erste Anzeige sucht am Standort mit Radius 5, Wiederanzeige ohne neue Aufrufe, `GpsUsage.Never` still, Adressmodus ohne Startsuche.
- `AutoSearchMockServerTests_Cache` (Integration, neu): bestanden — identische 5-km-Abfrage wird aus dem Zwischenspeicher bedient.
- `MapE2ETests_AutoLoad` (E2E, neu, 5 Tests): 5/5 bestanden — `OpeningMap_LoadsNearbyStationsWithoutSubmit`, `TabSwitch_PreservesResultWithoutNewRequest`, `DetailReturn_PreservesResultWithoutNewRequest`, `GpsNever_SkipsAutoSearchSilently`, `EmptyArea_ShowsEmptyStateWithoutRepeatedRequest`.

## Testabdeckung

**Abdeckung:** 94,2 % Zeilenabdeckung (11004/11675 Zeilen; Branch 89,4 %; Methoden 92,6 %). Mindestabdeckung 70 % via `scripts/check-coverage.mjs`: erfüllt. Quelle: Cobertura aus Unit- und Integrationstest-Lauf, Bericht `coverage-report/Summary.txt`.

Dateien unter 80 % Zeilenabdeckung (aus `coverage.cobertura.xml`):

| Datei | Abdeckung |
|-------|-----------|
| `Services\AppConfiguration.cs` | 35,0 % |
| `ViewModels\HomeStationViewModel.cs` | 43,3 % |
| `Services\Map\LegacyTileCache.cs` | 55,6 % |
| `ViewModels\HomePriceLineViewModel.cs` | 57,1 % |
| `Services\Pricing\ConnectionMonitor.cs` | 57,9 % |
| `Services\Location\MauiLocationService.cs` | 61,1 % |
| `Data\Migrations\20261006124841_AddNearbySetting.cs` | 63,6 % |
| `Data\Migrations\20261018094636_AddStationCustomName.cs` | 63,6 % |
| `Data\Migrations\20261011170920_AddTankbookEntryDetails.cs` | 65,0 % |
| `Services\Navigation\IMapOpener.cs` | 70,6 % |
| `ViewModels\MapViewModel.Panels.cs` | 72,7 % |
| `ViewModels\DataSourceViewModel.cs` | 79,1 % |
| `ViewModels\NearbySectionViewModel.cs` | 79,8 % |

Der für dieses Issue geänderte `ViewModels\MapViewModel.cs` liegt bei 97,4 %.

## Fehlende Tests

Quelle: `Coverage-Daten` — Dateien mit 0 % Zeilenabdeckung (überwiegend plattform-/UI-Glue-Code, der nur über FlaUI-E2E erreichbar ist und laut `coverlet.runsettings`-Kommentar bewusst nicht Teil der Coverage-Messung ist):

- `Data\TankradarDbContextFactory.cs` — 0 % Abdeckung
- `Models\Routing\RouteQueryPoint.cs` — 0 % Abdeckung
- `Models\Tankbook\VehicleLimits.cs` — 0 % Abdeckung
- `Resources\ChartPalette.cs` — 0 % Abdeckung
- `Services\Backup\MauiBackupFilePicker.cs` — 0 % Abdeckung (Plattform-Glue)
- `Services\Backup\MauiBackupSharer.cs` — 0 % Abdeckung (Plattform-Glue)
- `Services\Navigation\ShellFavoriteGroupNavigator.cs` — 0 % Abdeckung (Shell-Glue)
- `Services\Navigation\ShellStationNavigator.cs` — 0 % Abdeckung (Shell-Glue)
- `Services\Navigation\ShellTankbookNavigator.cs` — 0 % Abdeckung (Shell-Glue)
- `Services\Pricing\SecureStorageApiKeyStore.cs` — 0 % Abdeckung (Plattform-Glue)
- `Services\SemanticScreenReaderAnnouncer.cs` — 0 % Abdeckung (Plattform-Glue)
- `Views\Controls\MapRouteDrawable.cs` — 0 % Abdeckung (nur E2E)
- `Views\Controls\StationMapView.Pointer.cs` — 0 % Abdeckung (nur E2E)
- `Views\Controls\StationMapView.xaml.cs` — 0 % Abdeckung (nur E2E)
- `Views\Controls\ViewportChangedEventArgs.cs` — 0 % Abdeckung (nur E2E)

## Befunde während der Läufe

- E2E-Log zeigt wiederholt `Application failed to exit` — die App-Prozesse beenden sich nach einzelnen Tests nicht sauber selbst; ohne Einfluss auf die Testergebnisse (bekanntes Rauschen des Testmodus).
- Einmalig `Fatal error. 0xC0000005` in einem WinRT-Finalizer (`WinRT.IObjectReference.Finalize()` während `GC.RunFinalizers()`) im App-Prozess während `BackupE2ETests_InvalidFiles` — betroffener Test und alle Folgetests bestanden trotzdem; nicht reproduzierbar in diesem Lauf.
- `e2e-diagnostics/` enthält sechs Dateien vom Implementierungslauf (08:30 Uhr, `SearchE2ETests_Offline`, `ElementNotEnabledException` auf gesperrtem `Submit`); sie stammen nicht aus diesem Lauf — beide Tests waren diesmal fehlerfrei. Verzeichnis ist gitignoriert.

## Offene Punkte

Keine.
