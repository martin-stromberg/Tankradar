# Umsetzungsplan: Tankbuch öffnet mit Tank- und Ladevorgängen (Issue #20)

## Übersicht

Der Tab „Tankbuch“ zeigt künftig direkt die Tank- und Ladevorgänge eines Fahrzeugs. Die Tab-Wurzel wird `TankbookEntriesPage`; die bisherige Fahrzeugliste (`TankbookPage`) wird zur Fahrzeugverwaltung und ist aus dem Tab-Kopf erreichbar. Betroffen sind Shell-Navigation (Tab-Inhalt, Routen), die ViewModels `TankbookViewModel`, `TankbookEntriesViewModel`, `VehicleItemViewModel`, eine neue gerätelokale Speicherung der gewählten Fahrzeugkennung, Texte, Hilfe-Dokumentation und die UI-Tests (Unit und E2E, insbesondere der Einstieg über `TankbookE2EDriver.OpenEntries`). Eine neue ADR `0018` ersetzt die Entscheidung „eigene Seite je Fahrzeug über die Schaltfläche auf der Fahrzeugkarte“ aus ADR 0009.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Tab „Tankbuch“ (`ShellContent` `tankbook`) | Tab-Wurzel ist `TankbookEntriesPage` (Vorgänge des gewählten Fahrzeugs). Keine zweite Seite pro Fahrzeug auf dem Navigationsstapel (Verzicht auf Service Layer-Navigation `tankbookentries`). | Die Anforderung verlangt den direkten Einstieg. Ohne Stapelseite entfallen `CloseAsync` und `SelectPages` als Fehlerquelle. |
| Fahrzeugverwaltung | Bestehende `TankbookPage` (+ `TankbookViewModel`) bleibt als eigene Shell-Route `tankbookvehicles` erhalten und wird per `GoToAsync` gepusht (Muster der bisherigen Route `tankbookentries`). Titel „Fahrzeuge“. | Formulare, Löschrückfrage und Tests der Verwaltung bleiben unverändert. Ein Verwaltungsbereich im Tab hätte die Seite überladen. Beantwortet Offene Frage 3 der Anforderung. |
| Fahrzeugwechsel im Kopf | Inline-Auswahl: Schaltfläche mit dem Fahrzeugnamen klappt eine Chip-Liste der bestehenden Fahrzeuge auf (FlexLayout + `BindableLayout`, Stil `FuelKindChip`, Muster der bekannten Tankstellen in `TankbookEntriesPage`). Die Chips nutzen `ChoiceOptionViewModel` (Schnittstelle `IChoiceOption`). | Vorhandenes Auswahlmuster im Tankbuch. Es gibt im Projekt keinen ActionSheet- oder Dialog-Ansatz für Auswahlen; ein neuer Dialog würde ein zweites Muster einführen. Die Anzeige erfolgt nur über den Namen, nie über `Id`. |
| Sichtbarkeit der Auswahl bei nur einem Fahrzeug | Auswahl bleibt sichtbar (Schaltfläche mit Namen, Chip-Liste mit einem Eintrag). | Verwaltung bleibt im Kopf erreichbar (Offene Frage 5 der Anforderung). |
| Seitentitel | Tab-Wurzel `Title` = `TankbookTexts.PageTitle` („Tankbuch“), Fahrzeugname als Überschrift im Kopf. Verwaltung: `Title` „Fahrzeuge“ (neuer Text `TankbookTexts.VehiclesPageTitle`). `TankbookEntryTexts.PageTitle` entfällt aus dem Titel. | Konsistent zum Tab-Titel. `PageTitleRedundancyTests` prüft, dass der Titel nicht als Überschrift wiederholt wird; der Fahrzeugname ist eine andere Überschrift (Offene Frage 6). |
| Speicherung der gewählten Kennung | Gateway `IVehicleSelectionStore` mit Implementierung `PreferencesVehicleSelectionStore` über `IPreferences` (Schlüssel `tankbook.selectedVehicleId`, Wert: nur die `long`-Kennung). Keine Schemaänderung. | Reine Komfortfunktion, gerätelokal (Anforderung „gerätelokal“). Keine Migration, Erststart-Kennzeichen (`UserSettings`, ADR 0017) bleibt unberührt. `IPreferences` ist über die DI-Schnittstelle testbar, statisches `Preferences` nicht. Offene Frage 1 beantwortet. |
| Auswahlregel | Reine statische Funktion `VehicleSelectionResolver.Resolve(IReadOnlyList<Vehicle> sortedByName, long? storedId)`: gespeicherte Kennung, falls vorhanden; sonst erstes Fahrzeug nach Name; sonst `null`. | Ohne Abhängigkeiten, direkt unit-testbar. Deckt Einzelfahrzeug und Löschfall ab. |
| Wiederherstellung einer Sicherung | Bei `IRestoreNotifier.Restored` wird die gespeicherte Kennung entfernt und die Auswahl neu bestimmt (erstes Fahrzeug nach Name). | Kennungen aus einer Sicherung können anderen Fahrzeugen entsprechen; Zurücksetzen ist sicher. Offene Frage 2 beantwortet. |
| Fallback nach Löschen oder fehlendem Fahrzeug | Beim Laden wird die Auswahl über den Resolver neu bestimmt; ist die gespeicherte Kennung ungültig, wird die neue Kennung gespeichert. Keine Fehlermeldung, Ansage des neuen Fahrzeugs. | Anforderung: automatischer Wechsel auf das nächste Fahrzeug nach Name. |
| Sprachausgabe | `IScreenReaderAnnouncer.Announce` beim Öffnen des Tabs (`OnAppearing`) und nach jedem Wechsel. Rückkehr aus der Verwaltung löst `OnAppearing` ebenfalls aus und damit eine Ansage (akzeptiert). | Annahme A4 der Anforderung. |
| Leerzustand ohne Fahrzeug | Kopf zeigt Hinweis (`TankbookTexts.NoVehicles`) und Schaltfläche „Fahrzeug hinzufügen“ → `ITankbookNavigator.OpenVehicleManagementAsync(startCreate: true)`. Formular, Auswertung und Export sind ausgeblendet (`IsVisible="{Binding HasVehicle}"`). | Anforderung: Hinweis mit direktem Weg zum Anlegen. |
| Navigation zur Verwaltung | Interface `ITankbookNavigator` reduziert auf `OpenVehicleManagementAsync(bool startCreate)`. Route `tankbookvehicles`; `TankbookViewModel` implementiert `IQueryAttributable` für `startCreate`. | Ersetzt `OpenEntriesAsync`/`CloseAsync`. |
| Einstieg über die Fahrzeugkarte | Schaltfläche „Tank- und Ladevorgänge“ (`OpenEntriesCommand`) entfällt vollständig. | Annahme A3; vermeidet einen zweiten Einstiegsweg. |
| Auswertung und Export | Keine Änderung der Dienste; die Ansichten werden beim Fahrzeugwechsel mit der neuen `VehicleId` neu geladen (`Statistics.Reset()`, `Export.Reset()` wie bisher in `Show`). | Annahme A5; die Dienste sind bereits fahrzeugbezogen. |
| Datenschutz | Gespeichert wird nur die Kennung; keine Namen oder Werte in Logs (wie bisher). | Anforderung und ADR 0008/0009. |

