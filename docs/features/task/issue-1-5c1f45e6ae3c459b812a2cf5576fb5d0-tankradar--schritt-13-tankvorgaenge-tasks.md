# Tasks: Schritt 13 – Tank- und Ladevorgänge erfassen

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Modelle | Enums `TankbookEntryKind`, `TankbookConsumptionState`, `TankbookEntryResult`, `TankbookEntryFieldError`, `AmountField`, `ReceiptPhotoError` anlegen | Offen | — |
| 2 | Modelle | Records `TankbookEntry`, `TankbookEntryInput`, `TankbookEntryOverview`, `TankbookEntrySaveResult` anlegen | Offen | — |
| 3 | Modelle | Records `TankbookEntryFormValidation`, `AmountCompletion`, `KnownStation`, `VehicleDeleteResult`, `ReceiptPhotoImportResult`, Klasse `PickedReceiptPhoto` anlegen | Offen | — |
| 4 | Modelle | `TankbookEntryLimits` anlegen | Offen | — |
| 5 | Modelle | `VehicleResult` um `EntryCountChanged`, `StartOdometerAboveEntries` erweitern | Offen | — |
| 6 | Logik | `DecimalInputParser` anlegen | Offen | — |
| 7 | Logik | `AmountCalculator` (zwei aus drei, Toleranz 0,02 €) anlegen | Offen | — |
| 8 | Logik | `TankbookEntryOrder` anlegen | Offen | — |
| 9 | Logik | `ConsumptionCalculator` (Ketten je Art, Teilbetankungen) anlegen | Offen | — |
| 10 | Validierung | `TankbookEntryValidator.ValidateForm`, `Validate`, `IsKindAllowed` anlegen | Offen | — |
| 11 | Datenmodell | `TankbookEntryEntity` um neue Eigenschaften und `Apply` erweitern | Offen | — |
| 12 | Datenmodell | `TankradarDbContext.OnModelCreating` für `TankbookEntries` erweitern (Defaults, TEXT-Dezimale, Index) | Offen | — |
| 13 | Datenmodell | `TankradarDbContext.BuildConnectionString` mit `Foreign Keys=True` anlegen | Offen | — |
| 14 | Konfiguration | `MauiProgram` auf `BuildConnectionString` umstellen | Offen | — |
| 15 | Datenmodell | Migration `AddTankbookEntryDetails` per EF-CLI erzeugen und `Up` um Datenübernahme (`OccurredAt`, `OdometerKm`, `Kind`) ergänzen | Offen | — |
| 16 | Logik | `IDatabaseFileProtector.ProtectFile` + `DatabaseFileProtector` umsetzen | Offen | — |
| 17 | Logik | `IReceiptPhotoStore`/`ReceiptPhotoStore` anlegen | Offen | — |
| 18 | Logik | `IReceiptPhotoPicker`, `MauiReceiptPhotoPicker`, `TestReceiptPhotoPicker`, `ReceiptPhotoPickerSelector` anlegen | Offen | — |
| 19 | Konfiguration | `TestDataPaths.TestReceiptPhotoEnvironmentVariable` ergänzen | Offen | — |
| 20 | Konfiguration | `NSCameraUsageDescription`/`NSPhotoLibraryUsageDescription` in iOS- und MacCatalyst-`Info.plist` | Offen | — |
| 21 | Logik | `ITankbookEntryService` anlegen | Offen | — |
| 22 | Logik | `TankbookEntryService.GetEntriesAsync` (Verbrauch, neueste zuerst, Bereinigung verwaister Fotos) | Offen | — |
| 23 | Logik | `TankbookEntryService.CreateEntryAsync`/`UpdateEntryAsync` mit Kilometer- und Artprüfung | Offen | — |
| 24 | Logik | `TankbookEntryService.DeleteEntryAsync` inkl. Fotodatei | Offen | — |
| 25 | Logik | `TankbookEntryService.GetKnownStationsAsync` | Offen | — |
| 26 | Logik | `VehicleService.DeleteVehicleAsync(vehicleId, expectedEntryCount)` atomar mit Fotolöschung | Offen | — |
| 27 | Validierung | `VehicleService.UpdateVehicleAsync`: `StartOdometerAboveEntries` | Offen | — |
| 28 | UI | `TankbookEntryTexts` anlegen | Offen | — |
| 29 | UI | `TankbookTexts`: Hints, „Tank- und Ladevorgänge“, neue Ergebnistexte; Description-Methoden entfernen | Offen | — |
| 30 | UI | `ITankbookNavigator`/`ShellTankbookNavigator` anlegen, Route in `AppShell` registrieren | Offen | — |
| 31 | UI | Styles nach `Resources/TankbookStyles.xaml` verschieben und in `TankbookPage` einbinden | Offen | — |
| 32 | UI | `VehicleItemViewModel`: `ShownEntryCount`, Hints, `OpenEntriesCommand` | Offen | — |
| 33 | UI | `TankbookViewModel`: Navigation, Neuzählung beim Löschen, `StartOdometerAboveEntries` | Offen | — |
| 34 | UI | `TankbookPage.xaml`: WCAG-Hints für `DeleteConfirm`, `DeleteCancel`, `Form.Cancel`; Schaltfläche „Tank- und Ladevorgänge“ | Offen | — |
| 35 | UI | `KnownStationOptionViewModel` anlegen | Offen | — |
| 36 | UI | `TankbookEntryFormViewModel` anlegen (Felder, Chips, Rechenhinweis, Foto, Speichern/Abbrechen) | Offen | — |
| 37 | UI | `TankbookEntryItemViewModel` anlegen (Anzeige, Rückfrage, Beleg anzeigen) | Offen | — |
| 38 | UI | `TankbookEntriesViewModel` anlegen (`IQueryAttributable`, Laden, Löschen) | Offen | — |
| 39 | UI | `TankbookEntriesPage.xaml` + Code-Behind anlegen | Offen | — |
| 40 | Konfiguration | DI-Registrierungen in `MauiProgram` (Dienste, Picker, Navigator, ViewModels, Seite) | Offen | — |
| 41 | Tests | Unit `DecimalInputParserTests_Values` | Offen | — |
| 42 | Tests | Unit `AmountCalculatorTests_Completion`, `AmountCalculatorTests_Plausibility` | Offen | — |
| 43 | Tests | Unit `ConsumptionCalculatorTests_FullEntries`, `_Partial`, `_Electric` | Offen | — |
| 44 | Tests | Unit `TankbookEntryValidatorTests_Form`, `_Input` | Offen | — |
| 45 | Tests | Unit `TankbookEntryServiceTests_Create`, `_Odometer`, `_Read`, `_Change`, `_KnownStations` | Offen | — |
| 46 | Tests | Unit `VehicleServiceTests_Delete`/`_Manage` erweitern und an neue Signatur anpassen; `VehicleFixture` | Offen | — |
| 47 | Tests | Unit `ReceiptPhotoStoreTests_Files`, `ReceiptPhotoPickerSelectorTests_TestMode` | Offen | — |
| 48 | Tests | Unit `TankradarDbContextTests_ConnectionString`, `PlatformManifestTests_Photos` | Offen | — |
| 49 | Tests | Unit `TankbookViewModelTests_*` anpassen und erweitern (Neuzählung, WCAG, Navigation) | Offen | — |
| 50 | Tests | Unit `TankbookEntriesViewModelTests_*`, `TankbookEntryFormViewModelTests_*` | Offen | — |
| 51 | Tests | Unit `TankbookEntryTextsTests_*`, `TankbookTextsTests_*` anpassen | Offen | — |
| 52 | Tests | Integration `TankbookEntriesMigrationTests_Upgrade` | Offen | — |
| 53 | Tests | Integration `TankbookEntryServiceTests_RealDatabase` | Offen | — |
| 54 | Tests | Integration `ForeignKeysTests_Connection`; `TestDatabase` auf `BuildConnectionString` | Offen | — |
| 55 | Tests | Integration `DatabaseInitializerTests_*` auf 7 Migrationen, `VehicleServiceTests_RealDatabase` anpassen | Offen | — |
| 56 | E2E-Tests | `TankbookE2EDriver` herauslösen, `TankbookE2ETestBase` delegieren lassen | Offen | — |
| 57 | E2E-Tests | `TankbookTestDatabase` um vollständige Vorgänge, Abfragen und Fotodateien erweitern | Offen | — |
| 58 | E2E-Tests | `TankbookEntriesE2ETests_Flow` (Erfassen + Verbrauch) | Offen | — |
| 59 | E2E-Tests | `TankbookEntriesE2ETests_Partial` | Offen | — |
| 60 | E2E-Tests | `TankbookEntriesE2ETests_Electric` | Offen | — |
| 61 | E2E-Tests | `TankbookEntriesE2ETests_Validation` | Offen | — |
| 62 | E2E-Tests | `TankbookEntriesE2ETests_EditDelete` | Offen | — |
| 63 | E2E-Tests | `TankbookEntriesE2ETests_Receipt` | Offen | — |
| 64 | E2E-Tests | `OfflineE2ETests_Tankbook` | Offen | — |
| 65 | E2E-Tests | `TankbookE2ETests_DeleteWithEntries.EntryAddedBeforeConfirming_IsNamedBeforeDeleting` | Offen | — |
| 66 | E2E-Tests | `TankbookEntriesE2ETests_Persistence` | Offen | — |
| 67 | E2E-Tests | `TankbookE2ETests_Validation`: Start-Kilometerstand über erstem Vorgang | Offen | — |
| 68 | E2E-Tests | `TankbookE2ETests_Flow`, `TankbookE2ETests_DeleteWithEntries` an WCAG-Namen anpassen | Offen | — |
| 69 | Dokumentation | `docs/adr/0009-tankbook-entries.md` anlegen; ADR 0008 „Folgen“ verweisen | Offen | — |
| 70 | Dokumentation | `docs/help/Tankbuch/` (index, beschreibung, ablauf-anwender, ablauf-technisch) aktualisieren | Offen | — |
| 71 | Dokumentation | `changes.log`-Eintrag Version 0.1.17 | Offen | — |
| 72 | Auslieferung | `local-ci.ps1` und Pre-Push-Prüfungen grün | Offen | — |
| 73 | Auslieferung | Zwischenstand `review-versions/0.1.17_<Datum>/` erzeugen und Upgrade einer 0.1.16-Datenbank prüfen (nicht committen) | Offen | — |
