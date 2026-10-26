# Umsetzungsplan: Einrichtungsdialog beim ersten Start (Spritsorte wählen)

## Übersicht

Beim ersten Start nach einer Installation erscheint eine Einrichtungsansicht, in der der Anwender eine oder mehrere Spritsorten (`FuelType`: `SuperE5`, `SuperE10`, `Diesel`) auswählt. Die Auswahl wird über dieselbe Einstellung gespeichert wie der Bereich „Spritsorten“ in den Optionen (`AppSettings.FuelTypes` über `ISettingsService.SaveAsync`). Betroffen sind die Erststart-Erkennung über die bestehende Zeile `UserSettings` (ohne Schema-Änderung), ein neuer Erststart-Dienst, ein neues `SetupViewModel`, eine Einrichtungsansicht als Overlay in `AppShell` mit Startlogik, die E2E-Testbasis, ein neuer ADR und die Dokumentation.

Bewusste Abweichung von der Anforderung: Die Einrichtung erscheint als Overlay innerhalb der App, nicht als Dialog des Betriebssystems und nicht als eigene Route. Das folgt ADR 0006 und ADR 0008 (Formulare und Rückfragen als Bereiche in der Seite). Die Begründung wird im neuen ADR 0017 festgehalten.

## Getroffene Entscheidungen

Die folgenden Punkte wurden vom Anwender bestätigt und sind in diesem Plan umgesetzt:

