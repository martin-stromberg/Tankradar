# Tasks: Schritt 13 – Tank- und Ladevorgänge erfassen

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Modelle| Enums `TankbookEntryKind`, `TankbookConsumptionState`, `TankbookEntryResult`, `TankbookEntryFieldError`, `AmountField`, `ReceiptPhotoError` anlegen | Erledigt | Unit-Tests (Texte/Validator) |
| 2 | Modelle| Records `TankbookEntry`, `TankbookEntryInput`, `TankbookEntryOverview`, `TankbookEntrySaveResult` anlegen | Erledigt | Unit-Tests (Texte/Validator) |
| 3 | Modelle| Records `TankbookEntryFormValidation`, `AmountCompletion`, `KnownStation`, `VehicleDeleteResult`, `ReceiptPhotoImportResult`, Klasse `PickedReceiptPhoto` anlegen | Erledigt | Unit-Tests (Texte/Validator) |
| 4 | Modelle| `TankbookEntryLimits` anlegen | Erledigt | Unit-Tests (Texte/Validator) |
| 5 | Modelle| `VehicleResult` um `EntryCountChanged`, `StartOdometerAboveEntries` erweitern | Erledigt | Unit-Tests (Texte/Validator) |
| 6 | Logik| `DecimalInputParser` anlegen | Erledigt | Unit + Integration grün |
| 7 | Logik| `AmountCalculator` (zwei aus drei, Toleranz 0,02 €) anlegen | Erledigt | Unit + Integration grün |
| 8 | Logik| `TankbookEntryOrder` anlegen | Erledigt | Unit + Integration grün |
| 9 | Logik| `ConsumptionCalculator` (Ketten je Art, Teilbetankungen) anlegen | Erledigt | Unit + Integration grün |
| 10 | Validierung| `TankbookEntryValidator.ValidateForm`, `Validate`, `IsKindAllowed` anlegen | Erledigt | Unit grün |
| 11 | Datenmodell| `TankbookEntryEntity` um neue Eigenschaften und `Apply` erweitern | Erledigt | Integration grün (ModelSync, Upgrade) |
| 12 | Datenmodell| `TankradarDbContext.OnModelCreating` für `TankbookEntries` erweitern (Defaults, TEXT-Dezimale, Index) | Erledigt | Integration grün (ModelSync, Upgrade) |
| 13 | Datenmodell| `TankradarDbContext.BuildConnectionString` mit `Foreign Keys=True` anlegen | Erledigt | Integration grün (ModelSync, Upgrade) |
| 14 | Konfiguration| `MauiProgram` auf `BuildConnectionString` umstellen | Erledigt | Build, Unit grün |
| 15 | Datenmodell| Migration `AddTankbookEntryDetails` per EF-CLI erzeugen und `Up` um Datenübernahme (`OccurredAt`, `OdometerKm`, `Kind`) ergänzen | Erledigt | Integration grün (ModelSync, Upgrade) |
| 16 | Logik| `IDatabaseFileProtector.ProtectFile` + `DatabaseFileProtector` umsetzen | Erledigt | Unit + Integration grün |
| 17 | Logik| `IReceiptPhotoStore`/`ReceiptPhotoStore` anlegen | Erledigt | Unit + Integration grün |
| 18 | Logik| `IReceiptPhotoPicker`, `MauiReceiptPhotoPicker`, `TestReceiptPhotoPicker`, `ReceiptPhotoPickerSelector` anlegen | Erledigt | Unit + Integration grün |
| 19 | Konfiguration| `TestDataPaths.TestReceiptPhotoEnvironmentVariable` ergänzen | Erledigt | Build, Unit grün |
| 20 | Konfiguration| `NSCameraUsageDescription`/`NSPhotoLibraryUsageDescription` in iOS- und MacCatalyst-`Info.plist` | Erledigt | Build, Unit grün |
| 21 | Logik| `ITankbookEntryService` anlegen | Erledigt | Unit + Integration grün |
| 22 | Logik| `TankbookEntryService.GetEntriesAsync` (Verbrauch, neueste zuerst, Bereinigung verwaister Fotos) | Erledigt | Unit + Integration grün |
| 23 | Logik| `TankbookEntryService.CreateEntryAsync`/`UpdateEntryAsync` mit Kilometer- und Artprüfung | Erledigt | Unit + Integration grün |
| 24 | Logik| `TankbookEntryService.DeleteEntryAsync` inkl. Fotodatei | Erledigt | Unit + Integration grün |
| 25 | Logik| `TankbookEntryService.GetKnownStationsAsync` | Erledigt | Unit + Integration grün |
| 26 | Logik| `VehicleService.DeleteVehicleAsync(vehicleId, expectedEntryCount)` atomar mit Fotolöschung | Erledigt | Unit + Integration grün |
| 27 | Validierung| `VehicleService.UpdateVehicleAsync`: `StartOdometerAboveEntries` | Erledigt | Unit grün |
| 28 | UI| `TankbookEntryTexts` anlegen | Erledigt | Unit + FlaUI grün |
| 29 | UI| `TankbookTexts`: Hints, „Tank- und Ladevorgänge“, neue Ergebnistexte; Description-Methoden entfernen | Erledigt | Unit + FlaUI grün |
| 30 | UI| `ITankbookNavigator`/`ShellTankbookNavigator` anlegen, Route in `AppShell` registrieren | Erledigt | Unit + FlaUI grün |
| 31 | UI| Styles nach `Resources/TankbookStyles.xaml` verschieben und in `TankbookPage` einbinden | Erledigt | Unit + FlaUI grün |
| 32 | UI| `VehicleItemViewModel`: `ShownEntryCount`, Hints, `OpenEntriesCommand` | Erledigt | Unit + FlaUI grün |
| 33 | UI| `TankbookViewModel`: Navigation, Neuzählung beim Löschen, `StartOdometerAboveEntries` | Erledigt | Unit + FlaUI grün |
| 34 | UI| `TankbookPage.xaml`: WCAG-Hints für `DeleteConfirm`, `DeleteCancel`, `Form.Cancel`; Schaltfläche „Tank- und Ladevorgänge“ | Erledigt | Unit + FlaUI grün |
| 35 | UI| `KnownStationOptionViewModel` anlegen | Erledigt | Unit + FlaUI grün |
| 36 | UI| `TankbookEntryFormViewModel` anlegen (Felder, Chips, Rechenhinweis, Foto, Speichern/Abbrechen) | Erledigt | Unit + FlaUI grün |
| 37 | UI| `TankbookEntryItemViewModel` anlegen (Anzeige, Rückfrage, Beleg anzeigen) | Erledigt | Unit + FlaUI grün |
| 38 | UI| `TankbookEntriesViewModel` anlegen (`IQueryAttributable`, Laden, Löschen) | Erledigt | Unit + FlaUI grün |
| 39 | UI| `TankbookEntriesPage.xaml` + Code-Behind anlegen | Erledigt | Unit + FlaUI grün |
| 40 | Konfiguration| DI-Registrierungen in `MauiProgram` (Dienste, Picker, Navigator, ViewModels, Seite) | Erledigt | Build, Unit grün |
| 41 | Tests| Unit `DecimalInputParserTests_Values` | Erledigt | grün (Unit 1023, Integration 120) |
| 42 | Tests| Unit `AmountCalculatorTests_Completion`, `AmountCalculatorTests_Plausibility` | Erledigt | grün (Unit 1023, Integration 120) |
| 43 | Tests| Unit `ConsumptionCalculatorTests_FullEntries`, `_Partial`, `_Electric` | Erledigt | grün (Unit 1023, Integration 120) |
| 44 | Tests| Unit `TankbookEntryValidatorTests_Form`, `_Input` | Erledigt | grün (Unit 1023, Integration 120) |
| 45 | Tests| Unit `TankbookEntryServiceTests_Create`, `_Odometer`, `_Read`, `_Change`, `_KnownStations` | Erledigt | grün (Unit 1023, Integration 120) |
| 46 | Tests| Unit `VehicleServiceTests_Delete`/`_Manage` erweitern und an neue Signatur anpassen; `VehicleFixture` | Erledigt | grün (Unit 1023, Integration 120) |
| 47 | Tests| Unit `ReceiptPhotoStoreTests_Files`, `ReceiptPhotoPickerSelectorTests_TestMode` | Erledigt | grün (Unit 1023, Integration 120) |
| 48 | Tests| Unit `TankradarDbContextTests_ConnectionString`, `PlatformManifestTests_Photos` | Erledigt | grün (Unit 1023, Integration 120) |
| 49 | Tests| Unit `TankbookViewModelTests_*` anpassen und erweitern (Neuzählung, WCAG, Navigation) | Erledigt | grün (Unit 1023, Integration 120) |
| 50 | Tests| Unit `TankbookEntriesViewModelTests_*`, `TankbookEntryFormViewModelTests_*` | Erledigt | grün (Unit 1023, Integration 120) |
| 51 | Tests| Unit `TankbookEntryTextsTests_*`, `TankbookTextsTests_*` anpassen | Erledigt | grün (Unit 1023, Integration 120) |
| 52 | Tests| Integration `TankbookEntriesMigrationTests_Upgrade` | Erledigt | grün (Unit 1023, Integration 120) |
| 53 | Tests| Integration `TankbookEntryServiceTests_RealDatabase` | Erledigt | grün (Unit 1023, Integration 120) |
| 54 | Tests| Integration `ForeignKeysTests_Connection`; `TestDatabase` auf `BuildConnectionString` | Erledigt | grün (Unit 1023, Integration 120) |
| 55 | Tests| Integration `DatabaseInitializerTests_*` auf 7 Migrationen, `VehicleServiceTests_RealDatabase` anpassen | Erledigt | grün (Unit 1023, Integration 120) |
| 56 | E2E-Tests| `TankbookE2EDriver` herauslösen, `TankbookE2ETestBase` delegieren lassen | Erledigt | FlaUI grün (90/90) |
| 57 | E2E-Tests| `TankbookTestDatabase` um vollständige Vorgänge, Abfragen und Fotodateien erweitern | Erledigt | FlaUI grün (90/90) |
| 58 | E2E-Tests| `TankbookEntriesE2ETests_Flow` (Erfassen + Verbrauch) | Erledigt | FlaUI grün (90/90) |
| 59 | E2E-Tests| `TankbookEntriesE2ETests_Partial` | Erledigt | FlaUI grün (90/90) |
| 60 | E2E-Tests| `TankbookEntriesE2ETests_Electric` | Erledigt | FlaUI grün (90/90) |
| 61 | E2E-Tests| `TankbookEntriesE2ETests_Validation` | Erledigt | FlaUI grün (90/90) |
| 62 | E2E-Tests| `TankbookEntriesE2ETests_EditDelete` | Erledigt | FlaUI grün (90/90) |
| 63 | E2E-Tests| `TankbookEntriesE2ETests_Receipt` | Erledigt | FlaUI grün (90/90) |
| 64 | E2E-Tests| `OfflineE2ETests_Tankbook` | Erledigt | FlaUI grün (90/90) |
| 65 | E2E-Tests| `TankbookE2ETests_DeleteWithEntries.EntryAddedBeforeConfirming_IsNamedBeforeDeleting` | Erledigt | FlaUI grün (90/90) |
| 66 | E2E-Tests| `TankbookEntriesE2ETests_Persistence` | Erledigt | FlaUI grün (90/90) |
| 67 | E2E-Tests| `TankbookE2ETests_Validation`: Start-Kilometerstand über erstem Vorgang | Erledigt | FlaUI grün (90/90) |
| 68 | E2E-Tests| `TankbookE2ETests_Flow`, `TankbookE2ETests_DeleteWithEntries` an WCAG-Namen anpassen | Erledigt | FlaUI grün (90/90) |
| 69 | Dokumentation| `docs/adr/0009-tankbook-entries.md` anlegen; ADR 0008 „Folgen“ verweisen | Erledigt | erstellt |
| 70 | Dokumentation| `docs/help/Tankbuch/` (index, beschreibung, ablauf-anwender, ablauf-technisch) aktualisieren | Erledigt | erstellt |
| 71 | Dokumentation| `changes.log`-Eintrag Version 0.1.17 | Erledigt | erstellt |
| 72 | Auslieferung | `local-ci.ps1` und Pre-Push-Prüfungen grün | Offen | — |
| 73 | Auslieferung | Zwischenstand `review-versions/0.1.17_<Datum>/` erzeugen und Upgrade einer 0.1.16-Datenbank prüfen (nicht committen) | Offen | — |
