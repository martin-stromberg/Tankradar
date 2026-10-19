← [Zurück zur Übersicht](index.md)

# Tankbuch — Technischer Ablauf

## Daten

Migration `AddVehicles` (Schritt 12, rein additiv, `VehiclesMigrationTests_Upgrade`):

- `Vehicles`: `Id`, `Name` (`NOCASE`, eindeutiger Index `IX_Vehicles_Name`), `FuelKind` (Enum-Name von `VehicleFuelKind`: `Petrol`, `Diesel`, `Electric`, `Hybrid` – Persistenzvertrag), `StartOdometerKm` (`INTEGER`), `StartConsumption` (`decimal` als `TEXT`, optional), `CreatedUtc`.
- `TankbookEntries`: `Id`, `VehicleId` (`ON DELETE CASCADE`), `CreatedUtc`.

Migration `AddTankbookEntryDetails` (Schritt 13, erzeugt mit `dotnet ef migrations add`, rein additiv per `ALTER TABLE … ADD COLUMN`, `TankbookEntriesMigrationTests_Upgrade`):

- Neue Spalten in `TankbookEntries`: `OccurredAt` (lokale Wanduhrzeit, `TEXT`), `Kind` (Enum-Name von `TankbookEntryKind`: `Fuel`, `Electricity`; Standard `Fuel`), `Quantity`, `UnitPrice`, `TotalAmount` (`decimal` als `TEXT`, Standard 0), `OdometerKm`, `IsPartial` (Standard 0), `StationName` (Standard leer), `StationId` (ohne Fremdschlüssel), `Note`, `ReceiptPhotoFileName`.
- Index `IX_TankbookEntries_VehicleId_OccurredAt` ersetzt `IX_TankbookEntries_VehicleId`; Fremdschlüssel und Kaskade bleiben unverändert.
- Datenübernahme im `Up`: `OccurredAt` = `CreatedUtc` in Ortszeit, auf die Minute gekürzt, `OdometerKm` = Start-Kilometerstand des Fahrzeugs, `Kind = 'Electricity'` für Elektrofahrzeuge.

Verbindungszeichenfolge: `TankradarDbContext.BuildConnectionString(pfad, pooling)` setzt `Foreign Keys=True` (App in `MauiProgram`, Integrationstests über `TestDatabase`; `ForeignKeysTests_Connection` prüft `PRAGMA foreign_keys = 1`).

Unbekannte gespeicherte Enum-Namen werden als `Petrol` bzw. `Fuel` gelesen und als Warnung ohne Namen protokolliert.

## Dienste

`IVehicleService` / `VehicleService` (Singleton):

- `GetVehiclesAsync`, `GetVehicleAsync`, `CreateVehicleAsync`, `CountEntriesAsync`.
- `UpdateVehicleAsync` → `VehicleResult`; zusätzlich `StartOdometerAboveEntries`, wenn der Start-Kilometerstand über dem niedrigsten Vorgang liegt.
- `DeleteVehicleAsync(vehicleId, expectedEntryCount)` → `VehicleDeleteResult`: zählt in derselben Transaktion; abweichende Zahl → `EntryCountChanged` mit der aktuellen Zahl, nichts gelöscht. Sonst Kaskade, Commit und danach Löschen der Belegfotos (`IReceiptPhotoStore`).

`ITankbookEntryService` / `TankbookEntryService` (Singleton, `IDbContextFactory`, `IDatabaseInitializer`, `IReceiptPhotoStore`, `TimeProvider`, `ILogger`):

- `GetEntriesAsync(vehicleId)` → `TankbookEntryOverview` (neueste zuerst, Strecke, Verbrauch, `TankbookConsumptionState`); beim ersten Aufruf je Prozess `IReceiptPhotoStore.DeleteUnreferenced`.
- `CreateEntryAsync(vehicleId, input)`, `UpdateEntryAsync(entryId, input)` → `TankbookEntrySaveResult` (`Result`, `Entry`, `OdometerBoundKm`), `DeleteEntryAsync` → `TankbookEntryResult`.
- Ablauf beim Speichern: Schutzprüfung `TankbookEntryValidator.Validate`, Fahrzeug laden, Art erlaubt (`IsKindAllowed`; beim Korrigieren bleibt die gespeicherte Art erlaubt), Vorgänger/Nachfolger nach `TankbookEntryOrder` (Zeitpunkt, Kilometerstand, Kennung), Kilometerregeln (`OdometerBelowVehicleStart`, `OdometerBelowPrevious`, `OdometerAboveNext`), Load-and-Patch per `TankbookEntryEntity.Apply`. Ein ersetztes Foto wird nach dem Commit gelöscht.
- `GetKnownStationsAsync`: `FavoriteEntries` ⨝ `Stations` (eindeutig je Kennung, nach Namen de-DE), danach freie Namen aus `TankbookEntries` ohne `StationId` (ohne Duplikate, ohne Beachtung der Groß- und Kleinschreibung); höchstens `TankbookEntryLimits.MaxKnownStations` (30).

