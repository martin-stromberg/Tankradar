# Bestandsaufnahme

- `MapViewModel` (Suche) hält Ergebnis `_allStations` (nach Filter/Sort), Einstellungen (`ResultView` existiert bereits in `AppSettings`, Settings-Radio `Settings.View.*`), Suchposition nur lokal in `RunSearchAsync`.
- `StationListItem` hat keine Koordinaten/Offen-Information; `StationInfo` hat Latitude/Longitude/IsOpen.
- `StationResultBuilder` bestimmt Sortierfeld = Filter ?? erste gewählte Sorte (gleiche Regel wie Karte).
- `MapPage.xaml`: ScrollView mit Suchkarten + Liste; Chips als Buttons mit AutomationIds; E2E ohne Mausklicks (nur UIA-Muster, Off-Screen).
- Test-Mocks: `MockTankerkoenigServer`, `MockNominatimServer` (TestSupport, per Compile-Link eingebunden); Testmodus-Endpunkte per Umgebungsvariablen, Fail-secure ohne URL.
- Datenschutz-Tests: `MapViewModelTests_Privacy` verbietet GeoPosition-Felder/-Properties im ViewModel (anzupassen: Ursprung nur flüchtig als `MapOrigin`).
- Vorlage Unterlagen: ADR 0004, `scripts/validate-workflows.py` (Funktionen `check_key_policy`).
- Designentwurf `suche_kartenansicht`: Umschalter Liste/Karte, Zähler-Chip, Zoom +/-, Zentrieren, Preis-Pins in Grün/Teal/Rot, Detailkarte unten.
