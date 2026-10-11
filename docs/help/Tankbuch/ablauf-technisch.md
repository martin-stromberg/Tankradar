← [Zurück zur Übersicht](index.md)

# Tankbuch — Technischer Ablauf

## Daten

Migration `AddVehicles` (rein additiv, bestehende Daten bleiben erhalten, `VehiclesMigrationTests_Upgrade`):

- `Vehicles`: `Id`, `Name` (`NOCASE`, eindeutiger Index `IX_Vehicles_Name`), `FuelKind` (Enum-Name von `VehicleFuelKind`: `Petrol`, `Diesel`, `Electric`, `Hybrid` – Persistenzvertrag), `StartOdometerKm` (`INTEGER`), `StartConsumption` (`decimal` als `TEXT`, optional), `CreatedUtc`.
- `TankbookEntries`: bewusst minimal (`Id`, `VehicleId` mit `ON DELETE CASCADE`, `CreatedUtc`). Die Tabelle macht das Mitlöschen der Vorgänge schon jetzt prüfbar; Schritt 13 ergänzt die Angaben eines Vorgangs per eigener Migration.

Ein nicht erkennbarer gespeicherter `FuelKind` wird als `Petrol` gelesen und als Warnung ohne Namen protokolliert.

## Dienst

`IVehicleService` / `VehicleService` (Singleton, `IDbContextFactory`, `IDatabaseInitializer`, `TimeProvider`, `ILogger`):

- `GetVehiclesAsync` (Sortierung de-DE ohne Beachtung der Groß- und Kleinschreibung), `GetVehicleAsync`.
- `CreateVehicleAsync` → `VehicleSaveResult`, `UpdateVehicleAsync` und `DeleteVehicleAsync` → `VehicleResult` (`Ok`, `InvalidName`, `DuplicateName`, `InvalidFuelKind`, `InvalidOdometer`, `InvalidConsumption`, `VehicleNotFound`).
- Schutzprüfung mit `VehicleValidator.Validate`; doppelter Name per Vergleich im Speicher (`OrdinalIgnoreCase`) und als Sicherung SQLite-Fehler 2067 → `DuplicateName`.
- `CountEntriesAsync` zählt die Vorgänge eines Fahrzeugs (frisch beim Antippen von „Löschen“); das Löschen der Vorgänge übernimmt die Datenbank per Kaskade.

## Eingabe und Prüfung

- `VehicleInputParser.ParseOdometer` / `ParseConsumption`: deutsche Schreibweise (Tausenderpunkte bzw. Dezimalkomma, einzelner Punkt als Dezimaltrennzeichen), Ergebnis `VehicleFieldError`.
- `VehicleValidator.ValidateForm` liefert `VehicleFormValidation` mit je einem Fehler pro Feld und bei Erfolg die `VehicleInput`; Grenzen in `VehicleLimits`.
- Texte: `TankbookTexts` (Beschriftungen, Einheiten, Rückfrage `FormatDeleteQuestion`, Feld- und Ergebnismeldungen).

## Oberfläche

- `TankbookViewModel` (Liste `Vehicles` aus `VehicleItemViewModel`, Formular mit `FuelKindOptions` als `ChoiceOptionViewModel<VehicleFuelKind>`, Feldfehler, `StatusMessage`, `LastTask`) und `TankbookPage`.
- Formular und Löschrückfrage sind Bereiche in der Seite, keine Dialoge des Betriebssystems (wie [ADR 0006](../../adr/0006-favorite-groups.md)).
- Eingabefelder mit Einheiten-Suffix: `Border` (Radius 8, Rahmen `ColorBorderLight`/`ColorBorderDark`) um `Entry` und Einheiten-`Label`; Fokusrahmen 2 px `ColorPrimaryTeal` über `DataTrigger` auf `Entry.IsFocused` (`x:Reference`, kompiliert dank `MauiEnableXamlCBindingWithSourceCompilation`), Fehlerrahmen `ColorErrorRed`.
- AutomationIds: `TankbookPage.Headline`, `Tankbook.Status`, `Tankbook.Empty`, `Tankbook.AddVehicle`, `Tankbook.Form.*` (u. a. `Name`, `FuelKind.<Wert>`, `Odometer`, `OdometerUnit`, `Consumption`, `ConsumptionUnit`, `NameError`, `OdometerError`, `ConsumptionError`, `Save`, `Cancel`), `Tankbook.Vehicle.*` (`Name`, `Details`, `Edit`, `Delete`, `DeleteQuestion`, `DeleteConfirm`, `DeleteCancel`).

## Tests

- Unit: `VehicleInputParserTests_*`, `VehicleValidatorTests_*`, `VehicleServiceTests_Manage`/`_Delete`, `TankbookTextsTests_*`, `TankbookViewModelTests_Load`/`_Form`/`_Delete` (Hilfsklasse `VehicleFixture`).
- Integration: `VehiclesMigrationTests_Upgrade` (Datenerhalt, Kaskade, `NOCASE`), `VehicleServiceTests_RealDatabase`.
- FlaUI: `TankbookE2ETests_Flow`, `TankbookE2ETests_Validation`, `TankbookE2ETests_DeleteWithEntries` (Vorgänge über `TankbookTestDatabase` direkt in die isolierte Testdatenbank), `TankbookE2ETests_Persistence`.
