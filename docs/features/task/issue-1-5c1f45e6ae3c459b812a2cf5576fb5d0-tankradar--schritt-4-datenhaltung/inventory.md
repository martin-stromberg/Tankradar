# Bestandsaufnahme: Lokale Datenhaltung und Einstellungen (Schritt 4)

Analysiert wurden `src/` (MAUI-App, Unit-, Integrations-, E2E-Tests, TestSupport), `scripts/`, `.githooks/` und die Dokumentation bezogen auf die Anforderung in `requirement.md`.

## Zusammenfassung

- Es existiert keine Datenhaltung: kein DbContext, keine EF-Core-Pakete, keine Migrationen, kein Ordner `Data/`, keine Einstellungsmodelle oder Enums. `src/Tankradar.MAUI/Models/` enthält nur `.gitkeep`.
- `IAppDataPathProvider` / `AppDataPathProvider` liefern das Datenverzeichnis (Override über `TEST_DATA_PATH`), werden aber noch von keiner Datenhaltung genutzt.
- `SettingsViewModel` setzt nur den Titel „Optionen"; `SettingsPage.xaml` ist ein Platzhalter (Headline mit AutomationId `SettingsPage.Headline` und Hinweistext). Das Design-System (`DesignSystem.xaml`) liefert Farben (Hell/Dunkel), Abstände, `MinimumTouchTarget` = 44 und Label-Styles, aber keine Styles für Schalter, Auswahl oder Schaltflächen. Vergleichbare Auswahl-UI-Muster existieren nicht; ein Optionen-Screen im Designentwurf existiert ebenfalls nicht.
- `MauiProgram` registriert nur `AppConfiguration`, `IAppDataPathProvider`, ViewModels (Transient) und Seiten; `App` startet `AppConfiguration.LoadFromSettingsFileAsync` fire-and-forget (Muster für asynchrone Startarbeiten).
- `Tankradar.MAUI.csproj` hat `ApplicationTitle` Tankatlas, aber kein `<Product>`; Version 0.1.0 in der csproj (Versionen werden über Skripte/CI vergeben). Kein Entitlements-File für iOS; `Info.plist` ohne Datenschutzangaben.
- Testinfrastruktur: `TestDataContext` (Integration, GUID-Verzeichnis unter `TEST_DATA_PATH`), `E2ETestBase` startet die App einmal im Konstruktor mit `TEST_DATA_PATH`-Temp-Verzeichnis und löscht es in `Dispose`; ein Neustart bei gleichem Datenverzeichnis ist nicht vorgesehen.
- README Zeile 13 nennt veraltet „Entwicklungsschritt 1"; `docs/help/Navigation` beschreibt Optionen als Platzhalter. `docs/features.md` existiert nicht.
- Test-Ausgangszustand: Unit 16/16, Integration 3/3, E2E 4/4 erfolgreich, keine Fehlschläge, keine Lücken im ausgeführten Umfang; Nachweis in [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
