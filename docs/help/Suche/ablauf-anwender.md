← [Zurück zur Übersicht](index.md)

# Umkreissuche — Ablauf für Anwender

## Voraussetzungen

- In den **Optionen** ist mindestens eine Spritsorte gewählt und **Standort und GPS** steht auf **Immer** oder **Nur bei Nutzung** (siehe [Einstellungen](../Einstellungen/index.md)).
- Ein API-Schlüssel ist hinterlegt (siehe [API-Schlüssel hinterlegen](../Preisdaten/api-schluessel.md)).
- Für Live-Preise besteht eine Netzverbindung.

## Schritt-für-Schritt-Anleitung

### 1. Bereich öffnen

Tippen Sie in der unteren Menüleiste auf **Karte**. Die Karte füllt die Ansicht aus.

### 2. Suchdialog öffnen und Radius festlegen

Tippen Sie oben links auf die Schaltfläche **Suche** (Lupe). Im Suchdialog ist unter **Suchradius** voreingestellt **5 km** gewählt. Tippen Sie bei Bedarf auf einen anderen Chip (**1**, **2**, **5**, **10**, **15** oder **25 km**).

### 3. Suchen

Tippen Sie im Suchdialog auf **Suchen**. Der Dialog schließt sich, und das Ergebnis erscheint in der Trefferliste am unteren Rand. Beim ersten Mal fragt das Gerät eventuell, ob Tankatlas auf den Standort zugreifen darf; erlauben Sie es für die Umkreissuche. Während der Suche zeigt die App einen Ladeanzeiger.

> **Hinweis:** Steht **Standort und GPS** auf **Nie**, erscheint ein Hinweis und es wird nichts abgefragt.

### 4. Ergebnis eingrenzen

Ziehen Sie die Trefferliste am unteren Rand hoch, oder tippen Sie auf **Liste einblenden**. Oben in der Liste tippen Sie unter **Spritsorte** auf **Alle** oder eine Sorte und unter **Sortierung** auf **Preis**, **Entfernung** oder **Name**. Die Liste ändert sich sofort. Bei sehr langen Listen tippen Sie unten auf **Weitere anzeigen**.

### 5. Preise lesen

Je Tankstelle sehen Sie Name, Entfernung, Adresse, Preise mit Alter („vor X Min.“, ab 60 Minuten amberfarben) und gegebenenfalls **Preis unbestätigt** bzw. **Automatentankstelle**.

## Suche nach Adresse, Ort oder PLZ

1. Tippen Sie auf der Karte auf die Schaltfläche **Suche** (Lupe). Im Suchdialog tippen Sie unter **Suchen nach** auf **Adresse, Ort oder PLZ**. Ein Eingabefeld und die Quellenangabe „Geodaten © OpenStreetMap-Mitwirkende“ erscheinen.
2. Geben Sie eine Adresse, einen Ort oder eine Postleitzahl ein (z. B. „Frankfurt“ oder „60311“). Mit **Eingabe löschen** leeren Sie das Feld.
3. Wählen Sie den Radius (**1** bis **25 km**).
4. Tippen Sie auf **Suchen** (oder betätigen Sie die Such-/Eingabetaste). Erst jetzt wird die Eingabe an OpenStreetMap-Nominatim gesendet; dort darf höchstens eine Anfrage je Sekunde eingehen, die App wartet bei Bedarf kurz.
5. Über der Liste steht „Suche rund um: <Ort>“; die Entfernungen beziehen sich auf diesen Ort. Filter und Sortierung wirken wie bei der Standortsuche.

Ein GPS ist dafür nicht nötig; es funktioniert auch bei **Standort und GPS** = **Nie**. Zurück zur Standortsuche kommen Sie im Suchdialog über **Aktueller Standort**.

> **Hinweis:** Bei nicht gefundenem Ort, ungültiger Eingabe oder fehlender Verbindung erscheint eine verständliche Meldung (siehe [Beschreibung](beschreibung.md)); die Adresse wird nicht gespeichert.

## Ergebnis

Eine Liste der Tankstellen im gewählten Umkreis, im Panel am unteren Rand der Karte. Eingeklappt sehen Sie die Anzahl; ausgeklappt die Liste. Erscheint „Offline: keine Verbindung zum Preisdienst.“, sind es die zuletzt bekannten Preise; tippen Sie später erneut auf **Suche** und dann auf **Suchen**.

## Ergebnisse auf der Karte ansehen

1. Suche wie gewohnt starten (Standort oder Adresse).
2. Die Karte zeigt die Tankstellen als farbige Preismarkierungen; die Suchposition ist markiert. Unten links steht „© OpenStreetMap-Mitwirkende“.
3. Mit **+**/**−** zoomen, mit Wischgeste oder den Pfeilen verschieben, mit **◎** den Ausschnitt auf alle Ergebnisse zurücksetzen.
4. Verschieben oder zoomen Sie die Karte, erscheint **Tankstellen in diesem Bereich laden**. Tippen Sie darauf, um die Tankstellen des sichtbaren Ausschnitts zu laden. Die Schaltfläche ist danach 60 Sekunden gesperrt. Ist der Ausschnitt zu groß, erscheint „Bitte näher heranzoomen“.
5. Auf eine Markierung tippen, um die Details der Tankstelle zu öffnen; mit dem Zurück-Pfeil kommen Sie zur Karte zurück.
6. Ziehen Sie das Panel am unteren Rand nach oben, um die Liste zu sehen, oder tippen Sie auf **Liste ausblenden**, um mehr Karte zu sehen. Möchten Sie immer mit ausgeklappter Liste starten, wählen Sie in den **Optionen** die Standardansicht **Liste**; mit **Karte** startet das Panel eingeklappt.

## Suche entlang einer Route

0. Voraussetzung: In den **Optionen** unter **Routensuche** ist ein eigener OpenRouteService-Schlüssel gespeichert und **Routensuche aktivieren** eingeschaltet (siehe [API-Schlüssel](../Preisdaten/api-schluessel.md)). Sonst fehlt die Suchart **Route**.
1. Tippen Sie auf der Karte auf **Suche** (Lupe). Im Suchdialog tippen Sie unter **Suchen nach** auf **Route**.
2. Wählen Sie unter **Start** **Aktueller Standort** oder **Adresse, Ort oder PLZ** (dann den Start eingeben).
3. Geben Sie das **Ziel** ein. Mit **Start löschen** bzw. **Ziel löschen** leeren Sie die Felder.
4. Wählen Sie den **Abstand zur Route** (1, 2, 5 oder 10 km).
5. Tippen Sie auf **Suchen**. Erst jetzt gehen die Eingaben an Nominatim und OpenRouteService; der Fortschritt steht unter der Schaltfläche („Tankstellen werden gesucht (2 von 3)“). Zwischen den Preisabfragen liegen mindestens 10 Sekunden; mit **Suche abbrechen** brechen Sie ab. Während der Suche sind Eingaben und Auswahl gesperrt. Ist die Route für den gewählten Abstand zu lang, nennt der Hinweis die durchsuchbare Länge.
6. Die Tankstellen erscheinen in der Reihenfolge entlang der Route (bei der Sortierung **Entfernung**; sonst gilt die gewählte Sortierung, standardmäßig **Preis**), mit „km ab Start“ und dem Abstand zur Route. Auf der Karte sehen Sie die eingezeichnete Route.

> **Hinweis:** Bei Fehlern erscheint eine verständliche Meldung. Es werden keine Routen, Adressen oder Standorte gespeichert.
