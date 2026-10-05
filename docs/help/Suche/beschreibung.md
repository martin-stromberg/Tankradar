← [Zurück zur Übersicht](index.md)

# Umkreissuche — Beschreibung

## Zweck

Im Bereich **Karte** finden Sie Tankstellen in Ihrer Nähe und sehen auf einen Blick, was Ihre Kraftstoffe dort kosten. Die Preise stammen von Tankerkönig / MTS-K (siehe [Kraftstoffpreise und Preis-Cache](../Preisdaten/index.md)).

## Funktionsweise

### Suche starten

Unter **Suchradius** geben Sie einen Radius in **km** ein (ganze Zahl von 1 bis 25, voreingestellt 5) und tippen auf **Suchen** (oder bestätigen die Eingabe mit der Eingabetaste). Während der Suche erscheint ein Ladeanzeiger und die Schaltfläche ist gesperrt. Eine neue Suche ersetzt eine noch laufende. Bei ungültiger Eingabe erscheint „Bitte einen Radius von 1 bis 25 km eingeben.“ und es wird nichts abgefragt.

Die Suche läuft nur, wenn Sie sie auslösen; beim Öffnen des Bereichs wird weder der Standort abgefragt noch gesucht.

### Standort

Tankatlas ermittelt Ihren Standort erst beim Tippen auf **Suchen** und nur, wenn die Einstellung **Standort und GPS** in den **Optionen** auf **Immer** oder **Nur bei Nutzung** steht. Der Standort wird nie gespeichert.

| Situation | Hinweis |
|-----------|---------|
| Einstellung steht auf **Nie** | „Die Standortnutzung ist ausgeschaltet. Du kannst sie unter „Optionen“ ändern.“ (es wird nichts abgefragt) |
| Zugriff auf den Standort verweigert | „Ohne Zugriff auf den Standort ist keine Umkreissuche möglich. Bitte erlaube den Standortzugriff in den Systemeinstellungen.“ |
| Standort nicht ermittelbar | „Der Standort konnte nicht ermittelt werden. Bitte versuche es später erneut.“ |

### Ergebnisliste

Je Tankstelle zeigt die Liste:

- den Namen und die Entfernung (z. B. „1,4 km“), wenn die Quelle eine Position liefert,
- „Geöffnet“ oder „Geschlossen“, wenn die Quelle den Status liefert,
- je in den **Optionen** gewählter Spritsorte den Preis (z. B. „1,859 €“) und das Alter („vor X Min.“; ab 60 Minuten in Amber), in der Reihenfolge, die Sie in den **Optionen** festgelegt haben,
- die Hinweise **Preis unbestätigt** (mindestens ein angezeigter Preis ist veraltet) und **Automatentankstelle**.

Gibt es keine Tankstelle im Umkreis, erscheint „Keine Tankstellen im Umkreis gefunden“.

### Filtern und Sortieren

- **Spritsorte:** **Alle** oder eine Ihrer gewählten Sorten. Beim Filter bleiben nur Tankstellen mit einem Preis für diese Sorte.
- **Sortierung:** **Preis**, **Entfernung** oder **Name**; vorbelegt mit der **Standardsortierung** aus den **Optionen**. Bei **Preis** gilt die gefilterte Sorte, bei **Alle** die erste gewählte Sorte; Tankstellen ohne Preis dafür stehen am Ende.

Filter und Sortierung wirken sofort auf die vorhandene Liste, ohne neue Suche.

### Offline und Fehler

- Ohne Verbindung (oder wenn der Preisdienst nicht erreichbar ist) zeigt die Kopfzeile „Offline: keine Verbindung zum Preisdienst.“; vorhandene gespeicherte Preise erscheinen mit ihrem Alter und dem Hinweis „Es werden die zuletzt bekannten Preise angezeigt.“ Gibt es keine gespeicherten Preise für den Umkreis, lautet die Meldung „Keine Netzverbindung und keine gespeicherten Preise für diesen Umkreis.“
- Weitere Meldungen: „Der Preisdienst ist nicht erreichbar.“, „Es ist kein API-Schlüssel hinterlegt.“, „Der Preisdienst hat die Anfrage abgelehnt. Bitte den API-Schlüssel prüfen.“, „Der Preisdienst hat eine unerwartete Antwort geliefert.“ und allgemein „Die Suche konnte nicht durchgeführt werden.“

## Beispiele

**Diesel im Umkreis von 10 km, günstigste zuerst:** Radius 10 eingeben, **Suchen** tippen, Filter **Diesel**, Sortierung **Preis**.

**Nächstgelegene Tankstelle:** Sortierung **Entfernung** wählen.

## Einschränkungen

- Die Ergebnisse erscheinen als Liste; eine Kartenansicht gibt es noch nicht.
- Der Radius ist auf 25 km begrenzt (Vorgabe der Preisquelle).
- Die Liste zeigt nur Preise der in den **Optionen** gewählten Spritsorten.
