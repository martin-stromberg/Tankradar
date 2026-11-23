# Android-Plattform und Manifest

## Manifest-Inhalt (Stand)

`src/Tankradar.MAUI/Platforms/Android/AndroidManifest.xml` ist die einzige Android-Konfigurationsdatei und besteht vollständig aus (Zeilen 1–6):

- `<application android:allowBackup="true" android:icon="@mipmap/appicon" android:roundIcon="@mipmap/appicon_round" android:supportsRtl="true">` (Zeile 3) — **kein** `android:dataExtractionRules`, **kein** `android:fullBackupContent`, kein `android:hasFragileUserData`, kein `android:backupAgent`, kein `android:restoreAnyVersion`.
- `<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />` (Zeile 4)
- `<uses-permission android:name="android.permission.INTERNET" />` (Zeile 5)

Folge des Stands: Android-Auto-Backup (ab API 23 / Android 6, Mindest-SDK der App ist 21, also auf allen unterstützten Geräten ab Android 6 wirksam) und Gerät-zu-Gerät-Transfer nehmen standardmäßig `files/` (≙ `FileSystem.AppDataDirectory`), `shared_prefs/` und weitere App-Verzeichnisse mit — lediglich `cache/` und `no_backup/` sind von Hause aus ausgenommen. Die App definiert keinen `no_backup/`-Pfad (wäre nur mit eigenem `BackupAgent`/Regeln oder `fullBackupContent` relevant).

## Übrige Android-Dateien

Unter `src/Tankradar.MAUI/Platforms/Android/` liegt sonst nur:

- `MainActivity.cs` — Standard-`MauiAppCompatActivity` mit Splash-Theme, `LaunchMode.SingleTop` (Zeilen 10–12); keine `OnBackPressed`-/Backup-relevante Logik.
- `MainApplication.cs` — Standard-[Application]-Klasse.
- `Resources/values/colors.xml` — einzige Android-Ressourcendatei.

**Es gibt keinen Ordner `Platforms/Android/Resources/xml/`.** Für die Regel-Option (`dataExtractionRules` / `fullBackupContent`) müsste eine XML-Regeldatei dort angelegt werden (MAUI übernimmt `Platforms/Android/Resources/**` als `AndroidResource`); `android:fullBackupContent` erwartet ebenfalls eine `res/xml`-Datei mit `<full-backup-content>`-Struktur, `android:dataExtractionRules` eine mit `<data-extraction-rules>`.

## Projekt- und CI-Einbindung von Android

`src/Tankradar.MAUI/Tankradar.MAUI.csproj`:

- Zielframeworks per Eigenschaft ausblendbar (Zeilen 8–16): `IncludeAndroidTarget` ist standardmäßig `true` → `net11.0-android` gehört lokal zum Standardbuild; `SupportedOSPlatformVersion` für Android ist `21.0` (Zeile 57).
- `ApplicationId` = `de.martinstromberg.tankradar` (Zeile 42), Anzeigename „Tankatlas“ (Zeilen 37–39).

CI: **Alle** Workflows bauen ohne Android — `IncludeAndroidTarget: 'false'` in `pr-staging-ci.yml` (Zeilen 57, 116, 246), `staging-ci.yml` (Zeilen 53, 109, 239, 386), `release.yml` (Zeile 20), `security-scan.yml` (Zeile 19). Es gibt weder einen Android-Build-Job noch Android-Emulator- oder Instrumentation-Tests.

README (`README.md:147`, „Bekannte Einschränkungen“): „**Android/MacCatalyst-Ziele** des MAUI-Templates sind vorhanden, aber nicht im Fokus“ — Deckung mit dem Issue-Hinweis, dass minimale Standort-/Kamera-Berechtigungen erst bei produktiver Android-Unterstützung deklariert werden sollen.

## Plattform-Dateischutz auf Android

`DatabaseFileProtector` (`src/Tankradar.MAUI/Services/DatabaseFileProtector.cs`, Zeilen 9–11, 40–60) setzt `NSFileProtection` nur unter `#if IOS`; auf Android (und Windows/MacCatalyst) ist `Protect`/`ProtectFile` wirkungslos (nur `LogDebug`). Der „Dateischutz“ der Daten unter Android beruht damit allein auf der App-Sandbox — das stärkt das Argument, unverschlüsselte eigene Sicherungen nicht zusätzlich in ein Auto-Backup/Google-Cloud zu geben.

## Auswirkungen der beiden Issue-Optionen auf diese Codebasis

- **`allowBackup="false"`:** eine Zeile in `AndroidManifest.xml:3`; keine weiteren Dateien nötig. Deckt sich mit der Rolle der eigenen Sicherung als alleiniger, anwendergesteuerter Backup-Lösung (ADR 0011/0019) und mit „Android nicht im Fokus“.
- **Regel-Option:** benötigt neue `Platforms/Android/Resources/xml/*.xml` (im MAUI-Single-Project automatisch `AndroidResource`) plus `android:dataExtractionRules` (Android 12+) **und** `android:fullBackupContent` (ältere Versionen, wirksam bis Android 11) im Manifest. Auszuschließende Pfade wären mindestens die in [datenablage.md](datenablage.md) gelisteten (`tankatlas.db` samt `-wal`/`-shm`, `receipts/`, `backups/`, `restore-staging/`, `restore-rollback/`, `restore.journal`, `test-shared/` sowie die `SecureStorage`-/`Preferences`-Dateien in `shared_prefs/` — `dataExtractionRules` erlaubt `<exclude>` für `sharedpref`-Domains).
