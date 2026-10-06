← [Zurück zur Übersicht](index.md)

# Umkreissuche — Ablauf für Anwender

## Voraussetzungen

- In den **Optionen** ist mindestens eine Spritsorte gewählt und **Standort und GPS** steht auf **Immer** oder **Nur bei Nutzung** (siehe [Einstellungen](../Einstellungen/index.md)).
- Ein API-Schlüssel ist hinterlegt (siehe [API-Schlüssel hinterlegen](../Preisdaten/api-schluessel.md)).
- Für Live-Preise besteht eine Netzverbindung.

## Schritt-für-Schritt-Anleitung

### 1. Bereich öffnen

Tippen Sie in der unteren Menüleiste auf **Karte**.

### 2. Radius festlegen

Unter **Suchradius** ist voreingestellt **5 km** gewählt. Tippen Sie bei Bedarf auf einen anderen Chip (**1**, **2**, **5**, **10**, **15** oder **25 km**).

### 3. Suchen

Tippen Sie auf **Suchen**. Beim ersten Mal fragt das Gerät eventuell, ob Tankatlas auf den Standort zugreifen darf; erlauben Sie es für die Umkreissuche. Während der Suche zeigt die App einen Ladeanzeiger.

> **Hinweis:** Steht **Standort und GPS** auf **Nie**, erscheint ein Hinweis und es wird nichts abgefragt.

### 4. Ergebnis eingrenzen

Tippen Sie unter **Spritsorte** auf **Alle** oder eine Sorte und unter **Sortierung** auf **Preis**, **Entfernung** oder **Name**. Die Liste ändert sich sofort. Bei sehr langen Listen tippen Sie unten auf **Weitere anzeigen**.

### 5. Preise lesen

Je Tankstelle sehen Sie Name, Entfernung, Adresse, Preise mit Alter („vor X Min.“, ab 60 Minuten amberfarben) und gegebenenfalls **Preis unbestätigt** bzw. **Automatentankstelle**.

## Suche nach Adresse, Ort oder PLZ

1. Tippen Sie im Bereich **Karte** unter **Suchen nach** auf **Adresse, Ort oder PLZ**. Ein Eingabefeld und die Quellenangabe „Geodaten © OpenStreetMap-Mitwirkende“ erscheinen.
2. Geben Sie eine Adresse, einen Ort oder eine Postleitzahl ein (z. B. „Frankfurt“ oder „60311“). Mit **Eingabe löschen** leeren Sie das Feld.
3. Wählen Sie den Radius (**1** bis **25 km**).
4. Tippen Sie auf **Suchen** (oder betätigen Sie die Such-/Eingabetaste). Erst jetzt wird die Eingabe an OpenStreetMap-Nominatim gesendet; dort darf höchstens eine Anfrage je Sekunde eingehen, die App wartet bei Bedarf kurz.
5. Über der Liste steht „Suche rund um: <Ort>“; die Entfernungen beziehen sich auf diesen Ort. Filter und Sortierung wirken wie bei der Standortsuche.

Ein GPS ist dafür nicht nötig; es funktioniert auch bei **Standort und GPS** = **Nie**. Zurück zur Standortsuche kommen Sie über **Aktueller Standort**.

> **Hinweis:** Bei nicht gefundenem Ort, ungültiger Eingabe oder fehlender Verbindung erscheint eine verständliche Meldung (siehe [Beschreibung](beschreibung.md)); die Adresse wird nicht gespeichert.

## Ergebnis

Eine Liste der Tankstellen im gewählten Umkreis. Erscheint „Offline: keine Verbindung zum Preisdienst.“, sind es die zuletzt bekannten Preise; tippen Sie später erneut auf **Suchen**.