## Programmabläufe

### Öffnen des Tabs „Tankbuch“

1. `AppShell` zeigt `TankbookEntriesPage`; die Seite erhält `TankbookEntriesViewModel` über DI.
2. `OnAppearing()` ruft `LoadAsync()` auf.
3. `LoadAsync()` lädt alle Fahrzeuge über `IVehicleService.GetVehiclesAsync` (sortiert nach Name) und die gespeicherte Kennung über `IVehicleSelectionStore`.
4. `VehicleSelectionResolver.Resolve` bestimmt das Fahrzeug. Weicht das Ergebnis von der gespeicherten Kennung ab (gelöscht oder nie gesetzt), wird die neue Kennung über `IVehicleSelectionStore` gespeichert.
5. Ohne Fahrzeug: Leerzustand (Hinweis, „Fahrzeug hinzufügen“), keine weiteren Ladeschritte.
6. Mit Fahrzeug: `Vehicle` setzen, Vorgänge (`ITankbookEntryService`), Kopfangaben und Auswertung (`ITankbookStatisticsService`) laden; `VehicleId` setzen.
7. Ansage über `IScreenReaderAnnouncer` mit dem Hinweistext des Fahrzeugs (`FormatVehicleHint`).

Beteiligte Klassen: `TankbookEntriesPage`, `TankbookEntriesViewModel`, `IVehicleService`, `IVehicleSelectionStore`, `VehicleSelectionResolver`, `ITankbookEntryService`, `ITankbookStatisticsService`, `IScreenReaderAnnouncer`

### Fahrzeug wechseln

1. Nutzer tippt auf die Auswahl im Kopf (`ToggleVehiclePickerCommand`); `IsVehiclePickerOpen` wird umgeschaltet. Die Chip-Liste wird aus `VehicleOptions` (`ChoiceOptionViewModel` je Fahrzeug, Beschriftung = Name) gebildet.
2. Nutzer wählt einen Chip; `SelectCommand` ruft `SelectVehicleAsync(long vehicleId)` auf.
3. `SelectVehicleAsync` schließt die Auswahl, bricht ein offenes Vorgangsformular ab (`Show`-Verhalten bisher), speichert die Kennung über `IVehicleSelectionStore` und lädt Vorgänge, Auswertung und Export neu (wie `LoadAsync`, ohne erneutes Laden der Fahrzeugliste).
4. Ansage des neuen Fahrzeugs.

Beteiligte Klassen: `TankbookEntriesViewModel`, `ChoiceOptionViewModel`, `IVehicleSelectionStore`, `IScreenReaderAnnouncer`

### Fahrzeug anlegen aus dem Leerzustand

1. Nutzer tippt im Leerzustand auf „Fahrzeug hinzufügen“ (`AddVehicleCommand` der Tab-Seite).
2. `ITankbookNavigator.OpenVehicleManagementAsync(startCreate: true)` ruft `Shell.Current.GoToAsync("tankbookvehicles", { startCreate = true })` auf.
3. `TankbookViewModel.ApplyQueryAttributes` setzt `startCreate` → `BeginCreate()` öffnet das Formular „Neues Fahrzeug“.
4. Nach dem Speichern und Zurücknavigieren (Shell-Zurück) lädt `TankbookEntriesPage` über `OnAppearing` neu und zeigt das neue Fahrzeug (erstes nach Name bzw. gespeicherte Kennung).

Beteiligte Klassen: `TankbookEntriesViewModel`, `ITankbookNavigator`, `ShellTankbookNavigator`, `TankbookViewModel`

