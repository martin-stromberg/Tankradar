# Umsetzungsplan: Schritt 13 – Tank- und Ladevorgänge erfassen

## Übersicht

Die minimale Tabelle `TankbookEntries` aus Schritt 12 wird per additiver Migration zum vollständigen Tank-/Ladevorgang
ausgebaut. Eine neue Unterseite des Tankbuchs zeigt je Fahrzeug die Vorgänge (neueste zuerst) mit Verbrauch pro 100 km und
erlaubt Erfassen, Korrigieren und Löschen samt optionalem Belegfoto; Menge/Preis/Gesamtbetrag ergänzen sich gegenseitig und
werden auf Plausibilität geprüft, der Kilometerstand auf Reihenfolge. Dazu kommen die drei Restpunkte aus der Abnahme von
Schritt 12 (atomare Neuzählung beim Löschen eines Fahrzeugs, `Foreign Keys=True`, WCAG 2.5.3) und ein FlaUI-Offline-Szenario.
Zwischenstand: Version **0.1.17**.

Grundlagen: [requirement.md](requirement.md), [inventory.md](inventory.md).

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Ort der Vorgänge in der UI | Eigene Seite `TankbookEntriesPage` je Fahrzeug (Shell-Route `tankbookentries`, Parameter `vehicleId`), geöffnet über die neue Schaltfläche „Tank- und Ladevorgänge“ auf der Fahrzeugkarte. Formular und Löschrückfrage sind – wie in Schritt 12 / ADR 0006 / ADR 0008 – Bereiche in der Seite, keine Systemdialoge. | Das Fahrzeug wird per Namen (Karte) gewählt, nie per Kennung. `TankbookViewModel` hat bereits 555 Zeilen; eine eigene Seite hält die Klassen klein. Navigationsmuster wie `ShellFavoriteGroupNavigator` (`IQueryAttributable`). Die Karte „Aktives Fahrzeug“ des Entwurfs mit Auswahlliste ist damit funktional ersetzt (Abweichung in ADR 0009). |
| Reihenfolge „chronologisch“ | Absteigend nach Zeitpunkt (neueste zuerst); Ordnungsschlüssel (`OccurredAt`, `OdometerKm`, `Id`). Die Berechnungen laufen intern aufsteigend. | Der verbindliche Designentwurf zeigt „Letzte Tankvorgänge“ neueste zuerst; der Ordnungsschlüssel ist eindeutig, auch bei gleicher Minute. |
| Datum/Uhrzeit | Eingabe über MAUI `DatePicker` + `TimePicker` (vorbelegt mit jetzt, minutengenau, aus `TimeProvider.GetLocalNow()`); gespeichert als lokale Wanduhrzeit (`DateTime`, Kind `Unspecified`, `TEXT`), Sekunden = 0. Zeitpunkte in der Zukunft (nach jetzt + 1 Minute) sind unzulässig. | Der Nutzer gibt die Uhrzeit vom Beleg ein; Zeitzonenumrechnung würde den Belegwert verfälschen. Native Picker sind auf iOS die erwartete Bedienung; E2E nutzt die Vorbelegung. |
| Werte als Value Objects | `decimal` für Menge, Preis pro Einheit, Gesamtbetrag (Spalten `TEXT`, wie `StartConsumption`), `int` für Kilometerstand. | Exakte Speicherung ohne Rundungsfehler, gleiches Muster wie Schritt 12. |
| Plausibilität Menge/Preis/Gesamt (Rechenbaustein `AmountCalculator`, Transaction Script/reine Funktion) | Genau zwei Werte → dritter wird berechnet (Gesamt = Menge × Preis, 2 Nachkommastellen; Preis = Gesamt / Menge, 3 NKS; Menge = Gesamt / Preis, 2 NKS; jeweils kaufmännisch gerundet `MidpointRounding.AwayFromZero`), live als Hinweis „Ergibt …“ angezeigt und beim Speichern ins leere Feld übernommen. Alle drei angegeben und \|Menge × Preis − Gesamt\| > 0,02 € → Feldfehler am Gesamtbetrag mit dem rechnerischen Betrag; nicht gespeichert. Weniger als zwei Werte → Fehler „mindestens zwei der drei Werte“. | „Abweichungen werden gemeldet“ – eine blockierende Meldung verhindert stille Tippfehler, die Verbrauch und Kosten (Schritt 14) verfälschen. 0,02 € deckt die Rundung von Belegen (Menge 2 NKS × Preis 3 NKS, Gesamt auf Cent) ab. Belege mit Zusatzgebühren (z. B. Ladesitzung) erfasst der Nutzer mit Menge + Gesamtbetrag; der Preis pro Einheit wird dann berechnet. |
| Verbrauch (`ConsumptionCalculator`, reine Funktion) | Je Art (Kraftstoff bzw. Strom) eine eigene Kette in aufsteigender Ordnung. Vollvorgang V mit vorherigem Vollvorgang P derselben Art: Verbrauch = (Menge(V) + Summe Mengen der Teilbetankungen derselben Art zwischen P und V) / (km(V) − km(P)) × 100, auf 2 NKS gerundet. Kein vorheriger Vollvorgang → kein Wert („erster voller Vorgang“); Strecke ≤ 0 → kein Wert; Teilbetankung → kein eigener Wert. Teilbetankungen vor dem ersten Vollvorgang fließen nicht ein (keine Ausgangsbasis). Ergebnis wird nicht gespeichert, sondern beim Lesen berechnet. | Formel der Anforderung. Getrennte Ketten machen Hybridfahrzeuge (L und kWh) berechenbar, ohne Einheiten zu mischen. Berechnung beim Lesen hält Korrekturen und Löschungen automatisch konsistent. |
| Art des Vorgangs je Fahrzeug | Neues Enum `TankbookEntryKind` (`Fuel`, `Electricity`). Benzin/Diesel → nur Kraftstoff, Elektro → nur Strom (Art wird angezeigt, nicht wählbar), Hybrid → Chips „Kraftstoff“/„Strom“ (Vorauswahl Kraftstoff). Beim Korrigieren bleibt eine gespeicherte Art erlaubt, auch wenn sie nach einem Wechsel der Kraftstoffart des Fahrzeugs nicht mehr passt. Ein Wechsel der Kraftstoffart eines Fahrzeugs mit Vorgängen bleibt erlaubt, vorhandene Vorgänge behalten Art und Einheit. | Beantwortet die in ADR 0008 „Folgen“ offene Frage zum Wechsel der Kraftstoffart; Plug-in-Hybride brauchen beide Arten. |
| Kilometerstand | Muss ≥ Start-Kilometerstand des Fahrzeugs und ≥ dem des chronologischen Vorgängers (über alle Arten) sein; zusätzlich ≤ dem des chronologischen Nachfolgers, damit ein nachgetragener oder korrigierter Vorgang den nächsten nicht ungültig macht. Gleichstand ist erlaubt („nicht unter“). Zusätzlich lehnt `UpdateVehicleAsync` einen Start-Kilometerstand über dem niedrigsten Vorgang ab (neues `VehicleResult.StartOdometerAboveEntries`). | Anforderung plus Folgepunkt aus ADR 0008. Die Prüfung im Dienst (nicht nur im ViewModel) schützt gegen veraltete Formularstände. |
| Tankstelle (Auswahl benannter Entitäten) | Pflichtfeld „Tankstelle oder Ladeort“ (1–60 Zeichen) mit Freitext-`Entry` und darunter einer Liste „Bekannte Tankstellen“ als Klartext-Chips (Name; bei Namensgleichheit zusätzlich der Ort). Quelle: alle Favoriten-Tankstellen (`FavoriteEntries` ⨝ `Stations`, nach Namen de-DE) und danach bereits verwendete, frei eingegebene Namen (ohne Duplikate, ohne Groß-/Kleinschreibung), höchstens 30 Einträge. Gespeichert werden der Name (Momentaufnahme) und – nur bei Auswahl einer Favoriten-Tankstelle – deren Kennung `StationId` **ohne Fremdschlüssel**. Ändert der Nutzer den Text danach, entfällt die Kennung. Kennungen werden nie angezeigt oder eingegeben. | Anforderung „Auswahl aus einer Liste bekannter Tankstellen (z. B. aus den Favoriten) oder manuelle Eingabe“. Ohne Fremdschlüssel bleibt der Vorgang unabhängig vom Preis-Cache und von der Sicherung (Schritt 15). Chip-Muster wie die Kraftstoffart (`FuelKindChip`, „Ausgewählt“ im Hint). |
| Bekannte Tankstellen lesen | Eigene Abfrage im neuen `TankbookEntryService.GetKnownStationsAsync` direkt über den `TankradarDbContext` (kein Umweg über `IFavoritesService`, das nur gruppenweise liest). | Eine Abfrage statt N Gruppenaufrufen; nur lokale Daten, kein Netz. |
| Belegfoto | `IReceiptPhotoPicker` (Gateway): Plattform `MauiReceiptPhotoPicker` über `MediaPicker.Default` (Auswahl aus der Fotomediathek; „Foto aufnehmen“ nur, wenn `IsCaptureSupported`); im Testmodus `TestReceiptPhotoPicker`, der die Datei aus `TANKATLAS_TEST_RECEIPT_PHOTO` liefert (Auswahl per `ReceiptPhotoPickerSelector` wie `LocationServiceSelector`). Ablage `IReceiptPhotoStore`/`ReceiptPhotoStore`: Kopie nach `<Datenverzeichnis>/receipts/<Guid>.<ext>`, gespeichert wird nur der Dateiname. Dateischutz über `IDatabaseFileProtector.ProtectFile` (iOS `CompleteUntilFirstUserAuthentication`, wie die Datenbank). Erlaubt: `.jpg`, `.jpeg`, `.png`, `.heic`, `.heif`, höchstens 20 MB. Die Fotos bleiben unverändert (keine Neukodierung). | Entscheidung „Datenbank und Belegfotos über systemseitigen Dateischutz“ der Projekttabelle. Dateiname statt Pfad macht die Ablage portabel (Sicherung in Schritt 15, Test-Datenverzeichnis). Der Test-Picker macht den Ablauf ohne Systemdialog per FlaUI prüfbar. Falls `PickPhotoAsync` in der verwendeten MAUI-Version als veraltet markiert ist (Build mit Warnungen als Fehler), wird `PickPhotosAsync` mit `SelectionLimit = 1` verwendet. |
| Lebenszyklus der Fotodateien | Auswahl kopiert sofort in die Ablage („ausstehend“). Abbrechen oder Ersetzen vor dem Speichern löscht die ausstehende Datei; nach erfolgreichem Speichern löscht der Dienst die vorher referenzierte Datei. Löschen eines Vorgangs bzw. eines Fahrzeugs löscht die Dateien nach dem Commit (Fehler nur mit Ausnahmetyp protokolliert). Beim ersten Laden der Vorgänge je Prozess entfernt `ReceiptPhotoStore.DeleteUnreferenced` Dateien, die kein Vorgang referenziert (ohne Datei-Zeitstempel, daher keine neue Angabe im iOS-Privacy-Manifest). | Keine verwaisten Belegfotos auf dem Gerät (Datenschutz), auch nach Absturz zwischen Auswahl und Speichern. |
| Dienstschicht | Neuer `ITankbookEntryService`/`TankbookEntryService` (Service Layer über `IDbContextFactory`, Ergebnis-Enums statt Ausnahmen, Protokolle nur mit Ausnahmetyp) nach dem Muster von `VehicleService`. Die Übernahme der Eingabe in die Entität erfolgt per Load-and-Patch über `TankbookEntryEntity.Apply(TankbookEntryInput)` (Skill `entityframework-database`, Option A). | Bestehendes Muster; die Patch-Logik liegt an der Entität. |
| ViewModels | `TankbookEntriesViewModel` (Seite, Liste, Löschrückfrage, Navigation), `TankbookEntryFormViewModel` (Formularzustand, Feldfehler, Rechenhinweis, Foto), `TankbookEntryItemViewModel` (Listeneintrag), `KnownStationOptionViewModel` (Chip). | Kleine, thematisch getrennte Klassen statt eines großen ViewModels. |
| Texte | Neue zentrale Textklasse `TankbookEntryTexts` (Oberfläche duzt, wie `TankbookTexts`). | `TankbookTexts` hat bereits 360 Zeilen; zentrale Pflege für die Übersetzungsprüfung. |
| Neuzählung beim Löschen eines Fahrzeugs (Zusatz 12a) | `IVehicleService.DeleteVehicleAsync(vehicleId, expectedEntryCount)` zählt in derselben Transaktion; weicht die Zahl ab, wird **nicht** gelöscht, sondern `VehicleResult.EntryCountChanged` mit der aktuellen Zahl gemeldet. Das ViewModel aktualisiert dann Rückfrage und Beschriftung, sagt die neue Frage an und wartet auf eine erneute Bestätigung. | Zählen und Löschen atomar; der neue Vorgang wird in der Rückfrage genannt, bevor er mitgelöscht wird. |
| Fremdschlüssel (Zusatz 12b) | Neue statische Methode `TankradarDbContext.BuildConnectionString(databasePath, pooling = true)` (`SqliteConnectionStringBuilder` mit `ForeignKeys = true`), genutzt von `MauiProgram` und der Integrations-`TestDatabase`. | Eine Quelle für die Verbindungszeichenfolge; Tests prüfen genau die Einstellung der App. |
| WCAG 2.5.3 (Zusatz 12c) und neue Schaltflächen | Muster: vorgelesener Name = sichtbarer Text (kein `SemanticProperties.Description` an Schaltflächen mit Text), Kontext ausschließlich in `SemanticProperties.Hint`. Umgestellt: `Tankbook.Vehicle.DeleteConfirm` (Hint z. B. „Fahrzeug „Golf“ und 2 Tank- und Ladevorgänge endgültig löschen“), `Tankbook.Vehicle.DeleteCancel` (Hint „Fahrzeug „Golf“ behalten“), `Tankbook.Form.Cancel` (Hint „Formular schließen, ohne zu speichern“). Alle neuen Schaltflächen dieses Schritts folgen demselben Muster. `Tankbook.Vehicle.Edit`/`.Delete` bleiben unverändert (ihr Name enthält den sichtbaren Text bereits; nicht Teil des Zusatzes). | Vorschlag „Alternativ den Kontext in `SemanticProperties.Hint` legen“ aus `acceptance-schritt-12.md`, wie im Zusatz verlangt. Chips behalten „Ausgewählt“ im Hint. |
| E2E-Hilfen | Die Tankbuch-Hilfen aus `TankbookE2ETestBase` wandern in eine eigene Klasse `TankbookE2EDriver` (Komposition, arbeitet auf dem `Window` der laufenden App); `TankbookE2ETestBase` delegiert, und das Offline-Szenario (abgeleitet von `HomeE2ETestBase` wegen Mock-Preisdienst und Favoriten) nutzt denselben Treiber. | C# kennt keine Mehrfachvererbung; so entsteht keine Kopie der Hilfen, und die bisherigen Tankbuch-E2E-Tests laufen weiter ohne Mock-Server. |