1. **Mehrfachauswahl:** Mehrere Spritsorten sind wählbar, mindestens eine muss ausgewählt sein. Standardvorbelegung: alle drei Sorten.
2. **Elektro/Laden:** Die Einrichtung bietet Elektro oder Laden nicht an (ADR 0001, ADR 0008).
3. **Überspringen:** Erlaubt. Es speichert `AppSettings.CreateDefault()` (alle Sorten). Danach erscheint die Einrichtung nicht mehr.
4. **Erststart-Erkennung:** Fehlen der Zeile `UserSettings`. Ein eigenes Kennzeichen wird nicht eingeführt.
5. **Bestandsinstallationen ohne Zeile `UserSettings`:** Diese Installationen (nur Update, nie in den Optionen gespeichert) sehen die Einrichtung einmalig nach dem Update. Dieses Verhalten wird hingenommen. Es gibt keine Datenmigration und keine Schema-Änderung. Das Verhalten wird in der Dokumentation (technisch und anwenderseitig) bewusst beschrieben.
6. **Wiederherstellung:** Enthält die Sicherung eine Zeile `UserSettings`, erscheint nach der Wiederherstellung beim nächsten App-Start keine Einrichtung. Fehlt die Zeile, erscheint die Einrichtung beim nächsten App-Start, nicht unmittelbar nach der Wiederherstellung. Es gibt keinen Sonderpfad im Code.
7. **Darstellung:** Overlay in `AppShell` gemäß ADR 0006 und ADR 0008, keine Betriebssystem-Dialoge. Begründung im neuen ADR 0017.
8. **Testbasis:** Bestehende E2E-Tests erhalten über einen gemeinsamen Helfer eine bereits eingerichtete Testdatenbank. Die Einrichtungsansicht wird gezielt in den neuen `SetupE2ETests_*` getestet.
9. **Dokumentationsort:** `docs/help/Einstellungen/ablauf-anwender.md` und `docs/help/Einstellungen/ablauf-technisch.md` (beide Dateien existieren bereits und werden um den Abschnitt Einrichtung ergänzt), zusätzlich Verweis in `docs/help/index.md`. Es wird keine Übersichtsdatei `docs/features.md` angelegt.
10. **Reihenfolge:** In der Einrichtung gibt es keine Verschiebe-Schaltflächen. Die Reihenfolge ist Standard (E5, E10, Diesel) und wird in den Optionen geändert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Erststart-Erkennung | Gateway `SetupStatusService` mit Schnittstelle `ISetupStatusService` (Methode `IsSetupRequiredAsync`). Die Abfrage prüft über `IDbContextFactory<TankradarDbContext>`, ob die Zeile `UserSettingsEntity.SingletonId` existiert. | `SettingsService.LoadAsync` ersetzt fehlende Werte durch Standardwerte und kann den Erststart nicht melden. Ein eigener Dienst hält `ISettingsService` unverändert; `FakeSettingsService` und die bestehenden `SettingsViewModel`-Tests bleiben gültig. Kein Schema-Wechsel nötig (Entscheidung 4). |
| Abschluss der Einrichtung | Speichern und Überspringen schreiben über `ISettingsService.SaveAsync`. Überspringen speichert `AppSettings.CreateDefault()`. Die Zeile ist danach vorhanden, daher kein erneutes Erscheinen. | Eine Quelle der Wahrheit. Kein zusätzliches Kennzeichen, daher keine Migration (Entscheidungen 3 und 4). |
| Präsentation / ViewModel | Presentation Model `SetupViewModel` (eigenes ViewModel, Transient). Es hält die Auswahl als Liste von `FuelTypeItemViewModel` und baut daraus `AppSettings`. | Entspricht dem bestehenden MVVM-Muster der Optionen. Die Logik ist ohne UI testbar. |
| Auswahl-Steuerelemente | `FuelTypeItemViewModel` und das bestehende Umschalter-Muster der Optionen-Seite (`Settings.FuelType.{Name}.Switch`) wiederverwenden. Verschiebe-Schaltflächen werden in der Einrichtung nicht angezeigt. Die Reihenfolge ist Standard (E5, E10, Diesel) und wird in den Optionen geändert (Entscheidung 10). | Wiederverwendung statt neuer Komponente. Die Einrichtung soll schlank bleiben. |
| Validierung | `AppSettings.Validate()` wiederverwenden. Das ViewModel prüft vor dem Speichern „mindestens eine Sorte“ und zeigt die Meldung im Bereich, Muster wie `DeselectLastFuel_IsRevertedWithMessageAndNotSaved` in `SettingsViewModel` (Entscheidung 1). | Keine zweite Regel. Die Meldung bleibt in der Einrichtungsansicht sichtbar. |
| Darstellung | Einrichtungsbereich als Overlay über der Shell (`AppShell`), ohne eigene Route und ohne Betriebssystem-Dialog. Gestaltung Ebene 3 (Modal) gemäß `docs/design-system.md`. Begründung der Abweichung vom Begriff „modal“ im neuen ADR 0017 (Entscheidung 7). | ADR 0006 und ADR 0008 schreiben Bereiche in der Seite vor. Ein Overlay blockiert die TabBar, ohne den Navigationsstack zu verändern. |
| Startlogik | `AppShell` erhält `ISetupStatusService` per Konstruktor-Injection. Nach `IDatabaseInitializer.InitializeAsync` prüft die Shell den Status und zeigt bei Bedarf das Overlay. Die Initialisierung in `App` bleibt Fire-and-forget; die Prüfung wartet aber auf den Abschluss der Initialisierung. | Vermeidet ein Rennen mit der Migration beim Start. Die Konstruktor-Injection muss bei Umsetzung geprüft werden, da `AppShell` heute parameterlos erzeugt wird (`App.CreateWindow`). |
| Wiederherstellung | Kein Sonderpfad im Code. `BackupViewModel.Restored` lädt nur die Einstellungen neu. Enthält die Sicherung die Zeile `UserSettings`, bleibt die Einrichtung aus; fehlt sie, erscheint sie beim nächsten Start (Entscheidung 6). | Der Einrichtungsstatus wird nicht gesondert verwaltet. |
| E2E-Testbasis | Gemeinsamer Helfer legt die Zeile `UserSettings` in der Testdatenbank an; bestehende Tests nutzen ihn. Die Einrichtungsansicht wird gezielt in `SetupE2ETests_*` getestet (Entscheidung 8). | Bestehende E2E-Tests bleiben inhaltlich unverändert, ohne das Overlay zu treffen. |
| Texte | Neue Textklasse `SetupTexts` (Muster `SettingsTexts`, `BackupTexts`). Deutsch. | Konsistent mit der bestehenden Textkonvention. |

