# ADR 0022: Android-Backup deaktiviert — eigene Sicherung bleibt alleinige Backup-Lösung

## Status

Angenommen (2026-11-23)

## Kontext

Issue #66 (Befund aus dem Architektur- und Sicherheitsreview #62) verlangt eine bewusste Entscheidung über den Umgang
mit Android-Backups: `Platforms/Android/AndroidManifest.xml` setzte `android:allowBackup="true"`, ohne
`android:dataExtractionRules` oder `android:fullBackupContent` zu definieren. Damit wären alle App-Daten
unbeabsichtigt Teil von Auto-Backup (Google-Drive-Sicherung) und Gerätetransfer (Device-to-Device) geworden.

Die Datenverzeichniskarte zeigt: Alle schützenswerten Daten liegen unter `FileSystem.AppDataDirectory` (Android:
`files/` des App-Sandkastens, vollständig im Backup-Umfang) und `shared_prefs/` — die Datenbank `tankatlas.db` samt
`-wal`/`-shm`, `receipts/` (Belegfotos), `backups/` mit den eigenen Sicherungen `tankatlas-backup-*.zip` (inklusive
unverschlüsselter Dateien; der Passwortschutz aus [ADR 0019](0019-backup-encryption.md) ist optional), die
Wiederherstellungsbereiche `restore-staging/`, `restore-rollback/` und `restore.journal` sowie die `Preferences`- und
`SecureStorage`-Dateien. `FileSystem.CacheDirectory` (Kartenkacheln `tiles/`, Exporte `exports/`, Diagnosen
`diagnostics/`) ist unter Android ohnehin nie Teil von Auto-Backup. Einen Android-Dateischutz gibt es nicht
(`DatabaseFileProtector` wirkt nur unter `#if IOS`).

Die `SecureStorage`-Ablage verschärft den Befund: Unter Android verschlüsselt sie ihre Werte mit einem
gerätegebundenen Keystore-Schlüssel. Dieser Schlüssel wandert nicht in ein System-Backup; eine wiederhergestellte
SecureStorage-Datei wäre auf dem neuen Gerät daher unlesbar — ein korrupt-voller Zustand statt eines sauberen
Neubeginns.

Android ist Zielplattform (`net11.0-android`), aber nicht im Fokus (README) und wird in der CI nicht gebaut
(`IncludeAndroidTarget: 'false'`); es gibt kein Android-Testprojekt und keinen Emulator-/Device-Job. Das
Akzeptanzkriterium „getestet oder dokumentiert" wird daher über „dokumentiert" erfüllt.

## Entscheidungen

- `android:allowBackup="false"` am `<application>`-Element des Android-Manifests. Damit sind `files/` und
  `shared_prefs/` vollständig von Auto-Backup und Gerätetransfer ausgeschlossen — Datenbank, Belegfotos, eigene
  Sicherungen, Wiederherstellungs-Arbeitsstände, `Preferences` und `SecureStorage` in einem einzigen Attribut.
  Die eigene, anwendergesteuerte Sicherung (Optionen → Sicherung) ist unter Android die alleinige Backup-Lösung.