### Fahrzeugverwaltung öffnen

1. Nutzer tippt im Kopf auf „Fahrzeuge verwalten“ (`ManageVehiclesCommand`).
2. `ITankbookNavigator.OpenVehicleManagementAsync(startCreate: false)` ruft `GoToAsync("tankbookvehicles")` auf.
3. Die Verwaltungsseite (`TankbookPage`) zeigt die Fahrzeugliste mit Anlegen, Bearbeiten und Löschen (Rückfrage mit Zählung wie bisher). Zurück über Shell.

Beteiligte Klassen: `TankbookEntriesViewModel`, `ITankbookNavigator`, `ShellTankbookNavigator`, `TankbookViewModel`, `VehicleItemViewModel`

### Löschen des gewählten Fahrzeugs

1. In der Verwaltung löscht der Nutzer das Fahrzeug (unverändert: `TankbookViewModel.DeleteAsync` mit `DeleteVehicleAsync(id, shownCount)`).
2. Zurück auf dem Tab: `OnAppearing` → `LoadAsync` → `VehicleSelectionResolver` liefert das nächste Fahrzeug nach Name oder `null` (Leerzustand). Gespeicherte Kennung wird aktualisiert.

Beteiligte Klassen: `TankbookViewModel`, `TankbookEntriesViewModel`, `VehicleSelectionResolver`, `IVehicleSelectionStore`

### Wiederherstellung einer Sicherung

1. `BackupViewModel` ruft `IRestoreNotifier.NotifyRestored()` auf (bestehend).
2. `TankbookEntriesViewModel.OnRestored` löscht die gespeicherte Kennung über `IVehicleSelectionStore.Clear()` und lädt neu (`LoadAsync`).
3. `TankbookViewModel.OnRestored` lädt die Fahrzeugliste wie bisher neu.

Beteiligte Klassen: `TankbookEntriesViewModel`, `IVehicleSelectionStore`, `IRestoreNotifier`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `IVehicleSelectionStore` | Interface (Gateway) | Lesen, Speichern und Löschen der gewählten Fahrzeugkennung (`long?`). |
| `PreferencesVehicleSelectionStore` | Klasse (Gateway-Implementierung) | Implementierung über `IPreferences`; Schlüssel `tankbook.selectedVehicleId`. Liefert bei ungültigem Wert `null`. |
| `VehicleSelectionResolver` | Statische Klasse (reine Funktion) | `Resolve(IReadOnlyList<Vehicle>, long?)` nach Regel „gespeichert, sonst erstes nach Name, sonst `null`“. |
| `VehicleChoiceOption` | Nicht nötig | Entfällt; Auswahl nutzt vorhandenes `ChoiceOptionViewModel`. |

## Änderungen an bestehenden Klassen

### `TankbookEntriesViewModel` (ViewModel)

- **Entfernte Schnittstelle:** `IQueryAttributable` (kein Navigationsparameter mehr).
- **Neue Eigenschaften:** `VehicleOptions` (`IReadOnlyList<ChoiceOptionViewModel>`) — Chip-Liste der Fahrzeuge; `IsVehiclePickerOpen` (bool) — Zustand der Auswahl; `HasVehicles` (bool); `ShowNoVehiclesHint` (bool, `IsLoaded && !HasVehicles`).
- **Neue Methoden:**
  - `LoadAsync()` — geändert: lädt Fahrzeuge und gespeicherte Kennung, bestimmt das Fahrzeug über den Resolver, lädt Vorgänge/Auswertung. Kein `CloseAsync` mehr.
  - `SelectVehicleAsync(long vehicleId)` — speichert Kennung, schließt Auswahl und Formular, lädt neu, kündigt an.
  - `ToggleVehiclePickerCommand` — öffnet/schließt die Chip-Liste.
  - `ManageVehiclesCommand` — ruft `OpenVehicleManagementAsync(false)`.
  - `AddVehicleCommand` — ruft `OpenVehicleManagementAsync(true)`.
  - `Announce(Vehicle)` — private Hilfsmethode für `FormatVehicleHint`.
- **Geänderte Methoden:**
  - `Show(long)` — ersetzt durch `SelectVehicleAsync`; Verhalten (Formular schließen, Statistik/Export zurücksetzen) bleibt erhalten.
  - `OnAppearing()` — lädt neu (wie bisher) und kündigt das Fahrzeug an.
  - `OnRestored(...)` — ruft `IVehicleSelectionStore.Clear()` vor `LoadAsync` auf.
  - `CloseAsync(long)` — entfällt.
- **Neue Events:** keine.
- **Neue Event-Handler:** keine.
- **Konstruktor:** zusätzliche Abhängigkeiten `IVehicleSelectionStore`; `ITankbookNavigator` bleibt (nun für Verwaltung).

### `TankbookViewModel` (ViewModel)

- **Neue Schnittstelle:** `IQueryAttributable` — `ApplyQueryAttributes` liest `startCreate` (bool) und ruft nach dem Laden `BeginCreate()` auf.
- **Entfernte Methode:** `OpenEntries(VehicleItemViewModel)`.
- **Geänderte Methoden:** `OnAppearing()` bleibt; bei `startCreate` wird das Formular nach `LoadAsync` geöffnet.
- **Entfernte Befehle:** keine im ViewModel selbst; `VehicleItemViewModel`-Parameter `openEntries` entfällt (siehe unten).