`ITankbookStatisticsService` / `TankbookStatisticsService` (Schritt 14, Singleton, `IVehicleService`, `ITankbookEntryService`, `TimeProvider`; keine Schemaänderung, nichts wird gespeichert):

- `GetStatisticsAsync(vehicleId)` → `TankbookStatisticsLoad` (`Status` `Ok` oder `VehicleNotFound`, `Statistics`). Lädt das Fahrzeug und die Vorgänge (`TankbookEntryOverview` mit `IntervalQuantity` und `IntervalCost`) und ruft `TankbookStatisticsCalculator.Calculate(vehicle, overviews, today)` mit `today = TimeProvider.GetLocalNow().DateTime`. Speicherfehler werden als Ausnahme weitergereicht und im ViewModel abgefangen.
- `TankbookStatisticsCalculator` (reine statische Funktion): je vorhandener Art (Kraftstoff vor Strom) ein `TankbookKindStatistics` mit Durchschnittsverbrauch und Kosten pro 100 km (Σ Intervallwerte / Σ Strecke × 100, 2 Nachkommastellen, `MidpointRounding.AwayFromZero`, nur Intervalle mit Strecke > 0), Quelle (`TankbookAverageSource`: `None`, `Calculated`, `FromStart`, `StartConsumption`; reguläre Intervalle gehen vor, das Intervall ab Startstand bildet die Kennzahlen nur ohne reguläres Intervall), `OffersStartConsumption` (die Art ist die Startart und es ist kein Startverbrauch hinterlegt; steuert den Verweis im Hinweis), Gesamtkosten (Modellwert, nicht angezeigt), lückenloser Monatsreihe (`MonthlyCostSummary` mit Kosten, Menge und Anzahl, neuester Monat zuerst, bis zum Monat von `today`; `Months[0]` ist der laufende Monat) und Verlaufspunkten (`TrendPoint`, ältester zuerst).
- `ConsumptionCalculator` führt dafür zusätzlich Menge und Kosten des Intervalls (Vollvorgang plus Teilbetankungen dazwischen); die Verbrauchswerte sind unverändert.

Rechenbausteine (reine statische Funktionen):

- `AmountCalculator.Complete` (zwei aus drei, `MidpointRounding.AwayFromZero`; Abweichung über `AmountTolerance` = 0,02 €) und `IsConsistent`.
- `ConsumptionCalculator.Calculate(entries, startOdometerKm, startKind)`: Ketten je Art, Teilbetankungen addiert; für die Startart (Kraftstoff, bei Elektrofahrzeugen Strom) ist der Start-Kilometerstand die Basis des ersten Vollvorgangs (`TankbookEntryOverview.IsFromStart`, Teilbetankungen davor zählen mit); ohne Startstand bzw. für die andere Art eines Hybrids hat der erste Vollvorgang keinen Wert; Strecke ≤ 0 ohne Wert. `TankbookEntryService.GetEntriesAsync` übergibt den Start-Kilometerstand und die Startart des Fahrzeugs.
- `DecimalInputParser` (Komma oder einzelner Punkt, Nachkommastellen, Bereich) und `TankbookEntryValidator.ValidateForm` (alle Feldfehler als `TankbookEntryFieldError` gleichzeitig, berechneter dritter Wert in der Eingabe).

Belegfotos:

