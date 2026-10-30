# Test-Ergebnisse

## Ergebnis

**Status:** Fehler vorhanden

Die Unit-, Integrations- und Node-Tests sind vollständig grün. Im E2E-Lauf (173 Tests) ist ein Test fehlgeschlagen: `TankbookEntriesE2ETests_StartView.Tab_ShowsEntriesOfOnlyVehicleDirectly`. Der Test betrifft das Tankbuch und nicht die Kartenansicht. Ein gezielter Einzellauf dieses Tests (mit der Testklasse, 2 Tests) war anschließend grün. Der Fehler ist daher vermutlich instabil (Timing beim ersten Start, Einrichtungsdialog nicht rechtzeitig sichtbar) und nicht reproduzierbar. Er sollte trotzdem beobachtet werden.

## Fehlgeschlagene Tests

### Tankradar.Tests.E2E / TankbookEntriesE2ETests_StartView

- **Tab_ShowsEntriesOfOnlyVehicleDirectly** — „Die Einrichtung beim ersten Start wurde nicht angezeigt.“ (in `E2ETestBase.SkipSetup()`, beim Start der App). Einzellauf danach: bestanden.

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Karte füllt die Ansicht; Panel eingeklappt mit Trefferanzahl | `MapE2ETests_Layout.Map_FillsView_PanelCollapsedShowsCount` | Bestanden |
| Panel ausklappen zeigt Liste, Eintrag öffnet Detail | `MapE2ETests_Panel.ExpandPanel_ShowsList_AndEntryOpensDetails` | Bestanden |
| Symbolschaltfläche öffnet Overlay; Adresssuche schließt Overlay und markiert Position | `MapE2ETests_SearchOverlay.OverlayButton_AddressSearch_ClosesOverlayAndMarksPosition` | Bestanden |
| Route über Overlay suchen | `MapE2ETests_SearchOverlay.OverlayRouteSearch_ShowsRouteOnMap` | Bestanden |
| Verschieben oder Zoomen zeigt Nachlade-Schaltfläche | `MapE2ETests_AreaReload.PanOrZoom_ShowsReloadButtonWithoutQuery` | Bestanden |
| Klick auf Nachladen aktualisiert Anzahl und Marker, Ausschnitt bleibt | `MapE2ETests_AreaReload.ReloadArea_UpdatesCountAndMarkers_KeepsViewport` | Bestanden |
| Ohne Bewegung keine Nachlade-Schaltfläche | `MapE2ETests_AreaReload.WithoutMovement_NoReloadButton` | Bestanden |
| Ausschnitt über 25 km: Hinweis, Schaltfläche deaktiviert | `MapE2ETests_AreaReload.TooLargeArea_ShowsHintAndDisablesButton` | Bestanden |
| Bedienung ohne Geste (Panel-Schaltfläche, Zoom) | `MapE2ETests_Navigation.PanelToggleButton_CollapsesAndExpandsWithoutGesture`, `MapE2ETests_Navigation.Zoom_ChangesLevelWithinLimits` | Bestanden |
| Quellenangabe bleibt sichtbar | `MapE2ETests_View.Map_ShowsAttributionLegendAndLoadsTilesFromMockServerOnly` | Bestanden |

Angepasste Bestandstests im Umfeld der Karte (Auswahl): `MapE2ETests_View.SwitchView_BetweenListAndMap_ShowsTheSameResults` und `MapE2ETests_View.DefaultViewFromSettings_Map_OpensSearchAsMap` (bestanden), `MapE2ETests_Navigation.Pan_MovesMarkersOutOfAndBackIntoView` und `ZoomIn_HidesDistantMarkers_AndRecenterShowsAll` (bestanden).

## Zusammenfassung

Einzelergebnisse je Suite:

| Suite | Gesamt | Bestanden | Fehlgeschlagen | Übersprungen |
|-------|--------|-----------|----------------|--------------|
| Tankradar.Tests.Unit | 2006 | 2006 | 0 | 0 |
| Tankradar.Tests.Integration | 297 | 297 | 0 | 0 |
| Tankradar.Tests.E2E | 173 | 172 | 1 | 0 |
| Node-Tests (`npm test`, CI-Hilfsskripte) | 32 | 32 | 0 | 0 |

- Gesamt: 2508
- Bestanden: 2507
- Fehlgeschlagen: 1
- Übersprungen: 0

Build-Reihenfolge: Unit- und Integrationstests zuerst gebaut und ausgeführt, danach MAUI-Build (0 Fehler, 0 Warnungen) und erst dann E2E mit `--no-build` und `TANKRADAR_E2E_WINDOW=offscreen`. Die `Tankradar.MAUI.runtimeconfig.json` war vor dem E2E-Lauf vorhanden.

## Testabdeckung

