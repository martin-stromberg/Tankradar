← [Zurück zur Übersicht](index.md)

# Umkreissuche — Beschreibung

## Zweck

Im Bereich **Karte** finden Sie Tankstellen in Ihrer Nähe und sehen auf einen Blick, was Ihre Kraftstoffe dort kosten. Die Preise stammen von Tankerkönig / MTS-K (siehe [Kraftstoffpreise und Preis-Cache](../Preisdaten/index.md)).

## Funktionsweise

### Suche starten

Unter **Suchradius** wählen Sie per Chip eine Stufe: **1**, **2**, **5**, **10**, **15** oder **25 km** (voreingestellt 5 km) und tippen auf **Suchen**. Während der Suche erscheint ein Ladeanzeiger und die Schaltfläche ist gesperrt. Eine neue Suche ersetzt eine noch laufende. Die Oberfläche bietet nur gültige Radien (1 bis 25 km) an; intern wird jeder Radius vor dem Abruf geprüft, ein ungültiger Wert löst keine Abfrage aus (Meldung „Bitte einen Radius von 1 bis 25 km eingeben.“).

Die Suche läuft nur, wenn Sie sie auslösen; beim Öffnen des Bereichs wird weder der Standort abgefragt noch gesucht.

### Suchart: Standort oder Adresse

Unter **Suchen nach** wählen Sie die Suchart: **Aktueller Standort** (voreingestellt) oder **Adresse, Ort oder PLZ**. Ein Wechsel der Suchart leert die Ergebnisliste und Meldungen; eine eingegebene Adresse bleibt im Feld stehen, solange die App läuft.

### Suche nach Adresse, Ort oder PLZ

In der Suchart **Adresse, Ort oder PLZ** erscheint ein Eingabefeld („Adresse, PLZ oder Ort, z. B. Frankfurt oder 60311“) mit der Schaltfläche **Eingabe löschen**. Die Suche startet nur, wenn Sie **Suchen** tippen oder auf der Tastatur die Such-/Eingabetaste betätigen; es gibt keine Autovervollständigung und beim Tippen geht nichts ins Netz. Der Radius wird wie bei der Standortsuche über die Chips **1**, **2**, **5**, **10**, **15** oder **25 km** gewählt (Obergrenze der Preisquelle).

Tankatlas wandelt die Eingabe über den Dienst **OpenStreetMap-Nominatim** in eine Position um (Suche auf Deutschland beschränkt, nur der beste Treffer) und sucht dann Tankstellen im gewählten Umkreis dieser Position. Die Ergebnisliste ist dieselbe wie bei der Standortsuche (Preise der gewählten Sorten mit Alter, **Entfernung zur gesuchten Adresse**, Hinweise, Filter und Sortierung). Über der Liste steht „Suche rund um: <gefundener Ort>“. Die Adresssuche braucht keinen Standort und funktioniert auch bei **Standort und GPS** = **Nie**.

Quellenangabe: Im Adressmodus steht im Suchbereich „Geodaten © OpenStreetMap-Mitwirkende“.

**Eingabeprüfung (vor jeder Anfrage):** Leerraum am Rand wird entfernt, mehrfacher Leerraum zusammengefasst. Zulässig sind 3 bis 120 Zeichen aus Buchstaben, Ziffern, Leerzeichen und den Satzzeichen `. , - ' / ( ) & + # :`. Typografische Zeichen, die die iOS-Tastatur setzt (Apostrophe ’ ‘ und Striche – —, z. B. „Up’n Kamp“), werden vor der Prüfung zu ' bzw. - vereinheitlicht und nicht abgelehnt. Zwischen zwei Anfragen an den Ortssuchdienst liegen mindestens 1,1 Sekunden (Nutzungsrichtlinie: höchstens eine je Sekunde, mit Sicherheitsabstand).

| Situation | Meldung |
|-----------|---------|
| leere Eingabe | „Bitte eine Adresse, einen Ort oder eine Postleitzahl eingeben.“ |
| weniger als 3 Zeichen | „Die Eingabe ist zu kurz. Bitte mindestens 3 Zeichen eingeben.“ |
| mehr als 120 Zeichen | „Die Eingabe ist zu lang. Bitte höchstens 120 Zeichen eingeben.“ |
| unzulässige Zeichen | „Die Eingabe enthält ungültige Zeichen. Erlaubt sind Buchstaben, Ziffern und gängige Satzzeichen.“ |
| kein Ort gefunden | „Zu dieser Eingabe wurde in Deutschland kein Ort gefunden. Bitte die Schreibweise prüfen oder genauer angeben.“ |
| keine Netzverbindung | „Keine Netzverbindung: Die Adresse kann nicht in einen Ort umgewandelt werden.“ |
| Dienst nicht erreichbar | „Der Ortssuchdienst von OpenStreetMap ist nicht erreichbar. Bitte versuche es später erneut.“ |
| Dienst lehnt ab | „Der Ortssuchdienst hat die Anfrage abgelehnt. Bitte versuche es später erneut.“ |
| unerwartete Antwort | „Der Ortssuchdienst hat eine unerwartete Antwort geliefert.“ |

