# Bestandsaufnahme: Schritt 15 – Lokale Sicherung und Wiederherstellung

Analysiert wurde der Bestand rund um Datenhaltung (SQLite/EF Core, Migrationen), Belegfotos, Optionen-Seite, Testmodus und Tests mit Blick auf `requirement.md` dieses Features.

## Zusammenfassung

- Es gibt **keinerlei** Sicherungs-/Wiederherstellungscode: kein `IBackupService`, keine Sicherungstexte, keine Oberfläche, keine Tests. `System.IO.Compression` wird bisher nicht verwendet; eine neue NuGet-Abhängigkeit ist nicht nötig.
- Alle Nutzerdaten liegen in **einer** SQLite-Datei `tankatlas.db` (`TankradarDbContext.DatabaseFileName`) im Datenverzeichnis (`IAppDataPathProvider.GetDataDirectory`), Belegfotos im Unterverzeichnis `receipts/` (`ReceiptPhotoStore.DirectoryName`, Dateinamen `<Guid N>.<Endung>`). Kartenkacheln liegen im Cache-Verzeichnis (`tiles`), nicht im Datenverzeichnis. Neun Tabellen, sieben Migrationen (letzte: `20261011170920_AddTankbookEntryDetails`).
- Geheimnisse liegen nicht in der Datenbank: `IApiKeyStore` (Credential Locker unter Windows, `SecureStorageApiKeyStore` unter iOS). Ein Anwenderstandort wird nirgends persistiert (Standortdienste liefern ihn nur zur Laufzeit, der Suchcache `FuelPriceService._searchCache` liegt im Speicher); gespeichert sind nur Stationskoordinaten (`StationEntity.Latitude/Longitude`).
- `DatabaseInitializer` migriert beim Start (`MigrateAsync`) und setzt `IDatabaseFileProtector.Protect`; `ProtectFile` (iOS `CompleteUntilFirstUserAuthentication`) nutzt `ReceiptPhotoStore` für jedes Foto. Der Verbindungspool ist in der App eingeschaltet (`BuildConnectionString(pooling: true)`), Tests nutzen `pooling: false`.
- Dienste halten Zustand im Arbeitsspeicher, der nach einem Datenaustausch veraltet wäre: `FuelPriceService` (`_searchCache`, `_refreshed`). Die ViewModels der Seiten laden in `OnAppearing`/`LoadAsync` neu (`FavoritesViewModel`, `TankbookViewModel`, `TankbookEntriesViewModel`, `SettingsViewModel` …). `SettingsService` hält keinen Zwischenspeicher.
- Optionen-Seite (`SettingsPage.xaml`, 217 Zeilen) besteht aus Karten (Spritsorten, Standort, In der Nähe, Ansicht, Sortierung, Datenquelle); `SettingsViewModel` bettet optional `DataSourceViewModel` ein. `DisplayAlert` kommt nirgends vor; Rückfragen sind Inline-Bereiche (Muster: Fahrzeug löschen in `TankbookViewModel`/`VehicleItemViewModel`).
- Testmodus: `TANKATLAS_TEST_DATA_PATH` (isoliertes Datenverzeichnis), Selector-Muster `ReceiptPhotoPickerSelector` → `TestReceiptPhotoPicker` (`TANKATLAS_TEST_RECEIPT_PHOTO`); gleiches Muster für Standort und API-Schlüssel. Für `FilePicker` und `Share` gibt es keine Entsprechung.
- `TankbookTexts.FormatDeleteQuestion` kennt keinen Sicherungshinweis; die Texte prüfen Unit-Tests (`TankbookTextsTests_Labels`, `TankbookViewModelTests_Delete`) und E2E-Tests (`TankbookE2ETests_DeleteWithEntries`, `_Flow`).
- `Platforms/iOS/Info.plist` enthält nur Berechtigungstexte (Kamera, Fotos, Standort); für `FilePicker`/`Share` sind keine Schlüssel vorhanden (Auswahl und Teilen laufen außerhalb des Prozesses). `UIFileSharingEnabled` ist nicht gesetzt.
- Kachelzahlen der Auswertung: `TankbookEntriesPage.xaml` setzt die Zahl in ein `Label` mit Stil `price-hero` (28 pt) in einer `FlexLayout` (`Wrap="Wrap"`) innerhalb `StatisticsTile`; ein `LineBreakMode` ist nicht gesetzt, daher kann die Zahl mitten im Wert umbrechen.
- ADR 0010 enthält die Einträge zu Verbrauchsverlauf/vorletztem Balken nicht; die Begründung „Keine Symbole“ verweist nicht auf ADR 0004.
- Version/Dokumentation: `changes.log` steht bei 0.1.18; Hilfe unter `docs/help/` hat keinen Bereich „Sicherung“; ADRs reichen bis 0010; README nennt Stand Schritt 14.

Test-Ausgangszustand: Unit 1126/1126 und Integration 122/122 grün (0 Fehlschläge, 0 übersprungen). Die FlaUI-E2E-Suite wurde in dieser Bestandsaufnahme nicht ausgeführt (Testlücke, siehe [inventory/tests.md](inventory/tests.md)); letzter dokumentierter Stand aus der Abnahme von Schritt 14: 97/98, einziger Fehlschlag „Screenshot einfarbig“ (Bildschirm aus).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
