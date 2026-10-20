← [Zurück zur Übersicht](index.md)

# Sicherung — Beschreibung

## Zweck

Mit der Sicherung schützen Sie Ihre Daten vor Verlust, etwa beim Gerätewechsel oder vor einer größeren Änderung. Entscheidungen und Begründungen stehen in [ADR 0011](../../adr/0011-local-backup-restore.md).

## Was eine Sicherung enthält

- Ihre Einstellungen (Spritsorten, Standortnutzung, Ansicht, Sortierung, „In der Nähe“),
- Favoritengruppen und Favoriten mit Notiz und Priorität,
- Fahrzeuge sowie Tank- und Ladevorgänge samt Belegfotos,
- die gespeicherten Tankstellen (auch Ihre eigenen Anzeigenamen) und Preisstände mit ihren Zeitstempeln.

**Nicht enthalten** sind der Zugangsschlüssel zum Preisdienst, Kartenkacheln und Ihr Standort (er wird nie gespeichert). Nach einer Wiederherstellung gelten gesicherte Preise anhand ihres Alters als veraltet und werden bei Bedarf neu abgerufen.

**Datenschutz:** Auch ohne Ihren Standort lassen sich aus den gespeicherten Tankstellen und Preisständen mit Abrufzeit Gegenden und Zeiten ableiten, in denen Sie gesucht haben. Sicherungen sind nicht verschlüsselt; teilen Sie eine Sicherungsdatei nur mit Personen oder an Orten, denen Sie diese Angaben anvertrauen.

## Die Karte „Sicherung“ in den Optionen

Die Karte steht zwischen **Sortierung** und **Datenquelle**. Sie zeigt einen Hinweis zu Umfang und Ablage, die Schaltflächen **Sicherung erstellen** und **Sicherung aus Datei einspielen**, nach Aktionen eine Meldung und die Liste der Sicherungen, die neueste zuerst. Jeder Eintrag zeigt Datum und Uhrzeit (z. B. „12.10.2026 13:15“), die Größe (z. B. „1,2 MB“) und die Schaltflächen **Wiederherstellen**, **Teilen** und **Löschen**. Ohne Sicherung erscheint „Es gibt noch keine Sicherung. Mit „Sicherung erstellen“ legst du die erste an.“ Eine Datei, die sich nicht lesen lässt, steht mit dem Zusatz „(nicht lesbar)“ in der Liste und lässt sich löschen.

## Wiederherstellen

Vor dem Überschreiben fragt Tankatlas im Eintrag nach und nennt Datum, App-Version und was die Sicherung enthält (z. B. „2 Fahrzeuge, 14 Tank- und Ladevorgänge …“). Die Frage sagt ausdrücklich, dass die aktuellen Daten vollständig ersetzt werden, dass sich das nicht rückgängig machen lässt und dass Sie vorher eine Sicherung erstellen sollten, wenn Sie den aktuellen Stand behalten möchten. Mit **Abbrechen** bleibt alles unverändert. Nach **Ja, wiederherstellen** meldet Tankatlas „Sicherung wiederhergestellt. Die Daten sind auf den Stand der Sicherung zurückgesetzt.“, und alle Bereiche zeigen den gesicherten Stand.

**Sicher und atomar:** Die Datei wird zuerst vollständig geprüft und in einen Zwischenbereich entpackt; erst dann werden die Daten ausgetauscht. Schlägt ein Schritt fehl oder bricht die App ab, stellt Tankatlas den alten Stand wieder her (auch beim nächsten Start). Die Meldung lautet dann „Die Aktion konnte nicht ausgeführt werden. Deine Daten wurden nicht verändert.“

## Sicherungen aus anderen Versionen

