# ADR 0002: Abweichungen der Suchseite vom Designentwurf

## Status

Angenommen (2026-10-05)

## Kontext

Der Designentwurf (`design-draft/stitch_smart_fuel_charge_tracker.zip`, Screen
`suche_kartenansicht`) ist verbindlich. Die Suchseite („Karte“) setzt davon um: Radius, Spritsorte
und Sortierung als Chips (Pill-Form, ausgewählt in Teal mit weißem Text, mindestens 44 px hoch),
Ergebniskarten mit 16 px Rundung, Schattenebene 1, hervorgehobenem Preis (`price-hero`) und
Adresszeile. Der Radius (1 bis 25 km, Standard 5 km) wird in den Stufen 1, 2, 5, 10, 15 und 25 km gewählt.

## Entscheidung

Folgende Teile des Entwurfs sind bewusst nicht oder abweichend umgesetzt:

- **Kartenansicht, Karten-Pins und Preisniveau-Farben:** Der Schritt liefert die Ergebnisliste; eine
  Karte ist ein späterer Entwicklungsschritt.
- **Glassmorphismus der Suchleiste, Suchfeld mit Markenfavoriten, GPS-Fokus-Button:** gehören zur
  Kartenansicht bzw. zu späteren Schritten.
- **Markenlogos, Trendindikator, Belegungsampel:** Die Quelle liefert keine Logos und keine
  Trenddaten; Strom und Belegung sind laut ADR 0001 nicht Teil von Version 1.0.
- **Stufen-Chips statt freier Eingabe:** Gemäß Designentwurf wird der Radius über Chips mit den
  Stufen 1, 2, 5, 10, 15 und 25 km gewählt, nicht über ein freies Zahlenfeld. Die Stufen decken den
  zulässigen Bereich von 1 bis 25 km (Obergrenze der Preisquelle) sinnvoll ab und sind
  fingerfreundlich bedienbar; Fehleingaben sind ausgeschlossen.
- **Chips als Schaltflächen:** Die Chips sind `Button`-Elemente mit Auswahlzustand statt eines
  eigenen Segmentsteuerelements. Der Auswahlzustand wird zusätzlich als Hinweistext
  („Ausgewählt“) für Bedienhilfen bereitgestellt.
- **Preisschrift:** `price-hero` (Inter, 28 px) in Teal bzw. Emerald im Dunkelmodus; die
  Nicht-Proportionalschrift JetBrains Mono ist technischen Labels vorbehalten.
- **Schatten:** Per `Border.Shadow` mit den Werten der Ebene 1 (`0 1px 3px`, 4 % Deckkraft); auf
  Plattformen ohne Schattenunterstützung entfällt er ohne Funktionsverlust.
- **Listenlänge:** Die Liste zeigt höchstens 25 Karten gleichzeitig („Weitere anzeigen“), damit
  Filter- und Sortierwechsel auch bei mehreren hundert Treffern zügig bleiben.

## Folgen

Die Radiuseingabe über ein freies Zahlenfeld entfällt; die Prüfung 1 bis 25 km vor dem Abruf bleibt
im `MapViewModel` und im Preisdienst erhalten.