## Programmabläufe

### Erststart-Erkennung

1. `App` startet die Initialisierung (`IDatabaseInitializer.InitializeAsync`) im Hintergrund.
2. `AppShell` wartet auf den Abschluss dieser Initialisierung.
3. `AppShell` ruft `ISetupStatusService.IsSetupRequiredAsync()` auf.
4. Ist keine Zeile in `UserSettings` vorhanden, liefert der Dienst `true`, sonst `false`.
5. Bei `true` wird das Einrichtungs-Overlay gezeigt; bei `false` bleibt die Shell unverändert.

Beteiligte Klassen/Komponenten: `App`, `AppShell`, `ISetupStatusService`, `SetupStatusService`, `IDatabaseInitializer`, `TankradarDbContext`.

### Einrichtung speichern

1. Der Anwender wählt eine oder mehrere Spritsorten (Umschalter, Standard: alle ausgewählt).
2. Der Anwender betätigt „Speichern“ (`Setup.Save`).
3. `SetupViewModel` prüft „mindestens eine Sorte“. Ist keine ausgewählt, erscheint die Meldung im Bereich, nichts wird gespeichert.
4. `SetupViewModel` baut `AppSettings` aus `CreateDefault()` mit der Auswahl als `FuelTypeSelection`-Liste (Reihenfolge Standard).
5. `ISettingsService.SaveAsync` speichert (validiert durch `Validate`).
6. Bei Erfolg löst `SetupViewModel` `Completed` aus, `AppShell` blendet das Overlay aus. Bei Fehler bleibt es sichtbar, und eine Statusmeldung erscheint.

Beteiligte Klassen/Komponenten: `SetupViewModel`, `FuelTypeItemViewModel`, `AppSettings`, `ISettingsService`, `AppShell`.

### Einrichtung überspringen

1. Der Anwender betätigt „Überspringen“ (`Setup.Skip`).
2. `SetupViewModel` ruft `ISettingsService.SaveAsync(AppSettings.CreateDefault())` auf.
3. Bei Erfolg löst `SetupViewModel` `Completed` aus, `AppShell` blendet das Overlay aus. Die Zeile ist danach vorhanden, daher erscheint das Overlay beim nächsten Start nicht erneut.

Beteiligte Klassen/Komponenten: `SetupViewModel`, `ISettingsService`, `AppShell`.

### Erneutes Erscheinen verhindern / Optionen-Änderung nach Einrichtung

1. Nach dem Speichern oder Überspringen ändert sich die Auswahl über die Optionen (`SettingsViewModel.SaveAsync`) unverändert.
2. Beim nächsten Start liefert `ISetupStatusService.IsSetupRequiredAsync()` `false`, weil die Zeile existiert.

Beteiligte Klassen/Komponenten: `SettingsViewModel` (unverändert), `SetupStatusService`.

### Wiederherstellung einer Sicherung

1. `BackupViewModel.Restored` lädt die Einstellungen neu (unverändert). Der Einrichtungsstatus wird nicht gesondert geprüft oder gesetzt.
2. Enthält die wiederhergestellte Datenbank die Zeile `UserSettings`, erscheint beim nächsten App-Start keine Einrichtung.
3. Fehlt die Zeile, erscheint die Einrichtung beim nächsten App-Start (nicht sofort nach der Wiederherstellung).