### `VehicleItemViewModel` (ViewModel)

- **Geänderter Konstruktor:** `openEntries`-Parameter entfällt.
- **Entfernte Befehle:** `OpenEntriesCommand`.
- **Entfernte Texte:** Verwendung von `TankbookTexts.Entries` in diesem Typ entfällt.

### `ITankbookNavigator` (Interface)

- **Entfernte Methoden:** `OpenEntriesAsync(long vehicleId)`, `CloseAsync(long vehicleId)`.
- **Neue Methode:** `OpenVehicleManagementAsync(bool startCreate)` — Zweck: Verwaltung (Route `tankbookvehicles`) öffnen; Parameter `startCreate` → Query-Parameter.

### `ShellTankbookNavigator` (Klasse)

- **Entfernte Methoden:** `OpenEntriesAsync`, `CloseAsync`, `SelectPages<T>`, `ShowsVehicle`.
- **Entfernte Konstanten:** `EntriesRoute`, `VehicleIdParameter`.
- **Neue Konstante:** `VehiclesRoute = "tankbookvehicles"`.
- **Neue Methode:** `OpenVehicleManagementAsync(bool startCreate)` — `GoToAsync(VehiclesRoute, { startCreate })`.

### `AppShell` (XAML und Code-behind)

- **`AppShell.xaml`:** Tab `tankbook` → `ContentTemplate="{DataTemplate views:TankbookEntriesPage}"`.
- **`AppShell.xaml.cs`:** Routing-Registrierung `EntriesRoute` (`TankbookEntriesPage`) entfällt; neu `Routing.RegisterRoute(ShellTankbookNavigator.VehiclesRoute, typeof(TankbookPage))`.

### `TankbookEntriesPage.xaml` (View)

- **Titel:** `Title` = `TankbookTexts.PageTitle`.
- **Kopf:** Fahrzeugname als Auswahl-Schaltfläche (`ToggleVehiclePickerCommand`), darunter Chip-Liste (`VehicleOptions`, Muster der bekannten Tankstellen, `AutomationId` `TankbookEntries.Vehicle.Option.{Key}`). Schaltfläche „Fahrzeuge verwalten“ (`ManageVehiclesCommand`).
- **Leerzustand:** Hinweis (`NoVehicles`) und Schaltfläche „Fahrzeug hinzufügen“ (`AddVehicleCommand`), `AutomationId` `TankbookEntries.AddVehicle`.
- **Sichtbarkeit:** Formular, Vorgangsliste, Auswertung und Export `IsVisible="{Binding HasVehicle}"`.
- **Kopfangaben:** `TankbookEntries.Vehicle` und `TankbookEntries.VehicleDetails` bleiben; `AutomationId` unverändert.

### `TankbookPage.xaml` (View, Verwaltung)

- **Titel:** `Title` = `TankbookTexts.VehiclesPageTitle` („Fahrzeuge“).
- **Kartenvorlage:** Schaltfläche „Tank- und Ladevorgänge“ (`OpenEntriesCommand`) entfernt.
- **Einleitungstext:** Hinweis, dass die Vorgänge über den Tab „Tankbuch“ erfasst werden (neuer Text).

### `TankbookTexts` / `TankbookEntryTexts` (Texte)

- **Neu:** `TankbookTexts.VehiclesPageTitle` („Fahrzeuge“), `ManageVehicles` („Fahrzeuge verwalten“), `AddVehicle` („Fahrzeug hinzufügen“), `SelectVehicleLabel` („Fahrzeug“), `FormatSelectVehicleHint(name)` (z. B. `Fahrzeug „Golf“ auswählen`), `ChooseVehicleIntro` (Hinweis im Verwaltungsbereich).
- **Geändert:** `NoVehicles` (Text passt zum Tab-Kopf, Schaltfläche separat).
- **Entfernt:** `TankbookTexts.Entries` („Tank- und Ladevorgänge“ als Schaltflächentext), `TankbookEntryTexts.PageTitle` (wird nicht mehr als Titel verwendet; Entfernen nur, wenn ohne Verwendung).

### `PreferencesVehicleSelectionStore` und Registrierung

- **`MauiProgram`:** `IPreferences` (MAUI-Schnittstelle, Wert `Preferences.Default`) und `IVehicleSelectionStore` → `PreferencesVehicleSelectionStore` als Singleton registrieren; `ITankbookNavigator` bleibt.

## Datenbankmigrationen

Keine. Die Auswahl wird nicht in der Datenbank gespeichert (Entscheidung oben).

## Validierungsregeln

Keine neuen Eingaben. Die bestehenden Validierungen der Verwaltung (`VehicleInput`) und des Vorgangsformulars bleiben unverändert.

## Konfigurationsänderungen

Keine. Die Auswahl ist eine feste Gerätepräferenz ohne Anwenderkonfiguration.

## Seiteneffekte und Risiken

