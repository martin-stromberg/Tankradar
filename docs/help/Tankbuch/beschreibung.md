← [Zurück zur Übersicht](index.md)

# Tankbuch — Beschreibung

## Zweck

Das Tankbuch ist die Grundlage für die spätere Erfassung von Tank- und Ladevorgängen und die Verbrauchsauswertung. In dieser Version verwalten Sie dort Ihre Fahrzeuge. Entscheidungen und Abweichungen vom Designentwurf: [ADR 0008](../../adr/0008-vehicle-management.md).

## Fahrzeugliste

- Jedes Fahrzeug erscheint als Karte mit seinem Namen und darunter den Angaben, z. B. „Diesel • Start 84.350 km • Startverbrauch 5,4 L/100 km“. Ohne Startverbrauch endet die Zeile nach dem Kilometerstand.
- Die Fahrzeuge sind nach Namen geordnet (ohne Beachtung der Groß- und Kleinschreibung).
- Gibt es noch keine Fahrzeuge, erscheint der Hinweis „Noch keine Fahrzeuge. Lege über „Fahrzeug hinzufügen“ dein erstes Fahrzeug an.“
- Jede Karte hat die Schaltflächen **Bearbeiten** und **Löschen**. Sie wählen das Fahrzeug also direkt in der Liste anhand seines Namens; interne Kennungen werden nie angezeigt.

## Angaben eines Fahrzeugs

| Feld | Pflicht | Regeln |
|------|---------|--------|
| Name des Fahrzeugs | ja | 1 bis 40 Zeichen; Leerzeichen am Anfang und Ende werden entfernt; jeder Name nur einmal (ohne Beachtung der Groß- und Kleinschreibung) |
| Kraftstoffart | ja | feste Auswahl: **Benzin**, **Diesel**, **Elektro**, **Hybrid**; ein neues Fahrzeug hat **Benzin** vorausgewählt |
| Start-Kilometerstand | ja | ganze Kilometer von 0 bis 9.999.999, Eingabe mit oder ohne Tausenderpunkte („84350“ oder „84.350“); Einheit **km** |
| Startverbrauch | nein | größer als 0 und kleiner als 100, höchstens zwei Nachkommastellen; Komma oder Punkt als Dezimaltrennzeichen („5,4“ oder „5.4“); Einheit **L/100 km**, bei **Elektro** **kWh/100 km** |

Die Einheiten stehen fest rechts im Eingabefeld und wechseln mit der gewählten Kraftstoffart. Hybridfahrzeuge verwenden L/100 km.

## Fehlermeldungen

Ungültige Eingaben werden nicht gespeichert. Die Meldungen stehen direkt unter dem betroffenen Feld, alle gleichzeitig, und der Rahmen des Felds wird rot, zum Beispiel:

- „Bitte gib einen Namen ein.“
- „Der Kilometerstand darf nicht negativ sein.“
- „Bitte gib den Startverbrauch als Zahl ein, z. B. 5,4.“
- „Ein Fahrzeug mit diesem Namen gibt es bereits.“

## Löschen

Vor dem Löschen fragt Tankatlas immer nach, direkt in der Karte des Fahrzeugs:

- Ohne Tank- oder Ladevorgänge lautet die Frage „Fahrzeug „Name“ löschen?“ mit **Ja, löschen** und **Abbrechen**.
- Hat das Fahrzeug Vorgänge, nennt die Frage deren Zahl und sagt ausdrücklich, dass sie endgültig mitgelöscht werden. Die Bestätigung heißt dann **Fahrzeug und Vorgänge löschen**.
- Nach der Bestätigung sind das Fahrzeug und alle seine Vorgänge gelöscht; Vorgänge anderer Fahrzeuge bleiben unberührt. Das lässt sich nicht rückgängig machen.

## Datenschutz

Die Fahrzeuge liegen ausschließlich in der lokalen Datenbank von Tankatlas. Es gibt keine Netzwerkzugriffe für das Tankbuch, keine Weitergabe an Dritte, und Fahrzeugnamen oder Werte werden nicht protokolliert.
