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

- den Namen und die Entfernung (z. B. „1,4 km“), wenn die Quelle eine Position liefert; als Name erscheint Ihr eigener Name, sonst die Marke, sonst der Name der Quelle, der dann klein darunter steht, wenn er abweicht (bei manchen Tankstellen liefert die Quelle den Inhaber als Namen). Die Sortierung nach **Name** folgt dem angezeigten Namen,
- die Adresszeile (z. B. „Hauptstraße 1, 10115 Berlin“), soweit die Quelle sie liefert,
- „Geöffnet“ oder „Geschlossen“, wenn die Quelle den Status liefert,
- je in den **Optionen** gewählter Spritsorte den hervorgehobenen Preis (z. B. „1,859 €“) und das Alter („vor X Min.“; ab 60 Minuten in Amber), in der Reihenfolge, die Sie in den **Optionen** festgelegt haben,
- die Hinweise **Preis unbestätigt** (mindestens ein angezeigter Preis ist veraltet) und **Automatentankstelle**. Der Hinweis **Automatentankstelle** erscheint nur, wenn die Öffnungszeiten der Tankstelle bekannt sind: Die Umkreissuche der Quelle liefert sie nicht, sie stammen aus einer früheren Detailabfrage der Tankstelle (lokal gespeichert).

Gibt es keine Tankstelle im Umkreis, erscheint „Keine Tankstellen im Umkreis gefunden“.

Bei sehr vielen Treffern (in Großstädten bei 25 km mehrere hundert) zeigt die Liste zunächst die ersten 25 Tankstellen; **Weitere anzeigen (N übrig)** ergänzt jeweils 25 (N ist die Zahl der noch nicht angezeigten Treffer). Ein Wechsel von Filter oder Sortierung zeigt wieder die ersten 25 der neu geordneten Liste.

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

### Kartenansicht

Unter **Ansicht** wechseln Sie zwischen **Liste** und **Karte**. Voreingestellt ist die **Standardansicht** aus den Optionen; Ihre Wahl in der Suche gilt, solange die App läuft und Sie die Standardansicht nicht ändern. Der Wechsel löst keine neue Suche aus und fragt keinen Standort ab. Solange noch nicht gesucht wurde, zeigt die Kartenansicht den Hinweis „Noch keine Ergebnisse“.

Die Karte besteht aus Kacheln von OpenStreetMap und sieht unter iOS und Windows gleich aus. Unten rechts steht immer die Quellenangabe **© OpenStreetMap-Mitwirkende**. Nach einer Suche passt die Karte den Ausschnitt so an, dass alle Tankstellen und die Suchposition sichtbar sind.

- **Zoomen:** Schaltflächen **Vergrößern** (+) und **Verkleinern** (−) oder Kneifgeste; Zoomstufen 4 bis 18, die aktuelle Stufe steht oben links („Zoom 14“).
- **Verschieben:** Wischgeste oder die Pfeil-Schaltflächen links unten (**Ausschnitt nach Norden/Süden/Osten/Westen verschieben**), damit die Karte auch ohne Wischgeste (Tastatur, Sprachausgabe) bedienbar ist. **Ausschnitt zurücksetzen** (◎) passt den Ausschnitt wieder auf alle Ergebnisse ein.
- **Zähler:** „3 von 12 Stationen sichtbar“ zeigt, wie viele Markierungen im aktuellen Ausschnitt liegen.
- **Suchposition:** Bei der Standortsuche ist **Mein Standort** markiert, bei der Adresssuche die **Gesuchte Position**. Der Standort wird nur gemäß der Einstellung zur Standortnutzung abgefragt, nur im Arbeitsspeicher gehalten und bei der nächsten Suche verworfen.
- **Markierungen:** Jede Tankstelle erscheint mit ihrem Preis (Preis der gefilterten Spritsorte, ohne Filter der zuerst gewählten Sorte). Die Farbe zeigt das Preisniveau innerhalb der aktuellen Ergebnismenge: **Grün** günstigster Preis, **Rot** oberes Drittel der Spanne zwischen niedrigstem und höchstem Preis, **Teal** alle übrigen, **Grau** geschlossen. Bei gleichen Preisen oder nur einer Tankstelle gibt es kein Rot; geschlossene Tankstellen zählen nicht zur Preisspanne. Eine Legende unter der Karte erklärt die Farben; Bedienhilfen nennen das Preisniveau zusätzlich als Text. Tankstellen, die die gefilterte Sorte nicht führen, erscheinen bei „Alle“ ohne Preis (mit dem Anfangsbuchstaben).
- **Details:** Tippen auf eine Markierung öffnet die [Tankstellen-Detailansicht](../Tankstellendetails/index.md); der Zurück-Pfeil führt zur Karte zurück.
- **Filter und Sortierung** gelten für Liste und Karte gemeinsam; die Karte zeigt immer die gesamte gefilterte Ergebnismenge (nicht nur die ersten Listeneinträge).

