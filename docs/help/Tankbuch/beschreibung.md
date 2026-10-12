← [Zurück zur Übersicht](index.md)

# Tankbuch — Beschreibung

## Zweck

Im Tankbuch verwalten Sie Ihre Fahrzeuge und erfassen je Fahrzeug die Tank- und Ladevorgänge. Tankatlas berechnet daraus den Verbrauch. Entscheidungen und Abweichungen vom Designentwurf: [ADR 0008](../../adr/0008-vehicle-management.md) (Fahrzeuge) und [ADR 0009](../../adr/0009-tankbook-entries.md) (Vorgänge).

## Fahrzeugliste

- Jedes Fahrzeug erscheint als Karte mit seinem Namen und darunter den Angaben, z. B. „Diesel • Start 84.350 km • Startverbrauch 5,4 L/100 km“. Ohne Startverbrauch endet die Zeile nach dem Kilometerstand.
- Die Fahrzeuge sind nach Namen geordnet (ohne Beachtung der Groß- und Kleinschreibung).
- Gibt es noch keine Fahrzeuge, erscheint der Hinweis „Noch keine Fahrzeuge. Lege über „Fahrzeug hinzufügen“ dein erstes Fahrzeug an.“
- Jede Karte hat die Schaltflächen **Tank- und Ladevorgänge**, **Bearbeiten** und **Löschen**. Sie wählen das Fahrzeug also direkt in der Liste anhand seines Namens; interne Kennungen werden nie angezeigt.

## Angaben eines Fahrzeugs

| Feld | Pflicht | Regeln |
|------|---------|--------|
| Name des Fahrzeugs | ja | 1 bis 40 Zeichen; Leerzeichen am Anfang und Ende werden entfernt; jeder Name nur einmal (ohne Beachtung der Groß- und Kleinschreibung) |
| Kraftstoffart | ja | feste Auswahl: **Benzin**, **Diesel**, **Elektro**, **Hybrid**; ein neues Fahrzeug hat **Benzin** vorausgewählt |
| Start-Kilometerstand | ja | ganze Kilometer von 0 bis 9.999.999, Eingabe mit oder ohne Tausenderpunkte („84350“ oder „84.350“); Einheit **km**; nicht über dem Kilometerstand des ersten Tank- oder Ladevorgangs |
| Startverbrauch | nein | größer als 0 und kleiner als 100, höchstens zwei Nachkommastellen; Komma oder Punkt als Dezimaltrennzeichen („5,4“ oder „5.4“); Einheit **L/100 km**, bei **Elektro** **kWh/100 km** |

Die Einheiten stehen fest rechts im Eingabefeld und wechseln mit der gewählten Kraftstoffart. Hybridfahrzeuge verwenden L/100 km. Die Kraftstoffart lässt sich auch bei vorhandenen Vorgängen ändern; die Vorgänge behalten ihre Art und Einheit.

## Tank- und Ladevorgänge

Die Seite **Tank- und Ladevorgänge** zeigt die Vorgänge eines Fahrzeugs, die neuesten zuerst. Jeder Eintrag zeigt Tankstelle, Gesamtbetrag, Datum und Uhrzeit, den Preis pro Einheit, die Menge sowie bei vollen Vorgängen die Strecke und den Verbrauch, z. B. „Verbrauch 6,00 L/100 km“. Hinzu kommen gegebenenfalls die Kennzeichnung „Teilbetankung“, bei Hybridfahrzeugen die Art („Kraftstoff“ oder „Strom“), die Notiz in Anführungszeichen und **Beleg anzeigen**.

| Angabe | Pflicht | Regeln |
|--------|---------|--------|
| Datum und Uhrzeit | ja | mit dem aktuellen Zeitpunkt vorbelegt; nicht in der Zukunft |
| Tankstelle oder Ladeort | ja | 1 bis 60 Zeichen; frei eingeben (z. B. „Zuhause“) oder unter **Bekannte Tankstellen** antippen |
| Art | – | ergibt sich aus der Kraftstoffart: Benzin und Diesel **Kraftstoff** (Liter), Elektro **Strom** (kWh), Hybrid wählbar |
| Menge | siehe unten | größer als 0 und kleiner als 1.000 L bzw. kWh, höchstens drei Nachkommastellen |
| Preis pro Liter bzw. pro kWh | siehe unten | größer als 0 und kleiner als 10 €, höchstens drei Nachkommastellen |
| Gesamtbetrag | siehe unten | größer als 0 und kleiner als 10.000 €, höchstens zwei Nachkommastellen |
| Kilometerstand | ja | ganze Kilometer; nicht unter dem Start-Kilometerstand und dem vorherigen Vorgang, nicht über dem nächsten Vorgang |
| Teilbetankung | nein | Schalter, Standard aus (voll getankt bzw. geladen) |
| Notiz | nein | höchstens 500 Zeichen |
| Belegfoto | nein | JPG, PNG oder HEIC, höchstens 20 MB; Tankatlas prüft den Dateiinhalt |

**Menge, Preis und Gesamtbetrag:** Zwei der drei Werte genügen; der dritte wird berechnet und unter den Feldern angezeigt („Ergibt 74,36 €“). Beim Speichern wird er ins leere Feld übernommen. Geben Sie alle drei an und weicht Menge × Preis um mehr als 0,02 € vom Gesamtbetrag ab, wird nicht gespeichert. Die Meldung nennt den rechnerischen Betrag, z. B. „Menge × Preis ergibt 74,36 €, nicht 75,00 €. Bitte prüfe die Werte.“ Bei Belegen mit Zusatzgebühren (z. B. einer Ladesitzung) geben Sie Menge und Gesamtbetrag an.

