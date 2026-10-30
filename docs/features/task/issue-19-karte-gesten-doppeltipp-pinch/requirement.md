# Anforderung: Doppeltipp-Zentrierung und fließender Zwei-Finger-Zoom auf der Kartenansicht

Quelle: `docs/features/task/issue-19-karte-gesten-doppeltipp-pinch/continue.md`
Anlass: Zusatzanforderungen nach Abschluss von Issue #19 (PR #26)
Wörtlicher Wunsch des Anwenders: „bei einem doppelklick (tipp) soll die karte dort zentriert werden und die nächste stufe hineinzoomen. zweifinger gesten soll den zoom steuern wie man es von jeder kartenansicht kennt.“

## Fachliche Zusammenfassung

Die Kartenansicht (`StationMapView`) erhält zwei Gestenbedienungen. Erstens: Ein Doppeltipp (unter Windows ein Doppelklick mit der linken Maustaste) zentriert die Karte auf die angetippte Stelle und vergrößert den Ausschnitt um genau eine Zoomstufe, höchstens bis `MapViewport.MaxZoom`. Zweitens: Die Zwei-Finger-Geste (Kneifen) steuert den Zoom fließend um den Gestenmittelpunkt; während der Geste wird die Kachelebene skaliert angezeigt, bei Gestenende rastet der Zoom auf die nächste ganze Stufe ein, wobei der Punkt unter dem Gestenmittelpunkt stehen bleibt. Beide Gesten sind Nutzeraktionen und lösen `ViewportChanged` aus. Die bisherigen Bedienwege (Zoom-Schaltflächen, Tastatur, Sprachausgabe, Mausrad) bleiben unverändert.

Annahme: Es handelt sich um Gesten am Kartenausschnitt, nicht um eine Auswahl oder Identifikation aus einer bestehenden Datenmenge. Auswahl-/Identifikationsanforderungen liegen nicht vor.

## Betroffene Klassen und Komponenten

**Logik / Kartenmathematik**
- `MapViewport` (`src/Tankradar.MAUI/Services/Map/MapViewport.cs`): Erweiterung um eine Methode, die eine Zoomstufe um einen Bildschirmpunkt herum setzt (Vorschlag: `ZoomAtPoint(int delta, double x, double y)`) und eine Methode für die fraktionale Skalierung um einen Punkt (Vorschlag: `ScaleAtPoint(double factor, double x, double y)`, die Mitte und ganzzahlige Stufe liefert). Vorhandene Methoden `ZoomBy`, `ToLatLon`, `Create`, `PanByPixels` bleiben erhalten und werden wiederverwendet. Hinweis: `ZoomBy` behält die Geo-Mitte bei und ist damit für Doppeltipp und Pinch-Fixpunkt nicht ausreichend.
- Ein Hilfstyp für die Umrechnung Faktor → Zoomstufe (log2) und Gesamtfaktor → Zielstufe ist denkbar; ob als eigene Klasse oder als statische Methode in `MapViewport`, ist offen (siehe Offene Fragen).

**UI-Komponenten**
- `StationMapView` (`src/Tankradar.MAUI/Views/Controls/StationMapView.xaml.cs`):
  - `OnPinchUpdated`: Ersetzt die Logik `_pinchScale = e.Scale` und die Auswertung erst bei `Completed` mit den Schwellen 1,3 / 0,77.
  - Neue Hilfsfunktion für die Anzeige der skalierten Kachelebene während der Geste.
  - `Zoom(int delta)` und `RaiseViewportChanged()` werden für die neuen Pfade wiederverwendet.
- `StationMapView.xaml`: Ergänzung um einen `TapGestureRecognizer` mit `NumberOfTapsRequired="2"` am Grid, analog zum bestehenden `PinchGestureRecognizer`.
- `StationMapView.Pointer.cs` (nur `#if WINDOWS`): Die Zeigerbehandlung setzt `e.Handled = true` (Drücken, Bewegen, Loslassen, Mausrad). Ob der Doppelklick über den Tap-Recognizer zuverlässig ankommt, ist zu prüfen (siehe Implementierungsansatz). Ggf. Doppelklick-Erkennung direkt im Pointer-Teil.
- Markierungen (`MarkerCommand`, Tankstellen-Detailansicht): Verhalten bei Doppeltipp auf eine Markierung muss mit dem Tippen-Verhalten abgestimmt werden (siehe Offene Fragen).

