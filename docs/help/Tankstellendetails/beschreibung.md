← [Zurück zur Übersicht](index.md)

# Tankstellen-Detailansicht — Beschreibung

## Zweck

Die Detailansicht zeigt zu einer Tankstelle aus der Ergebnisliste alles, was die Datenquelle Tankerkönig / MTS-K liefert, nach dem Designentwurf „Stationsdetails“.

## Angezeigte Angaben

- **Kopfkarte:** Marke (falls sie vom Namen abweicht), Name, Adresse, Entfernung (aus der Suche), Öffnungsstatus („Geöffnet“ oder „Geschlossen“) und die Hinweise **Preis unbestätigt** (mindestens ein Preis ist 60 Minuten oder älter) sowie **Automatentankstelle** (durchgehend geöffnet).
- **Kraftstoffe:** je in den Einstellungen aktivierter Sorte eine Karte mit Preis und Alter „vor X Min.“; ab 60 Minuten ist das Alter amberfarben markiert. Die Reihenfolge entspricht den Einstellungen.
- **Öffnungszeiten:** je Abschnitt eine Zeile („Mo-Fr: 06:00 – 22:00 Uhr“) mit dem Stand der Angabe („Stand: vor 3 Std.“).
- **Zahlungsmöglichkeiten:** Tankerkönig liefert keine Zahlungsangaben; der Bereich erscheint daher nicht. Strompreise und Ladeinformationen gehören nicht zu Version 1.0.

Angaben, die die Quelle nicht liefert, werden weggelassen (keine leeren Felder, keine Platzhalter).

## Altersgrenze der Öffnungszeiten

Öffnungszeiten und der daraus abgeleitete Hinweis „Automatentankstelle“ stammen aus einer Detailabfrage und gelten **höchstens 24 Stunden** (`DetailFreshness.MaxAge`). Danach werden sie in der Detailansicht und in den Suchergebnissen ausgeblendet; die Detailansicht fragt sie bei bestehender Verbindung neu ab (nach 5 Minuten Cache-Dauer). Darunter wird das Alter als „Stand: vor X Min./Std.“ angezeigt.

## Offline und Wiederverbindung

Ohne Verbindung zeigt die Ansicht die zuletzt bekannten Daten mit Alter, den Hinweis „Es werden die zuletzt bekannten Daten angezeigt.“ und in der Kopfzeile „Offline: keine Verbindung zum Preisdienst.“. Wird die Verbindung bei geöffneter Ansicht wiederhergestellt, werden veraltete Daten (Offline-Stand oder Preise ab 60 Minuten) automatisch neu abgefragt und der Offline-Hinweis entfällt. Mit Verbindung aktualisiert **Preise aktualisieren** die Anzeige (innerhalb der 5-minütigen Cache-Dauer aus dem lokalen Speicher).