## Programmabläufe

### Vorgänge eines Fahrzeugs öffnen

1. Auf der Fahrzeugkarte (`TankbookPage`) tippt der Nutzer „Tank- und Ladevorgänge“ (`Tankbook.Vehicle.Entries`, Hint „Fahrzeug „Golf““).
2. `VehicleItemViewModel.OpenEntriesCommand` → `TankbookViewModel` ruft `ITankbookNavigator.OpenEntriesAsync(vehicleId)`.
3. `ShellTankbookNavigator` navigiert zur Route `tankbookentries` mit Parameter `vehicleId`.
4. `TankbookEntriesViewModel.ApplyQueryAttributes` übernimmt die Kennung; `OnAppearing` → `LoadAsync`:
   `IVehicleService.GetVehicleAsync` (fehlt es → Hinweis „Das Fahrzeug gibt es nicht mehr.“ und `CloseAsync`), dann
   `ITankbookEntryService.GetEntriesAsync(vehicleId)` und `GetKnownStationsAsync()`.
5. `TankbookEntryService.GetEntriesAsync`: beim ersten Aufruf je Prozess `ReceiptPhotoStore.DeleteUnreferenced(alle referenzierten Dateinamen)`;
   lädt die Vorgänge des Fahrzeugs, ordnet aufsteigend, `ConsumptionCalculator.Calculate` → `TankbookEntryOverview`-Liste, gibt sie
   absteigend zurück.
6. Das ViewModel baut je Eintrag ein `TankbookEntryItemViewModel` (Texte aus `TankbookEntryTexts`); leere Liste → Hinweis.

Beteiligte: `TankbookPage`, `VehicleItemViewModel`, `TankbookViewModel`, `ITankbookNavigator`, `ShellTankbookNavigator`, `AppShell`, `TankbookEntriesViewModel`, `ITankbookEntryService`, `ConsumptionCalculator`, `IReceiptPhotoStore`.

### Vorgang erfassen

1. „Vorgang erfassen“ (`TankbookEntries.Add`) → `TankbookEntryFormViewModel.BeginCreate(vehicle, lastOdometer)`: Datum/Uhrzeit = jetzt
   (minutengenau), Art nach Fahrzeug, Teilbetankung aus, übrige Felder leer; Platzhalter des Kilometerstands nennt den letzten Stand.
2. Nutzer wählt eine bekannte Tankstelle (Chip füllt Namen + `StationId`) oder tippt einen Namen; gibt zwei oder drei der Werte Menge,
   Preis pro Einheit, Gesamtbetrag ein; bei jeder Änderung ruft das Formular `AmountCalculator.Complete` und zeigt
   `ComputedHint` (z. B. „Ergibt 74,36 €“) bzw. nichts.
3. Optional „Foto auswählen“/„Foto aufnehmen“: `IReceiptPhotoPicker` → Stream + Endung → `IReceiptPhotoStore.ImportAsync` →
   Dateiname als ausstehendes Foto, Vorschau (`Image`), „Foto entfernen“ möglich.