Datenschutz: Beim Laden der Kacheln erfährt der Kachelserver von OpenStreetMap Ihre IP-Adresse und den betrachteten Kartenausschnitt, nicht aber die genaue Suchposition. Wie lange eine Kachel ohne erneute Prüfung gilt, bestimmt der Kachelserver; nennt er nichts, sind es sieben Tage (höchstens 100 MB Zwischenspeicher). Hat der Server eine Kachel schon eine Weile vorgehalten, gilt sie entsprechend kürzer; bestätigt er eine Kachel ohne neue Angaben, behält sie ihre bisherige Gültigkeitsdauer; ohne Verbindung zeigt die Karte bereits geladene Kacheln, sonst einen leeren Hintergrund, die Markierungen bleiben benutzbar. Begründung und Abweichungen vom Designentwurf: [ADR 0005](../../adr/0005-map-view-decisions.md).

## Suche entlang einer Route

Die Suchart **Route** zeigt Tankstellen entlang einer Strecke mit dem Auto.

- **Start:** **Aktueller Standort** (nur wenn **Standort und GPS** das erlaubt) oder **Adresse, Ort oder PLZ**. **Ziel:** Adresse, Ort oder Postleitzahl.
  Beide werden wie bei der Adresssuche über OpenStreetMap-Nominatim aufgelöst (erster Treffer; der aufgelöste Name steht in der Zusammenfassung).
- **Abstand zur Route:** 1, 2, 5 oder 10 km (voreingestellt 2 km). Es erscheinen nur Tankstellen innerhalb dieses Abstands.
- Die Route berechnet OpenRouteService (Profil Auto, eigener Schlüssel des Anwenders, siehe [API-Schlüssel](../Preisdaten/api-schluessel.md)). Die Suchart ist nur bei aktivierter Routensuche sichtbar.
  Die Preisabfragen erfolgen entlang der Strecke: **höchstens 3 Abfragen** je Suche, nacheinander im Abstand von **mindestens 10 Sekunden** zuzüglich einer kleinen Reserve von 0,25 Sekunden (vorläufige Grenze, siehe
  [ADR 0014](../../adr/0014-route-search.md)). Daraus folgt die durchsuchbare Länge: 137 km bei 1 km Abstand, 131 km bei 2 km, 114 km bei 5 km und 85 km bei 10 km. Die Länge wird in der Meldung abgerundet; bei 1 km Abstand schlägt sie keinen kleineren Abstand mehr vor. Längere Routen werden vor der
  ersten Preisabfrage mit einem Hinweis abgelehnt, der die durchsuchbare Länge nennt. Eine Suche dauert bis zu etwa 20 Sekunden; der Fortschritt („Tankstellen werden gesucht (2 von 3) …“) bleibt auch
  während der Wartezeit sichtbar, **Suche abbrechen** ist jederzeit möglich. Wiederholte Suchen nutzen den Cache. Zusammenfassung und Kartenhinweis beschreiben die Anfrage (Abstand, Start), auch
  wenn danach Eingaben geändert werden; während der Suche sind Chips und Eingaben gesperrt.
- **Ergebnis:** Liste und Karte mit den gewohnten Filtern, Preisen mit Alter, Hinweisen, Preisniveau-Farben und Sortierungen. Die **Entfernung** ist die Position
  entlang der Route („12,3 km ab Start“), zusätzlich steht „0,8 km abseits der Route“ da. Die Karte zeichnet die Route ein und markiert Start und Ziel.
  Antippen einer Tankstelle öffnet die Detailansicht. Über der Liste steht „Von A nach B, 123 km, N Tankstellen im Abstand von 2 km zur Route“.
- **Quellenangabe:** „Routen: OpenRouteService, Geodaten © OpenStreetMap-Mitwirkende“.
- **Fehler:** Meldungen gibt es für nicht gefundene Orte (Start oder Ziel), „Start und Ziel liegen am selben Ort“, nicht berechenbare oder zu lange Routen,
  einen ausgelasteten oder nicht erreichbaren Routendienst, fehlende Verbindung und Fehler des Preisdienstes. Ohne aktivierte Routensuche gibt es keine Suchart „Route“ und keinen Aufruf an OpenRouteService; die übrige Suche funktioniert weiter. Bei einem Fehler gibt es keine Teilergebnisse.
- **Datenschutz:** Start, Ziel, Standort und Route werden nicht gespeichert und nicht protokolliert; sie gehen nur an Nominatim und OpenRouteService.
  Die abgerufenen Tankstellen und Preise werden wie bei jeder Suche zwischengespeichert (öffentliche Daten).