Beteiligte Klassen/Komponenten: `BackupViewModel`, `BackupService` / `RestoreSwap` (unverändert), `SetupStatusService`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `ISetupStatusService` | Interface | Schnittstelle „Einrichtung nötig?“ mit `Task<bool> IsSetupRequiredAsync(CancellationToken)` |
| `SetupStatusService` | Klasse (Gateway) | Prüft über `IDbContextFactory<TankradarDbContext>`, ob die Zeile `UserSettingsEntity.SingletonId` existiert |
| `SetupViewModel` | ViewModel (Presentation Model) | Auswahl der Spritsorten, Validierung, Speichern, Überspringen, Statusmeldung, Ereignis `Completed` |
| `SetupTexts` | Textklasse | Deutsche Texte der Einrichtungsansicht (Überschrift, Hinweis, Schaltflächen, Fehlermeldung, Labels) |
| `SetupStatusServiceTests_FirstStart` | Testklasse (Integration) | Prüft die Erkennung auf einer echten SQLite-Datei |
| `SetupViewModelTests_Selection`, `SetupViewModelTests_Validation`, `SetupViewModelTests_Save`, `SetupViewModelTests_Skip` | Testklassen (Unit) | Auswahl, Validierung, Speichern und Überspringen |
| `SetupE2ETests_FirstStart`, `SetupE2ETests_Save`, `SetupE2ETests_Restart`, `SetupE2ETests_Skip`, `SetupE2ETests_Validation`, `SetupE2ETests_OptionsAfterSetup`, `SetupE2ETests_Backup` | Testklassen (E2E) | Benutzerflüsse der Einrichtungsansicht (siehe Tests) |

Keine neuen Enums und keine neuen Models. `FuelType` und `FuelTypeSelection` werden unverändert verwendet. Es gibt keine neue Testhilfe außerhalb der beschriebenen Helfer.

## Änderungen an bestehenden Klassen

### `App` (Klasse)

- **Geänderte Methoden:** `CreateWindow` — übergibt die für `AppShell` benötigten Abhängigkeiten (Konstruktor-Injection von `ISetupStatusService` und `SetupViewModel`). Die Auflösung über `IServiceProvider` wird im Umsetzungsschritt geprüft.
- **Unverändert:** `InitializeDatabaseAsync` (Fire-and-forget), `OnRequestedThemeChanged`.

### `AppShell` (Klasse, `AppShell.xaml.cs` und `AppShell.xaml`)

- **Neue Eigenschaften:** Overlay-Bereich (Sichtbarkeit, gebunden an `SetupViewModel`).
- **Neue Methoden:** `CheckSetupAsync` — wartet auf `IDatabaseInitializer.InitializeAsync` und ruft `IsSetupRequiredAsync` auf; setzt die Sichtbarkeit des Overlays. Aufruf aus dem Konstruktor oder `OnAppearing` (der Umsetzungsschritt klärt, welcher Zeitpunkt sicher ist).
- **Geänderte Methoden:** Konstruktor `AppShell()` — bekommt Abhängigkeiten per Injection. Routen und `OnNavigated` bleiben unverändert.
- **Neue Event-Handler:** Auf `SetupViewModel.Completed` (ausgelöst nach erfolgreichem Speichern oder Überspringen) — blendet das Overlay aus.

### `SettingsViewModel` (ViewModel)

- Keine Änderung. Das Verhalten der Optionen bleibt bestehen (Auswahl wird sofort gespeichert, Validierung wie bisher).

### `ISettingsService` / `SettingsService` (Interface / Service)

- Keine Änderung. Die Erkennung läuft über den neuen `SetupStatusService`. `FakeSettingsService` bleibt gültig.

### `TankradarDbContext` / `UserSettingsEntity`

- Keine Änderung. Die Erkennung erfolgt über die bestehende Zeile.

### `MauiProgram` (DI-Konfiguration)

- **Neue Registrierungen:** `ISetupStatusService` → `SetupStatusService` (Singleton, analog `ISettingsService`); `SetupViewModel` (Transient, analog `SettingsViewModel`).

## Datenbankmigrationen

Keine. Die Erkennung erfolgt über die bestehende Zeile `UserSettings`; es gibt keine Schemaänderung und keine Datenmigration (Entscheidung 5).

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Spritsorten-Auswahl | Mindestens eine Sorte ausgewählt (bestehende Regel in `AppSettings.Validate()`) | `SetupViewModel` zeigt die Meldung im Bereich; `SaveAsync` wird nicht aufgerufen. |
| Spritsorten-Auswahl | Keine doppelten oder undefinierten Sorten (bestehende Regel in `Validate()`) | Wird durch die Oberfläche nicht erzeugbar; eine Ausnahme aus `Validate` wird als Statusmeldung angezeigt, das Overlay bleibt sichtbar. |

## Konfigurationsänderungen

