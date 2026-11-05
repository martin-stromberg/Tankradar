# ADR 0012: Anzeigename von Tankstellen

## Status

Angenommen (2026-10-18)

## Kontext

Rückmeldungen aus TestFlight (12.10.2026): Bei manchen Tankstellen liefert der Preisdienst als Namen den Inhaber (z. B.
„Klaus Heinrich“ statt „Westfalen Tankstelle“). Die Liste der Favoriten ist dadurch schwer lesbar. Die Quelle liefert zusätzlich
die Marke (`brand`); die App liest und speichert sie seit Schritt 5 (`Stations.Brand`), zeigte sie bisher aber nur als Chip in
der Detailansicht. Schritt 15a richtet die Anzeige daran aus. Grundlage sind die [Favoritengruppen](0006-favorite-groups.md),
die [Detailansicht](0004-station-detail-design-deviations.md) und die [Sicherung](0011-local-backup-restore.md).

## Entscheidungen

- **Reihenfolge des Anzeigenamens:** eigener Name, sonst Marke, sonst Name der Quelle. Leere Werte und reiner Leerraum gelten als
  fehlend. Darunter steht klein der Stammdatenname, wenn er sich (ohne Beachtung der Groß- und Kleinschreibung) vom angezeigten
  Namen unterscheidet; sonst entfällt die zweite Zeile (z. B. Marke „ARAL“, Name „Aral“). Die Regel liegt an einer Stelle
  (`StationNaming`), `StationInfo.DisplayName` und `SubName` leiten daraus ab. Alle Anzeigeorte verwenden sie: Startseite und
  „In der Nähe“ (`StationTile`), Gruppenansicht, Suchergebnisse, Kartenmarkierungen (Beschreibung für die Sprachausgabe),
  Detailansicht und die Auswahl „Bekannte Tankstellen“ im Tankbuch.
- **Tankbuch-Auswahl unterscheidbar:** Die Chips zeigen den Anzeigenamen (einzeilig, wie bei Chips üblich). Weil mehrere Stationen
  derselben Marke im selben Ort denselben Anzeigenamen hätten (vor 15a hießen sie nach dem Quellnamen, z. B. „JET BERLIN HAUPTSTR. 1“),
  trägt `KnownStation` ein Unterscheidungsmerkmal (`Detail`: der Stammdatenname, sonst Straße und Hausnummer). Nur bei gleicher
  Beschriftung (Groß-/Kleinschreibung unbeachtet) wird es mit „ · “ angehängt („JET · JET BERLIN HAUPTSTR. 1“); bleibt die Beschriftung
  trotzdem gleich, kommt der Ort dazu, zuletzt die Adresse (`KnownStation.Address`). Die Beschriftung steht auch in der Sprachausgabe. Übernommen wird als Name des Vorgangs weiterhin
  nur der Anzeigename (Momentaufnahme, ADR 0009); die Zuordnung zur Tankstelle läuft über die Kennung. Die zweite Zeile unter jedem
  Chip entfällt bewusst: Sie würde die Chipleiste auf kleinen Bildschirmen unruhig machen, und die Unterscheidung ist nur bei Namensgleichheit nötig.
  Die Beschreibung der Kartenmarkierung für die Sprachausgabe nennt zusätzlich die Adresse („JET, Hauptstraße 1, 10115 Berlin, Super E5 …“).
- **`StationListItem.Name` trägt den Anzeigenamen**, neu ist `SubName`. Die Sortierung nach Name folgt damit dem, was der Anwender
  liest. Das Gleiche gilt für `FavoriteEntry.StationName` (Ordnung innerhalb einer Priorität) und `KnownStation.Name`.
  Gespeicherte Tankbuch-Vorgänge behalten ihren Namen als Momentaufnahme (ADR 0009); eine spätere Umbenennung ändert sie nicht.