**Tests**
- Unit-Tests `src/Tankradar.Tests.Unit/Unit/MapViewportTests_Math.cs` (oder neue, themenbezogene Datei, z. B. `MapViewportTests_Gestures.cs`, gemäß Konvention „Testklassen nach Themen teilen“):
  - Umrechnung Tippunkt → neue Mitte über `ToLatLon`, inkl. Ränder und Grenzen (`MinZoom`, `MaxZoom`, Breitengradgrenze `MaxLatitude`).
  - Umrechnung Faktor → Zoomstufe (Rundung auf ganze Stufe, Begrenzung).
  - Fixpunkt: Der geografische Punkt unter dem Gestenmittelpunkt bleibt nach dem Zoom an derselben Bildschirmposition (Toleranz festlegen).
  - Doppeltipp-Berechnung: Zoom +1, Obergrenze `MaxZoom` bleibt stehen.
- E2E-Test unter Windows: Doppelklick auf die Karte, Prüfung von Zoomstufe und Mitte sowie des Erscheinens der Nachlade-Schaltfläche (`ViewportChanged`). Kneifgeste per E2E ist voraussichtlich nicht automatisierbar; Abdeckung über Unit-Tests der Fixpunkt- und Rundungslogik (Annahme).

**Dokumentation**
- `docs/help/Suche/beschreibung.md`, Abschnitt „Kartenansicht“ (Zeile zu **Zoomen** und zur Gestenbedienung): Doppeltipp/Doppelklick und fließende Kneifgeste ergänzen. Bestehender Hinweis „Zoomstufen 4 bis 18“ bleibt.
- `docs/adr/0005-map-view-decisions.md`, Abschnitt „Abweichungen vom Designentwurf“: Aussage „Zoom über Schaltflächen und Kneifgeste mit ganzzahligen Stufen“ ist nach der Änderung nur noch für Schaltflächen und Mausrad zutreffend; Kneifgeste rastet ein, verläuft aber fließend. ADR ergänzen oder neuen ADR anlegen (Entscheidung).

## Implementierungsansatz

**Doppeltipp / Doppelklick**
- Unter Windows: Entweder `TapGestureRecognizer` (`NumberOfTapsRequired = 2`) oder Erkennung im Pointer-Teil. Risiko: Die Pointer-Handler setzen `e.Handled = true`, was die Gestenerkennung von MAUI stören kann. Vor der Umsetzung im Prototyp prüfen; Fallback ist eine eigene Doppelklick-Erkennung über Zeit- und Positionsvergleich der `PointerPressed`-Ereignisse.
- Position des Tipps relativ zur Karte ermitteln (`e.GetPosition` bzw. `GetCurrentPoint(target)`), dann `ToLatLon(x, y)` für die neue Mitte, `Create(lat, lon, Zoom + 1, Width, Height)` mit Begrenzung auf `MaxZoom`. Anschließend `Render()` und `RaiseViewportChanged()`.
- Doppeltipp auf eine Markierung: Die Markierung muss weiterhin bedienbar sein; die konkrete Regel ist zu klären (siehe Offene Fragen).
- Einfacher Tipp bleibt unverändert.