Keine. Die Einrichtung schreibt in die bestehende Einstellung `AppSettings` (lokal pro Gerät). Es gibt keine neuen Einträge in `appsettings` oder Konfigurationsklassen.

## Seiteneffekte und Risiken

- **Startablauf (`App`, `AppShell`):** Die Initialisierung läuft heute im Hintergrund. Die Erststart-Prüfung erfolgt nach `InitializeAsync`, sonst besteht ein Rennen mit der Migration. Kein Fehlerpfad darf die Shell blockieren (bei Fehler wird das Overlay nicht gezeigt und der Fehler protokolliert).
- **Bestandsinstallationen (Update, keine Neuinstallation):** Installationen ohne Zeile `UserSettings` sehen die Einrichtung nach dem Update einmalig. Dies ist bewusst hingenommen (Entscheidung 5). Die Auswirkung ist in `ablauf-technisch.md` und `ablauf-anwender.md` dokumentiert. Nach der Einrichtung oder dem Überspringen erscheint sie nicht mehr.
- **Wiederherstellung einer Sicherung (`BackupService`, `RestoreSwap`):** Die Sicherung enthält die Tabelle `UserSettings` (ADR 0011). Eine Sicherung ohne Zeile erzeugt beim nächsten Start das Overlay (Entscheidung 6).
- **E2E-Testbasis:** Die Tests starten mit frischer Testdatenbank (`TANKATLAS_TEST_DATA_PATH`). Das Overlay verdeckt die Oberfläche und bricht bestehende E2E-Tests ab (u. a. `SettingsE2ETests_FuelTypes`, `SettingsE2ETests_Persistence`, Tankbuch-, Offline- und Backup-E2E-Tests). Die Anzahl der betroffenen Tests ist nicht ermittelt, weil der E2E-Ausgangslauf nicht durchgeführt wurde.
- **`SettingsE2ETests_Defaults.FirstStart_ShowsDefaultValues`:** Setzt voraus, dass die Optionen direkt erreichbar sind. Dieser Test muss angepasst werden (siehe Tests).
- **Darstellung als Overlay:** Das Overlay muss Fokus und Vorlesereihenfolge korrekt führen (VoiceOver, Windows-Sprachausgabe) und darf nicht unter die TabBar fallen.
- **Keine Events oder Hooks** außer dem neuen `SetupViewModel.Completed`, der nur innerhalb der Shell verwendet wird.

## Umsetzungsreihenfolge

1. **E2E-Ausgangslauf (Baseline) auf dem Desktop**
   - Voraussetzungen: interaktive Desktop-Sitzung, Build aus dem Ausgangsstand.
   - Beschreibung: Vollständigen E2E-Lauf vor jeder Änderung ausführen und Ergebnis als Nachweis sichern (Anzahl und Ausgang aller E2E-Tests). Dient als Vergleich, ob spätere Fehler durch die Änderung entstehen.

2. **Schnittstelle `ISetupStatusService` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: Interface mit `IsSetupRequiredAsync(CancellationToken)` im Ordner `Services/` anlegen.

3. **`SetupStatusService` implementieren**
   - Voraussetzungen: Schritt 2; `IDbContextFactory<TankradarDbContext>` ist registriert (`MauiProgram`, Zeile 62); `IDatabaseInitializer` ist verfügbar, die Abfrage ruft vorher `InitializeAsync` auf.
   - Beschreibung: Gateway-Klasse anlegen, die `InitializeAsync` abwartet und `UserSettings` auf die Zeile mit `SingletonId` prüft.

4. **`SetupStatusService` in `MauiProgram` registrieren**
   - Voraussetzungen: Schritt 3.
   - Beschreibung: Singleton-Registrierung analog `ISettingsService` (Zeile 68).

5. **Integrationstests für die Erkennung**
   - Voraussetzungen: Schritte 3 und 4; Testdatenbank-Hilfsklasse `TestDatabase` (vorhanden).
   - Beschreibung: `SetupStatusServiceTests_FirstStart` mit folgenden Fällen: frische Datenbank liefert `true`; nach `SettingsService.SaveAsync` liefert `false`; nach Neustart (neue Dienstinstanz) bleibt `false`.

