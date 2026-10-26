# Anforderung: Einrichtungsdialog beim ersten Start (Spritsorte wählen)

## Fachliche Zusammenfassung

Beim ersten Start der App nach einer Installation (inklusive Neuinstallation) wird ein modaler Einrichtungsdialog angezeigt, in dem der Anwender eine oder mehrere Spritsorten aus der festen Menge `FuelType` (`SuperE5`, `SuperE10`, `Diesel`) auswählt. Die Auswahl schreibt dieselbe Einstellung wie der Bereich „Spritsorten“ in den Optionen, namentlich `AppSettings.FuelTypes` (Liste aus `FuelTypeSelection`, geordnet nach Priorität), und wird über `ISettingsService.SaveAsync` persistiert. Es gibt damit keine zweite Quelle der Wahrheit. Der Dialog erscheint nur einmal pro Installation, lässt sich über die Optionen jederzeit ändern und ist für die Barrierefreiheit (VoiceOver, Windows-Sprachausgabe) sowie in deutscher Sprache wie der Rest der App umzusetzen.

## Betroffene Klassen und Komponenten

Die Liste ist aus dem Bestand abgeleitet (Stand: Anforderungsübersetzung). Vollständige Prüfung erfolgt in der Bestandsaufnahme.

- **Datenmodell**
  - `UserSettingsEntity` (Singleton-Zeile `SingletonId`): ggf. Kennzeichen „Einrichtung abgeschlossen“ oder Erkennung über das Fehlen der Zeile. Neue Eigenschaft nur bei Bedarf, dann mit eigener EF-Core-Migration (siehe Skill `entityframework-database`).
  - `TankradarDbContext` und `TankradarDbContextModelSnapshot`, nur bei neuer Eigenschaft.
- **Modelle**
  - `AppSettings`: unverändert wiederverwendet (`CreateDefault`, `Normalize`, `Validate`). Die Spritsorten-Regel „mindestens eine Sorte ausgewählt“ gilt bereits.
  - `FuelType`, `FuelTypeSelection`: unverändert wiederverwendet.
  - Kein neues Enum für die Spritsorten. `VehicleFuelKind` (ADR 0008) ist ein eigener Persistenzvertrag und wird nicht verwendet.
- **Services**
  - `ISettingsService` / `SettingsService`: Erweiterung um eine Abfrage „Einrichtung nötig?“ oder um die Erkennung im Laden (Rückgabe ohne gespeicherte Zeile). Derzeit ersetzt `LoadAsync` fehlende Werte durch Standardwerte und unterscheidet den Erststart nicht.
  - Erststart-Logik ggf. als eigener Dienst, damit sie testbar bleibt.
- **Interfaces**: ggf. Erweiterung von `ISettingsService`.
- **Enums**: keine neuen.
- **ViewModels**
  - Neues `SetupViewModel` (oder vergleichbar): Auswahl der Spritsorten, Validierung „mindestens eine“, Speichern, Überspringen.
  - `SettingsViewModel`: bleibt Quelle für die spätere Änderung über die Optionen; Verhalten unverändert.
- **UI-Komponenten**
  - Neue Seite oder Dialog (modal, Bereich in der Seite gemäß bestehender Konvention, vgl. ADR 0008 und ADR 0006: keine eigene Route nötig, sofern das Projekt Dialoge über Seiten löst; zu bestätigen).
  - Anpassung der Startlogik in `App`/Shell, damit der Dialog vor der Startseite erscheint.
  - Barrierefreiheit: `SemanticProperties` (Hint/Description) für Auswahlschalter und Schaltflächen.
- **Tests**
  - Unit: `AppSettingsTests_Defaults`, `AppSettingsTests_Validation` (Regelprüfung), neue Tests für das Setup-ViewModel (Auswahl, Validierung, Speichern, Überspringen).
  - Unit: `SettingsServiceTests_LoadSave` (Erststart-Erkennung).
  - Integration: `SettingsServiceTests_RestartPersistence` (Auswahl bleibt nach Neustart erhalten, Dialog erscheint danach nicht mehr).
  - Integration: `DatabaseInitializerTests_Upgrade`, falls eine Migration nötig ist.
- **Dokumentation**: `docs/help/Einstellungen/` (Beschreibung, Ablauf Anwender, Ablauf technisch), `docs/help/index.md` bzw. Startseite, falls der Dialog dort erwähnt wird.