Bei ungültiger Eingabe und ohne Verbindung wird keine Anfrage gesendet.

**Nutzungsrichtlinie von Nominatim:** höchstens eine Anfrage je Sekunde (die App wartet bei Bedarf), nur auf ausdrückliches Absenden, ein Treffer je Anfrage ohne automatische Wiederholung, Kennung „Tankatlas/0.1 (de.martinstromberg.tankradar)“ im Anfragekopf, ausschließlich HTTPS.

**Datenschutz:** Die eingegebene Adresse und die daraus ermittelte Position werden weder gespeichert noch protokolliert; sie gehen nur an Nominatim (die Position danach an den Preisdienst, wie bei der Standortsuche).

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
- die Adresszeile (z. B. „Hauptstraße 1, 10115 Berlin“), soweit die Quelle sie liefert,
- „Geöffnet“ oder „Geschlossen“, wenn die Quelle den Status liefert,
- je in den **Optionen** gewählter Spritsorte den hervorgehobenen Preis (z. B. „1,859 €“) und das Alter („vor X Min.“; ab 60 Minuten in Amber), in der Reihenfolge, die Sie in den **Optionen** festgelegt haben,
- die Hinweise **Preis unbestätigt** (mindestens ein angezeigter Preis ist veraltet) und **Automatentankstelle**. Der Hinweis **Automatentankstelle** erscheint nur, wenn die Öffnungszeiten der Tankstelle bekannt sind: Die Umkreissuche der Quelle liefert sie nicht, sie stammen aus einer früheren Detailabfrage der Tankstelle (lokal gespeichert).

Gibt es keine Tankstelle im Umkreis, erscheint „Keine Tankstellen im Umkreis gefunden“.

Bei sehr vielen Treffern (in Großstädten bei 25 km mehrere hundert) zeigt die Liste zunächst die ersten 25 Tankstellen; **Weitere anzeigen (N weitere)** ergänzt jeweils 25. Ein Wechsel von Filter oder Sortierung zeigt wieder die ersten 25 der neu geordneten Liste.

### Filtern und Sortieren

- **Spritsorte** (Chips): **Alle** oder eine Ihrer gewählten Sorten. Beim Filter bleiben nur Tankstellen mit einem Preis für diese Sorte.
- **Sortierung** (Chips): **Preis**, **Entfernung** oder **Name**; vorbelegt mit der **Standardsortierung** aus den **Optionen**. Bei **Preis** gilt die gefilterte Sorte, bei **Alle** die erste gewählte Sorte; Tankstellen ohne Preis dafür stehen am Ende.

Filter und Sortierung wirken sofort auf die vorhandene Liste, ohne neue Suche.

### Offline und Fehler

- Ohne Verbindung (oder wenn der Preisdienst nicht erreichbar ist) zeigt die Kopfzeile „Offline: keine Verbindung zum Preisdienst.“; vorhandene gespeicherte Preise erscheinen mit ihrem Alter und dem Hinweis „Es werden die zuletzt bekannten Preise angezeigt.“ Gibt es keine gespeicherten Preise für den Umkreis, lautet die Meldung „Keine Netzverbindung und keine gespeicherten Preise für diesen Umkreis.“
- Weitere Meldungen: „Der Preisdienst ist nicht erreichbar.“, „Es ist kein API-Schlüssel hinterlegt.“, „Der Preisdienst hat die Anfrage abgelehnt. Bitte den API-Schlüssel prüfen.“, „Der Preisdienst hat eine unerwartete Antwort geliefert.“ und allgemein „Die Suche konnte nicht durchgeführt werden.“

## Beispiele

**Diesel im Umkreis von 10 km, günstigste zuerst:** Chip **10 km** wählen, **Suchen** tippen, Filter **Diesel**, Sortierung **Preis**.

**Nächstgelegene Tankstelle:** Sortierung **Entfernung** wählen.

## Einschränkungen

- Die Adresssuche liefert nur Orte in Deutschland, weil auch die Preisquelle nur deutsche Tankstellen kennt; sie bietet keine Vorschläge beim Tippen. Der Radius bleibt auf 25 km begrenzt (siehe [ADR 0003](../../adr/0003-address-search-decisions.md)).
- Die Ergebnisse erscheinen als Liste; eine Kartenansicht gibt es noch nicht.
- Der Radius ist auf 25 km begrenzt (Vorgabe der Preisquelle) und wird in den Stufen 1, 2, 5, 10, 15 und 25 km gewählt.
- Abweichungen vom Designentwurf der Suche sind in [ADR 0002](../../adr/0002-search-page-design-deviations.md) dokumentiert.
- Die Liste zeigt nur Preise der in den **Optionen** gewählten Spritsorten.