6. **`SetupTexts` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: Deutsche Texte im Muster `SettingsTexts` (Überschrift, Hinweis, Schaltflächen, Validierungsmeldung, Labels der Sorten).

7. **`SetupViewModel` anlegen**
   - Voraussetzungen: Schritt 6; vorhandene `FuelTypeItemViewModel`; `ISettingsService` ist per DI verfügbar (vorhanden).
   - Beschreibung: Auswahlliste mit Standardvorbelegung, Validierung „mindestens eine Sorte“, Speichern und Überspringen, Statusmeldung, Ereignis `Completed`.

8. **Hilfsmethode und Unit-Tests für `SetupViewModel`**
   - Voraussetzungen: Schritt 7; `FakeSettingsService` (vorhanden).
   - Beschreibung: Hilfsmethode `CreateSetupViewModel(fake)` in `Unit/Support` anlegen. Dann `SetupViewModelTests_Selection` (Standardvorbelegung, Umschalten), `SetupViewModelTests_Validation` (keine Sorte: Meldung, kein Speichern), `SetupViewModelTests_Save` (Speichern schreibt Auswahl und löst `Completed` aus; Fehler bleibt sichtbar), `SetupViewModelTests_Skip` (Überspringen speichert Standard).

9. **`SetupViewModel` in `MauiProgram` registrieren**
   - Voraussetzungen: Schritt 7.
   - Beschreibung: Transient-Registrierung analog `SettingsViewModel` (Zeile 120).

10. **Setup-Overlay in `AppShell` (XAML)**
    - Voraussetzungen: Schritte 6, 7, 9; bestehende Umschalter-Gestaltung der Optionen-Seite (`SettingsPage`); Ebene 3 (Modal) aus `docs/design-system.md`.
    - Beschreibung: Overlay-Bereich mit Überschrift, Hinweistext, Umschaltern (`Setup.FuelType.{Name}.Switch`), Schaltflächen `Setup.Save` und `Setup.Skip`, Statusmeldung. Verschiebe-Schaltflächen werden nicht angezeigt (Entscheidung 10). Wiederverwendung der bestehenden Umschalter-Muster.

11. **Barrierefreiheit im Overlay**
    - Voraussetzungen: Schritt 10.
    - Beschreibung: `SemanticProperties` (Hint, Description) für Umschalter, Schaltflächen und Hinweis setzen; Fokusreihenfolge prüfen.

12. **Konstruktor-Injection und Startlogik in `AppShell`/`App`**
    - Voraussetzungen: Schritte 4, 9, 10; Prüfung, wie `App.CreateWindow` an DI-Abhängigkeiten gelangt.
    - Beschreibung: `AppShell` erhält `ISetupStatusService` und `SetupViewModel`. Nach `InitializeAsync` `IsSetupRequiredAsync` abfragen, Overlay anzeigen; `Completed` blendet es aus. Fehler protokollieren, Overlay nicht zeigen.

13. **E2E-Testbasis anpassen (gemeinsamer Helfer)**
    - Voraussetzungen: Schritt 1 (Baseline), Schritt 12.
    - Beschreibung: Gemeinsamer Helfer legt in der Testdatenbank vor dem Start die Zeile `UserSettings` an (Einrichtung als abgeschlossen markiert, Entscheidung 8). Anzuwenden auf `SettingsE2ETestBase`, `TankbookE2ETestBase`, `OfflineE2ETests_Tankbook` und die Backup-E2E-Tests, soweit sie frische Datenbanken verwenden.

14. **`FirstStart_ShowsDefaultValues` anpassen**
    - Voraussetzungen: Schritt 13.
    - Beschreibung: Der Test prüft die Standardwerte in den Optionen bei eingerichteter Datenbank (über den Helfer). Das Erscheinen des Overlays bei frischer Datenbank wird in `SetupE2ETests_FirstStart` geprüft.

