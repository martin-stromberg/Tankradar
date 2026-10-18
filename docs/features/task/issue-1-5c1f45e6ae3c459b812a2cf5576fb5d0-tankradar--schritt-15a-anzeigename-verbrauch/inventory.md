# Bestandsaufnahme Schritt 15a

## Marke bereits vorhanden
- `TankerkoenigClient` liest `brand` (Zeile ~307) in `StationInfo.Brand`; `PriceRepository.SaveAsync` speichert sie in `StationEntity.Brand` (Spalte `Brand` seit Migration `AddPriceCache`). Keine Migration für die Marke nötig; Mock-Server (`MockTankerkoenigServer`) liefert `brand` bereits (ALPHA, BETA), `BackupTestEnvironment` seedet `Brand`.
- Die Marke wird bisher nur in der Detailansicht als Chip (`Detail.Brand`, `StationDetailBuilder.FormatBrand`) angezeigt.

## Anzeigeorte des Stationsnamens
- `StationResultBuilder.CreateItem` -> `StationListItem.Name` (Suche, Karte-Liste, Start/Nearby über `StationTile`, `MapMarkerBuilder`-Beschreibung, Sortierung nach Name).
- `StationDetailBuilder` -> `StationDetailItem.Name` / `BrandText` (`StationDetailPage.xaml`, `Detail.Name`, `Detail.Brand`).
- `FavoritesService.LoadEntriesAsync` -> `FavoriteEntry.StationName` (Gruppenansicht `FavoriteGroupPage.xaml`, `HomeOverviewBuilder` für unbekannte Stationen, `FavoriteGroupViewModel` Zeile ~305).
- `TankbookEntryService.GetKnownStationsAsync` -> `KnownStation.Name` (Tankbuch-Zuordnung). Gespeicherte Vorgänge behalten ihren Namen als Momentaufnahme (ADR 0009).
- `StationInfo` hat vier Kopiermethoden (`WithDetails`, `WithoutStaleDetails`, `WithDistance`, `With`), die jedes Feld einzeln kopieren.

## Datenhaltung / Sicherung
- `StationEntity` (Tabelle `Stations`), `PriceRepository` (Upsert aus der API überschreibt Name/Marke/Adresse, nicht andere Spalten). Eine neue Spalte `CustomName` wird vom Upsert nicht berührt.
- Sicherung (ADR 0011): `BackupDatabasePreparer` baut eine Referenzdatenbank auf dem Migrationsstand der Sicherung und kopiert tabellenweise die Spalten der Referenz; fehlt eine Spalte in der Quelle, wird die Sicherung abgelehnt (Schema-Prüfung); eine neue Migration ist also automatisch in neuen Sicherungen enthalten, ältere Sicherungen werden auf ihrem Stand eingelesen und danach migriert.
- Migrationen: `Data/Migrations/*`, Modellabgleich-Test `MigrationsTests_ModelSync`, Upgrade-Tests je Migration (`*MigrationTests_Upgrade`).

## Verbrauch
- `ConsumptionCalculator.Calculate(entries)` (je Art eine Kette; erster Vollvorgang = `NoPreviousFullEntry`; Teilbetankungen vor dem ersten Vollvorgang zählen nicht), aufgerufen von `TankbookEntryService.GetEntriesAsync` und von dort `TankbookStatisticsService`.
- `TankbookStatisticsCalculator` aggregiert Intervalle (`Calculated`); Startverbrauch nur Ersatzwert (ADR 0010).
- `VehicleEntity.StartOdometerKm`, `FuelKind`; Startart = Strom bei Elektro, sonst Kraftstoff (wie beim Startverbrauch).
- Anzeige: `TankbookEntryTexts` (Verbrauchszeile), `TankbookEntryItemViewModel`, `TankbookStatisticsTexts`.

## Tests
- Unit: `StationResultBuilderTests_*`, `StationDetailBuilderTests_*`, `ConsumptionCalculatorTests_*`, `TankbookStatisticsCalculatorTests_*`, `FavoritesServiceTests_*`. Integration: `PriceRepositoryTests_*`, `BackupServiceTests_*`, Migrationstests. E2E (FlaUI): `FavoritesE2ETestBase`, `DetailE2ETests_Open`, `TankbookEntriesE2ETests_*`, `TankbookStatisticsE2ETests_*`, Helfer `SetFieldValue`.
