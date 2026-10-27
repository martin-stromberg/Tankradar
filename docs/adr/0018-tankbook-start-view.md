# ADR 0018: Tankbuch öffnet mit den Tank- und Ladevorgängen

## Status

Angenommen (2026-10-27)

## Kontext

Rückmeldung nach TestFlight `v0.1.0-rc.13` (Issue #20): Der Tab „Tankbuch“ öffnete bisher die Fahrzeugliste; die Tank- und
Ladevorgänge waren erst über die Schaltfläche „Tank- und Ladevorgänge“ einer Fahrzeugkarte erreichbar (Shell-Route
`tankbookentries`, [ADR 0009](0009-tankbook-entries.md)). Gewünscht ist, dass der Tab direkt die Vorgänge des zuletzt genutzten
Fahrzeugs zeigt, das Fahrzeug unauffällig im Kopf gewechselt wird und die Fahrzeugverwaltung erreichbar bleibt. Ohne Fahrzeug
soll ein Hinweis mit direktem Weg zum Anlegen erscheinen.

## Entscheidungen

- **Tab-Wurzel ist die Vorgangsseite.** `AppShell` zeigt im Tab `tankbook` die `TankbookEntriesPage`
  (`TankbookEntriesViewModel`). Es gibt keine zweite Seite je Fahrzeug auf dem Navigationsstapel mehr; die Route
  `tankbookentries`, der Parameter `vehicleId` sowie `ITankbookNavigator.OpenEntriesAsync`/`CloseAsync` entfallen. Damit
  entfällt auch das Schließen einer Seite, deren Fahrzeug nicht mehr existiert. Seitentitel bleibt „Tankbuch“; der Fahrzeugname
  ist die Überschrift im Kopf.
- **Fahrzeugverwaltung als eigene Route.** Die bisherige Fahrzeugliste (`TankbookPage`, `TankbookViewModel`) ist die
  Verwaltung mit dem Titel „Fahrzeuge“ (Route `tankbookvehicles`). Sie wird über „Fahrzeuge verwalten“ im Kopf gepusht
  (`ITankbookNavigator.OpenVehicleManagementAsync(startCreate)`); mit `startCreate = true` öffnet sie sofort das Formular
  „Neues Fahrzeug“ (`IQueryAttributable`). Formulare, Löschrückfrage mit Zählung und Neuzählung bleiben unverändert
  ([ADR 0008](0008-vehicle-management.md), ADR 0009). Die Fahrzeugkarte verliert die Schaltfläche „Tank- und Ladevorgänge“.
- **Fahrzeugwechsel im Kopf.** Eine Schaltfläche mit dem Fahrzeugnamen klappt Chips aller Fahrzeuge (nach Namen geordnet) auf;
  gleiches Auswahlmuster wie „Bekannte Tankstellen“. Die Auswahl erfolgt nur über den Namen, nie über die Kennung. Der Hinweis
  eines Chips lautet z. B. „Fahrzeug „Golf“ auswählen“. Die Auswahl bleibt auch bei nur einem Fahrzeug sichtbar.
- **Gerätelokale Speicherung der Auswahl.** Gemerkt wird nur die Kennung über `IVehicleSelectionStore`
  (`PreferencesVehicleSelectionStore`, `IPreferences`, Schlüssel `tankbook.selectedVehicleId`). Keine Schemaänderung, keine
  Migration; die Kennung ist nicht Teil der Sicherung ([ADR 0011](0011-local-backup-restore.md)). Im Testmodus nutzt die App je
  Testdatenverzeichnis einen eigenen Bereich der Einstellungen, damit sich Testläufe nicht beeinflussen.
- **Auswahlregel.** `VehicleSelectionResolver`: das gemerkte Fahrzeug, sofern es noch existiert; sonst das erste nach Name; ohne
  Fahrzeuge keines. Weicht das Ergebnis von der gemerkten Kennung ab (gelöscht, nie gesetzt), wird die neue gemerkt; es erscheint
  keine Fehlermeldung. Nach einer Wiederherstellung wird die gemerkte Kennung verworfen, weil Kennungen der Sicherung anderen
  Fahrzeugen entsprechen können.
- **Leerzustand.** Ohne Fahrzeug zeigt der Kopf den Hinweis „Noch kein Fahrzeug angelegt …“ und „Fahrzeug hinzufügen“
  (öffnet die Verwaltung mit dem Formular). Erfassen, Auswertung und Export sind ausgeblendet.
- **Sprachausgabe.** Beim Erscheinen des Tabs (auch bei der Rückkehr aus der Verwaltung) und nach jedem Wechsel sagt
  `IScreenReaderAnnouncer` das gewählte Fahrzeug an („Fahrzeug „Golf““).
- **Auswertung und Export** bleiben je Fahrzeug; der Wechsel setzt beide zurück und lädt sie für das neue Fahrzeug.

## Folgen

- Ersetzt in ADR 0009 die Entscheidung „Eigene Seite je Fahrzeug … über die Schaltfläche auf der Fahrzeugkarte“.
- Die E2E-Tests erreichen die Vorgänge über den Tab und die Auswahl im Kopf (`TankbookE2EDriver.OpenEntries`), die Verwaltung
  über „Fahrzeuge verwalten“.
- Auf einem neuen Gerät oder nach einer Wiederherstellung zeigt der Tab zunächst das erste Fahrzeug nach Name.
