# Tasks: Einrichtungsdialog beim ersten Start (Spritsorte wählen)

Hinweis: Die Aufgaben sind thematisch geordnet, nicht in Umsetzungsreihenfolge. Die Reihenfolge steht in `docs/features/task/issue-17-a127d0ca006a4c01a6b6a497eb518e3a-einrichtungsdialog-beim-ersten/plan.md`. Es gibt keine Datenmigration (Entscheidung 5).

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Services | Interface `ISetupStatusService` mit `IsSetupRequiredAsync` anlegen | Offen | — |
| 2 | Services | `SetupStatusService` (Gateway über `IDbContextFactory<TankradarDbContext>`) implementieren | Offen | — |
| 3 | Services | `ISetupStatusService` / `SetupStatusService` in `MauiProgram` registrieren (Singleton) | Offen | — |
| 4 | Tests (Integration) | `SetupStatusServiceTests_FirstStart` schreiben (frische DB, nach Speichern, nach Neustart) | Offen | — |
| 5 | Texte | `SetupTexts` mit deutschen Texten anlegen | Offen | — |
| 6 | ViewModel | `SetupViewModel` anlegen (Auswahl, Standardvorbelegung, Speichern, Überspringen, Statusmeldung, Ereignis `Completed`) | Offen | — |
| 7 | ViewModel | Validierung „mindestens eine Sorte“ im `SetupViewModel` (Meldung im Bereich, kein Speichern) | Offen | — |
| 8 | ViewModel | `SetupViewModel` in `MauiProgram` als Transient registrieren | Offen | — |
| 9 | Tests (Unit) | Hilfsmethode `CreateSetupViewModel` in `Unit/Support` anlegen | Offen | — |
| 10 | Tests (Unit) | `SetupViewModelTests_Selection` schreiben (Standardvorbelegung, Umschalten) | Offen | — |
| 11 | Tests (Unit) | `SetupViewModelTests_Validation` schreiben (keine Sorte: Meldung, kein Speichern) | Offen | — |
| 12 | Tests (Unit) | `SetupViewModelTests_Save` schreiben (Speichern, Fehlerpfad bleibt offen) | Offen | — |
| 13 | Tests (Unit) | `SetupViewModelTests_Skip` schreiben (Überspringen speichert Standard) | Offen | — |
| 14 | UI | Setup-Overlay in `AppShell` (XAML): Überschrift, Hinweis, Umschalter, Schaltflächen `Setup.Save` und `Setup.Skip`, Statusmeldung | Offen | — |
| 15 | UI | Verschiebe-Schaltflächen im Overlay nicht anzeigen (Reihenfolge nur in Optionen) | Offen | — |
| 16 | UI | `SemanticProperties` (Hint, Description) für Umschalter, Schaltflächen und Hinweis; Fokusreihenfolge prüfen | Offen | — |
| 17 | Startlogik | `AppShell` erhält `ISetupStatusService` und `SetupViewModel` per Konstruktor-Injection (DI-Zugriff in `App.CreateWindow` prüfen) | Offen | — |
| 18 | Startlogik | Nach `IDatabaseInitializer.InitializeAsync` Status prüfen und Overlay anzeigen; Fehler protokollieren, Overlay nicht zeigen | Offen | — |
| 19 | Startlogik | Overlay nach `Completed` ausblenden | Offen | — |
| 20 | Tests (E2E) | Baseline-Lauf der E2E-Tests vor den Änderungen ausführen und Ergebnis sichern | Offen | — |
| 21 | Tests (E2E) | Gemeinsamer Helfer: Zeile `UserSettings` in der Testdatenbank anlegen, angewendet auf `SettingsE2ETestBase`, `TankbookE2ETestBase`, `OfflineE2ETests_Tankbook`, Backup-E2E-Tests mit frischer DB (Entscheidung 8) | Offen | — |
| 22 | Tests (E2E) | `SettingsE2ETests_Defaults.FirstStart_ShowsDefaultValues` anpassen (Helfer nutzen) | Offen | — |
| 23 | Tests (E2E) | `SetupE2ETests_FirstStart` schreiben (Erscheinen bei frischer DB, Standardauswahl) | Offen | — |
| 24 | Tests (E2E) | `SetupE2ETests_Save` schreiben (Auswahl speichern, Optionen zeigen Auswahl) | Offen | — |
| 25 | Tests (E2E) | `SetupE2ETests_Restart` schreiben (nach Neustart kein Overlay) | Offen | — |
| 26 | Tests (E2E) | `SetupE2ETests_Skip` schreiben (Überspringen, Optionen zeigen alle Sorten) | Offen | — |
| 27 | Tests (E2E) | `SetupE2ETests_Validation` schreiben (keine Sorte: Meldung, Overlay bleibt) | Offen | — |
| 28 | Tests (E2E) | `SetupE2ETests_OptionsAfterSetup` schreiben (Änderung in Optionen wirkt, kein erneutes Overlay) | Offen | — |
| 29 | Tests (E2E) | `SetupE2ETests_Backup` schreiben (Wiederherstellung mit Zeile: kein Overlay beim nächsten Start) (Entscheidung 6) | Offen | — |
| 30 | Dokumentation | ADR `docs/adr/0017-einrichtungsdialog.md` anlegen (Overlay in der App statt Betriebssystem-Dialog; Abweichung vom Begriff „modal“; Bezug zu ADR 0006 und 0008) (Entscheidung 7) | Offen | — |
| 31 | Dokumentation | `docs/help/Einstellungen/ablauf-anwender.md` um Abschnitt „Einrichtung beim ersten Start“ ergänzen (inkl. Verhalten nach Update ohne Zeile, Entscheidung 5) | Offen | — |
| 32 | Dokumentation | `docs/help/Einstellungen/ablauf-technisch.md` um Abschnitt Erststart-Erkennung und Wiederherstellung ergänzen (inkl. bewusst hingenommenes Verhalten bei Bestandsinstallationen) | Offen | — |
| 33 | Dokumentation | `docs/help/Einstellungen/beschreibung.md` um die Einrichtung ergänzen; Verweis in `docs/help/index.md` | Offen | — |
| 34 | Gesamtprüfung | Build (0 Warnungen), Unit- und Integrationstests vollständig grün | Offen | — |
| 35 | Gesamtprüfung | E2E-Lauf vollständig; Vergleich mit Baseline (Aufgabe 20) | Offen | — |