**Bekannte Tankstellen:** Die Liste enthält Ihre Favoriten-Tankstellen und danach die Namen, die Sie bereits frei eingegeben haben (höchstens 30). Haben zwei Tankstellen denselben Namen, steht der Ort dahinter. Ändern Sie den Namen nach der Auswahl von Hand, gilt er als frei eingegeben.

## Verbrauchsberechnung

- Der Verbrauch eines vollen Vorgangs ist (Menge + Mengen der Teilbetankungen seit dem vorherigen vollen Vorgang) / gefahrene Strecke × 100, in L/100 km bzw. kWh/100 km mit zwei Nachkommastellen.
- Der erste volle Vorgang hat noch keinen Verbrauch („Noch kein Verbrauch: erster voller Vorgang“).
- Eine Teilbetankung hat keinen eigenen Wert; ihre Menge zählt beim nächsten vollen Vorgang mit. Teilbetankungen vor dem ersten vollen Vorgang zählen nicht.
- Ohne gefahrene Strecke seit dem letzten vollen Vorgang gibt es keinen Wert.
- Bei Hybridfahrzeugen werden Kraftstoff und Strom getrennt gerechnet.
- Korrigieren oder löschen Sie einen Vorgang, werden die Verbrauchswerte sofort neu berechnet.

## Fehlermeldungen

Ungültige Eingaben werden nicht gespeichert. Die Meldungen stehen direkt unter dem betroffenen Feld, alle gleichzeitig, der Rahmen des Felds wird rot, und die erste Meldung wird von der Sprachausgabe vorgelesen, zum Beispiel:

- „Bitte gib einen Namen ein.“ / „Ein Fahrzeug mit diesem Namen gibt es bereits.“
- „Bitte gib eine Tankstelle oder einen Ladeort ein oder wähle eine bekannte Tankstelle.“
- „Bitte gib mindestens zwei der drei Werte Menge, Preis pro Liter und Gesamtbetrag an.“
- „Der Kilometerstand darf nicht unter 10.500 km (vorheriger Vorgang) liegen.“
- „Der Zeitpunkt darf nicht in der Zukunft liegen.“

## Belegfoto

Mit **Foto auswählen** übernehmen Sie ein Foto aus der Fotomediathek. Mit **Foto aufnehmen** fotografieren Sie den Beleg; das ist nur auf Geräten mit Kamera sichtbar. Das Foto wird unverändert als Kopie in den App-Daten abgelegt. **Foto entfernen** nimmt es wieder heraus. Brechen Sie das Formular ab, wird ein neu ausgewähltes Foto verworfen. In der Liste blendet **Beleg anzeigen** das Foto ein und **Beleg ausblenden** wieder aus. Fehlt die Datei, erscheint „Das Belegfoto ist nicht mehr vorhanden.“ Unter Windows kann die Vorschau eines HEIC-Fotos ohne die HEIF-Erweiterung leer bleiben; das Foto bleibt trotzdem gespeichert.

## Löschen

Vor dem Löschen fragt Tankatlas immer nach, direkt im Eintrag:

- **Fahrzeug:** Ohne Vorgänge lautet die Frage „Fahrzeug „Name“ löschen?“ mit **Ja, löschen** und **Abbrechen**. Hat das Fahrzeug Vorgänge, nennt die Frage deren Zahl und sagt ausdrücklich, dass sie endgültig mitgelöscht werden; die Bestätigung heißt dann **Fahrzeug und Vorgänge löschen**. Beim Bestätigen zählt Tankatlas die Vorgänge erneut. Ist inzwischen ein Vorgang hinzugekommen, wird nichts gelöscht; die Frage nennt die neue Zahl und muss noch einmal bestätigt werden.
- **Vorgang:** „Vorgang vom 18.05.2026, 14:30 (Alpha Tankstelle) löschen? Das lässt sich nicht rückgängig machen.“ mit **Ja, löschen** und **Abbrechen**. Das Belegfoto wird mitgelöscht.

## Barrierefreiheit

Die Sprachausgabe liest bei Schaltflächen den sichtbaren Text vor, z. B. „Ja, löschen“. Den Zusammenhang nennt sie als Hinweis, z. B. „Fahrzeug „Golf“ und 2 Tank- und Ladevorgänge endgültig löschen“ oder „Fahrzeug „Golf““ bei **Bearbeiten** und **Löschen** der Fahrzeugkarte. Fehlermeldungen, etwa wenn das Laden oder Löschen fehlschlägt, werden ebenfalls angesagt. So funktioniert auch die Sprachsteuerung mit dem sichtbaren Text.

## Datenschutz

Fahrzeuge, Vorgänge und Belegfotos liegen ausschließlich auf diesem Gerät. Die Datenbank und die Fotos sind durch den Dateischutz des Systems geschützt (iOS: verfügbar erst nach der ersten Entsperrung). Das Tankbuch nutzt kein Netzwerk und gibt nichts an Dritte weiter. Namen und Werte werden nicht protokolliert. Fotos, die kein Vorgang mehr verwendet, entfernt Tankatlas automatisch.
