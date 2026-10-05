← [Zurück zur Übersicht](index.md)

# Kraftstoffpreise und Preis-Cache — Beschreibung

## Zweck

Die Preisdaten sind die Grundlage für Suche, Detailansicht und Favoriten (folgende Entwicklungsschritte). In diesem Schritt entstehen der Abruf, der lokale Preis-Cache, der Offline-Betrieb und die Verbindungserkennung. Sichtbar ist davon im Bereich **Optionen** die Karte **Datenquelle**.

## Funktionsweise

### Was abgerufen wird

- **Umkreissuche:** Tankstellen im Umkreis einer Position (Radius 1 bis 25 km, das ist die Obergrenze der Quelle), gefiltert nach Spritsorten (Super E5, Super E10, Diesel).
- **Tankstellendetails:** Name, Adresse, Preise je Sorte und Öffnungszeiten, soweit die Quelle sie liefert.

Angaben, die die Quelle nicht liefert (zum Beispiel Zahlungsmöglichkeiten), werden nicht erfunden, sondern später in der Oberfläche ausgeblendet. Der Hinweis **Automatentankstelle** wird abgeleitet, wenn die Quelle „durchgehend geöffnet“ meldet oder alle Öffnungszeiten rund um die Uhr gelten.

### Aktualität der Preise

Die Aktualität ergibt sich ausschließlich aus dem Zeitstempel des Abrufs:

| Alter | Bewertung |
|-------|-----------|
| bis 59 Minuten | aktuell |
| ab 60 Minuten | veraltet, Hinweis **Preis unbestätigt** (spätere Ansichten markieren das in Amber) |

Das Alter wird immer als „vor X Min.“ angegeben.

### Lokaler Preis-Cache und Offline-Betrieb

- Jeder abgerufene Preis wird mit Zeitstempel in der lokalen Datenbank gespeichert. Ältere Preisstände bleiben für Auswertungen erhalten.
- Innerhalb von fünf Minuten nach einem Abruf liefert der Cache die Daten statt eines erneuten Abrufs (Schonung des Dienstes gemäß Nutzungsbedingungen).
- Ist kein Abruf möglich (keine Netzverbindung, Dienst nicht erreichbar, Schlüssel fehlt oder wird abgelehnt), liefert die App die zuletzt bekannten Preise samt Alter statt einer Fehlermeldung.
- Die App erkennt, ob eine Netzverbindung besteht und wann sie wiederhergestellt wird. Spätere Ansichten nutzen das, um veraltete Preise selbstständig zu aktualisieren und den Offline-Hinweis zu entfernen.

### Datenschutz und Sicherheit

- Die Kommunikation läuft ausschließlich über HTTPS, mit Zeitlimit (10 Sekunden je Anfrage), bis zu drei Versuchen mit zunehmender Wartezeit (1 s, 2 s) und einem Mindestabstand von einer Sekunde zwischen Anfragen.
- Standortdaten des Anwenders werden nur für die Anfrage verwendet und nie gespeichert; gespeichert werden Tankstellen und Preise.
- Der API-Schlüssel steht nie im Quellcode, liegt zur Laufzeit in der Keychain (iOS) bzw. im Credential Locker (Windows) und wird nie angezeigt oder protokolliert.

### Karte „Datenquelle“ in den Optionen

- Quellenangabe **Daten: Tankerkönig / MTS-K** mit Lizenzhinweis (CC BY 4.0).
- Status des API-Schlüssels (hinterlegt oder nicht hinterlegt).
- Schaltfläche **Verbindung zum Preisdienst prüfen**: sendet auf Ihre Veranlassung eine einzelne Testanfrage und meldet das Ergebnis verständlich.

## Grenzen

- Die Quelle nennt keinen eigenen Preiszeitpunkt; als Zeitstempel gilt der Abrufzeitpunkt.
- Der Radius ist auf 25 km begrenzt (Vorgabe der Quelle).