4. „Speichern“: `TankbookEntryValidator.ValidateForm(...)` parst und prüft alle Felder gleichzeitig (inkl. `AmountCalculator`);
   Fehler → Feldfehler, roter Rahmen, Ansage des ersten Fehlers über `IScreenReaderAnnouncer`, nichts gespeichert.
5. Gültig → das berechnete dritte Feld wird ins Formular übernommen; `ITankbookEntryService.CreateEntryAsync(vehicleId, input)`:
   `TankbookEntryValidator.Validate` (Schutzprüfung), Fahrzeug laden (fehlt → `VehicleNotFound`), Art erlaubt?, alle Vorgänge des
   Fahrzeugs laden, Vorgänger/Nachfolger nach Ordnungsschlüssel bestimmen, Kilometerstand prüfen (→ `OdometerBelowVehicleStart`,
   `OdometerBelowPrevious`, `OdometerAboveNext` mit Grenzwert in `TankbookEntrySaveResult.OdometerBoundKm`), neue Entität per
   `Apply`, `CreatedUtc` aus `TimeProvider`, `SaveChangesAsync`.
6. `Ok` → Formular schließt (ausstehendes Foto gilt als übernommen), Liste lädt neu, Verbrauch erscheint. Kilometerfehler → Feldfehler
   am Kilometerstand mit Grenzwert („Der Kilometerstand darf nicht unter 10.500 km (vorheriger Vorgang) liegen.“) und Ansage.
   Speicherfehler → Statusmeldung, Formular bleibt offen.

### Vorgang korrigieren

1. „Bearbeiten“ am Eintrag (Hint „Vorgang vom 18.05.2026, 14:30, Alpha Tankstelle“) → `BeginEdit(entry)`: Formular mit gespeicherten Werten
   (Dezimalzahlen deutsch formatiert), vorhandenes Foto als Vorschau.
2. Speichern wie oben über `UpdateEntryAsync(entryId, input)`: Load-and-Patch; die Kilometerprüfung schließt den Vorgang selbst aus; die
   gespeicherte Art bleibt erlaubt; ändert sich der Fotodateiname, wird die alte Datei nach dem Commit gelöscht.
3. Liste lädt neu; abhängige Verbrauchswerte (z. B. des nächsten Vollvorgangs) ändern sich automatisch.

### Vorgang löschen

1. „Löschen“ am Eintrag → Rückfrage im Eintrag: „Vorgang vom 18.05.2026, 14:30 (Alpha Tankstelle) löschen? Das lässt sich nicht
   rückgängig machen.“ (Ansage). Schaltflächen „Ja, löschen“ (Hint „Vorgang vom … endgültig löschen“) und „Abbrechen“ (Hint „Vorgang behalten“).
2. Bestätigen → `DeleteEntryAsync(entryId)` löscht die Zeile, danach die Fotodatei; `EntryNotFound` gilt als erledigt; Liste lädt neu.

### Bekannte Tankstellen ermitteln

1. `GetKnownStationsAsync`: Favoriten-Tankstellen = `FavoriteEntries` ⨝ `Stations` (eindeutig je `StationId`, mit `Name`, `Place`),
   nach Namen (de-DE, ohne Groß-/Kleinschreibung).
2. Danach eindeutige `StationName`-Werte aus `TankbookEntries` ohne `StationId`, die nicht schon als Name vorkommen, ebenfalls sortiert.
3. Kürzen auf `TankbookEntryLimits.MaxKnownStations` (30); `KnownStation(StationId?, Name, Place?)`. Die Beschriftung der Chips ergänzt
   den Ort nur bei mehrfach vorkommenden Namen.

### Fahrzeug löschen mit Neuzählung (Zusatz 12a)

1. „Löschen“ → `CountEntriesAsync` → `VehicleItemViewModel.ShowDeleteConfirmation(count)` merkt sich `ShownEntryCount`.
2. Bestätigen → `TankbookViewModel.DeleteAsync` ruft `DeleteVehicleAsync(id, item.ShownEntryCount)`.
3. `VehicleService`: Transaktion; Fahrzeug fehlt → `VehicleNotFound`; Zahl der Vorgänge neu zählen; abweichend → Rollback,
   `VehicleDeleteResult(EntryCountChanged, aktuelleZahl)`; sonst Fotodateinamen der Vorgänge lesen, Fahrzeug entfernen (Kaskade), Commit,
   danach `IReceiptPhotoStore.Delete` je Datei.
4. `EntryCountChanged` → `item.ShowDeleteConfirmation(aktuelleZahl)` (Frage nennt nun z. B. 3 statt 2 Vorgänge, Beschriftung ggf. von
   „Ja, löschen“ auf „Fahrzeug und Vorgänge löschen“), Ansage der neuen Frage; erst eine weitere Bestätigung löscht.

### Start-Kilometerstand eines Fahrzeugs ändern

1. `UpdateVehicleAsync` prüft zusätzlich: gibt es Vorgänge und liegt der neue Start-Kilometerstand über dem niedrigsten
   `OdometerKm` → `VehicleResult.StartOdometerAboveEntries`.
2. `TankbookViewModel.SaveAsync` zeigt den Text als Feldfehler am Start-Kilometerstand (Ansage), nichts gespeichert.

### Belegfoto anzeigen

1. Eintrag mit Foto zeigt „Beleg anzeigen“ (`TankbookEntry.Item.ShowReceipt`, Hint mit Vorgangsdatum); Antippen blendet das Foto im
   Eintrag ein bzw. aus („Beleg ausblenden“). Quelle: `IReceiptPhotoStore.GetFullPath(fileName)`; fehlt die Datei → Hinweis „Das Belegfoto
   ist nicht mehr vorhanden.“

### Start der Datenbankverbindung (Zusatz 12b)

1. `MauiProgram` → `options.UseSqlite(TankradarDbContext.BuildConnectionString(path))` → `Microsoft.Data.Sqlite` führt beim Öffnen
   `PRAGMA foreign_keys = ON` aus; die Kaskaden hängen nicht mehr vom Standard von `e_sqlite3` ab.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `Models/Tankbook/TankbookEntryKind` | Enum | `Fuel`, `Electricity`; als Name gespeichert |
