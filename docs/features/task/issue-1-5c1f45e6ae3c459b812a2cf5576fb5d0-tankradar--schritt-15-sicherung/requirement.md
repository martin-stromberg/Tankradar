# Anforderung: Schritt 15 – Lokale Sicherung und Wiederherstellung (Tankatlas, Version 0.1.19)

Quelle: `docs/projects/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar/project-plan.md`, Abschnitt „Schritt 15“ samt „Zusatz aus der Abnahme von Schritt 14“. Kontext: `requirement.md` des Projekts (Abschnitte Backup und Sicherheit), Vorgehensentscheidungen (Datenhaltung, Schutz lokaler Daten, Windows-Zwischenstände, Oberflächentests als Auslieferungs-Gate), `acceptance-schritt-14.md`, ADR 0004 bis 0010. Eine `docs/features.md` existiert nicht; vergleichbare Features stehen unter `docs/help/` und `docs/adr/`.

## Fachliche Zusammenfassung

Im Bereich „Optionen“ kann der Anwender auf eigene Veranlassung eine lokale Sicherung als ZIP-Datei erstellen und aus einer Sicherung wiederherstellen. Die Sicherung umfasst die gesamte SQLite-Datenbank `tankatlas.db` (Einstellungen, Spritsorten, Favoritengruppen mit Notizen und Prioritäten, Favoriten, Fahrzeuge, Tank- und Ladevorgänge, lokal bekannte Tankstellen und alle gespeicherten Preisstände mit Zeitstempel) und die Belegfotos aus `receipts/`, dazu ein Manifest mit Schemaversion, App-Version und Datenbank-Migrationsstand. Nicht enthalten sind API-Schlüssel (Keychain/Credential Locker), der Kartenkachel-Cache und Standortdaten. Die Wiederherstellung prüft die Datei vollständig vor jedem Eingriff, spielt ältere Sicherungen über die Migrationen der aktuellen App ein, lehnt zu neue Sicherungen ab und ist atomar (Staging, Austausch mit Rollback). Hinzu kommen: Sicherungsliste (Datum, Größe), Löschen nach Rückfrage, Teilen/Speichern über die Systemfunktion, Einspielen einer externen Datei, ein Hinweis auf die Sicherung in der Löschrückfrage für Fahrzeuge mit Vorgängen und drei Restpunkte aus der Abnahme von Schritt 14.

## Fachliche Punkte (aus der Anforderung abgeleitet)

1. **Sicherung erstellen:** nur auf Veranlassung; Ablage lokal im App-Datenverzeichnis (`backups/`); keine automatische Übertragung an Dritte oder Cloud; konsistenter Datenbankstand auch bei laufender App.
2. **Umfang:** alle Nutzerdaten (Einstellungen, Favoritengruppen mit Notizen/Prioritäten, Favoriten, Fahrzeuge, Tank-/Ladevorgänge mit Belegfotos) und alle gespeicherten Preisstände mit Zeitstempel (historische Daten bleiben für Auswertungen erhalten). Keine Geheimnisse, keine Standortdaten.
3. **Wiederherstellung:** Der Datenstand ist danach vollständig; veraltete Preisstände erkennt die App weiterhin am Zeitstempel und aktualisiert sie bei Bedarf über die API (kein neuer Mechanismus; der Preisdienst darf aber keine veralteten Zwischenspeicher im Arbeitsspeicher behalten).
4. **Versionen:** Eine Sicherung einer älteren App-Version lässt sich einspielen (Migration auf den aktuellen Stand); eine Sicherung einer neueren Version wird als „zu neu“ abgelehnt.
5. **Sicherheit der Wiederherstellung:** Rückfrage vor dem Überschreiben; beschädigte oder ungültige Datei führt zu einer verständlichen Fehlermeldung, ohne Daten zu verändern; Zip-Slip-Schutz; Atomarität (Validierung, Staging, Rollback); Belegfotos bleiben unter iOS unter Dateischutz.
6. **Auswahl-/Identifikationsanforderung:** Der Anwender wählt eine Sicherung aus einer Liste (Erstellungsdatum, Größe) zur Wiederherstellung, zum Teilen oder zum Löschen aus (nicht per Eingabe eines Dateinamens); eine externe Datei wählt er über den Dateiauswahldialog des Betriebssystems.
7. **Verwaltung:** Liste, Löschen einzelner Sicherungen nach Rückfrage, Teilen/Speichern (MAUI `Share`), Import (MAUI `FilePicker`).
8. **Fahrzeug löschen:** Die Rückfrage bei Fahrzeugen mit Tank-/Ladevorgängen weist darauf hin, dass sich vorher eine Sicherung erstellen lässt.
9. **Tests:** Unit/Integration für Vollständigkeit, Zeitstempel, ungültige Dateien, ältere Version, Liste, Löschen; FlaUI-Ablauf „Sicherung erstellen – Daten ändern – aus der Liste wiederherstellen – Sicherung löschen“; E2E-Tests dürfen keine Systemdialoge öffnen.
10. **Zwischenstand:** startfähiger Windows-Stand unter `review-versions/<Version>_<JJJJ-MM-TT>/` (gitignored, nicht committen).
11. **Restpunkte aus Abnahme 14:** ADR 0010 ergänzen (Verbrauchsverlauf nicht in der Monatskarte auf eigener Fläche, vorletzter Balken nicht abgestuft; Begründung „keine Symbole“ präzisieren, da Symbole laut ADR 0004 als Vektorpfade möglich sind); Kachelzahlen mit vier Stellen vor dem Komma bei 360–375 pt nicht mitten in der Zahl umbrechen; Sichtprüfung der Auswertung (hell/dunkel, 360/390 pt) nachholen bzw. vermerken.