- `IReceiptPhotoStore` / `ReceiptPhotoStore`: `<Datenverzeichnis>/receipts/<Guid>.<Endung>`, nur Endungen `.jpg`, `.jpeg`, `.png`, `.heic`, `.heif` mit passender Dateisignatur (`DetectExtension`; ein falsch benanntes Foto erhält die Endung des erkannten Formats) und höchstens 20 MB, unveränderte Kopie, Dateischutz über `IDatabaseFileProtector.ProtectFile`; nur Dateinamen im Format der Ablage werden akzeptiert (kein Pfad).
- Das Formular sperrt „Foto auswählen“/„Foto aufnehmen“ während einer laufenden Auswahl und löscht ein Foto, das erst nach dem Schließen oder Neuöffnen des Formulars fertig wird. `IReceiptPhotoStore.Delete` wirft nicht (Fehler werden protokolliert).
- `IReceiptPhotoPicker`: `MauiReceiptPhotoPicker` (`MediaPicker.PickPhotosAsync` mit `SelectionLimit = 1`, `CapturePhotoAsync` nur bei `IsCaptureSupported`); im Testmodus (`TANKATLAS_TEST_DATA_PATH`) `TestReceiptPhotoPicker` mit der Datei aus `TANKATLAS_TEST_RECEIPT_PHOTO` (Auswahl über `ReceiptPhotoPickerSelector`).
- iOS/MacCatalyst: `NSCameraUsageDescription`, `NSPhotoLibraryUsageDescription` in `Info.plist`.

## Oberfläche

- `TankbookPage` / `TankbookViewModel` mit `VehicleItemViewModel` (u. a. `OpenEntriesCommand`, `ShownEntryCount`, `ConfirmDeleteHint`, `CancelDeleteHint`, `VehicleHint` als Hinweis von „Tank- und Ladevorgänge“, „Bearbeiten“ und „Löschen“); `ITankbookNavigator` / `ShellTankbookNavigator` öffnet die Route `tankbookentries` mit `vehicleId`.
- `TankbookEntriesPage` / `TankbookEntriesViewModel` (`IQueryAttributable`; Liste aus `TankbookEntryItemViewModel`, Löschrückfrage im Eintrag), Formular `TankbookEntryFormViewModel` (Datum/Uhrzeit, Art-Chips `ChoiceOptionViewModel<TankbookEntryKind>`, Tankstellen-Chips `KnownStationOptionViewModel`, Rechenhinweis, Feldfehler, Foto mit ausstehender Datei).
- Auswertung: `TankbookStatisticsViewModel` (Eigenschaft `Statistics` von `TankbookEntriesViewModel`; `LoadAsync` nach jedem Laden der Vorgänge, `Reset` in `Show`) mit `TankbookKindStatisticsViewModel` je Art und `MonthlyCostRowViewModel` je Monat; Texte in `TankbookStatisticsTexts`. Die Diagramme zeichnet `ChartGraphicsView` (`GraphicsView`, bindbare Eigenschaft `Data` vom Typ `ChartData`) mit `BarChartDrawable` (`Views/Charts/`, MAUI Graphics, keine Zusatzbibliothek, siehe ADR 0010); die Seite blendet die View ohne Daten aus (`IsVisible` an `ChartData.HasData`). Die View meldet sich über eine schwache Referenz beim Ereignis `Application.RequestedThemeChanged` an und beim Verlust ihres Handlers ab, sodass neu aufgebaute Listeneinträge nicht am Ereignis hängen bleiben. Die Berechnung von Wertebereich, Koordinaten, Beschriftungsstellen und Textflächen (`ChartTextArea`: am Rand nach innen geschoben und links- bzw. rechtsbündig, damit nichts abgeschnitten wird) liegt in der reinen Klasse `ChartGeometry` (`Services/Charts/`, unit-getestet), die Farben in `ChartPalette` (hell/dunkel); `**/Views/Charts/**` ist von der Coverage ausgenommen.
- Gemeinsame Stile in `Resources/TankbookStyles.xaml` (Karten, Schaltflächen, Chips, Eingabefelder mit Einheiten-Suffix, Feldbeschriftung und -fehler), in beiden Seiten eingebunden.
- Texte: `TankbookTexts` und `TankbookEntryTexts` (Formatierungen de-DE, z. B. „74,36 €“, „1,859 €/L“, „6,00 L/100 km“, „18.05.2026, 14:30“).
- Sprachausgabe: Schaltflächen behalten ihren sichtbaren Text als Namen (WCAG 2.5.3); der Kontext steht in `SemanticProperties.Hint`. Rückfragen und der erste Feldfehler werden über `IScreenReaderAnnouncer` angesagt.
- AutomationIds der Vorgangsseite: `TankbookEntriesPage.Headline`, `TankbookEntries.Vehicle`, `TankbookEntries.VehicleDetails`, `TankbookEntries.Status`, `TankbookEntries.Add`, `TankbookEntries.Empty`, `TankbookEntries.Form.*` (u. a. `Title`, `Date`, `Time`, `Station`, `KnownStation.<Index>`, `KindText`, `Kind.<Wert>`, `Quantity`, `UnitPrice`, `TotalAmount`, `ComputedHint`, `Odometer`, `Partial`, `Note`, `PickPhoto`, `CapturePhoto`, `RemovePhoto`, `PhotoPreview`, die zugehörigen `…Error`, `Save`, `Cancel`) und `TankbookEntry.Item.*` (`Station`, `Total`, `Date`, `UnitPrice`, `Quantity`, `Distance`, `Consumption`, `Note`, `Partial`, `Kind`, `ShowReceipt`, `Receipt`, `ReceiptMissing`, `Edit`, `Delete`, `DeleteQuestion`, `DeleteConfirm`, `DeleteCancel`); auf der Fahrzeugkarte zusätzlich `Tankbook.Vehicle.Entries`. Auswertung: `TankbookEntries.Statistics.Heading`, `….Notice` sowie je Art (`{Key}` = `Fuel` oder `Electricity`) `TankbookEntries.Statistics.{Key}.` plus `Heading`, `Notice`, `Average` (Zahlenwert) und `AverageUnit`, `AverageSource`, `CostPer100Km` (Zahlenwert) und `CostPer100KmUnit` („€“ bzw. „noch nicht berechenbar“), `MonthAnalysisHeading`, `MonthCostLabel`, `MonthCost`, `MonthQuantity`, `ConsumptionChart` (fehlt ohne Werte), `ConsumptionChartSummary`, `CostChart` (fehlt ohne Werte), `CostChartSummary`, `MonthsHeading`, `MonthsNotice` (nur bei gekürzter Liste) und `Month` (je Zeile identisch); im Formular `TankbookEntries.Form.PartialHint`.

