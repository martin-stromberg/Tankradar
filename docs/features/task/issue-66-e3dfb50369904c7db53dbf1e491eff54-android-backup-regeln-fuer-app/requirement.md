# Übersetzte Anforderung – Issue #66: Android-Backup-Regeln für App-Daten und SecureStorage festlegen

## Quelle

- Issue #66 „Android-Backup-Regeln für App-Daten und SecureStorage festlegen"
- Befund aus dem Architektur- und Sicherheitsreview (#62)
- Priorität: P2

## Ausgangslage / Befund

`src/Tankradar.MAUI/Platforms/Android/AndroidManifest.xml` setzt aktuell `android:allowBackup="true"`, definiert aber weder `android:dataExtractionRules` noch `android:fullBackupContent`. Dadurch können lokale App-Daten – insbesondere die SQLite-Datenbank, Belegfotos und die eigenen Sicherungen der App – unbeabsichtigt in Android-Backups (Auto-Backup) oder Gerätetransfers einbezogen werden. Zusätzlich können wiederhergestellte `SecureStorage`-Einträge je nach Plattformverhalten unbrauchbar sein.

## Fachliche Anforderung

Es ist eine bewusste Entscheidung über den Umgang mit Android-Backups zu treffen und umzusetzen. Die beiden vom Issue genannten Optionen sind:

1. **`android:allowBackup="false"`** setzen – wenn die eigene Sicherung der App die alleinige Backup-Lösung bleiben soll, oder
2. **Backup-Regeln definieren** – `android:dataExtractionRules` für Android 12+ und `android:fullBackupContent` für ältere Android-Versionen festlegen und dabei sensible Dateien sowie die sichere Ablage gezielt ausschließen.

Die getroffene Entscheidung ist im Manifest eindeutig umzusetzen und zu dokumentieren.

## Fachlicher Scope

- **Zu schützende lokale Daten:** SQLite-Datenbank, Belegfotos, lokale (eigene) Sicherungen der App – diese dürfen nicht unbeabsichtigt Teil von Auto-Backup bzw. Gerätetransfer werden.
- **SecureStorage:** Das Verhalten wiederhergestellter `SecureStorage`-Einträge nach einer Wiederherstellung ist zu definieren und zu testen oder zu dokumentieren.
- **Regressionsschutz:** Ein Manifest-Test soll sicherstellen, dass die Backup-Entscheidung nicht versehentlich zurückgenommen wird.
- **Hinweis (nicht zwingend Teil dieser Umsetzung):** Wird Android später produktiv unterstützt, sollten zugleich die tatsächlich benötigten Standort- und Kamera-Berechtigungen minimal deklariert werden.

## Betroffene Komponenten

- `src/Tankradar.MAUI/Platforms/Android/AndroidManifest.xml` (zentraler Änderungspunkt)
- Ggf. neue Konfigurationsdateien für Backup-Regeln (`dataExtractionRules`/`fullBackupContent`), abhängig von der gewählten Option
- Manifest-Test zur Absicherung der Entscheidung
- Dokumentation der Entscheidung und des `SecureStorage`-Verhaltens

## Akzeptanzkriterien

- Die Backup-Entscheidung ist im Manifest eindeutig umgesetzt und dokumentiert.
- Datenbank, Belegfotos und lokale Sicherungen sind nicht unbeabsichtigt Teil von Auto-Backup/Gerätetransfer.
- Das Verhalten von `SecureStorage` nach Wiederherstellung ist definiert und getestet oder dokumentiert.
- Ein Manifest-Test verhindert Regressionen.

## Offene Fragen

1. **Grundsatzentscheidung:** `android:allowBackup="false"` (eigene Sicherung bleibt alleinige Backup-Lösung) oder differenzierte Backup-Regeln über `dataExtractionRules`/`fullBackupContent`? Das Issue stellt beide Optionen gleichwertig zur Entscheidung.
2. **Bei Wahl der Regel-Option:** Welche Dateien/Verzeichnisse gelten konkret als „sensible Dateien" bzw. „sichere Ablage" und sind gezielt auszuschließen?
3. **SecureStorage-Verhalten:** Soll das Verhalten nach Wiederherstellung getestet (ggf. mit Abhängigkeit von Android-Gerät/Emulator) oder nur dokumentiert werden – das Issue lässt beides zu („getestet oder dokumentiert").
4. **Standort- und Kamera-Berechtigungen:** Gehört die minimale Deklaration der benötigten Berechtigungen bereits zu dieser Aufgabe oder erst zu einer späteren produktiven Android-Unterstützung? Das Issue formuliert dies als ergänzende Empfehlung für „später".