- **Shell-Navigation und Tab-Wurzel:** Die Tab-Seite ist jetzt Wurzel des Reiters; Zurück-Navigation führt aus der Verwaltung auf die Tab-Seite. Die Route `tankbookentries` entfällt; Aufrufer außerhalb der beiden ViewModels sind laut Bestandsaufnahme nicht vorhanden (bei der Umsetzung per Suche erneut prüfen).
- **`TankbookEntriesPage` als Tab-Wurzel:** Die Seite wird beim App-Start direkt geladen. Lade-Fehler (z. B. Datenbank nicht verfügbar) müssen dort ebenso als Statusmeldung erscheinen wie bisher auf der Fahrzeugliste.
- **Ansage bei `OnAppearing`:** Rückkehr aus der Verwaltung und Wechsel zwischen Tabs erzeugen jeweils eine Ansage. Akzeptierter Nebeneffekt laut Annahme A4; bei Störung durch die Ansage später eingrenzen.
- **Gespeicherte Kennung und Sicherung:** Die Kennung ist nicht Teil der Sicherung (ADR 0011). Nach einer Wiederherstellung wird sie gelöscht; bei Geräteübergabe ohne Sicherung fällt die Auswahl auf das erste Fahrzeug nach Name zurück.
- **Löschen des gewählten Fahrzeugs in der Verwaltung:** Das ViewModel des Tabs ist nicht sichtbar; Neuladen erfolgt bei `OnAppearing`. Zwischenzustand ist unkritisch, da der Resolver bei jedem Laden neu bestimmt.
- **Auswertung und Export:** Arbeiten pro Fahrzeug; Wechsel setzt die Ansichten zurück (kein Gesamtexport, Annahme A5).
- **Setup-Overlay (ADR 0017):** `AppShell.CheckSetupAsync` ist unabhängig vom Tab-Inhalt; keine Änderung.
- **Bestehende Tests:** Umfangreich betroffen (siehe Abschnitt Tests). E2E-Hilfsmethode `TankbookE2EDriver.OpenEntries` betrifft ca. 25 Testklassen; der Einstieg ändert sich grundlegend.
- **Dokumentation:** `docs/help/Tankbuch/beschreibung.md`, `ablauf-anwender.md`, `ablauf-technisch.md` beschreiben den alten Einstieg; `docs/adr/0009-tankbook-entries.md` erhält einen Verweis auf ADR 0018. README auf Tankbuch-Einstieg prüfen.

## Umsetzungsreihenfolge

Hinweis: Die Schritte 6 bis 10 bilden einen zusammenhängenden Block. Erst nach Schritt 10 baut die Lösung wieder; Zwischenstände dazwischen sind nicht lauffähig.

1. **Texte ergänzen**
   - Voraussetzungen: Keine.
   - Beschreibung: Neue Schlüssel in `TankbookTexts` (`VehiclesPageTitle`, `ManageVehicles`, `AddVehicle`, `SelectVehicleLabel`, `FormatSelectVehicleHint`, `ChooseVehicleIntro`). Bestehende Texte noch nicht entfernen.

2. **Preferences-Abstraktion registrieren**
   - Voraussetzungen: Keine. Prüfen, ob `Microsoft.Maui.Storage.IPreferences` in der verwendeten MAUI-Version verfügbar ist (Standard in .NET MAUI).
   - Beschreibung: In `MauiProgram` `IPreferences` als `Preferences.Default` registrieren.

3. **Gateway `IVehicleSelectionStore` und `PreferencesVehicleSelectionStore` anlegen**
   - Voraussetzungen: Schritt 2 (`IPreferences`).
   - Beschreibung: Interface und Implementierung unter `src/Tankradar.MAUI/Services/Tankbook/`. Methoden `Get()`, `Set(long)`, `Clear()`. Ungültige gespeicherte Werte liefern `null`. Registrierung als Singleton in `MauiProgram`.

4. **`VehicleSelectionResolver` anlegen**
   - Voraussetzungen: Keine (nutzt `Vehicle`, vorhanden).
   - Beschreibung: Statische Methode `Resolve` unter `src/Tankradar.MAUI/Services/Tankbook/` (oder `ViewModels`-nahe Hilfsklasse im Tankbook-Namensraum).

5. **Unit-Tests für Schritte 3 und 4**
   - Voraussetzungen: Schritte 3 und 4; Fake `IPreferences` im Testprojekt (neu, `Unit/Support/FakePreferences.cs`).
   - Beschreibung: Tests gemäß Abschnitt „Neue Tests“.

6. **`ITankbookNavigator` und `ShellTankbookNavigator` umbauen**
   - Voraussetzungen: Schritt 1 (Routenname `VehiclesRoute` mit Konstante).
   - Beschreibung: `OpenEntriesAsync`, `CloseAsync`, `SelectPages`, `ShowsVehicle` entfernen; `OpenVehicleManagementAsync(bool startCreate)` und `VehiclesRoute` ergänzen.

7. **Routen in `AppShell` ändern**
   - Voraussetzungen: Schritt 6.
   - Beschreibung: `AppShell.xaml.cs`: Route `tankbookentries` entfernen, `tankbookvehicles` → `TankbookPage`. `AppShell.xaml`: Tab-Template → `TankbookEntriesPage`.

8. **`VehicleItemViewModel` bereinigen**
   - Voraussetzungen: Keine weiteren Abhängigkeiten.
   - Beschreibung: Parameter `openEntries` und `OpenEntriesCommand` entfernen. Alle Konstruktionsaufrufe in `TankbookViewModel` und Tests anpassen.

9. **`TankbookViewModel` für die Verwaltung umstellen**
   - Voraussetzungen: Schritte 6 und 8.
   - Beschreibung: `IQueryAttributable` (`startCreate`), `OpenEntries` entfernen.