- Es werden **keine** Attribute `android:dataExtractionRules`, `android:fullBackupContent`, `android:backupAgent`
  oder `android:restoreAnyVersion` ergänzt und **kein** `Platforms/Android/Resources/xml/`-Ordner angelegt —
  Teilkonfigurationen („Backup aus, aber Regeln deklariert") wären widersprüchlich.
- **Abgrenzung zu [ADR 0011](0011-local-backup-restore.md):** Dort bleiben eigene Sicherungen unter iOS bewusst im
  Gerätebackup, weil iOS die Hauptplattform ist und der Ausschluss anders motiviert war. Die ADRs widersprechen sich
  nicht — sie treffen plattformspezifische Entscheidungen. Für Android gilt: ohne System-Backup ist die eigene
  Sicherung der einzige Weg, Daten auf ein neues Gerät zu übertragen; das nennt die Hilfe ausdrücklich.
- **SecureStorage-Verhalten unter Android (Festlegung, nicht gerätetestet):** Die Keystore-verschlüsselte
  Einstellungsdatei wandert wegen `allowBackup="false"` nicht in System-Backups oder Gerätetransfers. Nach
  Neuinstallation oder Gerätewechsel startet die App ohne gespeicherte Schlüssel: Der anwenderseitige
  OpenRouteService-Schlüssel (`tankatlas.routing-api-key`) und der Schalter `tankatlas.routing-enabled` sind neu
  einzugeben; der Tankerkönig-Preisschlüssel (`tankatlas.price-api-key`) wird ohnehin beim Build mitgegeben und von
  `ApiKeyProvider.GetApiKeyAsync` (`ApiKeys.cs:117–128`) automatisch wieder in die Ablage geschrieben. Unlesbare
  Einträge — etwa nach einem Transfer über herstellerseitige Werkzeuge außerhalb von `allowBackup` — tolerieren die
  Aufrufer bereits: Sie fallen auf „kein Schlüssel" zurück (`ApiKeys.cs:101–114`,
  `RouteSearchAccess.cs:132–145, 227–237`; vgl. Schnittstellenkommentar `RouteSearchAccess.cs:10–12`).
- **Regressionsschutz:** Der Unit-Test `AndroidManifestTests_Backup` wertet das Manifest per `XDocument` aus und
  prüft `allowBackup="false"`, genau ein `<application>`-Element sowie die Abwesenheit der Regel-/Agent-Attribute.
  Er läuft unter `net11.0-windows` ohne Android-Workload und ist damit CI-kompatibel.
- **Berechtigungen:** Die minimale Deklaration der tatsächlich benötigten Standort- und Kamera-Berechtigungen bleibt
  — wie vom Issue empfohlen — Aufgabe der späteren produktiven Android-Unterstützung.

## Alternativen

**`dataExtractionRules` + `fullBackupContent` mit Exclude-Listen** (Dual-Format für Android 12+ und ältere
Versionen) wurde verworfen:

- Die Exclude-Listen müssten manuell mit den Dateinamen-Konstanten in `BackupFormat`, `ReceiptPhotoStore` und den
  `shared_prefs`-Dateinamen synchron gehalten werden; ein künftig neuer Ablageort würde still mitgesichert.
- Das SecureStorage-Problem ließe sich mit der Regel-Option nur über einen expliziten Exclude-Eintrag für die
  `sharedpref`-Domain lösen: Ohne diesen Eintrag würde die ohne den Geräte-Keystore unlesbare Datei weiter
  transferiert — genau der Befund, den das Issue beheben will. Der nötige Ausschluss verstärkt damit das
  Wartungsargument aus dem ersten Punkt, statt ein Gegenargument zu liefern.
- Der einzige fachliche Mehrwert eines System-Restores wäre `tankbook.selectedVehicleId` (unkritisch); sämtliche
  Einstellungen stecken ohnehin in `tankatlas.db` und damit in der eigenen Sicherung.
- Die Option brächte eine neue `Resources/xml/`-Struktur und zwei zu pflegende Regelformate für eine Plattform, die
  nicht im Fokus und nicht in der CI ist.

## Folgen

- **Kein automatischer Datenabfluss** in Google-Backup oder Device-to-Device-Transfer — insbesondere nicht die
  potenziell unverschlüsselten eigenen Sicherungen.
- **Datenverlust beim Gerätewechsel ohne eigene Sicherung:** Da das System nichts mitnimmt, ist die eigene Sicherung
  unter Android der einzige Übertragungsweg; die Hilfe (`docs/help/Sicherung`) nennt diesen Unterschied zum iPhone
  ausdrücklich.
- **SecureStorage sauber statt korrupt:** Auf einem neuen Gerät fehlen die Schlüssel einfach, statt als unlesbare
  Einträge zu verbleiben; Routing-Schlüssel und -Schalter sind neu einzugeben, der Preisschlüssel stammt aus dem
  Build.
- **Revision nötig**, sobald Android produktiv unterstützt wird und ein System-Backup gewünscht ist: Dann ist die
  verworfene Regel-Option neu zu bewerten, zugleich sind die Berechtigungen minimal zu deklarieren (siehe oben).
