← [Zurück zur Übersicht](index.md)

# Startseite — Beschreibung

## Zweck

Die Startseite gibt Ihnen auf einen Blick, was Ihre favorisierten Tankstellen aktuell kosten. Sie ist der Bereich **Favoriten**, der beim Start von Tankatlas als Erstes erscheint. Welche Elemente des Designentwurfs bewusst fehlen, steht in [ADR 0007](../../adr/0007-home-page-design-deviations.md).

## Funktionsweise

### Gruppen und Tankstellen

Jede Favoritengruppe erscheint mit Name, Beschreibung (falls vorhanden) und der Zahl ihrer Tankstellen; die Gruppen sind nach Namen geordnet. Mit **Gruppe ein-/ausklappen** blenden Sie die Tankstellen einer Gruppe ein oder aus. **Gruppe öffnen** führt zur Gruppenansicht (siehe [Favoritengruppen](../Favoriten/index.md)). Gehört eine Tankstelle mehreren Gruppen an, erscheint sie in jeder davon. Innerhalb einer Gruppe stehen Tankstellen mit hoher Priorität zuerst, dann nach Namen.

Zu jeder Tankstelle zeigt die Kachel:

- den **Namen**, bei hinterlegter Notiz auch die Notiz,
- die **Entfernung**, nur wenn Ihr Standort ermittelt werden darf und verfügbar ist,
- die **Preise der in den Optionen gewählten Spritsorten** in der dort festgelegten Reihenfolge, je Preis mit Alter („vor X Min.“); ab 60 Minuten ist die Altersangabe amberfarben markiert,
- die Hinweise **Preis unbestätigt** und **Automatentankstelle**, soweit sie zutreffen,
- „Keine Preise bekannt“, wenn für die gewählten Sorten noch kein Preis vorliegt.

Angaben, die die Datenquelle nicht liefert, werden weggelassen und nicht durch Platzhalter ersetzt. Tippen Sie auf die Kachel oder auf **Details öffnen**, öffnet sich die Detailansicht der Tankstelle; dort ordnen Sie Tankstellen Gruppen zu oder entfernen sie.

### Preise aktualisieren

Beim Öffnen der Startseite erscheinen zuerst die zuletzt bekannten Preise, danach werden sie aktualisiert (sofern Sie online sind und der letzte Abruf nicht erst kurz zurückliegt). Mit **Preise aktualisieren** erzwingen Sie einen neuen Abruf; die Schaltfläche ist ohne Verbindung und während eines laufenden Abrufs deaktiviert.

### Offline und Wiederverbindung

Ohne Verbindung (oder wenn der Preisdienst nicht erreichbar ist) zeigt die Startseite die zuletzt bekannten Preise mit ihrem Alter und oben den Offline-Hinweis. Kehrt die Verbindung zurück, während die Startseite geöffnet ist, aktualisiert Tankatlas veraltete Preise selbstständig und blendet den Hinweis aus.

### Bereich „In der Nähe“

Dieser Bereich ist optional. Er erscheint nur, wenn Sie in **Optionen** den Schalter bei **In der Nähe** eingeschaltet haben und die Standortnutzung nicht **Nie** ist. Er listet bis zu 10 Tankstellen im Umkreis von 5 km um Ihren Standort, nach Entfernung geordnet. Die Chips **Alle** und je gewählte Spritsorte filtern die Liste. Der Bereich dient nur der Information: Hier gibt es keine Favoritenaktionen; Antippen einer Tankstelle öffnet ihre Detailansicht. Ist kein Standort verfügbar, erscheint eine verständliche Meldung; gibt es keine Treffer, steht dort „Keine Tankstellen in der Nähe“.

### Ohne Favoriten

Gibt es noch keine Gruppen, erklärt die Startseite, wie Sie Favoriten anlegen: eine Tankstelle öffnen und in der Detailansicht über **Zu Favoriten hinzufügen** einer Gruppe zuordnen, oder hier mit **Neue Gruppe** eine Gruppe anlegen.

### Datenschutz

Ihr Standort wird nur verwendet, solange Sie die Startseite sehen, und nur im Arbeitsspeicher gehalten. Er wird nicht gespeichert, nicht protokolliert und beim Verlassen der Seite verworfen. Für die Suche „In der Nähe“ merkt sich Tankatlas außerdem höchstens fünf Minuten lang, nur im Arbeitsspeicher und auf etwa 110 m gerundet, dass für diesen Ort kürzlich gesucht wurde; danach ist der Merker wirkungslos und wird bei der nächsten Preisabfrage, spätestens beim Beenden der App, verworfen. Bei der Standortnutzung **Nie** fragt Tankatlas den Standort nicht ab.

## Beispiele

**Pendler:** Die Gruppe „Arbeitsweg“ enthält zwei Tankstellen; Diesel steht in den Optionen an erster Stelle. Die Startseite zeigt für beide zuerst den Dieselpreis mit „vor 5 Min.“.

**Unterwegs ohne Netz:** Die Preise bleiben sichtbar, der Offline-Hinweis steht oben. Sobald das Netz wieder da ist, werden veraltete Preise ohne Zutun erneuert.

## Einschränkungen

- Die Preise stammen von Tankerkönig (MTS-K); die Quelle nennt keinen eigenen Preiszeitpunkt, als Alter gilt der Abrufzeitpunkt (siehe [Kraftstoffpreise](../Preisdaten/index.md)).
- Der Radius (5 km) und die Zahl der Einträge (10) von **In der Nähe** sind fest.
- Die Spritsorten-Chips wirken nur auf **In der Nähe**, nicht auf die Favoritengruppen.
- Das Ein- und Ausklappen der Gruppen wird nicht über einen Neustart hinweg gespeichert.