10. **`TankbookEntriesViewModel` umbauen**
    - Voraussetzungen: Schritte 3, 4, 6.
    - Beschreibung: Konstruktor um `IVehicleSelectionStore` erweitern; `LoadAsync` mit Resolver; `SelectVehicleAsync`, Auswahl-Befehle, `ManageVehiclesCommand`, `AddVehicleCommand`, `OnRestored` mit `Clear()`; `IQueryAttributable`, `Show` und `CloseAsync` entfernen; Ansage bei Laden und Wechsel.

11. **`TankbookEntriesPage.xaml` umbauen**
    - Voraussetzungen: Schritt 10 (neue Eigenschaften), Schritt 1 (Texte), bestehendes Chip-Muster in derselben Datei.
    - Beschreibung: Kopf mit Auswahl und Chip-Liste, Verwaltungslink, Leerzustand, Sichtbarkeit der Abschnitte an `HasVehicle`, Titel auf `TankbookTexts.PageTitle`.

12. **`TankbookPage.xaml` anpassen**
    - Voraussetzungen: Schritt 8, Schritt 1.
    - Beschreibung: Titel „Fahrzeuge“, Schaltfläche „Tank- und Ladevorgänge“ entfernen, Einleitungstext ergänzen.

13. **Veraltete Texte entfernen**
    - Voraussetzungen: Schritte 11 und 12 (keine Verwendung mehr).
    - Beschreibung: `TankbookTexts.Entries` und `TankbookEntryTexts.PageTitle` entfernen, sofern im Code nicht mehr verwendet; Textetests anpassen.

14. **Bestehende Unit-Tests anpassen**
    - Voraussetzungen: Schritte 6 bis 13.
    - Beschreibung: Siehe Abschnitt „Betroffene bestehende Tests“.

15. **Neue Unit-Tests für ViewModels schreiben**
    - Voraussetzungen: Schritt 14 (Testbasis lauffähig), Schritte 3 und 4.
    - Beschreibung: Siehe Abschnitt „Neue Tests“.

16. **E2E-Hilfsmethode `TankbookE2EDriver` umstellen**
    - Voraussetzungen: Schritte 7 bis 12 (lauffähige App).
    - Beschreibung: `OpenEntries` wählt den Tab „Tankbuch“ und das gewünschte Fahrzeug über die Auswahl; neue Methoden `ManageVehicles`, `SelectVehicle`, `AddVehicleFromEmptyState`.

17. **Bestehende E2E-Tests anpassen**
    - Voraussetzungen: Schritt 16.
    - Beschreibung: Siehe Abschnitt „Bestehende E2E-Tests“.

18. **Neue E2E-Tests schreiben**
    - Voraussetzungen: Schritt 16.
    - Beschreibung: Siehe Abschnitt „E2E-Tests“.

19. **Dokumentation und ADR**
    - Voraussetzungen: Schritte 1 bis 13 (Texte und Verhalten final).
    - Beschreibung: ADR `docs/adr/0018-tankbook-start-view.md` (Status „Angenommen“, Verweis auf 0009). Hinweis in ADR 0009. `docs/help/Tankbuch/beschreibung.md`, `ablauf-anwender.md`, `ablauf-technisch.md` aktualisieren. README prüfen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `FakePreferences` (Hilfsklasse) | `Unit/Support` | In-Memory-Implementierung von `IPreferences` für Store-Tests. |