| `Models/Tankbook/TankbookEntry` | Record | gespeicherter Vorgang (`Id`, `VehicleId`, `OccurredAt`, `Kind`, `Quantity`, `UnitPrice`, `TotalAmount`, `OdometerKm`, `IsPartial`, `StationName`, `StationId?`, `Note?`, `ReceiptPhotoFileName?`) |
| `Models/Tankbook/TankbookEntryInput` | Record | Eingabe für Anlegen/Ändern (Felder wie oben ohne `Id`/`VehicleId`) |
| `Models/Tankbook/TankbookEntryOverview` | Record | `Entry`, `DistanceKm?`, `Consumption?`, `ConsumptionState` |
| `Models/Tankbook/TankbookConsumptionState` | Enum | `Calculated`, `Partial`, `NoPreviousFullEntry`, `NoDistance` |
| `Models/Tankbook/TankbookEntryResult` | Enum | `Ok`, `VehicleNotFound`, `EntryNotFound`, `InvalidOccurredAt`, `KindNotAllowed`, `InvalidQuantity`, `InvalidUnitPrice`, `InvalidTotalAmount`, `AmountMismatch`, `InvalidOdometer`, `OdometerBelowVehicleStart`, `OdometerBelowPrevious`, `OdometerAboveNext`, `InvalidStationName`, `NoteTooLong` |
| `Models/Tankbook/TankbookEntrySaveResult` | Record | `Result`, `Entry?`, `OdometerBoundKm?` |
| `Models/Tankbook/TankbookEntryFieldError` | Enum | `None`, `Required`, `NotANumber`, `Negative`, `OutOfRange`, `TooManyDecimals`, `TooLong`, `MissingAmounts`, `AmountMismatch`, `InFuture` |
| `Models/Tankbook/TankbookEntryFormValidation` | Record | Feldfehler je Feld (Datum, Station, Menge, Preis, Gesamt, Kilometerstand, Notiz), `ExpectedTotal?`, `Input?`, `HasErrors` (Form `get { return …; }`) |
| `Models/Tankbook/TankbookEntryLimits` | statische Klasse | Grenzen (siehe Validierungsregeln), `AmountTolerance = 0.02m`, `MaxKnownStations = 30`, `MaxReceiptPhotoBytes = 20 MB`, erlaubte Endungen |
| `Models/Tankbook/AmountCompletion` | Record | Ergebnis von `AmountCalculator.Complete`: `Quantity?`, `UnitPrice?`, `TotalAmount?`, `ComputedField` (`AmountField?`), `HasMismatch`, `ExpectedTotal?`, `IsMissing` |
| `Models/Tankbook/AmountField` | Enum | `Quantity`, `UnitPrice`, `TotalAmount` |
| `Models/Tankbook/KnownStation` | Record | `StationId?`, `Name`, `Place?` |
| `Models/Tankbook/VehicleDeleteResult` | Record | `Result`, `EntryCount` |
| `Services/Tankbook/DecimalInputParser` | statische Klasse | Dezimaleingabe mit Komma oder einzelnem Punkt, max. Nachkommastellen, Bereich; liefert `TankbookEntryFieldError` (Muster aus `VehicleInputParser.ParseConsumption`) |
| `Services/Tankbook/AmountCalculator` | statische Klasse | zwei aus drei, Abweichungsprüfung mit Toleranz |
| `Services/Tankbook/ConsumptionCalculator` | statische Klasse | Verbrauch je Vollvorgang, Strecke, Zustand |
| `Services/Tankbook/TankbookEntryOrder` | statische Klasse | Ordnungsschlüssel (`OccurredAt`, `OdometerKm`, `Id`) als `IComparer<TankbookEntry>` |
| `Services/Tankbook/TankbookEntryValidator` | statische Klasse | `ValidateForm(...)` (alle Feldfehler gleichzeitig) und `Validate(TankbookEntryInput, DateTime now)` (Schutzprüfung im Dienst), `IsKindAllowed(VehicleFuelKind, TankbookEntryKind)` |
| `Services/Tankbook/ITankbookEntryService` | Interface | `GetEntriesAsync`, `GetEntryAsync`, `CreateEntryAsync`, `UpdateEntryAsync`, `DeleteEntryAsync`, `GetKnownStationsAsync` |
| `Services/Tankbook/TankbookEntryService` | Klasse | EF-Umsetzung (Singleton) |
| `Services/Tankbook/IReceiptPhotoStore` | Interface | `ImportAsync(Stream, string extension, CancellationToken)` → `ReceiptPhotoImportResult`, `GetFullPath(fileName)`, `Exists(fileName)`, `Delete(fileName)`, `DeleteUnreferenced(IReadOnlySet<string>)` |
| `Services/Tankbook/ReceiptPhotoStore` | Klasse | Ablage unter `<IAppDataPathProvider.GetDataDirectory()>/receipts`, Prüfung von Endung/Größe, Dateischutz über `IDatabaseFileProtector.ProtectFile`; nur Dateinamen im Format `<Guid>.<ext>` werden akzeptiert (kein Pfad-Traversal) |
| `Models/Tankbook/ReceiptPhotoImportResult` | Record | `FileName?`, `Error` (`ReceiptPhotoError`) |
| `Models/Tankbook/ReceiptPhotoError` | Enum | `None`, `UnsupportedFormat`, `TooLarge`, `Failed` |
| `Services/Tankbook/IReceiptPhotoPicker` | Interface | `IsCaptureSupported`, `PickAsync`, `CaptureAsync` → `PickedReceiptPhoto?` (öffnet Stream, Endung) |
| `Models/Tankbook/PickedReceiptPhoto` | Klasse | `Extension`, `OpenReadAsync()` |
| `Services/Tankbook/MauiReceiptPhotoPicker` | Klasse | `MediaPicker.Default`; Abbruch durch den Nutzer → `null` |
| `Services/Tankbook/TestReceiptPhotoPicker` | Klasse | liefert die Datei aus `TANKATLAS_TEST_RECEIPT_PHOTO` (keine Datei → `null`); `IsCaptureSupported = false` |
| `Services/Tankbook/ReceiptPhotoPickerSelector` | statische Klasse | Testmodus (`TANKATLAS_TEST_DATA_PATH` gesetzt) → `TestReceiptPhotoPicker`, sonst Plattform |
| `Services/Navigation/ITankbookNavigator` | Interface | `OpenEntriesAsync(long vehicleId)`, `CloseAsync()` |
| `Services/Navigation/ShellTankbookNavigator` | Klasse | Route `tankbookentries`, Parameter `vehicleId` |
| `ViewModels/TankbookEntriesViewModel` | Klasse (`IQueryAttributable`) | Seite: Fahrzeugkopf, Liste, Leerhinweis, Status, Löschrückfrage, Formular-Host |
| `ViewModels/TankbookEntryFormViewModel` | Klasse | Formularfelder, Art-Chips (`ChoiceOptionViewModel<TankbookEntryKind>`), Tankstellen-Chips, Feldfehler, `ComputedHint`, Foto (ausstehend/vorhanden), `SaveCommand`, `CancelCommand` |
| `ViewModels/TankbookEntryItemViewModel` | Klasse | Anzeige eines Vorgangs, Bearbeiten/Löschen/Rückfrage/Beleg anzeigen, Hints |
| `ViewModels/KnownStationOptionViewModel` | Klasse | Chip einer bekannten Tankstelle (`Label`, `IsSelected`, `SelectionHint`, `SelectCommand`) |
| `Views/TankbookEntriesPage` (`.xaml` + `.xaml.cs`) | View | Seite; übernimmt die Styles aus `TankbookPage` (siehe unten) |
| `Resources/Texts/TankbookEntryTexts` | statische Klasse | alle Texte und Formatierungen (deutsch, `de-DE`) |
| `Resources/TankbookStyles.xaml` | ResourceDictionary | die bisher in `TankbookPage.Resources` definierten Styles (`TankbookCard`, `TankbookButton`, `TankbookOutlineButton`, `TankbookDangerButton`, `FuelKindChip`, `UnitFieldBorder`, `FieldLabel`, `FieldError`, `UnitLabel`), in beiden Seiten eingebunden – keine Kopie |

## Änderungen an bestehenden Klassen

### `TankbookEntryEntity` (Datenmodellklasse)

- **Neue Eigenschaften:** `OccurredAt` (`DateTime`), `Kind` (`string`, Name von `TankbookEntryKind`), `Quantity`, `UnitPrice`, `TotalAmount` (`decimal`), `OdometerKm` (`int`), `IsPartial` (`bool`), `StationName` (`string`), `StationId` (`string?`), `Note` (`string?`), `ReceiptPhotoFileName` (`string?`).
- **Neue Methoden:** `Apply(TankbookEntryInput input)` – überträgt alle Eingabewerte (Load-and-Patch).
- XML-Doku: Hinweis auf „minimal in Schritt 12“ entfernen.

### `TankradarDbContext` (DbContext)

- **Neue Methoden:** `static string BuildConnectionString(string databasePath, bool pooling = true)` – `Data Source=…;Foreign Keys=True` (+ `Pooling=False` bei Bedarf).
- **Geänderte Methoden:** `OnModelCreating` für `TankbookEntryEntity`: `Kind` Pflicht mit Standardwert `"Fuel"`; `Quantity`, `UnitPrice`, `TotalAmount` `HasColumnType("TEXT")` mit Standardwert `0m`; `StationName` Pflicht mit Standardwert `""`; `IsPartial` Standardwert `false`; `OccurredAt`, `OdometerKm` Pflicht; Index (`VehicleId`, `OccurredAt`) ersetzt den Einzelindex auf `VehicleId` (dient weiterhin dem Fremdschlüssel). `VehicleId`-Fremdschlüssel und Kaskade bleiben unverändert.

### `MauiProgram`

- **Geänderte Methoden:** `CreateMauiApp` – `UseSqlite(TankradarDbContext.BuildConnectionString(...))`; Registrierungen: `ITankbookEntryService` → `TankbookEntryService` (Singleton), `IReceiptPhotoStore` → `ReceiptPhotoStore` (Singleton), `IReceiptPhotoPicker` über `ReceiptPhotoPickerSelector.Create(Environment.GetEnvironmentVariable, () => new MauiReceiptPhotoPicker(...))`, `ITankbookNavigator` → `ShellTankbookNavigator`, `TankbookEntriesViewModel`, `TankbookEntryFormViewModel` (transient, vom Seiten-ViewModel erzeugt bzw. injiziert), `TankbookEntriesPage` (transient). Keine Änderung an `Tankradar.MAUI.csproj`-Einstellungen für Interpreter/Trimming.

### `AppShell`

- **Geänderte Methoden:** Konstruktor – `Routing.RegisterRoute(ShellTankbookNavigator.EntriesRoute, typeof(TankbookEntriesPage))`.

### `IVehicleService` / `VehicleService`

- **Geänderte Methoden:** `DeleteVehicleAsync(long vehicleId, int expectedEntryCount, CancellationToken)` → `Task<VehicleDeleteResult>` (atomare Neuzählung, Fotodateien nach Commit löschen). Die alte Überladung entfällt (einziger Aufrufer `TankbookViewModel`).
- **Geänderte Methoden:** `UpdateVehicleAsync` – Prüfung `StartOdometerAboveEntries`.
- **Neue Abhängigkeit:** `IReceiptPhotoStore` (Konstruktor).

### `VehicleResult` (Enum)

- **Neue Werte:** `EntryCountChanged`, `StartOdometerAboveEntries`.

### `TankbookViewModel`

- **Neue Abhängigkeit:** `ITankbookNavigator`.
- **Geänderte Methoden:** `LoadAsync` (übergibt `OpenEntries`-Aktion an `VehicleItemViewModel`), `DeleteAsync` (erwartete Zahl, Behandlung von `EntryCountChanged`), `SaveAsync` (Feldfehler bei `StartOdometerAboveEntries`).
- **Neue Methoden:** `OpenEntries(VehicleItemViewModel)` (privat, ruft den Navigator).

### `VehicleItemViewModel`

