# Offene Aufgaben

Erstellt am: 2026-10-30
Anlass: Zusatzanforderungen des Anwenders nach Abschluss von Issue #19 (PR #26, gemergt)

Die folgenden Aufgaben sind nach dem Abschluss hinzugekommen und müssen in einem erneuten Lauf bearbeitet werden.

## Zusatzanforderungen

- [ ] **Doppeltipp zentriert und zoomt hinein:** Ein Doppeltipp (bzw. Doppelklick unter Windows) auf die Karte zentriert die Karte auf die angetippte Stelle und zoomt eine Stufe hinein (`MapViewport.ZoomBy(1)`, höchstens `MapViewport.MaxZoom`). Der Doppeltipp auf eine Tankstellen-Markierung darf weiterhin die Markierung bedienen; ein einfacher Tipp behält sein bisheriges Verhalten. Die Änderung zählt als Nutzeraktion, löst also `ViewportChanged` aus (Nachlade-Schaltfläche erscheint). Unit-Test für die Umrechnung Tippunkt → neue Mitte (`MapViewport.ToLatLon`), E2E-Test unter Windows (Doppelklick).
- [ ] **Zwei-Finger-Zoom wie in üblichen Kartenansichten:** Die Kneifgeste steuert den Zoom fließend und um den Mittelpunkt der Geste (`PinchGestureUpdatedEventArgs.ScaleOrigin`), nicht erst nach Gestenende um eine feste Stufe. Befund im Ist-Code (`StationMapView.xaml.cs`, `OnPinchUpdated`): `e.Scale` ist in MAUI der Faktor seit dem letzten Update (inkrementell), der Code behandelt ihn aber wie einen Gesamtfaktor (`_pinchScale = e.Scale`) und wertet erst bei `Completed` mit den Schwellen 1,3 / 0,77 aus. Dadurch löst die Geste praktisch nie oder nur zufällig aus. Umsetzung: Faktoren kumulieren, während der Geste die Kachelebene skaliert anzeigen (um `ScaleOrigin`), bei Gestenende auf die nächste ganze Zoomstufe runden (log2 des Gesamtfaktors) und den Ausschnitt so wählen, dass der Punkt unter dem Gestenmittelpunkt stehen bleibt; Grenzen `MinZoom`/`MaxZoom` einhalten. Unit-Tests für die Umrechnung Faktor → Zoomstufe und Fixpunkt; Mausrad und Zoom-Schaltflächen bleiben unverändert.

## Hinweise

- Bedienung ohne Gesten (Zoom-Schaltflächen, Tastatur, Sprachausgabe) muss erhalten bleiben.
- Hilfe (`docs/help/Suche/beschreibung.md`, Abschnitt Kartenansicht) und ggf. ADR 0005 um die neuen Gesten ergänzen.