| `Resolve_StoredVehicleExists_ReturnsStored` | `VehicleSelectionResolverTests` | Gespeicherte Kennung hat Vorrang vor dem Namen. |
| `Resolve_StoredMissing_ReturnsFirstByName` | `VehicleSelectionResolverTests` | Fehlende Kennung (gelöscht) → erstes Fahrzeug nach Name. |
| `Resolve_NoStored_ReturnsFirstByName` | `VehicleSelectionResolverTests` | Ohne Kennung → erstes Fahrzeug nach Name. |
| `Resolve_SingleVehicle_ReturnsIt` | `VehicleSelectionResolverTests` | Einzelfahrzeug wird ohne Kennung gewählt. |
| `Resolve_NoVehicles_ReturnsNull` | `VehicleSelectionResolverTests` | Leerzustand liefert `null`. |
| `Store_SetThenGet_RoundTrips` | `PreferencesVehicleSelectionStoreTests` | Speichern und Lesen der Kennung. |
| `Store_Clear_RemovesValue` | `PreferencesVehicleSelectionStoreTests` | Löschen nach Wiederherstellung. |
| `Store_InvalidStoredValue_ReturnsNull` | `PreferencesVehicleSelectionStoreTests` | Ungültiger gespeicherter Wert wird als `null` gelesen. |
| `Load_ReusesStoredVehicle_AndAnnouncesIt` | `TankbookEntriesViewModelTests_Selection` | Tab öffnet das gespeicherte Fahrzeug, Ansage enthält den Namen. |
| `Load_StoredVehicleDeleted_SelectsNextByNameAndStoresIt` | `TankbookEntriesViewModelTests_Selection` | Fallback und Speicherung der neuen Kennung. |
| `Load_NoVehicles_ShowsNoVehiclesHintAndHidesSections` | `TankbookEntriesViewModelTests_Selection` | Leerzustand: `HasVehicle` false, Formular/Auswertung/Export ausgeblendet. |
| `AddVehicleCommand_CallsNavigatorWithStartCreate` | `TankbookEntriesViewModelTests_Selection` | Leerzustand-Schaltfläche öffnet Verwaltung mit `startCreate`. |
| `ManageVehiclesCommand_CallsNavigator` | `TankbookEntriesViewModelTests_Selection` | Verwaltungslink öffnet Verwaltung ohne `startCreate`. |
| `SelectVehicle_StoresIdReloadsAndAnnounces` | `TankbookEntriesViewModelTests_Selection` | Wechsel speichert Kennung, lädt Vorgänge der anderen Fahrzeuge, kündigt an. |
| `SelectVehicle_ClosesOpenFormAndPicker` | `TankbookEntriesViewModelTests_Selection` | Formular und Chip-Liste schließen sich beim Wechsel. |
| `SelectVehicle_ShowsOnlyEntriesOfSelectedVehicle` | `TankbookEntriesViewModelTests_Selection` | Vorgangsliste enthält nur Vorgänge des gewählten Fahrzeugs. |
| `Restored_ClearsStoredSelection_AndSelectsFirstByName` | `TankbookEntriesViewModelTests_Restore` | Wiederherstellung löscht Kennung und wählt neu. |
| `ApplyQueryAttributes_StartCreate_OpensCreateForm` | `TankbookViewModelTests_Navigation` | Verwaltung mit `startCreate` öffnet das Anlegen-Formular. |
| `OpenVehicleManagement_NavigatesToVehiclesRoute` | `ShellTankbookNavigatorTests` (nur falls testbar; sonst entfällt) | Route-Konstante und Parameter. Geringer Nutzen, daher nur bei Testbarkeit ohne Shell. |
| `Entries_TitleAndTexts_MatchTabTitle` | `ViewModelTests_PageTitles` (angepasst) | Titel der Tab-Wurzel ist „Tankbuch“. |
| `Texts_NewLabelsAndHints_ContainVehicleName` | `TankbookTextsTests_Labels` | `FormatSelectVehicleHint` enthält Name. |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `TankbookViewModelTests_Navigation.OpenEntries_NavigatesWithVehicleId` | Einstieg über Fahrzeugkarte entfällt; ersetzt durch `ApplyQueryAttributes_StartCreate_OpensCreateForm` und Navigationstest für Verwaltung. |
| `ShellTankbookNavigatorTests_SelectPages` (`SelectPages_OwnVehicle_SelectsPage`, `SelectPages_OtherVehicleOrOtherContext_SelectsNothing`) | `SelectPages` entfällt; Testdatei entfällt oder wird auf Verwaltungsnavigation reduziert. |
| `TankbookEntriesViewModelTests_Load` (`Load_VehicleMissing_ShowsMessageAndCloses`) | Kein Schließen der Seite mehr; ersetzt durch Fallback-Test. |
| `TankbookEntriesViewModelTests_Load` (übrige: `Load_ShowsEntriesNewestFirstWithConsumptionTexts`, `Load_NoEntries_ShowsEmptyHint`, `Load_StorageFailure_ShowsMessageWithoutLoggingValues`) | Setup muss zusätzlich `IVehicleSelectionStore` (Fake) liefern und die Kennung setzen statt `Show(id)`. |
| `TankbookEntriesViewModelTests_Restore` (`Restored_VehicleGone_ClosesPage`, `Restored_VehicleGone_HidesExportAndBlocksAdd`, `Restored_VehicleGone_CloseFails_RetriesOnAppearing`) | Schließen der Seite entfällt; ersetzt durch Leerzustand- und Fallback-Tests. |
| `TankbookEntriesViewModelTests_Restore` (`Restored_ReloadsEntriesAndStatistics`, `Restored_ClearsOldExportStatus`, `Restored_WithoutVehicle_DoesNothing`) | Vorbereitung über `Show` ersetzt; Erwartung „ohne Fahrzeug“ bleibt: keine Ladeaktion, aber Auswahl wird neu bestimmt. |
| `TankbookEntriesViewModelTestBase` | Neuer Konstruktor-Parameter `IVehicleSelectionStore`, Fake `FakeTankbookNavigator` um `VehicleManagementOpened` erweitern; `Navigator`-Feld bleibt. |
| `TankbookViewModelTests_Load`, `TankbookViewModelTests_Delete`, `TankbookViewModelTests_Form`, `TankbookViewModelTests_Restore` | Konstruktion von `VehicleItemViewModel` ohne `openEntries`; `TankbookViewModel` bekommt keine neuen Abhängigkeiten. |
| `ViewModelTests_PageTitles` (`TankbookEntriesViewModel_Constructor_SetsTitle`) | Erwarteter Titel „Tankbuch“ statt „Tank- und Ladevorgänge“. |
| `TankbookTextsTests_Labels` (`ButtonHints_NameActionAndVehicle`) | Hinweis der Entfernung von `TankbookTexts.Entries` prüfen; Erwartung anpassen. |
| `PageTitleRedundancyTests` | Dateien unverändert, Titel der Verwaltung ist „Fahrzeuge“; Test muss weiterhin grün sein. |
| `Integration`: `SetupStatusServiceTests_FirstStart`, `SettingsServiceTests_RestartPersistence` | Keine Änderung erwartet; Lauf zur Kontrolle. |

