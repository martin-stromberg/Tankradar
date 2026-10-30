# Test-Ergebnisse

## Ergebnis

**Status:** Ausführungsfehler

Unit- und Integrationstests sind vollständig bestanden. Die E2E-Suite konnte nicht vollständig und gültig ausgeführt werden: Die MAUI-Anwendung wurde nach dem Build nicht mehr mit `Tankradar.MAUI.runtimeconfig.json` gefunden (`src/Tankradar.MAUI/bin/Debug/net10.0-windows10.0.19041.0/win-x64/`). Die E2E-Tests scheitern deshalb beim App-Start (`hostpolicy.dll` nicht gefunden). Der E2E-Lauf war zum Zeitpunkt des Berichts noch aktiv und wurde nicht beendet, da das Beenden des eigenen Testprozesses vom Berechtigungssystem abgelehnt wurde.

## Fehlgeschlagene Tests

Nicht ausgewertet. Die E2E-Suite ist nicht abgeschlossen. Die bisherigen Fehler sind alle Startfehler der Anwendung (`runtimeconfig.json` fehlt), keine inhaltlichen Testfehler.

## E2E-Abdeckung

Geplante Pflicht-Szenarien laut `plan.md` (Abschnitt „E2E-Tests"). Eine `plan-check.md` existiert nicht.

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Karte füllt die Ansicht; Panel eingeklappt mit Trefferanzahl | `MapE2ETests_Layout` | Nicht ausgeführt (App startet nicht) |
| Panel ausklappen zeigt Liste; Eintrag öffnet Detail | `MapE2ETests_Panel` | Nicht ausgeführt (App startet nicht) |
| Symbolschaltfläche öffnet Overlay; Adresssuche markiert Position | `MapE2ETests_SearchOverlay` | Nicht ausgeführt (App startet nicht) |
| Routensuche über Overlay | `MapE2ETests_SearchOverlay` | Nicht ausgeführt (App startet nicht) |
| Verschieben/Zoomen zeigt „Tankstellen in diesem Bereich laden“ | `MapE2ETests_AreaReload` | Nicht ausgeführt (App startet nicht) |
| Nachladen aktualisiert Anzahl und Marker, Ausschnitt bleibt | `MapE2ETests_AreaReload` | Nicht ausgeführt (App startet nicht) |
| Ohne Bewegung keine Nachlade-Schaltfläche | `MapE2ETests_AreaReload` | Nicht ausgeführt (App startet nicht) |
| Ausschnitt über 25 km: Hinweis, Schaltfläche deaktiviert | `MapE2ETests_AreaReload` | Nicht ausgeführt (App startet nicht) |
| Bedienung ohne Geste (Panel-Schaltfläche, Zoom-/Verschiebeschaltflächen) | `MapE2ETests_Navigation` | Nicht ausgeführt (App startet nicht) |
| Quellenangabe bleibt bei ausgeklapptem Panel | `MapE2ETests_View` | Nicht ausgeführt (App startet nicht) |

Hinweis: Im unvollständigen Lauf wurden bis zum Abbruch 8 Tests bestanden gemeldet (u. a. `HomeE2ETests_Overview`, `RouteE2ETests_Accessibility`). Diese Ergebnisse sind nicht aussagekräftig für die Kartenänderungen.

## Zusammenfassung

- Unit: 2012 gesamt, 2012 bestanden, 0 fehlgeschlagen
- Integration: 297 gesamt, 297 bestanden, 0 fehlgeschlagen
- E2E: unvollständig, Lauf ungültig (Startfehler), keine verlässliche Zählung

## Testabdeckung

**Abdeckung:** 80,7 % (Zeilen, zusammengeführt aus Unit- und Integrationstests; ohne E2E). Quelle: Cobertura-Berichte unter `C:\Users\Martin\AppData\Local\Temp\claude\D--Repositories-softwareschmiede-159935ab-daf8-42be-95a4-4b122012e75c\193abe06-cc86-41f8-b9a0-a855350ff68c\scratchpad\e2e4\unit-cov` und `...\integ-cov`.

| Datei | Abdeckung |
|-------|-----------|
| `MauiProgram.cs` | 24,8 % |
| `Platforms\Windows\CredentialLockerApiKeyStore.cs` | 13,8 % |
| `Models\Routing\RouteQueryPoint.cs` | 25,0 % |
| `Services\AppConfiguration.cs` | 38,7 % |
| `ViewModels\HomeStationViewModel.cs` | 45,2 % |
| `ViewModels\HomePriceLineViewModel.cs` | 52,6 % |
| `Services\Map\LegacyTileCache.cs` | 57,1 % |
| `Services\Pricing\ConnectionMonitor.cs` | 58,6 % |
| `Services\Location\MauiLocationService.cs` | 64,2 % |
| `Services\Map\HttpTileSource.cs` | 77,8 % |
| `ViewModels\DataSourceViewModel.cs` | 79,7 % |

Die Migrationsdateien unter `Data\Migrations\` liegen teilweise unter 80 %. Sie sind generiert und nicht aufgeführt.

## Fehlende Tests

Quelle: `Coverage-Daten` (0 % Zeilenabdeckung in Unit- und Integrationstests). Generierte Dateien (`obj\...`) sind ausgenommen. Die Views und Steuerelemente werden hauptsächlich über E2E (FlaUI) geprüft und sind daher ohne gültigen E2E-Lauf nicht nachweisbar.

- `App.xaml.cs`, `AppShell.xaml.cs`, `Platforms\Windows\App.xaml.cs` — 0 % Abdeckung (Einstiegspunkte)
- `Data\TankradarDbContextFactory.cs` — 0 % Abdeckung
- `Models\Tankbook\VehicleLimits.cs` — 0 % Abdeckung
- `Resources\ChartPalette.cs`, `Resources\DesignSystem.xaml.cs`, `Resources\MapStyles.xaml.cs`, `Resources\TankbookStyles.xaml.cs` — 0 % Abdeckung
- `Services\Backup\MauiBackupFilePicker.cs`, `Services\Backup\MauiBackupSharer.cs` — 0 % Abdeckung
- `Services\Navigation\ShellFavoriteGroupNavigator.cs`, `ShellStationNavigator.cs`, `ShellTankbookNavigator.cs` — 0 % Abdeckung
- `Services\Pricing\SecureStorageApiKeyStore.cs` — 0 % Abdeckung
- `Services\SemanticScreenReaderAnnouncer.cs` — 0 % Abdeckung
- `Views\Charts\BarChartDrawable.cs`, `Views\Charts\ChartGraphicsView.cs` — 0 % Abdeckung
- `Views\Controls\MapRouteDrawable.cs` — 0 % Abdeckung
- `Views\Controls\MapSearchOverlay.xaml.cs`, `Views\Controls\StationBottomSheet.xaml.cs` — 0 % Abdeckung (neue Kartenkomponenten, nur über E2E prüfbar)
- `Views\Controls\StationMapView.xaml.cs`, `Views\Controls\StationMapView.Pointer.cs` — 0 % Abdeckung
- `Views\Controls\StationTile.xaml.cs`, `Views\Controls\ViewportChangedEventArgs.cs` — 0 % Abdeckung
- `Views\MapPage.xaml.cs` — 0 % Abdeckung
- `Views\FavoriteGroupPage.xaml.cs`, `Views\FavoritesPage.xaml.cs`, `Views\SettingsPage.xaml.cs`, `Views\StationDetailPage.xaml.cs`, `Views\TankbookEntriesPage.xaml.cs`, `Views\TankbookPage.xaml.cs`, `Views\TankradarContentPage.cs` — 0 % Abdeckung
