← [Zurück zur Übersicht](index.md)

# Tankstellen-Detailansicht — Beschreibung

## Zweck

Die Detailansicht zeigt zu einer Tankstelle aus der Ergebnisliste alles, was die Datenquelle Tankerkönig / MTS-K liefert, in Anlehnung an den Designentwurf „Tankstellendetails“. Abweichungen vom Entwurf und die Teile, die zu späteren Schritten gehören (Tankbuch, Navigation), sind in [ADR 0004](../../adr/0004-station-detail-design-deviations.md) festgehalten.

## Angezeigte Angaben

- **Kopfkarte:** Chips für die Preisaktualität (**Live-Preise**, wenn alle Preise unter 60 Minuten alt sind, sonst die Altersangabe des ältesten Preises, z. B. „Preise: vor 135 Min.“) und **Preis unbestätigt** (mindestens ein Preis ist 60 Minuten oder älter), darunter der Name der Tankstelle und Adresse. Als Name steht Ihr **eigener Name**, sonst die **Marke** (z. B. „Westfalen“), sonst der Name der Quelle; ist der Name der Quelle ein anderer (bei manchen Tankstellen der Inhaber, z. B. „Klaus Heinrich“), steht er klein darunter.
- **Info-Box mit Symbolen:** Entfernung (aus der Suche), Öffnungsstatus („Geöffnet“ oder „Geschlossen“) und der Chip **Automat 24/7** (durchgehend geöffnet; nur wenn die Angaben vorliegen).
- **Kraftstoffe:** je in den Einstellungen aktivierter Sorte eine Karte mit Preis (dritte Nachkommastelle hochgestellt, Einheit „€/L“) und Alter „vor X Min.“; ab 60 Minuten ist das Alter amberfarben markiert. Die Reihenfolge entspricht den Einstellungen.
- **Öffnungszeiten:** je Abschnitt eine Zeile („Mo-Fr: 06:00 – 22:00 Uhr“) mit dem Stand der Angabe („Stand: vor 3 Std.“).
- **Eigener Name:** Eingabefeld mit **Namen speichern** und **Eigenen Namen entfernen** (die Schaltfläche erscheint nur, wenn ein eigener Name gespeichert ist). Der Name darf bis zu 60 Zeichen lang sein, wird nur auf diesem Gerät gespeichert (der Preisdienst erfährt ihn nie, ein Preisabruf ändert ihn nicht), ist aber Teil einer [Sicherung](../Sicherung/index.md) und ersetzt in der ganzen App (Startseite, Favoriten, Suche, Karte, Tankbuch-Auswahl) die Marke und den Namen der Tankstelle; der Stammdatenname steht klein darunter. Ein leeres Feld zu speichern entfernt den Namen. Er bleibt bei jedem Preisabruf erhalten und ist Teil der [Sicherung](../Sicherung/index.md).
- **Favoritengruppen:** zugeordnete Gruppen als Chips, **Zu Favoriten hinzufügen** und **Aus Favoriten entfernen** (siehe [Favoritengruppen](../Favoriten/index.md)).
- **Zahlungsmöglichkeiten:** Tankerkönig liefert keine Zahlungsangaben; der Bereich erscheint daher nicht. Strompreise und Ladeinformationen gehören nicht zu Version 1.0.

Angaben, die die Quelle nicht liefert, werden weggelassen (keine leeren Felder, keine Platzhalter).

## Altersgrenze der Öffnungszeiten

Öffnungszeiten und der daraus abgeleitete Chip „Automat 24/7“ stammen aus einer Detailabfrage und gelten **höchstens 24 Stunden** (`DetailFreshness.MaxAge`). Danach werden sie in der Detailansicht und in den Suchergebnissen ausgeblendet; die Detailansicht fragt sie bei bestehender Verbindung neu ab (nach 5 Minuten Cache-Dauer). Darunter wird das Alter als „Stand: vor X Min./Std.“ angezeigt.

## Offline und Wiederverbindung

Ohne Verbindung zeigt die Ansicht die zuletzt bekannten Daten mit Alter, den Hinweis „Es werden die zuletzt bekannten Daten angezeigt.“ und fest oben (er scrollt nicht mit, wie auf der Suchseite) „Offline: keine Verbindung zum Preisdienst.“. **Preise aktualisieren** ist ohne Verbindung deaktiviert. Wird die Verbindung bei geöffneter Ansicht wiederhergestellt, werden veraltete Daten (Offline-Stand oder Preise ab 60 Minuten) automatisch neu abgefragt und der Offline-Hinweis entfällt. Mit Verbindung (auch wenn nur zuletzt bekannte Daten vorliegen) aktualisiert **Preise aktualisieren** die Anzeige (innerhalb der 5-minütigen Cache-Dauer aus dem lokalen Speicher).

## Rückkehr

Zur Ergebnisliste führt der Zurück-Pfeil der Kopfleiste; die Seite hat keine eigene „Zurück“-Schaltfläche.