Keine neuen Integrationstests: Es entsteht keine neue Datenbankstruktur; die gespeicherte Kennung ist eine Präferenz und wird über den Gateway-Fake und Unit-Tests abgesichert.

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Tab „Tankbuch“ zeigt direkt die Vorgänge des einzigen Fahrzeugs (Fahrzeug über Verwaltung angelegt, dann Tab-Wechsel, Kopf zeigt Name, Vorgänge sichtbar) | `TankbookEntriesE2ETests_Flow` (angepasst) oder neue Klasse `TankbookEntriesE2ETests_StartView` | Einstieg: Tab zeigt Vorgänge direkt | Kern der Anforderung; nur über UI-Navigation prüfbar. |
| Pflicht | Fahrzeugwechsel über die Auswahl im Kopf: zwei Fahrzeuge, Vorgang bei A, Wechsel auf B zeigt keine Vorgänge von A | `TankbookEntriesE2ETests_VehicleSwitch` | Fahrzeugwechsel, Trennung der Vorgänge | Nur UI-Auswahl und Liste prüfen den Benutzerfluss. |
| Pflicht | Leerzustand ohne Fahrzeug: Hinweis sichtbar, Formular/Auswertung/Export ausgeblendet; „Fahrzeug hinzufügen“ öffnet Anlegen-Formular, Speichern zeigt das Fahrzeug im Tab | `TankbookEntriesE2ETests_EmptyStart` | Leerzustand, direkter Weg zum Anlegen | Sichtbarkeit und Navigation nur in der UI prüfbar. |
| Pflicht | Auswahl bleibt nach Neustart erhalten (gewähltes Fahrzeug B, App-Neustart, Tab zeigt B) | `TankbookEntriesE2ETests_Persistence` (angepasst, bzw. neu) | Gespeicherte Auswahl | Persistenz nur über echten App-Neustart zuverlässig prüfbar. |
| Pflicht | Gewähltes Fahrzeug in der Verwaltung löschen: Tab zeigt das nächste Fahrzeug nach Name; bei letztem Fahrzeug Leerzustand | `TankbookEntriesE2ETests_DeleteSelected` | Fallback nach Löschen | Zusammenspiel Verwaltung und Tab nur in der UI prüfbar. |
| Soll | Wiederherstellung einer Sicherung setzt die Auswahl zurück und zeigt das erste Fahrzeug nach Name | `TankbookEntriesE2ETests_RestoreReload` (ergänzen) | Verhalten nach Wiederherstellung | UI-auslösbar; Logik ist per Unit-Test abgesichert, daher nur „Soll“. |
| Pflicht | Verwaltung erreichbar über „Fahrzeuge verwalten“ und Zurück zur Tab-Seite | `TankbookEntriesE2ETests_StartView` | Verwaltung bleibt erreichbar | Navigation nur in der UI prüfbar. |

Die Sprachausgabe (Ansage) wird nicht per E2E geprüft (keine Ausgabe in der Testumgebung messbar); sie ist per Unit-Test mit `FakeScreenReaderAnnouncer` abgesichert.

Welche bestehenden E2E-Tests müssen angepasst werden?

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `TankbookE2EDriver.OpenEntries` (und alle Aufrufer: `BackupE2ETests_Photo`, `BackupE2ETests_TamperedSchema`, `FavoritesE2ETests_DisplayName`, `OfflineE2ETests_Tankbook`, `TankbookEntriesE2ETests_EditDelete`, `..._Electric`, `..._Partial`, `..._PartialHint`, `..._Persistence`, `..._Receipt`, `..._RestoreReload`, `..._Validation`, `TankbookExportE2ETestBase`, `TankbookExportE2ETests_Empty`, `..._Hybrid`, `..._Range`, `TankbookStatisticsE2ETests_*`) | Einstieg ändert sich: Tab wählen, Fahrzeug über Auswahl wählen (falls nötig), kein Fahrzeugkarten-Button mehr. |
| `TankbookEntriesE2ETests_Flow` | Prüft den Schaltflächentext und Hinweis `Tankbook.Vehicle.Entries`, der entfällt. Ersetzen durch Prüfung der Auswahl im Kopf. |
| `TankbookE2ETests_Flow`, `TankbookE2ETests_Validation`, `TankbookE2ETests_DeleteWithEntries` | Warten auf Fahrzeugliste (`Tankbook.Empty`, `Tankbook.Vehicle.*`) auf dem Tab; nun Verwaltung über „Fahrzeuge verwalten“ öffnen. |
| `TankbookE2EDriver` (Methoden `PressForVehicle`, `CreateVehicle`, `LeaveEntries`) | `LeaveEntries` entfällt (kein Stapel); `CreateVehicle` über Verwaltung bzw. Leerzustand. |
| `NavigationE2ETests` | Tab „Tankbuch“ zeigt nun Vorgangsseite statt Fahrzeugliste; Erwartung an Seitentitel bzw. Inhalt anpassen. |
| `TankbookEntriesE2ETests_RestoreReload` | Nach Wiederherstellung ist die Auswahl zurückgesetzt; Erwartung prüfen. |

## Offene Punkte

Keine. Die sechs offenen Fragen der Anforderung sind über die Designentscheidungen beantwortet (Speicherort, Wiederherstellung, Verwaltung als Route, Ansage, Sichtbarkeit bei einem Fahrzeug, Seitentitel). Verbleibende Umsetzungsdetails (Verfügbarkeit von `IPreferences` in der verwendeten MAUI-Version, Prüfung der Fundstellen für `tankbookentries` außerhalb der ViewModels) sind Prüfschritte in den Umsetzungsschritten 2 und 7.