## Tests

- Unit: `DecimalInputParserTests_Values`, `AmountCalculatorTests_*`, `ConsumptionCalculatorTests_*` (auch `_Interval`), `TankbookStatisticsCalculatorTests_*` (`Average`, `Threshold`, `Cost`, `Months`, `Trend`), `TankbookKindStatisticsViewModelTests_Notice`/`_MonthCard`, `TankbookStatisticsServiceTests_Read`, `TankbookStatisticsViewModelTests_Load`, `TankbookEntriesViewModelTests_Statistics`/`_DeleteResult`, `TankbookStatisticsTextsTests_*`, `ChartGeometryTests`, `ReceiptPhotoStoreTests_Detection`, `TankbookEntryValidatorTests_*`, `TankbookEntryServiceTests_*`, `VehicleServiceTests_*`, `ReceiptPhotoStoreTests_Files`, `ReceiptPhotoPickerSelectorTests_TestMode`, `TankradarDbContextTests_ConnectionString`, `PlatformManifestTests_Photos`, `TankbookViewModelTests_*`, `TankbookEntriesViewModelTests_*`, `TankbookEntryFormViewModelTests_*`, `TankbookEntryTextsTests_*`, `TankbookTextsTests_*` (Hilfsklassen `VehicleFixture`, `TankbookEntryFixture`, `FakeReceiptPhotoStore`, `FakeReceiptPhotoPicker`, `FakeTankbookNavigator`).
- Integration: `VehiclesMigrationTests_Upgrade`, `TankbookEntriesMigrationTests_Upgrade` (Bestandsdaten), `VehicleServiceTests_RealDatabase`, `TankbookEntryServiceTests_RealDatabase`, `TankbookStatisticsServiceTests_RealDatabase`, `ForeignKeysTests_Connection`.
- FlaUI: `TankbookE2ETests_*` (Fahrzeuge, WCAG-Namen, Neuzählung beim Löschen), `TankbookEntriesE2ETests_Flow`/`_Partial`/`_PartialHint`/`_Electric`/`_Validation`/`_EditDelete`/`_Receipt`/`_Persistence`, `TankbookStatisticsE2ETests_Flow`/`_Update`/`_Fallback`/`_Months`/`_Partial`/`_Hybrid`/`_Persistence` und das Offline-Szenario `OfflineE2ETests_Tankbook`. Die Bedienung kapselt `TankbookE2EDriver`, und `TankbookTestDatabase` schreibt und liest die isolierte Testdatenbank direkt (u. a. `AddEntry` mit Art, Betrag und Zeitpunkt). Der Inhalt eines `GraphicsView` ist per UI-Automation nicht prüfbar; die E2E-Tests prüfen die Zusammenfassungs-Texte, die Kennzahlen und die Existenz der Diagramm-AutomationIds, ohne Werte auch deren Fehlen.