**Zwei-Finger-Zoom**
- Während `GestureStatus.Running`: Gesamtfaktor kumulieren (Annahme: `e.Scale` ist inkrementell seit dem letzten Update, so der Befund in der Quelle; vor Umsetzung gegen die MAUI-Dokumentation und das Verhalten auf Windows verifizieren, da die Semantik von `Scale` nicht eindeutig dokumentiert ist). Den Faktor auf die Zoomstufe als Anzeige-Skalierung der Kachelebene anwenden, um `ScaleOrigin` zentriert.
- Bei `GestureStatus.Completed`: Zielstufe = Zoom + round(log2(Gesamtfaktor)), begrenzt auf `MinZoom`..`MaxZoom`. Neue Mitte so wählen, dass der Punkt unter dem Gestenmittelpunkt fixiert bleibt; Anzeige-Skalierung zurücksetzen; `Render()` und `RaiseViewportChanged()`.
- Schwellen 1,3 / 0,77 entfallen. Eine Geste mit Faktor nahe 1 (kein Zoom) ändert die Stufe nicht und löst keine `ViewportChanged`-Meldung aus (Annahme, konsistent mit „Nutzeraktion“).
- Während der Geste wird keine Kachelabfrage für Zwischenstufen gestartet; Kacheln werden erst nach Gestenende nachgeladen (Annahme, zur Vermeidung unnötiger Tile-Anfragen am OSM-Server).

**Gemeinsam**
- Mausrad (`OnPointerWheelChanged` / `MapPointerTracker`) und Zoom-Schaltflächen werden nicht verändert.
- `ViewportChanged` wird bei Doppeltipp und bei Kneifgeste mit Stufenwechsel ausgelöst; das bestehende Verhalten der Nachlade-Schaltfläche (`OnViewportChanged`, ADR 0005 / ADR 0014) bleibt unverändert.

## Konfiguration

Keine neue Konfiguration. Begründung: ADR 0005 legt fest, dass keine neue Konfiguration entsteht; die Gesten sind feste Bedienkonventionen, keine benutzerspezifische oder pro-Datensatz-Einstellung.

## Offene Fragen

1. **Doppeltipp auf eine Tankstellen-Markierung:** Ein Einzeltipp öffnet bereits die Detailansicht. Der erste Tipp eines Doppeltipps würde diese Navigation auslösen, bevor der zweite Tipp erkannt wird. Soll
   a) der erste Tipp weiterhin sofort die Detailansicht öffnen (Doppeltipp auf Markierung dann ohne eigenen Effekt), oder
   b) die Einzeltipp-Auslösung auf Markierungen um die Doppeltipp-Wartezeit verzögert werden?
   Empfehlung: a), da die Anforderung den einfachen Tipp ausdrücklich unverändert lässt.
2. **Doppeltipp-Wartezeit und Erkennung:** Ist die Standard-Doppelklickzeit des Betriebssystems (Windows) maßgeblich, oder eine feste Grenze? Auf mobilen Geräten (iOS) gilt eine eigene Erkennung; ist die Doppeltipp-Gestenerkennung dort ebenfalls gefordert (der Wortlaut „Doppelklick (tipp)“ spricht dafür)?
3. **Semantik von `PinchGestureUpdatedEventArgs.Scale`:** Der Befund im Ist-Code stellt `Scale` als inkrementell dar. Ist das auf allen Zielplattformen (Windows, iOS, Android) gleich? Vor Umsetzung verifizieren.
4. **Einheit von `ScaleOrigin`:** Liegt der Wert relativ zur Elementgröße (0 bis 1) oder in absoluten Koordinaten? Wirkt sich direkt auf die Fixpunktberechnung aus.
5. **Animation:** Soll der Doppeltipp-Zoom animiert erfolgen oder sofort springen? Die Anforderung nennt keine Animation; Annahme: sofort.
6. **Pinch-Geste ohne Stufenwechsel:** Soll bei sehr kleinen Gesten, die auf dieselbe Stufe runden, die Mitte trotzdem am Gestenmittelpunkt verschoben werden, oder bleibt die Ansicht unverändert? Annahme: unverändert.
7. **Wiederholte Doppeltipps an der Grenze:** Bei `MaxZoom` zentriert der Doppeltipp weiterhin auf die angetippte Stelle, ohne Zoom? Annahme: ja (Zentrierung bleibt, Zoom bleibt stehen).
8. **Abstimmung mit ADR 0005:** Neuer ADR oder Ergänzung des bestehenden? Entscheidung des Projekts.