- **Eigener Anzeigename:** neue Spalte `Stations.CustomName` (`TEXT`, null, höchstens 60 Zeichen). Er wird in der Detailansicht in
  der Karte „Eigener Name“ vergeben, geändert und entfernt (leeres Feld speichern oder **Eigenen Namen entfernen**). Voraussetzung
  ist eine lokal bekannte Tankstelle; eine geöffnete Detailansicht ist es nach dem Abruf in der Regel (scheitert das
  Speichern, meldet die Karte, dass der Name erst nach dem Laden vergeben werden kann). Der Abruf der Quelle überschreibt die Spalte nie
  (`PriceRepository.SaveAsync` setzt Name, Marke, Adresse, Position und Details, nicht den eigenen Namen). Frisch abgerufene
  Tankstellen, die die Quelle ohne eigenen Namen liefert, ergänzt `FuelPriceService` aus der Datenbank (`GetCustomNamesAsync`);
  ein Lesefehler lässt die Anzeige ohne eigenen Namen, die frischen Daten bleiben nutzbar.
- **Migration `AddStationCustomName`** ist rein additiv (`ALTER TABLE … ADD COLUMN`); alle Daten bleiben erhalten, bestehende
  Tankstellen haben keinen eigenen Namen. Die Marke braucht keine Migration; sie wird bereits gespeichert.
- **Sicherung:** Der Neuaufbau (ADR 0011) kopiert die Spalten der Referenzdatenbank auf dem Migrationsstand
  der Sicherung. Neue Sicherungen enthalten den eigenen Namen, Sicherungen vom Stand 0.1.19 und davor (ohne die Spalte) werden
  eingespielt, migriert und haben keinen eigenen Namen. Beides ist durch Integrationstests belegt. Der
  Neuaufbau lehnt einen eigenen Namen mit mehr als 60 Zeichen als ungültige Sicherung ab (`InvalidFile`, Bestand unverändert); ein
  präparierter Name mit 200.000 Zeichen ließ die Oberfläche bei „Vorgang erfassen“ abstürzen. Dasselbe gilt für die übrigen frei
  befüllbaren Textspalten mit großzügigen Obergrenzen (ADR 0011, `BackupTextLimits`).
- **Defensive Anzeige:** Namen, Zweitnamen, Adressen, Gruppennamen und Notizen sind in Kacheln und Listen auf wenige Zeilen begrenzt
  (`MaxLines` mit Auslassungspunkten), damit auch unerwartete Daten nichts sprengen.
- **Datenschutz:** Der eigene Name ist ein vom Anwender frei gewählter Text. Er bleibt lokal und gelangt nie an den Preisdienst;
  Protokolle enthalten nur Ausnahmetypen.
- **Testdaten:** Der Mock-Preisdienst liefert für seine Teststationen, wie viele echte Tankstellen, keine Marke (`"brand":""`);
  die vorhandenen Tests behalten dadurch ihre Namen. Eine Station mit Inhabernamen („Klaus Heinrich“, Marke „Westfalen“) gibt es
  nur nach `AddOwnerNamedStation()`.

## Abweichungen vom Designentwurf

- Der Marken-Chip der Kopfkarte (Entwurf `tankstellen_details_favoriten`, ADR 0004) entfällt. Die Marke ist jetzt der Name der
  Tankstelle in der Überschrift, der Stammdatenname steht klein darunter. Damit steht dieselbe Information nicht zweimal da.
- Die Karte „Eigener Name“ ist neu und hat keinen Entwurf. Sie verwendet die Karte, Eingabe- und Schaltflächenstile der
  Detailansicht (ADR 0004, 0006).

## Folgen

- Bei einem eigenen Namen steht darunter der Stammdatenname, nicht die Marke. So bleibt die zweite Zeile kurz; die Marke ist in
  der Detailansicht nach dem Entfernen des eigenen Namens wieder zu sehen.
- Ändert die Quelle Name oder Marke einer Tankstelle, folgt die Anzeige beim nächsten Abruf; der eigene Name bleibt.
- Die Hinweise im Tankbuch sprechen bei Strom vom Akku statt vom Tank und von Teilladung statt Teilbetankung; der Hinweis zum eigenen Namen nennt, dass er nur auf dem
  Gerät gespeichert, aber Teil der Sicherung ist.
- Zukünftige Anzeigeorte (z. B. Route in Schritt 17) müssen `StationInfo.DisplayName`/`SubName` bzw. `StationNaming` verwenden.
