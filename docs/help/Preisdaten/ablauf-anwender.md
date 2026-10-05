← [Zurück zur Übersicht](index.md)

# Kraftstoffpreise — Ablauf für Anwender

## Quellenangabe ansehen

1. Öffnen Sie den Bereich **Optionen**.
2. Am Ende der Seite zeigt die Karte **Datenquelle** die Angabe **Daten: Tankerkönig / MTS-K** und den Lizenzhinweis.

## Verbindung zum Preisdienst prüfen

1. Öffnen Sie **Optionen** und gehen Sie zur Karte **Datenquelle**.
2. Prüfen Sie die Zeile **API-Schlüssel**: „hinterlegt“ oder „nicht hinterlegt“. Wie Sie den Schlüssel hinterlegen, steht unter [API-Schlüssel hinterlegen](api-schluessel.md).
3. Tippen Sie auf **Verbindung zum Preisdienst prüfen**.
4. Darunter erscheint das Ergebnis:

| Meldung | Bedeutung |
|---------|-----------|
| Der Preisdienst ist erreichbar. | Alles in Ordnung. |
| Keine Netzverbindung. Es werden die zuletzt bekannten Preise verwendet. | Das Gerät ist offline. |
| Der Preisdienst ist nicht erreichbar. Es werden die zuletzt bekannten Preise verwendet. | Verbindung zum Dienst nicht möglich (auch nach Wiederholungen). |
| Es ist kein API-Schlüssel hinterlegt. … | Ohne Schlüssel sind keine neuen Preise abrufbar. |
| Der Preisdienst hat die Anfrage abgelehnt. Bitte den API-Schlüssel prüfen. | Schlüssel ungültig oder Nutzungsgrenze erreicht. |
| Der Preisdienst hat eine unerwartete Antwort geliefert. | Dienst liefert Unbrauchbares. |

## Offline

Ohne Verbindung zeigt Tankatlas die zuletzt gespeicherten Preise samt Alter („vor X Min.“). Sobald die Verbindung wieder besteht, werden veraltete Preise von den Ansichten aktualisiert (ab dem Schritt, in dem Suche und Favoriten die Preise anzeigen).