15. **Neue E2E-Tests für die Einrichtung**
    - Voraussetzungen: Schritte 12 und 13; Baseline aus Schritt 1.
    - Beschreibung: Szenarien aus dem Abschnitt „E2E-Tests (primärer Funktionsnachweis)“ umsetzen.

16. **ADR 0017 anlegen**
    - Voraussetzungen: Keine (Entscheidung 7 steht fest).
    - Beschreibung: `docs/adr/0017-einrichtungsdialog.md` anlegen. Inhalt: Overlay in der App statt Betriebssystem-Dialog; Abweichung vom Begriff „modal“ aus der Anforderung; Bezug zu ADR 0006 und ADR 0008; Ablauf der Erststart-Erkennung über die Zeile `UserSettings`.

17. **Dokumentation**
    - Voraussetzungen: Schritt 12 (Verhalten abgeschlossen), Schritt 16.
    - Beschreibung: `docs/help/Einstellungen/ablauf-anwender.md` und `ablauf-technisch.md` um den Abschnitt „Einrichtung beim ersten Start“ ergänzen (inkl. Verhalten nach Update ohne Zeile und nach Wiederherstellung, Entscheidungen 5 und 6). `docs/help/Einstellungen/beschreibung.md` um die Einrichtung ergänzen; Verweis in `docs/help/index.md`.

18. **Gesamtprüfung**
    - Voraussetzungen: alle vorherigen Schritte.
    - Beschreibung: Build (0 Warnungen), Unit, Integration, E2E-Lauf vollständig; Vergleich mit Baseline aus Schritt 1. Hinweis: `dotnet format --verify-no-changes` und iOS-Kompilierung sind in der Bestandsaufnahme nicht ausgeführt.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `IsSetupRequired_NoSettingsRow_ReturnsTrue` | `SetupStatusServiceTests_FirstStart` | Frische Datenbank: Einrichtung nötig |
| `IsSetupRequired_AfterSave_ReturnsFalse` | `SetupStatusServiceTests_FirstStart` | Nach Speichern: keine Einrichtung nötig |
| `IsSetupRequired_AfterRestart_StaysFalse` | `SetupStatusServiceTests_FirstStart` | Neue Dienstinstanz: Zustand bleibt erhalten |
| `SetupViewModel_DefaultsAllFuelTypesSelected` | `SetupViewModelTests_Selection` | Standardvorbelegung E5, E10, Diesel ausgewählt |
| `SetupViewModel_ToggleFuelType_UpdatesSelection` | `SetupViewModelTests_Selection` | Umschalten einer Sorte ändert die Auswahl |
| `SetupViewModel_NoFuelTypeSelected_ShowsMessageAndDoesNotSave` | `SetupViewModelTests_Validation` | Validierung „mindestens eine“ |
| `SetupViewModel_Save_PersistsSelectionAndRaisesCompleted` | `SetupViewModelTests_Save` | Speichern schreibt über `ISettingsService.SaveAsync`, löst `Completed` aus |
| `SetupViewModel_Save_Fails_ShowsStatusAndStaysOpen` | `SetupViewModelTests_Save` | Fehler bleibt sichtbar, `Completed` wird nicht ausgelöst |
| `SetupViewModel_Skip_SavesDefaults` | `SetupViewModelTests_Skip` | Überspringen speichert `AppSettings.CreateDefault()` |
| Hilfsmethode `CreateSetupViewModel(fake)` | Hilfsklasse in `Unit/Support` | Erzeugt `SetupViewModel` mit `FakeSettingsService` |
| Hilfsmethode E2E-Testbasis (Einrichtungsstatus setzen) | gemeinsame Basis `SettingsE2ETestBase` (und weitere Basen, siehe Umsetzungsreihenfolge, Schritt 13) | Legt die Zeile `UserSettings` in der Testdatenbank an, bevor die App startet |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `AppSettingsTests_Defaults`, `AppSettingsTests_Validation` | Unverändert; Regeln werden wiederverwendet. Keine Anpassung. |
| `SettingsServiceTests_LoadSave` (inkl. `LoadAsync_WithoutData_ReturnsDefaults`) | Unverändert, da `SettingsService` nicht geändert wird. |
| `SettingsViewModelTests_*` | Keine Änderung der Logik. Keine Anpassung erwartet. |
| `DatabaseInitializerTests_FirstStart`, `DatabaseInitializerTests_Upgrade` | Keine Schemaänderung, keine Migration; keine Anpassung erwartet. |