## Betroffene Klassen und Komponenten (voraussichtlich)

Neu (Namespace `Tankradar.MAUI.Services.Backup`, Modelle in `Tankradar.MAUI.Models.Backup`):
- `IBackupService` / `BackupService` (Erstellen, Auflisten, Löschen, Prüfen, Wiederherstellen, Import), `BackupArchiveWriter`, `BackupArchiveValidator`, `BackupManifest`, `BackupFileInfo`, Ergebnis-/Fehler-Enums, `BackupLimits`/`BackupFormat`.
- `IBackupFilePicker` / `MauiBackupFilePicker` / `TestBackupFilePicker` / `BackupFilePickerSelector` sowie `IBackupSharer` / `MauiBackupSharer` / `TestBackupSharer` / `BackupSharerSelector` (Muster `ReceiptPhotoPickerSelector`).
- `BackupViewModel`, `BackupItemViewModel`, `BackupTexts` (zentrale Texte unter `Resources/Texts`).
- Erweiterung `TestDataPaths` um Umgebungsvariablen für den Testmodus (Importdatei, Freigabeziel).

Geändert: `SettingsPage.xaml`, `SettingsViewModel`, `MauiProgram` (DI), `DatabaseInitializer` (Wiederaufnahme nach unterbrochener Wiederherstellung), `TankbookTexts.FormatDeleteQuestion`, `FuelPriceService`/`IFuelPriceService` (Zwischenspeicher nach Wiederherstellung verwerfen), `TankbookEntriesPage.xaml` und Statistik-ViewModel (Kachelzahl), `Platforms/iOS/Info.plist` (nur prüfen), ADR 0010, Hilfe, README, `changes.log`. Neu außerdem ADR 0011 und die Hilfe `docs/help/Sicherung/`.

Tests: Unit (`Tankradar.Tests.Unit`), Integration (`Tankradar.Tests.Integration`, echte SQLite-Dateien, Migrationsstand), E2E (`Tankradar.Tests.E2E`, `BackupE2ETestBase`, `BackupE2ETests_*`).

## Implementierungsansatz

- ZIP mit `System.IO.Compression` (Teil der .NET-Basisbibliothek, keine neue Abhängigkeit). Konsistente Datenbankkopie über die SQLite-Online-Backup-Funktion (`SqliteConnection.BackupDatabase`) in eine temporäre Datei, nicht durch Kopieren der laufenden Datei (WAL).
- Manifest `manifest.json` mit Format-Kennung, `schemaVersion` (Sicherungsformat), `appVersion`, `databaseMigration` (letzte Zeile von `__EFMigrationsHistory`), `createdUtc`, Dateiliste (Pfad, Größe, SHA-256) und Zählwerten.
- Wiederherstellung: Validieren (Struktur, Namen, Größen, Prüfsummen, Version, SQLite-Integritätsprüfung) – Staging im Datenverzeichnis – Migration der Staging-Datenbank auf den aktuellen Stand – Austausch von Datenbank und `receipts/` mit Rücksicherung (Rollback) – Aufräumen; Markierungsdatei zur Wiederaufnahme nach einem Absturz.
- Vorhandene Muster: `IAppDataPathProvider` (Testmodus `TANKATLAS_TEST_DATA_PATH`), `IDatabaseFileProtector.ProtectFile` (iOS-Dateischutz), Selector-Muster für testbare Plattformdienste, Inline-Rückfragen statt Systemdialoge (wie beim Löschen von Fahrzeugen), `IScreenReaderAnnouncer`, zentrale Texte, `SettingsViewModel` mit eingebettetem Teil-ViewModel (`DataSourceViewModel`).

## Konfiguration

Keine Benutzereinstellung. Technische Konstanten (Grenzen für Eintragszahl und Größe, Dateinamensmuster, Format-Schemaversion) in `BackupLimits`/`BackupFormat`. Testmodus nur über Umgebungsvariablen (`TANKATLAS_TEST_DATA_PATH` plus neue Variablen für Importdatei und Freigabeziel).

## Annahmen und Entscheidungen (keine offenen Fragen)

- Standortdaten: Die Datenbank enthält keinen Anwenderstandort (die Einstellung „Standortnutzung“ ist nur eine Präferenz, der Suchcache mit gerundeten Koordinaten liegt nur im Arbeitsspeicher); die Koordinaten der Tankstellen sind öffentliche Stammdaten und für Favoriten nötig. Ein Test sichert zu, dass keine Standort- oder Schlüsseldaten ins Archiv gelangen.
- Keine Verschlüsselung der Sicherung (Entscheidung „Schutz lokaler Daten“); die Hilfe weist darauf hin, dass eine geteilte Datei unverschlüsselt ist.
- Keine automatischen oder zeitgesteuerten Sicherungen und keine automatische Begrenzung der Zahl der Sicherungen; Aufräumen erfolgt durch den Anwender.
- Für den Bereich „Sicherung“ in den Optionen enthält der Designentwurf keine Vorlage; es gelten die vorhandenen Karten der Optionen (Abweichungsvermerk im ADR 0011).