- **Ältere Sicherung:** wird eingespielt und auf den aktuellen Stand gebracht; Ihre Daten bleiben erhalten.
- **Neuere Sicherung:** wird abgelehnt mit „Diese Sicherung stammt aus einer neueren Version von Tankatlas (…). Bitte aktualisiere die App. Deine Daten wurden nicht verändert.“
- **Ungültige oder beschädigte Datei:** wird abgelehnt mit „Die Datei ist keine gültige Tankatlas-Sicherung oder beschädigt. Deine Daten wurden nicht verändert.“ Das gilt auch für Dateien mit unzulässigen Inhalten, falschen Prüfsummen, fehlenden Tabellen oder Spalten oder unbrauchbaren Werten in der Datenbank (zum Beispiel ein ungültiges Datum oder unplausibel lange Texte, etwa ein eigener Tankstellenname mit mehr als 60 Zeichen).

## Löschen, Teilen, Einspielen

- **Löschen** fragt nach („Die Sicherungsdatei wird endgültig gelöscht. Deine aktuellen Daten bleiben unverändert.“) und entfernt nur die Sicherungsdatei.
- **Teilen** öffnet die Systemfunktion zum Weitergeben oder Speichern (unter iOS „In Dateien sichern“, unter Windows der Freigabedialog). Tankatlas überträgt die Datei nicht selbst.
- **Sicherung aus Datei einspielen** öffnet die Dateiauswahl. Eine gültige Datei wird vollständig geprüft (einschließlich der Datenbank) und in die Liste übernommen („Die Datei ist eine gültige Sicherung und wurde in die Liste übernommen.“); anschließend erscheint gleich die Rückfrage zur Wiederherstellung. Ein Abbruch der Auswahl verändert nichts.

## Abgrenzung zum Export

Der Export von Tank- und Ladevorgängen als CSV oder PDF (siehe [Tankbuch](../Tankbuch/beschreibung.md#export)) ist keine Sicherung: Er enthält nur Vorgänge eines Fahrzeugs, keine Belegfotos, Einstellungen oder Favoriten, und lässt sich nicht wiedereinspielen.

## Datenschutz und Sicherheit

Sicherungen entstehen nur auf Ihre Veranlassung und liegen nur auf diesem Gerät. **Die Dateien sind nicht verschlüsselt** und enthalten Fahrdaten und Belege. Eine geteilte oder gespeicherte Kopie liegt außerhalb des Dateischutzes der App; geben Sie sie nur an Orte, denen Sie vertrauen. Eingespielte Dateien werden streng geprüft (nur erlaubte Inhalte, Größen- und Prüfsummenkontrolle), bevor sie etwas verändern. Aus der Datenbank einer Sicherung übernimmt die App nur die Daten der bekannten Tabellen und Spalten in eine neu aufgebaute Datenbank; zusätzliche Trigger, Ansichten, Tabellen, Spalten, Einschränkungen oder Indizes aus der Datei werden nicht übernommen und haben keine Wirkung. Die Prüfung ist zeitlich begrenzt (zwei Minuten, geprüft zwischen ihren Schritten) und endet bei einer Zeitüberschreitung, ohne Ihre Daten zu verändern. Die Zahlen in der Rückfrage vor dem Wiederherstellen werden aus der geprüften Datenbank gezählt, nicht der Datei entnommen. Auf dem iPhone gehört der Ordner mit den Sicherungen wie die App-Daten zum Gerätebackup des Systems (iCloud oder Computer), sofern Sie dieses eingeschaltet haben; die App selbst überträgt nichts.

## Hinweis beim Löschen eines Fahrzeugs

Hat ein Fahrzeug Vorgänge, weist die Löschrückfrage im Tankbuch darauf hin: „Erstelle vorher in den Optionen eine Sicherung, wenn du die Vorgänge behalten möchtest.“

## Barrierefreiheit

Die Schaltflächen im Eintrag nennen das Datum als Hinweis (z. B. „Sicherung vom 12.10.2026 13:15 wiederherstellen“), damit die Sprachausgabe sie unterscheiden kann; der vorgelesene Name ist der sichtbare Text. Meldungen werden angesagt. Während eine Aktion läuft, sind die Schaltflächen gesperrt.

← [Zurück zur Übersicht](index.md)
