# Anforderung: Tankbuch öffnet mit Tank- und Ladevorgängen

Quelle: Rückmeldung nach TestFlight `v0.1.0-rc.13` (Projekt #1), Issue #20.

## Fachliche Zusammenfassung

Der Tab „Tankbuch“ (`ShellContent` mit `Route="tankbook"`, Seite `TankbookPage`) öffnet heute die Fahrzeugliste; die Tank- und Ladevorgänge sind erst über die Schaltfläche „Tank- und Ladevorgänge“ einer Fahrzeugkarte erreichbar (Shell-Route `tankbookentries`, Parameter `vehicleId`, `TankbookEntriesPage`). Die Anforderung verlagert den Einstieg: Der Tab zeigt direkt die Vorgänge eines Fahrzeugs (`TankbookEntriesViewModel`: Kopf, Formular, Auswertung, Export). Der Fahrzeugwechsel wird zu einer unauffälligen Auswahl im Kopf der Seite, die ein bestehendes Fahrzeug über seinen Namen wählt. Die Fahrzeugverwaltung (anlegen, bearbeiten, löschen) bleibt von dort erreichbar. Ohne Fahrzeug erscheint ein Hinweis mit direktem Weg zum Anlegen.

## Betroffene Klassen und Komponenten

Bestehende Artefakte (Stand Code-Lesung, keine Bestandsaufnahme im Detail):

- **Shell und Navigation**
  - `AppShell.xaml`: Tab „Tankbuch“ mit `ContentTemplate="{DataTemplate views:TankbookPage}"`. Voraussichtlich Änderung des Tab-Inhalts.
  - `AppShell.xaml.cs`: Registrierung `Routing.RegisterRoute(ShellTankbookNavigator.EntriesRoute, typeof(TankbookEntriesPage))`. Die Route entfällt oder bleibt nur für Sonderfälle.
  - `Services/Navigation/ITankbookNavigator.cs`, `ShellTankbookNavigator.cs`: `OpenEntriesAsync` (GoTo mit `vehicleId`) und `CloseAsync` (entfernt Seiten aus dem Navigationsstapel des Reiters). Beide setzen einen Stapel voraus, der mit der Fahrzeugliste beginnt; das ändert sich mit dem neuen Einstieg.
- **ViewModels**
  - `ViewModels/TankbookViewModel.cs`: Fahrzeugliste, Formular, Löschrückfrage, `OpenEntries`. Wird zur Fahrzeugverwaltung umgewidmet oder in eine Verwaltungsansicht überführt.
  - `ViewModels/TankbookEntriesViewModel.cs`: `IQueryAttributable` (`vehicleId`), Kopf mit `VehicleName`/`VehicleDetails`, `Form`, `Statistics`, `Export`. Braucht einen Fahrzeugwechsel und die Auswahl des aktuellen Fahrzeugs ohne Navigationsparameter.
  - `ViewModels/VehicleItemViewModel.cs`: Schaltfläche „Tank- und Ladevorgänge“ (`OpenEntriesCommand`) entfällt im Einstieg; Bearbeiten und Löschen bleiben.
  - `ViewModels/TankbookStatisticsViewModel.cs`, `TankbookExportViewModel.cs`: bleiben unverändert, werden aber pro Fahrzeug neu geladen, wenn das Fahrzeug wechselt.
- **Services**
  - `Services/Tankbook/IVehicleService.cs` (`GetVehiclesAsync`, `GetVehicleAsync`, `CreateVehicleAsync`, `UpdateVehicleAsync`, `CountEntriesAsync`, `DeleteVehicleAsync`): unverändert nutzbar. Die Auswahl des „zuletzt genutzten“ Fahrzeugs braucht einen neuen Speicher (siehe Implementierungsansatz).
  - `Services/Tankbook/ITankbookEntryService.cs`, `ITankbookStatisticsService.cs`: unverändert.
  - Sprachausgabe: `IScreenReaderAnnouncer` (vorhanden) für die Ansage des gewählten Fahrzeugs.
- **Daten**
  - `Data/UserSettingsEntity.cs` (Tabelle `UserSettings`, Singleton-Zeile `Id = 1`): Kandidat für die Speicherung der Auswahl, sofern eine Schemaänderung akzeptiert wird. Ohne Schemaänderung ist `Preferences` eine Alternative.
  - `Data/VehicleEntity.cs`, `Data/TankbookEntryEntity.cs`: unverändert.
- **UI / XAML**
  - `Views/TankbookPage.xaml` (+ `.xaml.cs`): Fahrzeugliste, wird zur Fahrzeugverwaltung umgewidmet oder ersetzt.
  - `Views/TankbookEntriesPage.xaml` (+ `.xaml.cs`): Kopf mit Fahrzeugname (`TankbookEntries.Vehicle`) und Details; bekommt die Auswahl „Fahrzeug wechseln“ und den Einstieg zur Verwaltung. Die Bereiche Formular, Auswertung und Export bleiben.
  - Leerzustand „ohne Fahrzeug“: neuer Hinweis mit Schaltfläche „Fahrzeug hinzufügen“ (Text ergänzt in `TankbookTexts`, vorhandener Text `NoVehicles` ist ein Vorbild).
- **Texte**
  - `Resources/Texts/TankbookTexts.cs` (`PageTitle = "Tankbuch"`, `NoVehicles`, `FormatVehicleHint`, `FormatVehicleDetails`), `Resources/Texts/TankbookEntryTexts.cs` (`PageTitle`, `Intro`, `AddEntry`).
- **Dokumentation**
  - `docs/help/Tankbuch/beschreibung.md`, `ablauf-anwender.md`, `ablauf-technisch.md`: beschreiben heute die Fahrzeugliste als Einstieg („Unten den Bereich Tankbuch öffnen … Fahrzeug hinzufügen“). Müssen angepasst werden.
  - `docs/adr/0009-tankbook-entries.md`: Entscheidung „Eigene Seite je Fahrzeug … über die Schaltfläche auf der Fahrzeugkarte“ wird durch die Anforderung teilweise überholt. Neue ADR (voraussichtlich `0018`, Annahme) empfohlen.
- **Tests**
  - Unit: `TankbookViewModelTests_Load.cs`, `TankbookViewModelTests_Navigation.cs`, `TankbookViewModelTests_Form.cs`, `TankbookViewModelTests_Delete.cs`, `TankbookEntriesViewModelTests_Load.cs`, `TankbookEntriesViewModelTestBase.cs`, `ViewModelTests_PageTitles.cs`, `PageTitleRedundancyTests.cs`, `TankbookTextsTests_Labels.cs`, `TankbookTextsTests_Messages.cs`.
  - E2E (FlaUI): `TankbookE2ETests_Flow.cs`, `TankbookE2ETests_Validation.cs`, `TankbookE2ETests_DeleteWithEntries.cs`, `TankbookEntriesE2ETests_*.cs`, `TankbookStatisticsE2ETests_*.cs`, `TankbookExportE2ETests_*.cs`, `NavigationE2ETests.cs`, `OfflineE2ETests_Tankbook.cs`. Die Einstiege „Tank- und Ladevorgänge“ auf der Fahrzeugkarte müssen angepasst werden.
  - Neu: Tests für Fahrzeugwechsel, Ansage des gewählten Fahrzeugs, Leerzustand, Rückfall auf ein anderes Fahrzeug nach Löschen des gewählten, Speicherung der Auswahl.

## Implementierungsansatz

- **Einstieg:** Der Tab „Tankbuch“ zeigt beim Öffnen die Vorgänge eines Fahrzeugs. Das Fahrzeug ist das zuletzt gewählte, sofern es noch existiert; sonst das einzige Fahrzeug; sonst das erste nach Name (Annahme, siehe unten). Technisch bietet sich an, den Inhalt des Tabs aus `TankbookEntriesViewModel` aufzubauen, statt eine zweite Seite über `Routing` zu öffnen. Dann entfällt der Navigationsstapel als Fehlerquelle für `ITankbookNavigator.CloseAsync`.
- **Fahrzeugwechsel:** Eine Auswahl im Kopf (z. B. Symbolschaltfläche mit Namen, öffnet eine Liste bestehender Fahrzeuge über `IVehicleService.GetVehiclesAsync`). Die Auswahl erfolgt über den Namen; interne Kennungen werden nie angezeigt (wie ADR 0008). Der Wechsel lädt Vorgänge, Auswertung und Export des gewählten Fahrzeugs neu, ohne eine neue Seite auf den Stapel zu legen.
- **Fahrzeugverwaltung:** Aus der Auswahl heraus erreichbar: „Fahrzeuge verwalten“ öffnet die bestehende Liste mit Anlegen, Bearbeiten, Löschen. Das Löschen behält die Rückfrage mit Zählung und die Neuzählung nach ADR 0008/0009 (`DeleteVehicleAsync(vehicleId, expectedEntryCount)`).
- **Merken der Auswahl:** Zu entscheiden zwischen
  - (a) neuer Spalte in `UserSettings` plus Migration (Schemaänderung, `Data/Migrations/`; ADR 0017 hat die Zeile bereits als Erststart-Kennzeichen genutzt, eine Spalte wäre additiv), oder
  - (b) `Preferences` (gerätelokal, kein Schema). Bei Sicherung und Wiederherstellung (ADR 0011) ist die Kennung dann nicht enthalten; der Rückfall nach Name/Kennung muss fehlende oder gelöschte Fahrzeuge abfangen.
  Vorschlag: (b), weil es nur eine Komfortfunktion ist; Speicherung nur der Kennung, keine Namen.
- **Ohne Fahrzeug:** Der Kopf zeigt den Hinweis (`NoVehicles`-Text, angepasst) mit der Schaltfläche „Fahrzeug hinzufügen“, die das Anlegen-Formular öffnet. Formular, Auswertung und Export sind dann ausgeblendet.
- **Nach Löschen des gewählten Fahrzeugs:** automatischer Wechsel auf das nächste Fahrzeug nach Name, sonst Leerzustand.
- **Barrierefreiheit (WCAG 2.5.3, ADR 0009):** Sichtbarer Text der Auswahl ist der Fahrzeugname; Kontext in `SemanticProperties.Hint` (z. B. „Fahrzeug „Golf“ auswählen“). Nach dem Wechsel und beim Öffnen des Tabs wird der Name des aktuell gewählten Fahrzeugs über `IScreenReaderAnnouncer` angesagt. Zusätzlich trägt der Kopf den Namen als Beschriftung für Hilfstechnik.
- **Datenschutz:** keine Netzwerkzugriffe, Protokolle ohne Namen oder Werte (wie bisher).
- **Abhängigkeiten:** Die Auswertung (`ITankbookStatisticsService`) und der Export (`ITankbookExportService`) sind bereits fahrzeugbezogen; sie müssen nur mit dem gewählten `vehicleId` aufgerufen werden.

## Konfiguration

- Keine Konfiguration durch den Anwender nötig; das Verhalten ist fest.
- Die gemerkte Auswahl ist eine **benutzerspezifische, gerätelokale** Einstellung (Ebene: pro Gerät, nicht pro Datensatz). Speicherort siehe Implementierungsansatz.

## Annahmen

- A1: „Die Vorgänge des zuletzt genutzten Fahrzeugs“ bedeutet: das Fahrzeug, dessen Vorgänge der Anwender zuletzt im Tab geöffnet bzw. gewechselt hat. Die Auswahl wird gemerkt.
- A2: Der Tab bleibt Tab „Tankbuch“ (Titel unverändert). Die Fahrzeugliste ist nicht mehr der Einstieg, bleibt aber als Verwaltung erreichbar.
- A3: Die Fahrzeugkarte in der Liste verliert die Schaltfläche „Tank- und Ladevorgänge“ im Einstieg; das Öffnen eines Fahrzeugs erfolgt über die Auswahl im Kopf. Sollte die Schaltfläche in der Verwaltung bleiben, ist das ebenfalls konform.
- A4: „Sprachausgabe nennt das aktuell gewählte Fahrzeug“ wird als Ansage beim Öffnen des Tabs und nach jedem Wechsel umgesetzt, nicht als Dauerhinweis.
- A5: Auswertung und Export arbeiten unverändert pro Fahrzeug (kein Gesamtexport über alle Fahrzeuge).
- A6: Eine neue ADR (voraussichtlich `0018`) dokumentiert die Änderung gegenüber ADR 0009 („eigene Seite je Fahrzeug“).

## Offene Fragen

1. Speicherort der Auswahl: `UserSettings`-Spalte mit Migration oder `Preferences` (gerätelokal)? Vorschlag: `Preferences`, nur Kennung.
2. Verhalten bei Wiederherstellung einer Sicherung: Soll die zuletzt gewählte Kennung nach einer Wiederherstellung zurückgesetzt werden (Rückfall auf das erste Fahrzeug nach Name)? Vorschlag: ja.
3. Soll die Fahrzeugverwaltung eine eigene Seite (Route) bleiben oder als Bereich im Tab erscheinen? Vorschlag: Bereich im Tab, analog zu ADR 0006/0008 (keine eigene Route).
4. Ist die Ansage beim Öffnen des Tabs gewünscht oder nur nach einem Wechsel? (Annahme A4: beides.)
5. Bei nur einem Fahrzeug: Ist der Wechsel-Einstieg trotzdem sichtbar, oder ausgeblendet? Vorschlag: sichtbar, damit die Verwaltung erreichbar bleibt.
6. Seitentitel: Bleibt die Titelzeile „Tankbuch“ im Tab, oder erscheint der Fahrzeugname als Titel? Achtung: `PageTitleRedundancyTests` prüft Titelredundanz mit dem Shell-Titel.