Keine weiteren Unit- oder Integrationstests betroffen.

### E2E-Tests (primärer Funktionsnachweis)

Die Einrichtung ist ein Benutzerablauf. Die Happy-Path-Szenarien und die sichtbaren Fehlerfälle werden deshalb als E2E-Tests geplant. Unit- und Integrationstests ergänzen sie.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Frische Datenbank: Einrichtungsansicht erscheint, alle drei Sorten vorausgewählt, Überschrift und Hinweis sichtbar | `SetupE2ETests_FirstStart` | Erscheinen beim Erststart; Standardauswahl | Sichtbarkeit und Vorbelegung sind nur über die echte Oberfläche prüfbar |
| Pflicht | Auswahl ändern (eine Sorte abwählen), „Speichern“: Ansicht verschwindet, Optionen zeigen die gespeicherte Auswahl | `SetupE2ETests_Save` | Speichern über die Einstellung der Optionen; keine zweite Quelle | Prüft die Verknüpfung mit `SettingsViewModel` über die UI |
| Pflicht | Nach Neustart der App erscheint die Einrichtungsansicht nicht mehr; Optionen zeigen die Auswahl | `SetupE2ETests_Restart` | Dialog nur einmal pro Installation | Neustart und Persistenz nur mit echter App prüfbar |
| Pflicht | „Überspringen“: Ansicht verschwindet, Optionen zeigen alle Sorten ausgewählt | `SetupE2ETests_Skip` | Überspringen mit Standardwert | Benutzerfluss |
| Pflicht | Alle Sorten abwählen, „Speichern“: Meldung „mindestens eine“ erscheint, Ansicht bleibt offen | `SetupE2ETests_Validation` | Validierung „mindestens eine Sorte“ | Fehlermeldung ist nur in der Oberfläche prüfbar |
| Pflicht | Nach Einrichtung: Änderung in den Optionen wirkt, Ansicht erscheint nicht erneut | `SetupE2ETests_OptionsAfterSetup` | Änderbarkeit über die Optionen | Verknüpfung der beiden Oberflächen |
| Pflicht | Wiederherstellung einer Sicherung mit Einstellungen: keine Einrichtungsansicht beim nächsten Start (Entscheidung 6) | `SetupE2ETests_Backup` (oder in `BackupE2ETests_*`) | Verhalten nach Wiederherstellung | Nur über den Sicherungsablauf der Oberfläche prüfbar |

Bestehende E2E-Tests, die angepasst werden müssen:

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `SettingsE2ETests_Defaults.FirstStart_ShowsDefaultValues` | Die Einrichtungsansicht verdeckt die Optionen bei frischer Datenbank. Test nutzt den Helfer für eine eingerichtete Datenbank. |
| `SettingsE2ETests_FuelTypes`, `SettingsE2ETests_Persistence` | Frische Testdatenbank: Einrichtungsansicht muss vorab beendet sein (über gemeinsame Testbasis `SettingsE2ETestBase`). |
| `TankbookE2ETestBase`, `OfflineE2ETests_Tankbook`, Backup-E2E-Tests mit frischer Datenbank | Gleiche Ursache: Overlay blockiert die Oberfläche. Über den gemeinsamen Helfer anpassen. |

Die Anzahl der betroffenen E2E-Tests ist nicht ermittelt; der Ausgangslauf (Umsetzungsreihenfolge, Schritt 1) liefert sie.

Ein Ausgangslauf der E2E-Tests ist Voraussetzung (Schritt 1). Der Plan ist für den Desktop-Lauf auszulegen; iOS ist nicht Teil des Nachweises.

## Offene Punkte

Keine.

Alle Punkte aus der Anforderung und der Bestandsaufnahme sind durch die Anwenderentscheidungen geklärt und oben unter „Getroffene Entscheidungen“ festgehalten.
