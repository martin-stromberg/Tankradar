# Plan-Prüfung – Issue #66: Android-Backup-Regeln für App-Daten und SecureStorage

**Status: Plan vollständig**

Der Plan wurde gegen `requirement.md`, `inventory.md` samt Detaildokumenten und den tatsächlichen
Repository-Stand geprüft. Alle Akzeptanzkriterien sind belegt erfüllbar, alle vier offenen Fragen der
Anforderung sind entschieden, und die genannten Dateiorte, Zeilen und Konventionen stimmen mit dem
Repo überein. Die untenstehenden Fundstellen sind Verbesserungshinweise, keine Lücken.

## 1. Fachliche Entscheidung: `allowBackup="false"` als einfachste vollständige Lösung

Bestätigt. Die Verzeichniskarte ([inventory/datenablage.md](inventory/datenablage.md)) zeigt, dass
alle zu schützenden Daten (Datenbank samt `-wal`/`-shm`, `receipts/`, `backups/`,
`restore-staging/`, `restore-rollback/`, `restore.journal`, `test-shared/`) unter `files/` liegen
und `Preferences`/`SecureStorage` unter `shared_prefs/` — beide Bereiche werden durch ein einziges
Attribut aus Auto-Backup und Gerät-zu-Gerät-Transfer ausgenommen. Die Regel-Option hätte zwei neue
`Resources/xml`-Dateien plus doppelt gepflegte Exclude-Listen erfordert (Ordner existiert nicht,
verifiziert: `Platforms/Android/` enthält nur Manifest, `MainActivity.cs`, `MainApplication.cs`,
`Resources/values/colors.xml`). Die Entscheidung ist zudem fachlich deckungsgleich mit der
Architektur (eigene Sicherung als alleiniger Backup-Weg, ADR 0011/0019; sämtliche Einstellungen
liegen in `tankatlas.db`) und mit „Android nicht im Fokus" (`README.md:147`, verifiziert).

