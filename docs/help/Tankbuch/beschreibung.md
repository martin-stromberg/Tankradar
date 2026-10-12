← [Zurück zur Übersicht](index.md)

# Tankbuch — Beschreibung

## Zweck

Im Tankbuch verwalten Sie Ihre Fahrzeuge und erfassen je Fahrzeug die Tank- und Ladevorgänge. Tankatlas berechnet daraus den Verbrauch und wertet Verbrauch und Kosten aus. Entscheidungen und Abweichungen vom Designentwurf: [ADR 0008](../../adr/0008-vehicle-management.md) (Fahrzeuge), [ADR 0009](../../adr/0009-tankbook-entries.md) (Vorgänge) und [ADR 0010](../../adr/0010-tankbook-statistics.md) (Auswertung).

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

**Menge, Preis und Gesamtbetrag:** Zwei der drei Werte genügen; der dritte wird berechnet und unter den Feldern angezeigt („Ergibt 74,36 €“). Beim Speichern wird er ins leere Feld übernommen. Geben Sie alle drei an und weicht Menge × Preis um mehr als 0,02 € vom Gesamtbetrag ab, wird nicht gespeichert. Die Meldung nennt den rechnerischen Betrag, z. B. „Menge × Preis ergibt 74,36 €, nicht 75,00 €. Bitte prüfe die Werte.“ Eine Ausnahme von der Toleranz: Als stimmig gilt auch ein Wert, der genau dem gerundeten Quotienten der beiden anderen entspricht. Gerundet wird der Gesamtbetrag auf 2, der Preis pro Einheit auf 3 und die Menge auf 2 Nachkommastellen. So lässt sich ein berechneter Preis auch bei großen Mengen beim Korrigieren wieder speichern. Bei Belegen mit Zusatzgebühren (z. B. einer Ladesitzung) geben Sie Menge und Gesamtbetrag an.

**Bekannte Tankstellen:** Die Liste enthält Ihre Favoriten-Tankstellen und danach die Namen, die Sie bereits frei eingegeben haben (höchstens 30). Haben zwei Tankstellen denselben Namen, steht der Ort dahinter. Ändern Sie den Namen nach der Auswahl von Hand, gilt er als frei eingegeben.

## Verbrauchsberechnung

- Der Verbrauch eines vollen Vorgangs ist (Menge + Mengen der Teilbetankungen seit dem vorherigen vollen Vorgang) / gefahrene Strecke × 100, in L/100 km bzw. kWh/100 km mit zwei Nachkommastellen.
- Der erste volle Vorgang hat noch keinen Verbrauch („Noch kein Verbrauch: erster voller Vorgang“).
- Eine Teilbetankung hat keinen eigenen Wert; ihre Menge zählt beim nächsten vollen Vorgang mit. Teilbetankungen vor dem ersten vollen Vorgang zählen nicht für den Verbrauch, ihre Kosten erscheinen aber in den Monatskosten. Im Formular steht dazu unter dem Schalter **Teilbetankung** ein Hinweis, solange der Schalter eingeschaltet ist.
- Ohne gefahrene Strecke seit dem letzten vollen Vorgang gibt es keinen Wert.
- Bei Hybridfahrzeugen werden Kraftstoff und Strom getrennt gerechnet.
- Korrigieren oder löschen Sie einen Vorgang, werden die Verbrauchswerte sofort neu berechnet.

## Auswertung

Unter dem Formular zeigt die Seite **Tank- und Ladevorgänge** den Bereich **Auswertung** des Fahrzeugs. Hybridfahrzeuge haben je einen Block für **Kraftstoff** (L) und **Strom** (kWh); die Werte beider Arten werden nie vermischt. Fahrzeuge mit nur einer Art zeigen einen Block.

