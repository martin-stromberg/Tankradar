# Bestandsaufnahme: Einrichtungsdialog beim ersten Start (Spritsorte wählen)

Analysiert wurden die Einstellungs-, Daten- und Startkomponenten der App sowie die bestehenden Tests, bezogen auf die Anforderung „Einrichtungsdialog beim ersten Start“ (Issue #17, Branch `task/issue-17-a127d0ca006a4c01a6b6a497eb518e3a-einrichtungsdialog-beim-ersten`). Es wurde kein Code geändert.

## Zusammenfassung

**Vorhanden:**

- Die Spritsorten-Einstellung existiert vollständig: `AppSettings.FuelTypes` als geordnete Liste aus `FuelTypeSelection` über `FuelType` (`SuperE5`, `SuperE10`, `Diesel`). Die Regel „mindestens eine Sorte ausgewählt“ ist in `AppSettings.Validate()` umgesetzt, und `AppSettings.CreateDefault()` wählt alle Sorten in der Reihenfolge E5, E10, Diesel aus.
- Speichern und Laden laufen über `ISettingsService` (`SettingsService.SaveAsync` / `LoadAsync`), in die Tabellen `UserSettings` und `FuelTypeSettings`. Der Bereich „Spritsorten“ in den Optionen (`SettingsViewModel`) nutzt denselben Pfad. Eine zweite Quelle existiert nicht.
- Die Optionen-Seite besitzt bereits Umschalter und Verschiebe-Schaltflächen je Spritsorte mit `AutomationId`s (`Settings.FuelType.{Name}.Switch`, `.MoveUp`, `.MoveDown`).
- Die Datenbank wird beim Start über `DatabaseInitializer` (EF-Core-Migrationen) angelegt. Die App-Initialisierung läuft im Hintergrund in `App`.
- Die Abdeckung ist breit: 1940 Unit-Tests und 291 Integrationstests bestehen im Ausgangslauf. Dazu gibt es Neustart-Tests für die Einstellungen und Upgrade-Tests der Datenbank.

**Fehlt bzw. offen:**

- Es gibt kein Erststart-Merkmal. Ein Suchtreffer für `FirstRun`, `Erststart`, `Onboarding` oder `IsFirstStart` existiert nicht.
- `SettingsService.LoadAsync` ersetzt fehlende Werte durch Standardwerte. Ob eine Einstellungszeile existiert, ist über die öffentliche Schnittstelle nicht sichtbar. Die Zeile `UserSettings` entsteht erst durch `SaveAsync`, und Migrationen legen keine Startdaten an. Eine Erststart-Erkennung über die fehlende Zeile ist daher technisch möglich, muss aber in den Dienst aufgenommen werden.
- Es gibt keine Startlogik für einen Dialog. `AppShell` ist eine reine TabBar mit Routen, und `App.CreateWindow` erzeugt das Fenster sofort mit `AppShell`.
- Es gibt kein `SetupViewModel`, keine Setup-Seite und keine Setup-Tests.
- Die Anforderung nennt ein Dialogmuster. Die ADRs 0006 und 0008 legen fest, dass Formulare und Rückfragen als Bereiche in der Seite erscheinen und nicht als Dialoge des Betriebssystems. Ein modaler Dialog muss deshalb ein bestehendes Muster ergänzen oder bewusst davon abweichen.
- Der bestehende E2E-Test `SettingsE2ETests_Defaults.FirstStart_ShowsDefaultValues` setzt voraus, dass beim frischen Start die Optionen ohne Dialog erreichbar sind. Der Test wurde in diesem Lauf nicht ausgeführt (siehe Tests).
- Die offenen Fragen aus der Anforderung (Mehrfachauswahl, Laden, Überspringen, Erststart-Merkmal, Sicherung) sind nicht Teil dieser Bestandsaufnahme und bleiben zu klären.

**Test-Ausgangszustand:** Restore, Build (0 Warnungen, 0 Fehler), Unit (1940 bestanden), Integration (291 bestanden), Node-Skripttests (32 bestanden), Python-Tests (14 bestanden) und Workflow-Validierung (OK) liefen grün. Keine Testfehler sind nachgewiesen. Nicht ausgeführt wurden die E2E-Tests (interaktive Desktop-Sitzung erforderlich), die iOS-Kompilierung (kein Mac) und die Formatprüfung. Details und Nachweise: [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md): `AppSettings`, `FuelTypeSelection`, `UserSettingsEntity`, `FuelTypeSettingEntity`, Tabellen und Migrationen
- [Logik](inventory/logic.md): `SettingsService`, `SettingsViewModel`, `FuelTypeItemViewModel`, `DatabaseInitializer`, `App`, `AppShell`, DI-Registrierung
- [Enums](inventory/enums.md): `FuelType`, `GpsUsage`, `ResultView`, `ResultSortOrder`
- [Interfaces](inventory/interfaces.md): `ISettingsService`, `IDatabaseInitializer`
- [Tests](inventory/tests.md): Test-Ausgangszustand, Testläufe, Testlücken, bestehende Testklassen und Hilfsmethoden
