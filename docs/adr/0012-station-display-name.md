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
  Detailansicht und die Auswahl „Bekannte Tankstellen“ im Tankbuch (dort nur der Anzeigename ohne zweite Zeile, wie bei den Chips üblich).
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
- **Sicherung:** Keine Codeänderung. Der Neuaufbau (ADR 0011) kopiert die Spalten der Referenzdatenbank auf dem Migrationsstand
  der Sicherung. Neue Sicherungen enthalten den eigenen Namen, Sicherungen vom Stand 0.1.19 und davor (ohne die Spalte) werden
  eingespielt, migriert und haben keinen eigenen Namen. Beides ist durch Integrationstests belegt.
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
- Zukünftige Anzeigeorte (z. B. Route in Schritt 17) müssen `StationInfo.DisplayName`/`SubName` bzw. `StationNaming` verwenden.
