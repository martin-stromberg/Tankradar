# Bestandsaufnahme: Tankbuch öffnet mit Tank- und Ladevorgängen

Analysiert wurden Shell-Navigation, ViewModels, Dienste, Datenmodell und Tests des Tankbuchs (Issue #20), bezogen auf die Anforderung in [requirement.md](requirement.md). Die Bestandsaufnahme beschreibt nur den vorhandenen Stand; es gibt keine Änderungen am Code.

## Zusammenfassung

- **Einstieg heute:** Der Tab „Tankbuch“ (`ShellContent` Route `tankbook`, `TankbookPage`) zeigt die Fahrzeugliste. Die Vorgänge sind nur über die Schaltfläche „Tank- und Ladevorgänge“ einer Fahrzeugkarte erreichbar (Shell-Route `tankbookentries`, `TankbookEntriesPage`). Bestätigt durch `AppShell.xaml` und `AppShell.xaml.cs`.
- **Navigation:** `ITankbookNavigator` / `ShellTankbookNavigator` öffnen und schließen die Vorgangsseite über `Shell.Current.GoToAsync` mit Parameter `vehicleId`. `CloseAsync` arbeitet mit dem Navigationsstapel des Reiters. Das bestätigt die Annahme der Anforderung zum Stapel.
- **Vorhanden, für die Anforderung nutzbar:** `TankbookEntriesViewModel` implementiert `IQueryAttributable`, hat `Show(long)` und `LoadAsync`, lädt Fahrzeug, Vorgänge, Auswertung und Export pro Fahrzeug. Das ist die Grundlage für den Kopf mit Fahrzeugwechsel.
- **Vorhanden, für die Anforderung nutzbar:** `IVehicleService` mit `GetVehiclesAsync`, `GetVehicleAsync`, `CountEntriesAsync`, `DeleteVehicleAsync(id, expectedEntryCount)`. Die Verwaltung mit Rückfrage und Neuzählung ist in `TankbookViewModel` vorhanden.
- **Vorhanden:** `IScreenReaderAnnouncer` (Ansage) und `IRestoreNotifier.Restored` (Neuladen nach Wiederherstellung) werden bereits in beiden ViewModels genutzt.
- **Vorhanden:** Hinweistexte für Sprachausgabe (`VehicleHint`, `FormatVehicleHint` → `Fahrzeug „Golf“`). Die Schaltfläche „Tank- und Ladevorgänge“ hat den Hinweis `VehicleHint`.
- **Fehlt (offensichtlich):**
  - Kein Speicher für die „zuletzt gewählte“ Fahrzeugkennung. Im Quellcode gibt es keine Verwendung von `Preferences`, keine Spalte in `UserSettingsEntity` für eine Fahrzeugauswahl und keinen Dienst dafür.
  - Kein Fahrzeugwechsel-Element im Kopf der Vorgangsseite.
  - Kein Einstieg, bei dem der Tab direkt die Vorgänge zeigt. `TankbookPage` zeigt immer die Fahrzeugliste.
  - Kein Leerzustand mit Schaltfläche „Fahrzeug hinzufügen“ im Kopf der Vorgangsseite. Der bestehende Text `NoVehicles` gilt nur für die Fahrzeugliste.
  - Keine Ansage des gewählten Fahrzeugs beim Öffnen des Tabs oder nach einem Wechsel.
  - Keine Tests für die neuen Verhalten (siehe Testlücken).
- **Dokumentation, die angepasst werden muss:** `docs/help/Tankbuch/beschreibung.md`, `ablauf-anwender.md`, `ablauf-technisch.md` beschreiben die Fahrzeugkarte als Einstieg. `docs/adr/0009-tankbook-entries.md` legt „Eigene Seite je Fahrzeug … über die Schaltfläche auf der Fahrzeugkarte“ fest.
- **Test-Ausgangszustand:** Alle Läufe grün. Unit 1949/1949, Integration 294/294, E2E (FlaUI) 156/156, Pipeline-Skripte (`npm test`) 32/32, jeweils Exit-Code 0. Keine übersprungenen Tests. Keine bekannten Fehlschläge. Nachweis und Befehle: [inventory/tests.md](inventory/tests.md).
- **Verbleibende Testlücken:** Die neuen Verhalten (Fahrzeugwechsel, Ansage, Leerzustand, Rückfall nach Löschen, Speicherung der Auswahl) sind in keinem Test abgedeckt. Zwei bestehende Tests setzen den heutigen Einstieg über die Fahrzeugkarte voraus: `TankbookViewModelTests_Navigation.OpenEntries_NavigatesWithVehicleId` und `TankbookEntriesE2ETests_Flow` (Schaltfläche `Tankbook.Vehicle.Entries`). Außerdem nutzt eine größere Gruppe der E2E-Tests über `TankbookE2EDriver.OpenEntries` denselben Einstieg.

## Details

Für jeden analysierten Bereich ein eigenes Dokument:

- [Datenmodell](inventory/models.md): `Vehicle`, `VehicleDeleteResult`, `UserSettingsEntity`.
- [Logik](inventory/logic.md): `TankbookViewModel`, `TankbookEntriesViewModel`, `VehicleItemViewModel`, `ShellTankbookNavigator`, `AppShell`, `SetupStatusService`.
- [Enums](inventory/enums.md): `VehicleFuelKind`.
- [Interfaces](inventory/interfaces.md): `ITankbookNavigator`, `IVehicleService`, `IScreenReaderAnnouncer`, `ISettingsService`, `ISetupStatusService`, `IRestoreNotifier`.
- [Tests](inventory/tests.md): Test-Ausgangszustand, Testläufe, Testklassen und Hilfsmethoden.
- Rohdaten der Testläufe: [run-full.log](inventory/test-results/run-full.log), [Unit-TRX](inventory/test-results/test-results-unit.trx), [Integration-TRX](inventory/test-results/test-results-integration.trx), [E2E-TRX](inventory/test-results/test-results-e2e.trx).

Hinweis zur Genauigkeit: Die Hilfsklassen `BackupTestEnvironment` und `TankbookTestDatabase` sind nach Name und Fundstelle eingeordnet, nicht im Detail gelesen.