**Abdeckung:** 88,9 % (Zeilen, Unit-Tests, Coverlet-Konfiguration aus `coverlet.runsettings`; E2E-Tests sind nicht in der Coverage enthalten)

| Datei | Abdeckung |
|-------|-----------|
| Views\Controls\MapRouteDrawable.cs | 0 % |
| Views\Controls\StationMapView.Pointer.cs | 0 % |
| Views\Controls\StationMapView.xaml.cs | 0 % |
| Views\Controls\ViewportChangedEventArgs.cs | 0 % |
| Services\SemanticScreenReaderAnnouncer.cs | 0 % |
| Services\SetupStatusService.cs | 0 % |
| Services\Pricing\SecureStorageApiKeyStore.cs | 0 % |
| Services\Navigation\ShellFavoriteGroupNavigator.cs | 0 % |
| Services\Navigation\ShellStationNavigator.cs | 0 % |
| Services\Navigation\ShellTankbookNavigator.cs | 0 % |
| Services\Backup\BackupArchiveWriter.cs | 0 % |
| Services\Backup\BackupDatabasePreparer.cs | 0 % |
| Services\Backup\BackupService.cs | 0 % |
| Services\Backup\MauiBackupFilePicker.cs | 0 % |
| Services\Backup\MauiBackupSharer.cs | 0 % |
| Resources\ChartPalette.cs | 0 % |
| Models\Tankbook\VehicleLimits.cs | 0 % |
| Models\Routing\RouteQueryPoint.cs | 0 % |
| Models\Backup\BackupTextLimits.cs | 0 % |
| Data\TankradarDbContextFactory.cs | 0 % |
| ViewModels\HomeStationViewModel.cs | 37,8 % |
| Services\Backup\RestoreSwap.cs | 45,3 % |
| Services\Location\MauiLocationService.cs | 55,5 % |
| Services\Map\LegacyTileCache.cs | 57,1 % |
| ViewModels\HomePriceLineViewModel.cs | 60,0 % |
| Data\Migrations\20261006124841_AddNearbySetting.cs | 61,5 % |
| Data\Migrations\20261018094636_AddStationCustomName.cs | 61,5 % |
| Services\Tankbook\MauiReceiptPhotoPicker.cs | 63,6 % |
| Data\Migrations\20261011170920_AddTankbookEntryDetails.cs | 64,7 % |
| Services\Map\HttpTileSource.cs | 69,7 % |
| Data\Migrations\20261004180847_AddFuelTypeSettings.cs | 77,8 % |
| Data\Migrations\20261004180805_InitialCreate.cs | 78,9 % |

Dateien mit 100 % sind nicht aufgeführt. Die Liste umfasst 216 Quelldateien.

## Fehlende Tests

Quelle: `Coverage-Daten`

- `Views\Controls\MapRouteDrawable.cs` — „0 % Abdeckung“ (Teil der neuen Kartenansicht, nur über E2E-Pfade erreichbar)
- `Views\Controls\StationMapView.Pointer.cs` — „0 % Abdeckung“ (Teil der neuen Kartenansicht, Zeigerereignisse)
- `Views\Controls\StationMapView.xaml.cs` — „0 % Abdeckung“ (XAML-Code-Behind, laut `coverlet.runsettings` als UI-Glue eigentlich ausgeschlossen)
- `Views\Controls\ViewportChangedEventArgs.cs` — „0 % Abdeckung“
- `Services\SemanticScreenReaderAnnouncer.cs` — „0 % Abdeckung“
- `Services\SetupStatusService.cs` — „0 % Abdeckung“
- `Services\Pricing\SecureStorageApiKeyStore.cs` — „0 % Abdeckung“
- `Services\Navigation\ShellFavoriteGroupNavigator.cs` — „0 % Abdeckung“
- `Services\Navigation\ShellStationNavigator.cs` — „0 % Abdeckung“
- `Services\Navigation\ShellTankbookNavigator.cs` — „0 % Abdeckung“
- `Services\Backup\BackupArchiveWriter.cs` — „0 % Abdeckung“
- `Services\Backup\BackupDatabasePreparer.cs` — „0 % Abdeckung“
- `Services\Backup\BackupService.cs` — „0 % Abdeckung“
- `Services\Backup\MauiBackupFilePicker.cs` — „0 % Abdeckung“
- `Services\Backup\MauiBackupSharer.cs` — „0 % Abdeckung“
- `Resources\ChartPalette.cs` — „0 % Abdeckung“
- `Models\Tankbook\VehicleLimits.cs` — „0 % Abdeckung“
- `Models\Routing\RouteQueryPoint.cs` — „0 % Abdeckung“
- `Models\Backup\BackupTextLimits.cs` — „0 % Abdeckung“
- `Data\TankradarDbContextFactory.cs` — „0 % Abdeckung“