Je Art besteht der Block aus zwei Kacheln mit großer Zahl (**Ø Verbrauch** und **Kosten / 100 km**), der Karte **Monatsanalyse** mit den **Gesamtkosten des laufenden Monats** und der in diesem Monat getankten bzw. geladenen Menge (z. B. „81,55 €“ und „41,00 L getankt“), dem **Verbrauchsverlauf**, dem **Kostenverlauf** und der Liste der **Monatskosten**. Auf einem schmalen Bildschirm steht die Einheit unter der Zahl, wenn sie daneben nicht mehr Platz hat. Die großen Zahlen bleiben dabei einzeilig; sehr lange Werte (z. B. „9.989,00“) werden in der Schrift etwas kleiner dargestellt, statt umzubrechen.

- **Ø Verbrauch:** Summe der Mengen aller berechneten Intervalle geteilt durch die Summe ihrer Strecken × 100, mit zwei Nachkommastellen. Längere Strecken zählen dadurch stärker als kurze. Ein Intervall reicht von einem vollen Vorgang zum nächsten; die Mengen dazwischenliegender Teilbetankungen gehören dazu. Intervalle ohne Strecke gehen nicht ein.
- **Kosten pro 100 km:** Summe der Kosten derselben Intervalle (voller Vorgang plus Teilbetankungen dazwischen) geteilt durch die Summe ihrer Strecken × 100, mit zwei Nachkommastellen.
- **Gesamtkosten (Monatsanalyse):** Summe der Gesamtbeträge aller Vorgänge der Art im laufenden Monat, dazu die Summe der Mengen dieses Monats. Ohne Vorgang im laufenden Monat stehen dort 0,00 € und 0,00 L bzw. kWh.
- **Monatskosten:** Summe der Gesamtbeträge je Kalendermonat nach dem Datum des Vorgangs (Ortszeit; 31.12. 23:59 gehört zum Dezember, 01.01. 00:00 zum Januar). Teilbetankungen zählen immer mit, auch vor dem ersten vollen Vorgang. Die Liste reicht lückenlos vom ersten Vorgang bis zum aktuellen Monat; Monate ohne Vorgang erscheinen mit 0,00 €. Der neueste Monat steht oben. Angezeigt werden höchstens die letzten 24 Monate (ein Hinweis weist darauf hin); Durchschnitt und Kosten pro 100 km enthalten immer alle Vorgänge.
- **Diagramme:** Beide sind Balkendiagramme, der jeweils neueste Balken ist hervorgehoben. Der **Verbrauchsverlauf** zeigt einen Balken je berechnetem vollen Vorgang, höchstens die letzten 6; der **Kostenverlauf** zeigt die Monatskosten, höchstens die letzten 12 Monate. Kennzahlen und Monatsliste enthalten alle Daten. Die Balken beginnen bei 0; die Werte stehen über den Balken (beim Kostenverlauf bei mehr als sechs Balken nur am letzten). Die Beschriftung der Achse am linken und rechten Rand ist so ausgerichtet, dass sie nicht abgeschnitten wird. Gibt es noch keinen Wert, erscheint statt des leeren Diagramms nur der Hinweis „Noch kein Verlauf: Es liegen keine berechneten Werte vor.“

**Wann erscheinen Werte?** Ø Verbrauch und Kosten pro 100 km erscheinen, sobald mindestens ein Intervall vorliegt, also bei zwei vollen Vorgängen derselben Art. Davor steht der Hinweis „Für den Verbrauch werden mindestens zwei volle Vorgänge benötigt.“ und bei den Kosten „noch nicht berechenbar“. Der Satz „Alternativ kannst du am Fahrzeug einen Startverbrauch hinterlegen.“ folgt nur dann, wenn ein Startverbrauch für diese Art überhaupt gelten würde, also wenn die Art die Startart des Fahrzeugs ist (Strom bei Elektrofahrzeugen, sonst Kraftstoff) und noch kein Startverbrauch hinterlegt ist. Im Block „Strom“ eines Hybridfahrzeugs steht er nie.

