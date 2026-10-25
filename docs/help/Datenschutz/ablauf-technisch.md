← [Zurück zur Übersicht](index.md)

# Datenschutz — Technischer Ablauf

## Komponenten

| Komponente | Aufgabe |
|-----------|---------|
| `src/Tankradar.MAUI/Platforms/iOS/Info.plist` | Berechtigungstexte (nur Kamera und Standort bei Nutzung), `ITSAppUsesNonExemptEncryption` = false, Geräteangaben |
| `src/Tankradar.MAUI/Platforms/iOS/Resources/PrivacyInfo.xcprivacy` | Privacy-Manifest: kein Tracking, erfasste Datenart, Required-Reason-APIs; landet im Bundle-Root |
| `IDatabaseFileProtector` / `DatabaseFileProtector` | setzt unter iOS `NSFileProtectionCompleteUntilFirstUserAuthentication` für Datenbank (mit `-wal`/`-shm`), Belegfotos, Sicherungsdateien und Exportdateien; außerhalb von iOS ohne Wirkung |
| `SecureStorageApiKeyStore` / `CredentialLockerApiKeyStore` | Schlüssel in Keychain (iOS, Zugriffsklasse AfterFirstUnlock durch `SecureStorage`) bzw. Credential Locker (Windows) |
| `IosProjectFilesTests` | prüft Info.plist und Manifest per XML-Auswertung |
| `scripts/test-ios-deployment.ps1`, `iOS-Deployment.ps1` (`Invoke-IpaValidation`), Schritt *Validate iOS package* in `package-ios` | prüfen Manifest, Verschlüsselungsangabe und Mindestversion im gebauten Paket |

## Berechtigungstexte

Die Fotoauswahl („Foto auswählen“) nutzt die Foto-Auswahl des Betriebssystems (PHPicker, iOS 14 und neuer); sie läuft außerhalb der App und braucht weder eine Berechtigung noch eine Beschreibung. Die Aufnahme braucht nur die Kamera ([ADR 0016](../../adr/0016-maui-version-and-photo-permissions.md)).

| Schlüssel | Text | Verwendung |
|-----------|------|-----------|
| `NSLocationWhenInUseUsageDescription` | Zur Ermittlung von Tankstellen in deiner Nähe und – wenn du es wählst – als Startpunkt einer Route. | Suche „In der Nähe“, Routenstart „aktueller Standort“ |
| `NSCameraUsageDescription` | Foto eines Tankbelegs aufnehmen | Belegfoto |

Weitere `NS…UsageDescription`-Schlüssel sind nicht erlaubt (Test `Info_plist_DeclaresOnlyCameraAndLocationWhenInUse`). `NSLocationAlwaysAndWhenInUseUsageDescription` und Hintergrundmodi fehlen bewusst.

## Privacy-Manifest

| Eintrag | Wert | Begründung |
|---------|------|-----------|
| `NSPrivacyTracking` | false | kein Tracking |
| Erfasste Datenart | `PreciseLocation`, Zweck `AppFunctionality`, nicht verknüpft, kein Tracking | Standort für Suche und Routenstart (wird an Dienste übermittelt, nicht gespeichert) |
| `FileTimestamp` | `C617.1`, `3B52.1` | Zeitstempel von Dateien im App-Container (Belege, Sicherungen); vom Anwender gewählte Datei (Sicherung einspielen) |
| `SystemBootTime` | `35F9.1` | Laufzeit (Zeitmessung) |
| `DiskSpace` | `E174.1` | Laufzeit (Speicherprüfung) |
| `UserDefaults` | `CA92.1` | MAUI Essentials (`Preferences`) bleibt im Bundle; Zugriff nur auf Daten der eigenen App. Fehlende Deklaration würde bei Apple ITMS-91053 auslösen, Überdeklaration nicht |

## Dateischutz

Die Schutzklasse ist bewusst `CompleteUntilFirstUserAuthentication`: Die höchste Klasse (`Complete`) würde Datenbankzugriffe im gesperrten Zustand (Aktualisierung im Hintergrund) verhindern. Neue Dateien in geschützten Verzeichnissen erben die Klasse; ausdrücklich geschützt werden Datenbank, `-wal`, `-shm`, Belegfotos (`ReceiptPhotoStore`), fertige Sicherungen (`BackupService`) und fertige Exportdateien (`TankbookExportService`, Test `Create_ProtectsFinishedFile`).

## Prüfungen ohne Mac

Unter Windows prüfbar: Textmuster und XML-Inhalt (siehe oben), Skriptlogik, Workflow-Validierung. **Nicht** prüfbar ohne Mac: ob Apple das Paket annimmt (Verarbeitung nach dem Upload), tatsächliches Verhalten der Berechtigungsdialoge, `MinimumOSVersion`, Icon 1024 px und Linker-Ergebnis in der gebauten IPA (das prüfen `Invoke-IpaValidation` und der CI-Schritt zur Laufzeit).

← [Zurück zur Übersicht](index.md)