- **Neue Eigenschaften:** `ShownEntryCount` (`int`), `ConfirmDeleteHint`, `CancelDeleteHint`, `EntriesHint` (`string`), `OpenEntriesCommand` (`ICommand`).
- **Entfällt:** `ConfirmDeleteDescription`, `CancelDeleteDescription` (ersetzt durch die Hints).
- **Geänderte Methoden:** Konstruktor (zusätzliche Aktion `openEntries`), `ShowDeleteConfirmation(int)` setzt `ShownEntryCount` und `ConfirmDeleteHint`.

### `TankbookTexts`

- **Neu:** `Entries = "Tank- und Ladevorgänge"`, `FormatVehicleHint(name)` („Fahrzeug „Golf““), `FormatConfirmDeleteHint(name, entryCount)`, `FormatCancelDeleteHint(name)`, `CancelFormHint`, Text für `StartOdometerAboveEntries` („Der Start-Kilometerstand darf nicht über dem Kilometerstand des ersten Tank- oder Ladevorgangs liegen.“); `GetResultMessage` deckt `EntryCountChanged` und `StartOdometerAboveEntries` ab.
- **Entfällt:** `FormatConfirmDeleteDescription`, `FormatCancelDeleteDescription`, `CancelFormDescription` (umbenannt in Hint-Varianten).

### `TankbookPage.xaml`

- Styles aus `Resources/TankbookStyles.xaml` statt lokal.
- Fahrzeugkarte: neue Schaltfläche „Tank- und Ladevorgänge“ (`Tankbook.Vehicle.Entries`, `TankbookButton`, Hint `EntriesHint`).
- `Tankbook.Vehicle.DeleteConfirm`, `Tankbook.Vehicle.DeleteCancel`, `Tankbook.Form.Cancel`: `SemanticProperties.Description` entfernen, `SemanticProperties.Hint` setzen.
- `Intro`-Text bleibt („Deine Fahrzeuge. Alle Angaben bleiben auf diesem Gerät.“).

### `IDatabaseFileProtector` / `DatabaseFileProtector` / `FakeDatabaseFileProtector`

- **Neue Methoden:** `ProtectFile(string filePath)` – iOS: Schutzklasse auf Datei und deren Verzeichnis; sonst Debug-Log. Gemeinsame private Hilfsmethode mit `Protect`.

### `TestDataPaths` (TestSupport, in die App verlinkt)

- **Neue Konstante:** `TestReceiptPhotoEnvironmentVariable = "TANKATLAS_TEST_RECEIPT_PHOTO"`.

### `Platforms/iOS/Info.plist` und `Platforms/MacCatalyst/Info.plist`

- `NSCameraUsageDescription` („Foto eines Tankbelegs aufnehmen“) und `NSPhotoLibraryUsageDescription` („Foto eines Tankbelegs auswählen“).

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `AddTankbookEntryDetails` (erzeugt mit `dotnet ef migrations add AddTankbookEntryDetails --project src/Tankradar.MAUI --startup-project src/Tankradar.MAUI --framework net10.0-windows10.0.19041.0`, nie per Hand) | `TankbookEntries`: neue Spalten `OccurredAt` (TEXT, NOT NULL), `Kind` (TEXT, NOT NULL, Default `'Fuel'`), `Quantity`, `UnitPrice`, `TotalAmount` (TEXT, NOT NULL, Default `'0'`), `OdometerKm` (INTEGER, NOT NULL, Default 0), `IsPartial` (INTEGER, NOT NULL, Default 0), `StationName` (TEXT, NOT NULL, Default `''`), `StationId`, `Note`, `ReceiptPhotoFileName` (TEXT, NULL); Index `IX_TankbookEntries_VehicleId` → `IX_TankbookEntries_VehicleId_OccurredAt` | Rein additiv per `ALTER TABLE … ADD COLUMN` (kein Tabellenneubau, `VehicleId`-Fremdschlüssel und Kaskade unverändert). Nach dem Hinzufügen per `migrationBuilder.Sql` im `Up`: `OccurredAt = CreatedUtc`; `OdometerKm` = Start-Kilometerstand des Fahrzeugs; `Kind = 'Electricity'` für Vorgänge von Fahrzeugen mit `FuelKind = 'Electric'`. Bestehende Zeilen und alle anderen Tabellen bleiben unverändert. `Down` entfernt Spalten und Index (EF-Standard). Prüfung: `MigrationsTests_ModelSync` ohne offene Änderungen; Upgrade-Test mit Bestandsdaten; praktisch Start von 0.1.17 auf einer 0.1.16-Datenbank. |

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Datum + Uhrzeit | Pflicht; nicht nach jetzt (+ 1 Minute Toleranz) | „Bitte wähle Datum und Uhrzeit.“ / „Der Zeitpunkt darf nicht in der Zukunft liegen.“ |
| Tankstelle/Ladeort | getrimmt 1–60 Zeichen | „Bitte gib eine Tankstelle oder einen Ladeort ein oder wähle eine bekannte Tankstelle.“ / „… höchstens 60 Zeichen …“ |
| Art | Benzin/Diesel → `Fuel`, Elektro → `Electricity`, Hybrid → beide; gespeicherte Art beim Korrigieren erlaubt | `KindNotAllowed` (nur Schutzprüfung im Dienst; die Oberfläche bietet nur erlaubte Arten an) |
| Menge (L bzw. kWh) | optional einzeln, Zahl mit Komma oder Punkt, > 0 und < 1.000, höchstens 3 Nachkommastellen | `NotANumber`, `Negative`, `OutOfRange`, `TooManyDecimals` mit Einheit im Text |
| Preis pro Einheit (€/L bzw. €/kWh) | optional einzeln, > 0 und < 10, höchstens 3 Nachkommastellen | wie oben |
| Gesamtbetrag (€) | optional einzeln, > 0 und < 10.000, höchstens 2 Nachkommastellen | wie oben |
| Menge/Preis/Gesamt gemeinsam | mindestens zwei Werte; bei drei Werten \|Menge × Preis − Gesamt\| ≤ 0,02 € | `MissingAmounts` („Bitte gib mindestens zwei der drei Werte Menge, Preis pro Liter und Gesamtbetrag an.“, Einheit je Art) / `AmountMismatch` am Gesamtbetrag („Menge × Preis ergibt 74,36 €, nicht 75,00 €. Bitte prüfe die Werte.“) |
| Kilometerstand | Pflicht, ganze Zahl 0–9.999.999, Tausenderpunkte erlaubt (`VehicleInputParser.ParseOdometer`); ≥ Start-Kilometerstand; ≥ Vorgänger; ≤ Nachfolger | Parserfehler wie Schritt 12; Dienst: „… nicht unter dem Start-Kilometerstand (10.000 km) …“, „… nicht unter 10.500 km (vorheriger Vorgang) …“, „… nicht über 11.000 km (nächster Vorgang) …“ |
| Teilbetankung | Schalter, Standard aus | – |
| Notiz | optional, getrimmt höchstens 500 Zeichen (`MaxLength` am Editor) | `TooLong` |
| Belegfoto | Endung `.jpg/.jpeg/.png/.heic/.heif`, höchstens 20 MB | „Dieses Dateiformat wird nicht unterstützt. Bitte wähle ein JPG-, PNG- oder HEIC-Foto.“ / „Das Foto ist zu groß (höchstens 20 MB).“ |
| Start-Kilometerstand Fahrzeug (Änderung) | nicht über dem niedrigsten Kilometerstand der Vorgänge | `StartOdometerAboveEntries` als Feldfehler |

Alle Feldfehler erscheinen gleichzeitig unter dem Feld mit rotem Rahmen; der erste wird angesagt (Muster `TankbookViewModel.SaveAsync`).

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `TANKATLAS_TEST_RECEIPT_PHOTO` (Umgebungsvariable, Konstante `TestDataPaths.TestReceiptPhotoEnvironmentVariable`) | Dateipfad | nicht gesetzt | nur im Testmodus: Datei, die die Test-Fotoauswahl liefert |
| Verbindungszeichenfolge (`TankradarDbContext.BuildConnectionString`) | string | `Data Source=<Pfad>;Foreign Keys=True` | Fremdschlüssel ausdrücklich an |
| `Info.plist` (iOS, MacCatalyst) | plist | `NSCameraUsageDescription`, `NSPhotoLibraryUsageDescription` | Systemabfrage für Kamera/Fotos |

Keine neue Benutzereinstellung, keine Änderung an `appsettings.json` oder an den iOS-Build-Einstellungen in `Tankradar.MAUI.csproj`.

## Seiteneffekte und Risiken