## Implementierungsansatz

- **Auswahl wiederverwenden:** Der Dialog arbeitet auf demselben `AppSettings`-Objekt wie die Optionen. Speichern erfolgt nur über `ISettingsService.SaveAsync`, das bereits über `Validate` prüft. Eine zweite Spritsorten-Quelle wird nicht angelegt.
- **Erststart erkennen (Annahme):** Es existiert derzeit kein Erststart-Merkmal (keine Treffer für `FirstRun`, `Erststart`, `Onboarding`, `IsFirstStart`). Der Erststart wird über das Fehlen der `UserSettingsEntity`-Zeile oder über ein eigenes Kennzeichen erkannt. Beides ist vor der Umsetzung zu entscheiden (siehe offene Fragen).
- **Startablauf:** Die App lädt die Einstellungen beim Start. Ist kein Erststart-Kennzeichen gesetzt, wird der Dialog modal vor der Startseite angezeigt. Nach Speichern oder Überspringen wird das Kennzeichen gesetzt und die Startseite geladen.
- **Sicherung/Wiederherstellung:** Laut Hilfe (`docs/help/Einstellungen/beschreibung.md`, Abschnitt Sicherung) setzt eine Wiederherstellung die Einstellungen auf den Stand der Sicherung zurück. Es ist zu klären, ob eine Wiederherstellung den Dialog unterdrückt (Annahme: ja, wenn die Sicherung Einstellungen enthält).
- **Neuinstallation:** Die Datenbank liegt lokal im Gerät. Eine Neuinstallation erzeugt eine leere Datenbank, damit erscheint der Dialog erneut. Dies entspricht der Anforderung.
- **Erweiterungspunkte:** Keine Events oder Hooks nötig. Die Sichtbarkeit wird aus dem Startzustand (`App`/Shell) bestimmt.

## Konfiguration

- Ebene: **Anwendungseinstellung pro Gerät** (wie bestehende `AppSettings`, lokal in der Datenbank, nicht synchronisiert; vgl. `docs/help/Einstellungen/beschreibung.md`).
- Änderbarkeit: Über den Bereich „Spritsorten“ in den Optionen, unverändert.
- Standardwert bei Überspringen: `AppSettings.CreateDefault()` (alle Spritsorten ausgewählt, Reihenfolge Super E5, Super E10, Diesel).

## Offene Fragen

1. **Mehrfachauswahl:** Sollen eine oder mehrere Spritsorten wählbar sein? Vorschlag: mehrere, mindestens eine (entspricht `AppSettings.Validate` und der Optionen-Logik).
2. **Elektro/Laden:** Soll der Dialog Elektro oder Laden berücksichtigen? Nach ADR 0001 (keine Strompreise in Version 1.0) und ADR 0008 (`Elektro` nur als Fahrzeugkraftstoffart) ist eine Auswahl „Laden“ im Dialog nicht vorgesehen. Bestätigung durch den Stakeholder nötig.
3. **Überspringen:** Ist das Überspringen erlaubt? Vorschlag: ja, mit Standardwert „alle ausgewählt“. Soll der Dialog bei Überspringen beim nächsten Start erneut erscheinen oder endgültig entfallen?
4. **Erststart-Merkmal:** Erkennung über fehlende Einstellungszeile oder über ein eigenes Kennzeichen? Ein eigenes Kennzeichen erfordert eine EF-Core-Migration.
5. **Sicherung/Wiederherstellung:** Unterdrückt eine Wiederherstellung den Dialog? Wenn die Sicherung Einstellungen enthält, Vorschlag: nein, der Dialog entfällt.
6. **Darstellung:** Modaler Dialog als eigene Seite oder Bereich in der Startseite? Muster der bestehenden Dialoge in der App ist zu prüfen (Bestandsaufnahme).
7. **Dokumentationsdatei:** `docs/features.md` existiert im Repository nicht. Die Übersichtsdatei für Features ist daher nicht vorhanden. Der Einstiegspunkt für Feature-Dokumentation ist zu klären.
8. **Herkunft:** Rückmeldung nach TestFlight `v0.1.0-rc.13` (Projekt #1); Issue #17 (Branch `task/issue-17-a127d0ca006a4c01a6b6a497eb518e3a-einrichtungsdialog-beim-ersten`).
