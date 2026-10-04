# Bestandsaufnahme (Schritt 5)

(Bestand ohne Unteragenten erhoben; Abweichung von lifecycle.md dokumentiert im Abschlussbericht.)

- MAUI-App `src/Tankradar.MAUI`: MVVM (`BaseViewModel`), DI in `MauiProgram`, EF Core SQLite (`TankradarDbContext`, Tabellen `UserSettings`, `FuelTypeSettings`, Migrationen `InitialCreate`, `AddFuelTypeSettings`), `IAppDataPathProvider` (`TEST_DATA_PATH`), `IDatabaseInitializer` (MigrateAsync), `ISettingsService`.
- `FuelType`-Enum: SuperE5, SuperE10, Diesel (entspricht Tankerkönig `e5`, `e10`, `diesel`).
- Optionen-Seite: `SettingsPage.xaml` mit Karten, `SettingsViewModel(ISettingsService, ILogger)`, Texte zentral in `Resources/Texts/SettingsTexts.cs`.
- Tests: `Tankradar.Tests.Unit`, `Tankradar.Tests.Integration` (echte SQLite, `TestDatabase`), `Tankradar.Tests.E2E` (FlaUI, `E2ETestBase`, `NavigateToTab`), gemeinsame Hilfen in `src/TestSupport`.
- Hooks blockieren API-Schlüssel-Muster (`forbidden-patterns-check.py`), verlangen XML-Doku (CS1591 als Fehler).
- Es existiert kein HTTP-Client, keine Preis-Persistenz, keine Konnektivitätserkennung, kein Schlüsselspeicher.
- Tankerkönig-API (dokumentiert): `GET /json/list.php?lat&lng&rad&type=all&sort=dist&apikey` (Radius maximal 25 km), `GET /json/detail.php?id&apikey`; Preise ohne eigenen Zeitstempel -> Abrufzeit gilt als Preiszeitpunkt.