- **Fremdschlüssel jetzt ausdrücklich an:** Alle Kontexte der App öffnen mit `PRAGMA foreign_keys = ON`. Bisher galt das durch `e_sqlite3` ohnehin; Migrationen mit Tabellenneubau schaltet EF selbst ab. Die neue Migration baut nichts neu. `StationId` der Vorgänge hat bewusst keinen Fremdschlüssel.
- **Löschen von Fahrzeugen:** Signaturänderung von `DeleteVehicleAsync`; Löschen braucht ggf. eine zweite Bestätigung, wenn sich die Zahl geändert hat.
- **Barrierefreiheit:** Die vorgelesenen Namen dreier Schaltflächen ändern sich (E2E- und Unit-Erwartungen anpassen).
- **Bestandszeilen aus `AddVehicles`:** Produktiv konnte 0.1.16 keine Vorgänge erfassen; Zeilen existieren nur in Testdatenbanken. Sie erhalten Standardwerte (Menge/Preis/Gesamt 0, Kilometerstand = Start) und erscheinen als Vorgang ohne Verbrauch; beim Korrigieren müssen gültige Werte eingegeben werden.
- **Rohe SQL-Einfügungen in Tests** (`TankbookTestDatabase.AddEntries`, `VehicleFixture.AddEntriesAsync`, `VehiclesMigrationTests_Upgrade`) funktionieren dank Spalten-Standardwerten weiter; `TankbookTestDatabase.AddEntries` wird trotzdem auf vollständige Zeilen umgestellt.
- **iOS-Release:** Neue Entität-Eigenschaften und Typen liegen in der App-Assembly (`TrimmerRootAssembly Include="Tankradar.MAUI"`); `MediaPicker` ist Teil von MAUI Essentials. Interpreter-/Trimming-Einstellungen bleiben unverändert.
- **Fotos auf iOS:** Neue Berechtigungstexte; ohne sie stürzt die Kameraauswahl ab (Test `PlatformManifestTests_Photos`). Unter Windows ist „Foto aufnehmen“ ausgeblendet (`IsCaptureSupported`).
- **HEIC unter Windows:** Die Vorschau kann ohne HEIF-Erweiterung leer bleiben; das Foto bleibt gespeichert (Hinweis in der Hilfe).
- **Pre-Commit-Hook `csproj-xmldoc-check.py`:** berechnete Eigenschaften in Ausweichform `get { return …; }` statt Ausdruckskörper.
- **Pre-Push `enum-coverage-check.py --strict`:** alle neuen Enum-Werte müssen in den Text-`switch`es abgedeckt sein.
- **Größe der Testklassen:** Skill `test-class-structure` – Testklassen nach Thema getrennt (`Klasse_Thema`).

## Umsetzungsreihenfolge

1. **Modelle und Grenzwerte**
   - Voraussetzungen: keine.
   - Beschreibung: `TankbookEntryKind`, `TankbookConsumptionState`, `TankbookEntryResult`, `TankbookEntryFieldError`, `AmountField`, `ReceiptPhotoError`, Records `TankbookEntry`, `TankbookEntryInput`, `TankbookEntryOverview`, `TankbookEntrySaveResult`, `TankbookEntryFormValidation`, `AmountCompletion`, `KnownStation`, `VehicleDeleteResult`, `ReceiptPhotoImportResult`, `PickedReceiptPhoto`, `TankbookEntryLimits`; `VehicleResult` um `EntryCountChanged`, `StartOdometerAboveEntries` erweitern.
2. **Reine Rechen- und Prüfbausteine**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `DecimalInputParser`, `AmountCalculator`, `TankbookEntryOrder`, `ConsumptionCalculator`, `TankbookEntryValidator` samt Unit-Tests (Randfälle erster Eintrag, Teilbetankungen, kWh, Hybrid, Strecke 0, Toleranz).
3. **Entität, Kontext, Verbindungszeichenfolge**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `TankbookEntryEntity` erweitern (+ `Apply`), `TankradarDbContext.OnModelCreating` und `BuildConnectionString`, `MauiProgram` auf `BuildConnectionString` umstellen, Integrations-`TestDatabase` ebenfalls.
4. **Migration `AddTankbookEntryDetails`**
   - Voraussetzungen: Schritt 3, `Microsoft.EntityFrameworkCore.Design` (vorhanden).
   - Beschreibung: per EF-CLI erzeugen, `Up` um die drei `UPDATE`-Anweisungen ergänzen, `Up`/`Down` auf Datenerhalt prüfen; `DatabaseInitializerTests_*` auf 7 Migrationen; neuer Upgrade-Test.
5. **Dateischutz und Fotoablage**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `IDatabaseFileProtector.ProtectFile` (+ `DatabaseFileProtector`, `FakeDatabaseFileProtector`), `IReceiptPhotoStore`/`ReceiptPhotoStore`, `TestDataPaths.TestReceiptPhotoEnvironmentVariable`, `IReceiptPhotoPicker`, `MauiReceiptPhotoPicker`, `TestReceiptPhotoPicker`, `ReceiptPhotoPickerSelector`, Info.plist-Einträge.
6. **`TankbookEntryService`**
   - Voraussetzungen: Schritte 2–5.
   - Beschreibung: `ITankbookEntryService` + Umsetzung (Lesen mit Verbrauch, Anlegen/Ändern mit Kilometerprüfung, Löschen mit Fotodatei, bekannte Tankstellen, einmalige Bereinigung verwaister Fotos); Unit- und Integrationstests.
7. **`VehicleService`-Anpassungen (Zusatz 12a, ADR-0008-Folgen)**
   - Voraussetzungen: Schritte 1, 5.
   - Beschreibung: `DeleteVehicleAsync(vehicleId, expectedEntryCount)` atomar mit Fotolöschung, `UpdateVehicleAsync` mit `StartOdometerAboveEntries`; `VehicleFixture` um `ReceiptPhotoStore`-Double ergänzen; Tests anpassen/ergänzen.
8. **Texte**
   - Voraussetzungen: Schritte 1, 7.
   - Beschreibung: `TankbookEntryTexts` (Formatierungen de-DE: „74,36 €“, „1,859 €/L“, „0,59 €/kWh“, „40,00 L“, „12,345 kWh“, „10.500 km“, „6,00 L/100 km“, Datum „18.05.2026, 14:30“, alle Fehler- und Ergebnistexte, Hints); `TankbookTexts` (Hints, neue Ergebniswerte, Schaltfläche „Tank- und Ladevorgänge“).
9. **Navigation und Styles**
   - Voraussetzungen: keine (nur bestehende Shell).
   - Beschreibung: `ITankbookNavigator`, `ShellTankbookNavigator`, Route in `AppShell`; Styles nach `Resources/TankbookStyles.xaml` verschieben und in `TankbookPage` einbinden.
10. **Tankbuchseite (Zusatz 12a/12c, Einstieg)**
    - Voraussetzungen: Schritte 7–9.
    - Beschreibung: `VehicleItemViewModel`, `TankbookViewModel`, `TankbookPage.xaml` (WCAG-Muster, „Tank- und Ladevorgänge“, Neuzählung), Unit-Tests anpassen.
11. **Vorgangsseite**
    - Voraussetzungen: Schritte 5, 6, 8, 9.
    - Beschreibung: `KnownStationOptionViewModel`, `TankbookEntryFormViewModel`, `TankbookEntryItemViewModel`, `TankbookEntriesViewModel`, `TankbookEntriesPage.xaml` (+ `.xaml.cs`), DI-Registrierungen in `MauiProgram`. UI-Muster aus `TankbookPage` wiederverwenden: Formular als `Border`-Bereich (`TankbookCard`), Eingabefelder `UnitFieldBorder` mit Fokus-/Fehler-`DataTrigger` und festem Einheiten-Suffix (`L`/`kWh`, `€/L`/`€/kWh`, `€`, `km` laut `DESIGN.md` „Tankbuch-Formulare“), Feldfehler `FieldError`, Art- und Tankstellen-Chips nach `FuelKindChip`/`ChoiceOptionViewModel`, Rückfrage im Eintrag wie `Tankbook.Vehicle.DeleteQuestion`, `x:DataType` überall (kompilierte Bindungen). Teilbetankung als `Switch` (Toggle-Muster wie `Settings.Nearby.Switch`), Notiz als `Editor`, Datum/Uhrzeit als `DatePicker`/`TimePicker` (`MaximumDate` = heute). Eintragskarte nach Entwurf: Tankstelle, Datum, Gesamtbetrag, Preis pro Einheit, „Menge“, „Strecke“, „Verbrauch“ bzw. Zustandstext, Notiz in Anführungszeichen, „Beleg anzeigen“, Kennzeichnung „Teilbetankung“ und bei Hybrid „Kraftstoff“/„Strom“. Unit-Tests der ViewModels.
12. **E2E-Infrastruktur**
    - Voraussetzungen: Schritte 10–11.
    - Beschreibung: `TankbookE2EDriver` aus `TankbookE2ETestBase` herauslösen und um Vorgangshilfen erweitern; `TankbookTestDatabase` um vollständige Vorgänge, Abfrage von Werten und Fotodateien ergänzen; Erzeugung einer kleinen Test-PNG zur Laufzeit.
13. **E2E-Tests**
    - Voraussetzungen: Schritt 12.
    - Beschreibung: neue Szenarien laut Tabelle, bestehende Tankbuch-Tests anpassen.