**Präzisierung (keine Lücke):** Der Plan formuliert, die Regel-Option „löst das SecureStorage-Problem
nicht". Streng genommen kann sowohl `dataExtractionRules` als auch `fullBackupContent` die Domain
`sharedpref` ausschließen — die Bestandsaufnahme sagt das selbst („müsste … die sharedpref-Domain
ausschließen"). Korrekt wäre: Das Problem wäre nur über zusätzliche Exclude-Einträge lösbar, was das
Wartungsargument (Exclude-Listen synchron zu `BackupFormat`-/`ReceiptPhotoStore`-Konstanten halten)
verstärkt statt schwächt. Beim Schreiben von ADR 0022 die Begründung entsprechend formulieren; die
Entscheidung selbst bleibt unverändert richtig.

## 2. Akzeptanzkriterien — Abdeckung belegt

| Akzeptanzkriterium | Abdeckung im Plan | Prüfung |
|--------------------|-------------------|---------|
| Entscheidung im Manifest eindeutig umgesetzt und dokumentiert | `android:allowBackup="false"` (Schritt 1) + XML-Kommentar + ADR 0022 (Schritt 3) + A1 | `AndroidManifest.xml:3` enthält aktuell `allowBackup="true"` ohne Regelattribute — verifiziert |
| DB, Belegfotos, eigene Sicherungen nicht Teil von Auto-Backup/Gerätetransfer | Plattformsemantik von `allowBackup="false"` deckt `files/` und `shared_prefs/` vollständig (A3) | Cache-Verzeichnis ist ohnehin nie im Backup; kein `no_backup/`-Pfad nötig |
| SecureStorage-Verhalten definiert und getestet **oder** dokumentiert | Dokumentiert: Scope-Klärung + ADR-Inhaltsliste + Datenschutz-Hilfe (A4) | „Oder"-Alternative zulässig; Gerätetest-Infrastruktur fehlt nachweisbar (kein Android-Testprojekt, `IncludeAndroidTarget: 'false'` in allen vier Workflows — verifiziert) |
| Manifest-Test verhindert Regressionen | Schritt 2 mit Positiv-, Negativ- und Struktur-Assertions (A2) | Konvention `MauiSourceFiles`/`XDocument`/`BaseTest`/`CS1591` verifiziert |

## 3. Manifest-Test — Korrektheit

- Attribut-Namespace korrekt bedacht: `{http://schemas.android.com/apk/res/android}allowBackup`.
  Ohne Namespace-Präfix fände `XDocument` das Attribut nicht — die im Plan geforderte Beachtung ist
  genau die Stelle, an der ein naiver Test scheitern würde.
- Negativliste (`dataExtractionRules`, `fullBackupContent`, `backupAgent`, `restoreAnyVersion`)
  verhindert widersprüchliche Teilkonfigurationen — sinnvoll. `hasFragileUserData` fehlt bewusst
  zu Recht (kein Backup-Mechanismus, sondern Deinstallations-Rückfrage).
- Fall „Attribut entfernt" (Plattform-Default `true` ab API 23) ist durch die Wert-Assertion
  implizit abgedeckt; der Plan nennt das selbst. Mindest-SDK 21 (`csproj:57`) → Auto-Backup
  wäre auf allen unterstützten Geräten wirksam — verifiziert.
- Infrastruktur stimmt: `MauiSourceFiles.PathOf` (`src/Tankradar.Tests.Unit/Unit/MauiSourceFiles.cs:29`)
  sucht `Tankradar.sln` (existiert am Root); `IosProjectFilesTests.cs` liefert das
  `XDocument`-Muster inkl. Negativlisten; `BaseTest` liegt unter `src/Tankradar.Tests.Unit/BaseTest.cs`
  (Namespace `Tankradar.Tests.Unit`, Datei im `Unit/`-Ordner der Testklasse ist konsistent);
  Testprojekt `net11.0-windows10.0.19041.0` mit `WarningsAsErrors CS1591` — verifiziert.
- Namenskonvention `<Thema>Tests_<Aspekt>.cs` → `AndroidManifestTests_Backup.cs` passt zu
  `PlatformManifestTests_Location.cs`/`_Photos.cs`.

## 4. ADR 0022 — Korrektheit

- Nummer 0022 ist die nächste freie (0001–0021 belegt, verifiziert).
- Inhaltsliste vollständig: Kontext (Befund #62/#66, Datenverzeichniskarte, ADR 0019, fehlender
  Android-Dateischutz — `DatabaseFileProtector` ist `#if IOS`-only, verifiziert), Entscheidung,
  Abgrenzung zu ADR 0011 (iOS lässt eigene Sicherungen bewusst im Gerätebackup — plattformspezifisch
  unterschiedliche Antworten, kein Widerspruch), verworfene Alternative, SecureStorage-Festlegung,
  Folgen inkl. Revisionsvorbehalt.
- Format-Angabe „wie ADR 0019" mit Abschnitt `## Alternativen` ist kleinteilig ungenau: ADR 0019 hat
  `## Abweichungen vom Designentwurf` statt `## Alternativen`; letzteres existiert in ADR 0021.
  Beides sind etablierte Überschriften — bei der Umsetzung einfach die passenden Abschnitte wählen.

## 5. Hilfe- und Dokumentationsänderungen — Korrektheit

- `docs/help/Sicherung/beschreibung.md` — Abschnitt „Datenschutz und Sicherheit" beginnt exakt an
  Z. 52; der iPhone-Gerätebackup-Satz steht am Ende von Z. 54 — Anker stimmt.
- `docs/help/Datenschutz/beschreibung.md` — „Schlüssel in der Keychain" liegt an Z. 31–37 — Anker
  stimmt; der dortige Drei-Punkte-Katalog (nicht in Sicherung, Zugriffsklasse, „gerätelokal"-Klarstellung)
  ist der richtige Ort für den Android-Absatz.
- `changes.log` — Muster `## YYYY-MM-DD — Titel (Issue #N)` mit Detailaufzählung verifiziert
  (Einträge enden üblicherweise mit „Keine Schemaänderung." — hier ebenfalls zutreffend).
- `docs/RELEASE_NOTES.md` — Entfall begründet stimmig: Android wird nicht gebaut/ausgeliefert,
  keine anwendersichtbare Änderung auf einer released Plattform.
- `docs/help/index.md` — korrekt, keine neue Seite, kein Indexeintrag nötig.

**Optionale Ergänzungen (nicht erforderlich):**
- `docs/help/Datenschutz/ablauf-technisch.md:12` — die Komponententabelle nennt
  `SecureStorageApiKeyStore`/`CredentialLockerApiKeyStore` nur für iOS/Windows; ein Android-Eintrag
  (Keystore-verschlüsselte SharedPreferences, kein Backup) würde die Tabelle vervollständigen.
  Vertretbar wegzulassen, da die Datei iOS-Auslieferung/Privacy fokussiert.
- `RouteSearchAccess.cs:9–12` — der Schnittstellenkommentar nennt „Keychain bzw. Credential Locker"
  ohne Android; die Aussagen bleiben zutreffend, eine Namensnennung der Android-Ablage wäre
  Genauigkeitsbonus.

## 6. Grenzen der Android-Abnahme — korrekt gezogen

- Kein Geräte-/Emulator-Test: begründet über fehlende Infrastruktur und die „oder dokumentiert"-
  Alternative im Issue. Die dokumentierte Festlegung ist fachlich korrekt: Build-Schlüssel wird von
  `ApiKeyProvider.GetApiKeyAsync` automatisch neu geschrieben (`ApiKeys.cs:117–128`, verifiziert),
  Routing-Schlüssel/Schalter sind anwenderseitig neu einzugeben, Aufrufer tolerieren unlesbare
  Einträge (`ApiKeys.cs:101–114`, `RouteSearchAccess.cs:132–145, 227–237` — alle verifiziert).
- Kein `dotnet build -f net11.0-android`: begründet stimmig — Workload-Vorhandensein lokal nicht
  voraussetzbar (`IncludeAndroidTarget` default `true`, aber nur mit installiertem Android-Workload),
  CI baut nicht, und die Änderung ist eine Attributwert-Änderung ohne Build-Risiko; der gültige Wert
  wird durch den Unit-Test verifiziert.
- Rot-Prüfung des Tests (temporär gegen `"true"`) ist als lokale Gedankenstütze beschrieben —
  angemessen.

## 7. Restliche Kleinigkeiten (bei Umsetzung beachten)

- `AndroidManifest.xml` beginnt mit UTF-8-BOM und nutzt Tabs — beim Edit beibehalten.
- Der geplante XML-Kommentar im `<application>`-Block ist zulässig (aapt2 ignoriert Kommentare).
- Beim `changes.log`-Eintrag das Repo-Muster vollständig übernehmen (inkl. abschließendem
  „Keine Schemaänderung.").

## Fazit

Alle vier Akzeptanzkriterien sind durch konkrete, gegen den Repo-Stand verifizierte Schritte belegt;
Dateiorte, Zeilenangaben, Test- und Dokumentationskonventionen stimmen; die offenen Fragen aus der
Anforderung sind sämtlich entschieden (Option 1, sensible-Dateien-Frage entfällt mit Option 1,
SecureStorage = dokumentiert, Berechtigungen = begründet ausgeschlossen). Keine fehlenden Testfälle,
Dateiorte oder Risiken. Die notierten Punkte sind Formulierungs- und Vollständigkeitsverbesserungen
für die Umsetzung, keine Planlücken.
