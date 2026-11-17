← [Zurück zur Übersicht](index.md)

# Sicherung — Ablauf für Anwender

## Voraussetzungen

Tankatlas ist gestartet. Die Sicherung finden Sie unter **Optionen**.

## Schritt-für-Schritt-Anleitung

### 1. Sicherung erstellen

1. Öffnen Sie **Optionen** und gehen Sie zur Karte **Sicherung**.
2. Möchten Sie die Datei schützen, tragen Sie in **Passwort** und **Passwort wiederholen** dasselbe Passwort ein; lassen Sie beide Felder frei für eine unverschlüsselte Datei. Das Passwort wird nicht gespeichert — ohne es lässt sich eine verschlüsselte Sicherung nicht mehr öffnen.
3. Tippen Sie auf **Sicherung erstellen**. Nach kurzer Zeit erscheint „Sicherung erstellt.“ und die neue Sicherung steht oben in der Liste; eine verschlüsselte trägt den Zusatz „(verschlüsselt)“.

### 2. Sicherung wiederherstellen

1. Tippen Sie beim gewünschten Eintrag auf **Wiederherstellen**.
2. Bei einer verschlüsselten Sicherung fragt Tankatlas zuerst das Passwort ab. Tippen Sie es ein und auf **Prüfen**. Die Abfrage bleibt auch bei fehlender oder falscher Eingabe offen — Sie können es direkt erneut versuchen.
3. Lesen Sie die Rückfrage. Möchten Sie den aktuellen Stand behalten, tippen Sie auf **Abbrechen**, erstellen zuerst eine neue Sicherung und beginnen dann erneut.
4. Tippen Sie auf **Ja, wiederherstellen**. Tankatlas meldet „Sicherung wiederhergestellt …“. Favoriten, Tankbuch und Optionen zeigen jetzt den Stand der Sicherung.

### 3. Sicherung löschen

1. Tippen Sie beim Eintrag auf **Löschen**.
2. Bestätigen Sie mit **Ja, löschen** oder brechen Sie mit **Abbrechen** ab. Ihre aktuellen Daten bleiben in beiden Fällen unverändert.

### 4. Sicherung weitergeben oder speichern

Tippen Sie beim Eintrag auf **Teilen** und wählen Sie im Systemdialog das Ziel, z. B. „In Dateien sichern“. Die Datei ist nur dann verschlüsselt, wenn Sie sie beim Erstellen mit einem Passwort geschützt haben.

### 5. Sicherung aus einer Datei einspielen

1. Tippen Sie auf **Sicherung aus Datei einspielen** und wählen Sie die ZIP-Datei. Ist sie verschlüsselt, fragt die App das Passwort ab; mit **Prüfen** geht es weiter, mit **Abbrechen** wird die Datei verworfen. Lassen Sie das Feld frei, bleibt die Abfrage mit der Aufforderung „Diese Sicherung ist verschlüsselt. Gib das Passwort ein. …“ offen.
2. Ist sie gültig (einschließlich ihrer Datenbank), erscheint sie in der Liste, und die Rückfrage zur Wiederherstellung öffnet sich gleich — das eingegebene Passwort gilt mit, es wird nicht noch einmal abgefragt. Sie können die Rückfrage bestätigen oder die Sicherung später über **Wiederherstellen** nutzen.
3. Ist sie ungültig, beschädigt, unbrauchbar (zum Beispiel mit fehlenden Tabellen oder ungültigen Werten) oder aus einer neueren Version, erscheint eine Meldung; Ihre Daten und die Liste bleiben unverändert.

## Beispiele

**Gerätewechsel:** Auf dem alten Gerät **Sicherung erstellen** und **Teilen** („In Dateien sichern“). Auf dem neuen Gerät die Datei bereitstellen, **Sicherung aus Datei einspielen** und die Wiederherstellung bestätigen.

**Vor einer Änderung:** Erstellen Sie eine Sicherung, ändern Sie Daten, und stellen Sie die Sicherung bei Bedarf wieder her.

## Häufige Fehler und Lösungen

| Meldung | Bedeutung | Lösung |
|---------|-----------|--------|
| „Die beiden Passwörter stimmen nicht überein. Es wurde keine Sicherung erstellt.“ | Die beiden Passwortfelder beim Erstellen enthielten verschiedene Eingaben. | Beide Felder gleich ausfüllen (oder für eine unverschlüsselte Sicherung frei lassen) und erneut erstellen. |
| „Die Datei ist keine gültige Tankatlas-Sicherung oder beschädigt …“ | Die Datei ist keine Sicherung, wurde verändert oder unvollständig übertragen. | Datei erneut übertragen oder eine andere Sicherung wählen. |
| „Diese Sicherung ist verschlüsselt. Gib das Passwort ein. …“ | **Prüfen** wurde ohne Eingabe gewählt. | Passwort eingeben und erneut auf **Prüfen** tippen. |
| „Das Passwort ist falsch oder die Datei ist beschädigt …“ | Das eingegebene Passwort stimmt nicht, oder die verschlüsselte Datei wurde verändert/unvollständig übertragen. | Passwort prüfen und erneut eingeben, sonst Datei erneut übertragen. |
| „Diese Sicherung stammt aus einer neueren Version von Tankatlas …“ | Die Sicherung wurde mit einer neueren App erstellt. | App aktualisieren. |
| „Die Sicherung konnte nicht erstellt werden …“ | Meist zu wenig freier Speicherplatz. | Speicher freigeben und erneut versuchen. |
| „Die Aktion konnte nicht ausgeführt werden. Deine Daten wurden nicht verändert.“ | Die Wiederherstellung ist fehlgeschlagen; der alte Stand wurde zurückgegeben. | Erneut versuchen, bei Wiederholung eine andere Sicherung wählen. |
| „Es läuft bereits ein anderer Sicherungsvorgang …“ | Eine andere Aktion läuft noch. | Warten und erneut versuchen. |
| „Die Sicherung konnte nicht weitergegeben werden.“ | Das Teilen ist fehlgeschlagen. | Erneut versuchen. |

← [Zurück zur Übersicht](index.md)