14. **Dokumentation**
    - Voraussetzungen: Schritte 1–13.
    - Beschreibung: `docs/adr/0009-tankbook-entries.md` (Entscheidungen dieses Plans, Designabweichungen: keine Karte „Aktives Fahrzeug“ mit Auswahlliste, eigene Vorgangsseite, Monatsanalyse/Verlauf → Schritt 14, Export → Schritt 16, Sync-Banner entfällt, „Alle 42 anzeigen“ entfällt, weil die Seite alle Vorgänge zeigt; WCAG-Muster); ADR 0008 „Folgen“ um Verweis auf ADR 0009 ergänzen; `docs/help/Tankbuch/` (`index.md` ohne „folgt in einer späteren Version“, `beschreibung.md` mit Angaben, Berechnung, Teilbetankung, Plausibilität, Kilometerregeln, Belegfoto, Datenschutz; `ablauf-anwender.md` Erfassen/Korrigieren/Löschen/Foto; `ablauf-technisch.md` Tabelle, Migration, Dienste, Rechenbausteine, `Foreign Keys=True`, Sprachausgabe-Muster und `IScreenReaderAnnouncer`, Tests), ggf. Zeile in `docs/help/index.md`; `changes.log`-Eintrag „Tank- und Ladevorgänge erfassen (Tankradar Schritt 13, Version 0.1.17)“ oben.
