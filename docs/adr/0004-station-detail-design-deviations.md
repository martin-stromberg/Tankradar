# ADR 0004: Abweichungen der Detailansicht vom Designentwurf

## Status

Angenommen (2026-10-06)

## Kontext

Der Designentwurf (`design-draft/stitch_smart_fuel_charge_tracker.zip`, Screen
`tankstellen_details_favoriten`) ist verbindlich. Die Detailansicht (Entwicklungsschritt 8) setzt davon
um: Kopfkarte mit Marken-Chip, Chip zur Preisaktualität („Live-Preise“ bei Preisen unter 60 Minuten,
sonst Altersangabe des ältesten Preises, z. B. „Preise: vor 135 Min.“), Name und Adresse, Info-Box mit
Symbolen (Entfernung, Öffnungsstatus, Chip „Automat 24/7“, nur bei vorliegenden Daten), Preiskarten mit
hochgestellter dritter Nachkommastelle und Einheit „€/L“ (Schrift `price-hero`), die Rückkehr über den
Zurück-Pfeil der Kopfleiste der Shell sowie einen fest oben stehenden Offline-Hinweis.

## Entscheidung

Folgende Teile des Entwurfs sind bewusst nicht oder abweichend umgesetzt:

- **Favoriten (Stern, Favoritengruppen):** gehören zu Entwicklungsschritt 10.
- **„Im Tankbuch erfassen“:** gehört zu Entwicklungsschritt 13.
- **„Navigation“ / Route und „Anfahrt ca. X Min.“:** gehören zu Entwicklungsschritt 17 (Routing); die
  Fahrzeit entfällt bis dahin, die Info-Box zeigt nur die Entfernung aus der Suche.
- **Preisverlauf, „Spartipp“, Trendpfeile, „Top-Preis in Ihrer Region“, „unter Tagesschnitt“:** Die
  Quelle (Tankerkönig) liefert nur den aktuellen Preis, keinen Verlauf und keinen Regionsvergleich;
  es gibt keine Datenquelle, erfundene Werte werden nicht angezeigt.
- **„Ausstattung & Services“:** Die Quelle liefert keine Ausstattungs- oder Zahlungsangaben und die
  Anforderung von Schritt 8 verlangt sie nicht; der Bereich entfällt (keine Platzhalter).
- **Umschalter Kraftstoffe / Laden (EV), Ladepunkte:** Strom ist laut ADR 0001 nicht Teil von
  Version 1.0.
- **Sortenuntertitel (B7, ROZ 95):** Die App führt keine solchen Zusatzbezeichnungen; stattdessen steht
  unter dem Sortennamen das Alter des Preises („vor X Min.“, ab 60 Minuten amberfarben).
- **Preisfarbe:** Alle Preise erscheinen in der Preisschrift `price-hero` in Teal bzw. Emerald (Dunkelmodus);
  die Hervorhebung nur des günstigsten Preises entfällt mangels Vergleichsdaten.
- **Symbole:** Statt der Material-Symbols-Schrift werden die Symbole `near_me` und `schedule`
  (Apache-2.0) als Vektorpfade eingebettet; die Schrift selbst wird nicht mitgeliefert.
- **Zusätzlich zum Entwurf** (begründet): „Preise aktualisieren“ (Abruf auf Wunsch; ohne Verbindung
  deaktiviert, der Offline-Hinweis oben erklärt den Grund), Quellenhinweis zu zuletzt bekannten Daten,
  Quellenangabe „Daten: Tankerkönig / MTS-K“ (CC BY 4.0) und die Öffnungszeiten-Karte mit Stand der
  Angabe (die Daten liefert die Quelle, der Entwurf zeigt sie nicht).
- **Offline-Hinweis:** Auf Detail- und Suchseite steht er fest oberhalb des scrollenden Inhalts.

## Folgen

Spätere Schritte (10, 13, 17) ergänzen die zurückgestellten Elemente an der Detailansicht. Bis dahin
sind sie dort nicht sichtbar; die Hilfe verweist darauf.