**Startverbrauch:** Haben Sie am Fahrzeug einen Startverbrauch hinterlegt, ersetzt er den Ø Verbrauch, solange noch kein Intervall vorliegt. Er ist als „Startverbrauch – noch keine zwei vollen Vorgänge“ gekennzeichnet und verschwindet, sobald der berechnete Wert vorliegt; beide werden nie gemischt. Er gilt nur für die Art, deren Einheit er trägt: bei Elektrofahrzeugen für Strom (kWh/100 km), sonst für Kraftstoff (L/100 km), bei Hybridfahrzeugen also nur für Kraftstoff. Für die Kosten pro 100 km gibt es keinen Ersatzwert.

**Barrierefreiheit:** Jedes Diagramm hat eine sichtbare Textzusammenfassung, z. B. „Verlauf, 3 Werte, zuletzt 6,00 L/100 km, niedrigster 5,80, höchster 6,20“. Sie ist ein eigener Text unter dem Diagramm und wird von der Sprachausgabe als Text vorgelesen; das Diagramm selbst trägt nur die kurze Beschreibung („Balkendiagramm des Verbrauchsverlaufs“ bzw. „Balkendiagramm der Monatskosten“), damit nichts doppelt vorgelesen wird. Ohne Werte gibt es kein Diagramm und damit auch keine Ansage eines leeren Diagramms. Die Kacheln heißen für die Sprachausgabe „Durchschnittsverbrauch“ und „Kosten pro 100 km“. Die Monatsliste ist die Tabelle zum Kostendiagramm. Beim Wechsel zwischen hellem und dunklem Erscheinungsbild werden die Diagramme neu gezeichnet. Die Auswertung wird beim Öffnen sowie nach Erfassen, Korrigieren und Löschen eines Vorgangs neu berechnet; sie wird nicht gespeichert. Bei einem Ladefehler erscheint „Die Auswertung konnte nicht geladen werden.“, die Liste der Vorgänge bleibt nutzbar.

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

- **Fahrzeug:** Ohne Vorgänge lautet die Frage „Fahrzeug „Name“ löschen?“ mit **Ja, löschen** und **Abbrechen**. Hat das Fahrzeug Vorgänge, nennt die Frage deren Zahl und sagt ausdrücklich, dass sie endgültig mitgelöscht werden; die Bestätigung heißt dann **Fahrzeug und Vorgänge löschen**. Die Frage rät außerdem: „Erstelle vorher in den Optionen eine Sicherung, wenn du die Vorgänge behalten möchtest.“ (siehe [Sicherung](../Sicherung/index.md)). Beim Bestätigen zählt Tankatlas die Vorgänge erneut. Ist inzwischen ein Vorgang hinzugekommen, wird nichts gelöscht; die Frage nennt die neue Zahl und muss noch einmal bestätigt werden.
- **Vorgang:** „Vorgang vom 18.05.2026, 14:30 (Alpha Tankstelle) löschen? Das lässt sich nicht rückgängig machen.“ mit **Ja, löschen** und **Abbrechen**. Das Belegfoto wird mitgelöscht.

## Barrierefreiheit

Die Sprachausgabe liest bei Schaltflächen den sichtbaren Text vor, z. B. „Ja, löschen“. Den Zusammenhang nennt sie als Hinweis, z. B. „Fahrzeug „Golf“ und 2 Tank- und Ladevorgänge endgültig löschen“ oder „Fahrzeug „Golf““ bei **Bearbeiten** und **Löschen** der Fahrzeugkarte. Fehlermeldungen, etwa wenn das Laden oder Löschen fehlschlägt, werden ebenfalls angesagt. So funktioniert auch die Sprachsteuerung mit dem sichtbaren Text.

## Datenschutz

Fahrzeuge, Vorgänge und Belegfotos liegen ausschließlich auf diesem Gerät. Die Datenbank und die Fotos sind durch den Dateischutz des Systems geschützt (iOS: verfügbar erst nach der ersten Entsperrung). Das Tankbuch nutzt kein Netzwerk und gibt nichts an Dritte weiter. Namen und Werte werden nicht protokolliert. Fotos, die kein Vorgang mehr verwendet, entfernt Tankatlas automatisch.