15. **Prüfläufe und Zwischenstand**
    - Voraussetzungen: Schritte 1–14.
    - Beschreibung: `scripts/local-ci.ps1` vollständig grün (inkl. FlaUI off-screen, iOS-Compile-Prüfung), Pre-Push-Prüfungen; dann `scripts/create-review-version.ps1 -Version 0.1.17 -ChangelogFile <Changelog-Text>` → `review-versions/0.1.17_<JJJJ-MM-TT>/`, Start im Testmodus und Upgrade einer 0.1.16-Datenbank mit Fahrzeugen prüfen (nicht committen).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Parse_ValidInput_ReturnsValue` / `Parse_InvalidInput_ReturnsError` (Theory) | `DecimalInputParserTests_Values` | Komma/Punkt, Nachkommastellen, Bereich, leer, negativ, Text |
| `Complete_TwoValues_ComputesThird` (Theory: je fehlendes Feld) | `AmountCalculatorTests_Completion` | Berechnung und Rundung |
| `Complete_ThreeValues_WithinTolerance_IsValid`, `Complete_ThreeValues_Deviation_ReportsMismatchAndExpectedTotal`, `Complete_LessThanTwoValues_IsMissing` | `AmountCalculatorTests_Plausibility` | Toleranz 0,02 €, Meldung |
| `Calculate_TwoFullEntries_ReturnsConsumptionPer100Km`, `Calculate_FirstEntry_HasNoConsumption`, `Calculate_SameOdometer_HasNoDistanceValue`, `Calculate_EntriesOutOfOrder_UsesChronologicalOrder` | `ConsumptionCalculatorTests_FullEntries` | Formel, erster Eintrag, Strecke 0, Ordnung |
| `Calculate_PartialBeforeFull_AddsQuantityToNextFull`, `Calculate_SeveralPartials_AreSummed`, `Calculate_PartialBeforeFirstFull_IsIgnored`, `Calculate_TrailingPartial_HasNoValue` | `ConsumptionCalculatorTests_Partial` | Teilbetankungen |
| `Calculate_ElectricEntries_ReturnsKwhPer100Km`, `Calculate_Hybrid_KeepsSeparateChainsPerKind` | `ConsumptionCalculatorTests_Electric` | kWh, getrennte Ketten |
| `ValidateForm_AllFieldsInvalid_ReportsEveryField`, `ValidateForm_Valid_BuildsInputWithComputedValue`, `ValidateForm_FutureDate_ReportsInFuture`, `ValidateForm_StationTooLong`, `ValidateForm_NoteTooLong` | `TankbookEntryValidatorTests_Form` | Formularprüfung |
| `Validate_Input_ReturnsResult` (Theory), `IsKindAllowed_ByVehicleFuelKind` (Theory) | `TankbookEntryValidatorTests_Input` | Schutzprüfung, Arten je Kraftstoffart |
| `CreateEntry_StoresAllFields`, `CreateEntry_UnknownVehicle_ReportsNotFound`, `CreateEntry_KindNotAllowed_IsRejected`, `CreateEntry_InvalidInput_IsRejectedWithoutSaving` | `TankbookEntryServiceTests_Create` | Anlegen |
| `CreateEntry_BelowVehicleStart_IsRejectedWithBound`, `CreateEntry_BelowPrevious_IsRejectedWithBound`, `CreateEntry_Backdated_AboveNext_IsRejected`, `CreateEntry_EqualToPrevious_IsAllowed`, `UpdateEntry_IgnoresItselfAsNeighbour` | `TankbookEntryServiceTests_Odometer` | Kilometerregeln |
| `GetEntries_NewestFirstWithConsumption`, `GetEntries_OtherVehiclesAreIgnored`, `GetEntries_FirstCall_DeletesUnreferencedPhotos` | `TankbookEntryServiceTests_Read` | Lesen, Ordnung, Bereinigung |
| `UpdateEntry_ChangesAllFields`, `UpdateEntry_StoredKindStaysAllowedAfterVehicleKindChange`, `UpdateEntry_ReplacedPhoto_DeletesOldFile`, `UpdateEntry_Unknown_ReportsNotFound`, `DeleteEntry_RemovesRowAndPhoto`, `DeleteEntry_Unknown_ReportsNotFound` | `TankbookEntryServiceTests_Change` | Korrigieren, Löschen |
| `GetKnownStations_FavoritesFirstThenUsedNames`, `GetKnownStations_NoDuplicatesIgnoringCase`, `GetKnownStations_LimitedTo30` | `TankbookEntryServiceTests_KnownStations` | bekannte Tankstellen |
| `DeleteVehicle_EntryCountChanged_KeepsVehicleAndReportsCount`, `DeleteVehicle_WithEntries_DeletesReceiptPhotos` | `VehicleServiceTests_Delete` (erweitert) | Zusatz 12a, Fotos |
| `UpdateVehicle_StartOdometerAboveFirstEntry_IsRejected`, `UpdateVehicle_FuelKindChangeWithEntries_IsAllowed` | `VehicleServiceTests_Manage` (erweitert) | ADR-0008-Folgen |
| `Import_SupportedPhoto_CopiesAndProtects`, `Import_UnsupportedExtension_IsRejected`, `Import_TooLarge_IsRejectedAndLeavesNoFile`, `Delete_RemovesFile`, `Delete_InvalidFileName_IsIgnored`, `DeleteUnreferenced_KeepsReferencedFiles` | `ReceiptPhotoStoreTests_Files` | Ablage, Dateischutz-Aufruf, Pfadschutz |
| `Create_TestMode_UsesTestPicker`, `Create_Production_UsesPlatformPicker`, `TestPicker_NoFile_ReturnsNull` | `ReceiptPhotoPickerSelectorTests_TestMode` | Auswahl Test/Plattform |
| `BuildConnectionString_EnablesForeignKeys`, `BuildConnectionString_WithoutPooling` | `TankradarDbContextTests_ConnectionString` | Zusatz 12b |
| `InfoPlist_ContainsCameraAndPhotoDescriptions` (Theory iOS/MacCatalyst) | `PlatformManifestTests_Photos` | Berechtigungstexte |
| `Delete_EntryAddedMeanwhile_ShowsUpdatedQuestionAndKeepsVehicle`, `Delete_ButtonsNameIsVisibleTextAndHintNamesVehicle` | `TankbookViewModelTests_Delete` (erweitert/ersetzt) | Zusatz 12a/12c |
| `Save_StartOdometerAboveEntries_ShowsOdometerError`, `OpenEntries_NavigatesWithVehicleId` | `TankbookViewModelTests_Form` (erweitert) bzw. neue `TankbookViewModelTests_Navigation` | Einstieg, ADR-0008-Folge |
| `Load_ShowsEntriesNewestFirstWithConsumptionTexts`, `Load_NoEntries_ShowsEmptyHint`, `Load_VehicleMissing_ShowsMessageAndCloses`, `Load_StorageFailure_ShowsMessageWithoutLoggingValues` | `TankbookEntriesViewModelTests_Load` | Seite |
| `Add_PrefillsNowAndKindByVehicle`, `Hybrid_OffersBothKinds`, `TwoAmounts_ShowsComputedHint`, `Save_Valid_CreatesAndReloads`, `Save_InvalidFields_ShowsAllErrorsAndAnnouncesFirst`, `Save_OdometerBelowPrevious_ShowsBoundError`, `SelectKnownStation_FillsNameAndKeepsStationId`, `EditStationName_ClearsStationId`, `Cancel_DeletesPendingPhoto` | `TankbookEntryFormViewModelTests_Form` / `_Amounts` / `_Station` | Formular |
| `PickPhoto_ImportsAndShowsPreview`, `PickPhoto_Unsupported_ShowsError`, `RemovePhoto_ClearsPendingFile`, `Capture_HiddenWhenNotSupported` | `TankbookEntryFormViewModelTests_Photo` | Foto |
| `Delete_AsksAndDeletesAfterConfirmation`, `Delete_Cancel_KeepsEntry`, `Edit_PrefillsForm`, `Buttons_NameIsVisibleTextAndHintGivesContext` | `TankbookEntriesViewModelTests_Delete` / `_Edit` | Korrigieren, Löschen, WCAG |
| `Format_Amounts_Units_Dates`, `ConsumptionText_ByState`, `FieldAndResultMessages_CoverAllValues` | `TankbookEntryTextsTests_Format` / `_Messages` | Texte, Enum-Abdeckung |
| `Initialize_FromSchemaBeforeEntryDetails_KeepsDataAndFillsDefaults` | `TankbookEntriesMigrationTests_Upgrade` (Integration) | Migration mit Bestandsdaten (Fahrzeuge Benzin/Elektro mit Altvorgängen, Favoriten, Preise, Einstellungen) |
| `Entries_SurviveRestartOnRealDatabaseFile` (Dezimalwerte exakt), `DeleteVehicle_WithEntries_OnRealFile_RemovesEntriesAndPhotos` | `TankbookEntryServiceTests_RealDatabase` (Integration) | echte Datei |
| `AppConnection_HasForeignKeysEnabled` | `ForeignKeysTests_Connection` (Integration) | `PRAGMA foreign_keys` = 1 mit `BuildConnectionString` |
| `TankbookE2EDriver` (`OpenEntries`, `FillEntryForm`, `SaveEntryForm`, `CreateEntry`, `ReadEntryTexts`, `PressForEntry`, `ToggleSwitch`) | E2E-Hilfsklasse | Vorgangsabläufe |
| `TankbookTestDatabase.AddEntries` (vollständige Zeilen), `ReadEntries`, `ListReceiptFiles` | E2E-Hilfsklasse | Datenbank- und Dateiprüfung |
| `VehicleFixture` (Fotoablage-Double), `FakeReceiptPhotoStore`, `FakeReceiptPhotoPicker`, `FakeTankbookNavigator`, `TankbookEntryFixture` | Unit-Support | Testdoubles |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `TankbookViewModelTests_Delete` (alle Löschtests, `Delete_AnnouncesQuestionAndDescribesButtonsWithName`) | neue Signatur `DeleteVehicleAsync(id, expected)`, Hints statt Descriptions |
| `TankbookViewModelTests_Form`, `TankbookViewModelTests_Load` | Konstruktor mit `ITankbookNavigator`; `Cancel`-Hint |
| `TankbookTextsTests_Labels.ButtonDescriptions_NameActionAndVehicle` | Description-Methoden entfallen → Hint-Methoden |
| `TankbookTextsTests_Messages.FieldAndResultMessages_CoverAllValues` | neue `VehicleResult`-Werte |
| `VehicleServiceTests_Delete` (alle), `VehicleServiceTests_Manage`, `VehicleFixture` | neue Signatur, neue Abhängigkeit `IReceiptPhotoStore` |
| `VehicleServiceTests_RealDatabase.DeleteVehicle_WithEntries_OnRealFile_RemovesEntries` | neue Signatur |
| `DatabaseInitializerTests_FirstStart`, `DatabaseInitializerTests_Upgrade` | 7 statt 6 Migrationen, neue Spalten |
| `VehiclesMigrationTests_Upgrade` | läuft auf den neuesten Stand; Erwartungen zu `TankbookEntries` prüfen (Spalten-Standardwerte) |
| `TestDatabase` (Integration-Support) | `BuildConnectionString(..., pooling: false)` |
| `DatabaseFileProtectorTests_Platform`, `FakeDatabaseFileProtector` | neue Methode `ProtectFile` |
| `ViewModelTests_PageTitles` | neuer Seitentitel der Vorgangsseite |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Fahrzeug (Diesel, Start 10.000 km) anlegen → „Tank- und Ladevorgänge“ → Vorgang 1 (Menge 40, Preis 1,859 → „Ergibt 74,36 €“, km 10.500) → Vorgang 2 (Menge 30, Gesamt 55,77, km 11.000) → Liste neueste zuerst, Vorgang 2 zeigt „Verbrauch 6,00 L/100 km“ und „Strecke 500 km“, Vorgang 1 „noch kein Verbrauch“, berechneter Preis/Gesamtbetrag angezeigt; Werte in der Datenbank | `TankbookEntriesE2ETests_Flow` | Erfassen + Verbrauchsanzeige (FlaUI-Vorgabe), zwei aus drei, chronologisch | Ganzer Benutzerfluss mit Navigation, Pickern, Formular und Liste |
| Pflicht | Teilbetankung (Schalter an) zwischen zwei Vollvorgängen → Teilbetankung ohne Wert mit Hinweis, nächster Vollvorgang rechnet mit Summe | `TankbookEntriesE2ETests_Partial` | Teilbetankungsregel | sichtbare Regel in der UI |
| Pflicht | Elektrofahrzeug: zwei Ladevorgänge in kWh (Ladeort „Zuhause“ frei eingegeben) → „kWh/100 km“, Einheiten `kWh` und `€/kWh` | `TankbookEntriesE2ETests_Electric` | Ladevorgänge in kWh, manueller Ladeort | Einheitenwechsel in der Oberfläche |
| Pflicht | Kilometerstand unter Vorgänger → Feldfehler mit Grenzwert, nichts gespeichert; drei Werte mit Abweichung → Fehler am Gesamtbetrag; nur ein Wert → Fehler; Pflichtfelder leer | `TankbookEntriesE2ETests_Validation` | Plausibilität, Kilometerregel | Feldfehler müssen sichtbar erscheinen |
| Pflicht | Vorgang korrigieren (Kilometerstand ändern → Verbrauch des Vorgangs ändert sich) und löschen (Rückfrage, Abbrechen behält, „Ja, löschen“ entfernt; Name der Schaltfläche = sichtbarer Text, Hint mit Kontext) | `TankbookEntriesE2ETests_EditDelete` | Korrigieren, Löschen, WCAG-Muster | Benutzerfluss |
| Pflicht | Belegfoto über Test-Auswahl (`TANKATLAS_TEST_RECEIPT_PHOTO` → zur Laufzeit erzeugte PNG) anhängen, „Beleg anzeigen“ zeigt Bild, Datei liegt unter `receipts/`; Vorgang löschen entfernt Datei | `TankbookEntriesE2ETests_Receipt` | Belegfoto lokal | Dateiablage und Anzeige im echten Prozess |
| Pflicht | Offline: Favorit mit „Alpha Tankstelle“ anlegen (Mock erreichbar) → Neustart mit `ClosedPortUrl()` → Startseite: `Favorites.OfflineBanner`, Preis „1,859 €/L“, Alter „vor X Min.“ → Detailansicht: `Detail.OfflineBanner`, `Detail.Age.SuperE5`, Preis → Tankbuch: Fahrzeug anlegen, Vorgang mit bekannter Tankstelle „Alpha Tankstelle“ aus der Liste, zweiter Vorgang, Verbrauch ansehen, Vorgang korrigieren, Vorgang löschen | `OfflineE2ETests_Tankbook` (abgeleitet von `HomeE2ETestBase`, nutzt `TankbookE2EDriver`) | Offline-Szenario, Auswahl bekannter Tankstelle | vorgeschriebenes FlaUI-Szenario |
| Pflicht | Rückfrage „Fahrzeug löschen“ offen (2 Vorgänge) → per `TankbookTestDatabase` einen dritten Vorgang einfügen → Bestätigen → Fahrzeug bleibt, Rückfrage nennt 3 Vorgänge → erneut bestätigen → gelöscht, 0 Vorgänge | `TankbookE2ETests_DeleteWithEntries.EntryAddedBeforeConfirming_IsNamedBeforeDeleting` | Zusatz 12a | Zusammenspiel UI/Datenbank |
| Soll | Vorgänge überstehen einen Neustart (Werte, Verbrauch, Foto) | `TankbookEntriesE2ETests_Persistence` | Offline/lokal gespeichert | echter Neustart |
| Soll | Fahrzeug mit Vorgängen: Start-Kilometerstand über erstem Vorgang → Feldfehler | `TankbookE2ETests_Validation` (erweitert) | ADR-0008-Folge | Feldfehler sichtbar |

Bestehende E2E-Tests, die angepasst werden:

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `TankbookE2ETests_Flow.CreateEditDeleteVehicle_Flow` | `DeleteConfirm`-Name jetzt „Ja, löschen“, Hint enthält „Golf Variant“; `Form.Cancel` Name „Abbrechen“ |
| `TankbookE2ETests_DeleteWithEntries.DeleteVehicleWithEntries_AsksExplicitlyAndDeletesEntries` | Name „Fahrzeug und Vorgänge löschen“, Hint mit Fahrzeugname; `TankbookTestDatabase.AddEntries` mit vollständigen Zeilen |
| `TankbookE2ETestBase` | delegiert an `TankbookE2EDriver` |

## Offene Punkte

Keine.

Die in `requirement.md` genannten Auslegungsfragen sind oben begründet entschieden (Designentscheidungen, Validierungsregeln) und
werden in ADR 0009 festgehalten: Toleranz 0,02 € mit blockierender Meldung, „chronologisch“ = neueste zuerst (Designentwurf),
Tankstelle Pflicht (frei oder aus der Liste), Arten je Kraftstoffart inkl. Hybrid und Kraftstoffart-Wechsel, Kilometerstand
zusätzlich nicht über dem Nachfolger, Zeitpunkt als lokale Wanduhrzeit.
