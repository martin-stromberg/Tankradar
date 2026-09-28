# ADR 0001: Keine Strompreis-Elemente in Version 1.0

## Status

Angenommen (2026-09-28)

## Kontext

Der verbindliche Designentwurf (`design-draft/stitch_smart_fuel_charge_tracker.zip`, Screens
`startseite_favoriten_gruppen` u. a.) enthält Gestaltungselemente für Ladestationen und
Strompreise: einen Umschalter „Kraftstoff/Laden" (Segmented Control) sowie Ladestecker-
Kennzeichnungen (z. B. „HPC 300kW", „DC 150kW", „AC 22kW") auf Stationskarten und Karten-Pins.

Für Version 1.0 der App „Tankradar" gibt es in Deutschland keine offizielle, frei nutzbare
Strompreis-API. Das Ladesäulenregister der Bundesnetzagentur liefert ausschließlich Standorte und
Leistungsdaten, keine Preise. Eine reine Standortanzeige ohne Preise wäre eine nicht angeforderte
Erweiterung und stünde außerhalb der ursprünglichen Anforderung, die sich auf Preisvergleich und
-transparenz konzentriert.

## Entscheidung

Die für Strompreise und Ladestationen vorgesehenen Designelemente werden in Version 1.0 **nicht**
implementiert:

- der Umschalter „Kraftstoff/Laden" (Segmented Control) in Suche, Karte, Startseite und Detailansicht
- die Ladestecker-Kennzeichnungen (HPC/DC/AC mit Leistungsangabe) auf Stationskarten und Karten-Pins
- der Abruf von Ladestationen und Strompreisen sowie alles, was davon fachlich abhängt (Filter nach
  Strom/Ladetyp, Strompreise in Detailansicht/Startseite/„In der Nähe", Ladestationen als Favoriten,
  Sicherung von Strompreisständen)

Diese Entscheidung wurde vom Stakeholder am 2026-09-28 getroffen (siehe
`docs/projects/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar/project-plan.md`, Abschnitt
„Grobe Vorgehensentscheidungen", Zeile „Keine Strompreise in 1.0").

Erhalten bleiben alle Funktionen, die keine Strompreisquelle voraussetzen:

- Fahrzeuge mit Kraftstoffart „Elektro" in der Fahrzeugverwaltung
- manuell erfasste Ladevorgänge im Tankbuch (Menge in kWh, Preis und Gesamtbetrag durch den Nutzer
  eingegeben)
- Verbrauchs- und Kostenauswertung in kWh/100 km für Elektrofahrzeuge

## Begründung

Keine offizielle, frei nutzbare Strompreis-API vorhanden (Stakeholder-Entscheidung zu einem offenen
Punkt der Anforderung: „wenn es keine API gibt, dann eben ohne Strompreise"). Die
Ladestations-Funktionen des Designentwurfs drehen sich fachlich um Preise; ohne Preisquelle wäre
eine Umsetzung nicht sinnvoll möglich, ohne über den angeforderten Umfang hinauszugehen.

## Konsequenzen

- Der Designentwurf wird an dieser Stelle bewusst nicht vollständig umgesetzt; die Abweichung ist
  hiermit dokumentiert.
- Benutzer sehen in Version 1.0 ausschließlich Kraftstoffpreise, keine Ladestations- oder
  Strompreisanzeigen.
- Ladevorgänge sind im Tankbuch weiterhin manuell erfassbar (keine API-Abhängigkeit).
- Sollte künftig eine offizielle, frei nutzbare Strompreis-API verfügbar werden, kann diese
  Entscheidung revidiert und der Umschalter „Kraftstoff/Laden" sowie die Ladestecker-Kennzeichnungen
  nachträglich ergänzt werden.
