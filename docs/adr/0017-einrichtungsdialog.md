# ADR 0017: Einrichtung beim ersten Start (Wahl der Spritsorten)

## Status

Angenommen (2026-10-26)

## Kontext

Rückmeldung nach TestFlight `v0.1.0-rc.13` (Issue #17): Beim ersten Start nach einer Installation soll ein „modaler
Einrichtungsdialog“ erscheinen, in dem der Anwender die Spritsorten wählt (Super E5, Super E10, Diesel). Die Auswahl muss
dieselbe Einstellung schreiben wie die Karte „Spritsorten“ in den Optionen (`AppSettings.FuelTypes`), nur einmal pro
Installation erscheinen und über die Optionen jederzeit änderbar bleiben. [ADR 0006](0006-favorite-groups.md) und
[ADR 0008](0008-vehicle-management.md) legen fest, dass Formulare und Rückfragen als Bereiche in der App erscheinen und nicht
als Dialoge des Betriebssystems.

## Entscheidungen

- **Darstellung in der App statt Dialog des Betriebssystems.** Der Begriff „modal“ aus der Anforderung wird als „überlagert
  die App und blockiert sie bis zum Abschluss“ umgesetzt, nicht als `DisplayAlert`/`DisplayActionSheet` oder Systemdialog.
  Gründe wie in ADR 0006 und ADR 0008: Systemdialoge lassen sich im FlaUI-Test (Auslieferungs-Gate) nicht zuverlässig bedienen,
  der Bereich ist mit Tastatur und Sprachausgabe bedienbar (AutomationIds `Setup.*`, `SemanticProperties`).
- **Technische Umsetzung des Overlays.** Eine .NET-MAUI-`Shell` kann keine eigenen Elemente über TabBar und Seiten legen. Der
  Bereich ist deshalb als `DataTemplate` (`SetupOverlayTemplate`) in `AppShell.xaml` beschrieben; `AppShell` erzeugt daraus eine
  schlichte `ContentPage` und legt sie mit `Navigation.PushModalAsync` über die Shell. Es gibt keine Route und keinen Eintrag im
  Navigationsstapel der Shell; die TabBar ist bis zum Abschluss verdeckt. Die Gestaltung folgt Ebene 3 (Modal) aus
  `docs/design-system.md` (Karte mit `ShadowLevel3`).
- **Erststart-Erkennung über die vorhandene Zeile `UserSettings`.** `SetupStatusService` (`ISetupStatusService`) wartet auf
  `IDatabaseInitializer.InitializeAsync` und prüft, ob die Zeile mit `UserSettingsEntity.SingletonId` existiert. Die Zeile entsteht
  nur durch `ISettingsService.SaveAsync`. Es gibt kein eigenes Kennzeichen, keine Migration und keine Schemaänderung.
  `ISettingsService` bleibt unverändert, weil `LoadAsync` fehlende Werte bewusst durch Standardwerte ersetzt.
- **Startablauf.** `App.CreateWindow` erzeugt `AppShell` über die registrierte Fabrik `Func<AppShell>`; die Dependency Injection
  liefert dabei `ISetupStatusService`, `SetupViewModel` und Logger. Die Shell wird erst in `CreateWindow` erzeugt, weil ihr XAML
  die App-Ressourcen benötigt. Nach dem Laden der Shell (`Loaded`) läuft die Prüfung; die Datenbankinitialisierung in `App` bleibt
  Fire-and-forget, die Prüfung wartet aber auf denselben Initialisierungslauf. Ein Fehler wird protokolliert und die Einrichtung
  nicht gezeigt, damit die Shell bedienbar bleibt.
- **Speichern und Überspringen schreiben dieselbe Einstellung.** „Speichern“ übernimmt die Auswahl (mindestens eine Sorte,
  Standardreihenfolge E5, E10, Diesel) mit den übrigen Standardwerten; „Überspringen“ speichert `AppSettings.CreateDefault()`.
  Danach existiert die Zeile, die Einrichtung erscheint nicht erneut. Bei einem Speicherfehler bleibt sie mit Meldung offen.
- **Kein Elektro/Laden, keine Verschiebe-Schaltflächen.** Die Einrichtung bietet nur die Sorten der Preisquelle an
  ([ADR 0001](0001-no-electricity-prices-in-v1.md)); die Reihenfolge wird in den Optionen geändert.

## Folgen

- **Bestandsinstallationen ohne Zeile `UserSettings`** (nur aktualisiert, nie in den Optionen gespeichert) sehen die
  Einrichtung einmalig nach dem Update. Das ist bewusst hingenommen.
- **Wiederherstellung einer Sicherung:** Enthält die Sicherung die Zeile, erscheint beim nächsten Start keine Einrichtung; fehlt
  sie, erscheint die Einrichtung beim nächsten Start, nicht sofort nach der Wiederherstellung. Es gibt keinen Sonderpfad.
- **E2E-Tests:** Die gemeinsame Testbasis (`E2ETestBase`) beendet die Einrichtung nach dem Start über „Überspringen“, wenn die
  Testdatenbank vor dem Start keine Zeile `UserSettings` enthält. Die App legt dabei die Zeile mit den Standardwerten an; die
  übrigen Tests arbeiten so auf einer eingerichteten Installation. Eine Zeile vor dem ersten Start direkt in die Datei zu
  schreiben ist nicht möglich, weil das Schema erst durch die Migration der App entsteht. Die Tests `SetupE2ETests_*` schalten
  dieses Verhalten ab und prüfen die Einrichtung selbst.
