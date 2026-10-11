# Bestandsaufnahme: Schritt 13 – Tank- und Ladevorgänge erfassen

Analysiert wurden Datenhaltung, Dienste, ViewModels, Seiten und Tests des Tankbuchs aus Schritt 12 sowie die angrenzenden
Bereiche (Favoriten/Tankstellen, Dateischutz, Testmodus, Offline-Anzeige) bezogen auf [requirement.md](requirement.md).

## Zusammenfassung

- `TankbookEntries` existiert nur minimal (`Id`, `VehicleId` mit `ON DELETE CASCADE`, `CreatedUtc`); alle fachlichen
  Spalten eines Vorgangs fehlen. Sechs Migrationen, zuletzt `20261011105508_AddVehicles`.
- Fahrzeugverwaltung vollständig vorhanden (`IVehicleService`/`VehicleService`, `VehicleValidator`, `VehicleInputParser`,
  `TankbookViewModel`, `VehicleItemViewModel`, `TankbookPage`, `TankbookTexts`); Parser für Kilometerstand und
  Dezimalzahlen sind wiederverwendbar.
- Keine Erfassung, Liste, Verbrauchsberechnung, Plausibilitätsprüfung oder Belegfoto-Funktion; keine Unterseite des
  Tankbuchs; kein `MediaPicker` im Projekt; keine Kamera-/Foto-Berechtigungstexte in den Info.plist-Dateien.
- Restpunkte aus Abnahme 12 bestätigt: `DeleteVehicleAsync` zählt beim Bestätigen nicht erneut; die Verbindungszeichenfolge
  enthält kein `Foreign Keys=True`; drei Schaltflächen setzen `SemanticProperties.Description`, die nicht mit dem sichtbaren
  Text beginnt.
- Bekannte Tankstellen: `Stations` (Preis-Cache, nie gelöscht) und `FavoriteEntries`; `IFavoritesService` liest nur je Gruppe.
- Wiederverwendbar: Dateischutz-Muster `DatabaseFileProtector` (iOS `CompleteUntilFirstUserAuthentication`),
  Testmodus-Auswahl `LocationServiceSelector`, Shell-Navigation mit Parametern (`ShellFavoriteGroupNavigator`), Chip-Muster
  `ChoiceOptionViewModel`, Formular-/Fehler-/Einheitenfeld-Styles der `TankbookPage`, Offline-Banner und Preisalter
  (`Favorites.*`, `Detail.*`) mit E2E-Basis `HomeE2ETestBase` und `ClosedPortUrl()`.

Test-Ausgangszustand: Build Release ohne Warnungen; Unit 889/889, Integration 116/116, FlaUI-E2E 80/80 grün; keine
bekannten Fehlschläge. Nicht ausgeführt: Coverage-, Node-, Workflow-, Sicherheits- und iOS-Prüfungen von `local-ci.ps1`.
Nachweis: [inventory/tests.md](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md)
- [Logik, ViewModels und Oberfläche](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
