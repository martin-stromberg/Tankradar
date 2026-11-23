# Umsetzungsplan – Issue #66: Android-Backup-Regeln für App-Daten und SecureStorage

## Entscheidung: `android:allowBackup="false"` (Option 1)

**Begründung:**

- **Vollständige Abdeckung mit minimaler Änderung.** Alle zu schützenden Daten liegen unter `FileSystem.AppDataDirectory` (Android: `files/`) und `shared_prefs/` — beide komplett im Auto-Backup-/Transfer-Umfang ([inventory/datenablage.md](inventory/datenablage.md)). `allowBackup="false"` schließt beide Bereiche in einem Attribut aus: `tankatlas.db` samt `-wal`/`-shm`, `receipts/`, `backups/` (inkl. unverschlüsselter eigener Sicherungen, ADR 0019), `restore-staging/`, `restore-rollback/`, `restore.journal`, `test-shared/` sowie die `Preferences`- und `SecureStorage`-Dateien. Eine Ein-Zeilen-Änderung statt zwei zu pflegender Exclude-Listen.
- **Fachlich deckungsgleich mit der Architektur.** Die eigene, anwendergesteuerte Sicherung ist bewusst die alleinige Backup-Lösung (ADR 0011/0019); sämtliche Einstellungen stecken ohnehin in `tankatlas.db` und damit in der eigenen Sicherung. Ein System-Restore würde fachlich nur `tankbook.selectedVehicleId` (unkritisch) zusätzlich retten — kein reeller Verlust.
- **Löst das SecureStorage-Problem ersatzlos.** Wiederhergestellte `SecureStorage`-Einträge wären ohne den gerätegebundenen Keystore-Schlüssel unlesbar; mit `allowBackup="false"` wandert die Datei gar nicht erst mit — kein korrupt-voller Zustand, sondern sauberer Neubeginn (Verhalten siehe Schritt 3).
- **Vermeidet den riskanteren Restzustand der Regel-Option.** Bei `dataExtractionRules`/`fullBackupContent` müssten die Exclude-Listen manuell mit den Konstanten in `BackupFormat`, `ReceiptPhotoStore` und den `shared_prefs`-Dateinamen synchron gehalten werden; ein neuer Ablageort würde still mitgesichert. Zusätzlich würde weiterhin die unlesbare SecureStorage-Datei transferiert — genau der Befund, den das Issue beheben will.
- **Passt zu „Android nicht im Fokus“** (`README.md:147`, kein Android-Build in CI): keine neue `Resources/xml/`-Struktur, kein Dual-Format (`<data-extraction-rules>` + `<full-backup-content>`).

Die Regel-Option wird damit ausdrücklich verworfen; sollte Android später produktiv unterstützt werden und ein System-Restore gewünscht sein, ist die Entscheidung im neuen ADR zu revidieren.

## Scope-Klärung

- **Im Scope:** Manifest-Entscheidung (`allowBackup="false"`), Manifest-Regressionstest, Dokumentation der Entscheidung (ADR), Dokumentation des SecureStorage-Verhaltens unter Android, `changes.log`-Eintrag.
- **Ausgeschlossen (Status: bestätigt):** Die minimale Deklaration der tatsächlich benötigten Standort- und Kamera-Berechtigungen im Android-Manifest bleibt Aufgabe der späteren produktiven Android-Unterstützung (vom Issue ausdrücklich als „später“ empfohlen; README „nicht im Fokus“). Der neue Manifest-Test prüft die Berechtigungsliste daher nicht auf Vollständigkeit, sondern nur die Backup-Entscheidung.
- **Ausgeschlossen (Status: bestätigt):** Echte Android-Restore-/Gerätetests. Es gibt kein Android-Testprojekt, keinen Emulator-/Device-Job und CI baut mit `IncludeAndroidTarget: 'false'`; das Akzeptanzkriterium „getestet **oder** dokumentiert“ wird über „dokumentiert“ erfüllt (ADR + Datenschutz-Hilfe). Zusätzlich bleibt der Android-Build selbst ungeprüft — das ist im Repo-Status quo angelegt und wird nicht neu eingeführt.
- **SecureStorage-Verhalten:** Es wird **definiert und dokumentiert** (nicht gerätegetestet): Unter Android wandert die Keystore-verschlüsselte SecureStorage-Datei wegen `allowBackup="false"` nicht in Auto-Backup/Gerätetransfer; auf einem neuen/ neu installierten Gerät startet die App ohne gespeicherte Schlüssel. Konsequenzen: Der anwenderseitige OpenRouteService-Schlüssel und der Schalter `routing-enabled` sind neu einzugeben; der Tankerkönig-Preisschlüssel wird ohnehin beim Build mitgegeben und von `ApiKeyProvider.GetApiKeyAsync` (`ApiKeys.cs:117–128`) automatisch wieder geschrieben. Die vorhandene Fehlertoleranz (`ApiKeys.cs:101–114`, `RouteSearchAccess.cs:132–145, 227–237`) fängt dennoch jede Form unlesbarer Einträge ab — relevant, falls ein Anwender die App-Daten über herstellerseitige Werkzeuge (außerhalb von `allowBackup`) überträgt.

## Umsetzungsschritte

### Schritt 1: Manifest ändern

`src/Tankradar.MAUI/Platforms/Android/AndroidManifest.xml`, Zeile 3:

```xml
<!-- vorher -->
<application android:allowBackup="true" android:icon="@mipmap/appicon" android:roundIcon="@mipmap/appicon_round" android:supportsRtl="true"></application>
<!-- nachher -->
<application android:allowBackup="false" android:icon="@mipmap/appicon" android:roundIcon="@mipmap/appicon_round" android:supportsRtl="true"></application>
```

Zusätzlich einen kurzen XML-Kommentar im `<application>`-Block einfügen, der die Entscheidung am Ort der Wirkung dokumentiert, z. B.:

```xml
<!-- Auto-Backup/Gerätetransfer abgeschaltet: Die eigene Sicherung (Optionen → Sicherung) ist die
     alleinige Backup-Lösung; System-Backups würden unverschlüsselte Sicherungen und die ohne den
     Geräte-Keystore unlesbare SecureStorage-Datei mitnehmen. Entscheidung: docs/adr/0022-*.md -->
```

Es werden **keine** Attribute `android:dataExtractionRules`, `android:fullBackupContent`, `android:backupAgent` oder `android:restoreAnyVersion` ergänzt und **kein** `Platforms/Android/Resources/xml/`-Ordner angelegt.

### Schritt 2: Manifest-Regressionstest (Unit-Test)

Neue Datei `src/Tankradar.Tests.Unit/Unit/AndroidManifestTests_Backup.cs` (Namenskonvention `<Thema>Tests_<Aspekt>.cs`), Muster nach `IosProjectFilesTests.cs` (strukturierte `XDocument`-Auswertung) und `MauiSourceFiles` (`MauiSourceFiles.PathOf("Platforms","Android","AndroidManifest.xml")`). Klasse erbt `BaseTest`, deutsche XML-Dokumentationskommentare auf Klasse und allen Mitgliedern (`CS1591` ist als Fehler konfiguriert).

Mindest-Assertions:

1. `application`-Element hat `android:allowBackup` mit Wert `"false"` (Namespace `http://schemas.android.com/apk/res/android` beachten — Attribut lautet `{http://schemas.android.com/apk/res/android}allowBackup`).
2. Negativliste: Das `application`-Element trägt **keine** der Attribute `android:dataExtractionRules`, `android:fullBackupContent`, `android:backupAgent`, `android:restoreAnyVersion` — verhindert widersprüchliche Teilkonfigurationen („Backup aus, aber Regeln deklariert“ bzw. „Regeln ohne allowBackup“).
3. Existenz/Lesbarkeit: Das Manifest lässt sich per `XDocument.Load` parsen und enthält genau ein `<application>`-Element (implizit über `Assert.NotNull`/`Single`).

Optional (empfohlen, geringer Zusatzaufwand): Assertion, dass `allowBackup` überhaupt deklariert ist — ein Entfernen des Attributs würde den Plattform-Default (`true` ab SDK-Ziel ≥ 23) reaktivieren, das muss der Test als Regression erkennen (ist durch Assertion 1 ohnehin abgedeckt).

Der Test läuft als Windows-Unit-Test (`net11.0-windows`) ohne Android-Workload — kompatibel zur CI (`IncludeAndroidTarget: 'false'`).

### Schritt 3: ADR anlegen

Neue Datei `docs/adr/0022-android-backup.md` (nächste freie Nummer; Format wie ADR 0019: `## Status` „Angenommen (<Datum>)“, `## Kontext`, `## Entscheidungen`, `## Alternativen`, `## Folgen`).

Inhalte:

- **Kontext:** Befund aus #62/#66 — `allowBackup="true"` ohne Regeln; Datenverzeichniskarte (Verweis auf `BackupFormat`-/`ReceiptPhotoStore`-Konstanten); unverschlüsselte eigene Sicherungen möglich (ADR 0019); kein Android-Dateischutz (`DatabaseFileProtector` nur `#if IOS`); Android nicht im Fokus, nicht in CI gebaut.
- **Entscheidung:** `allowBackup="false"`; eigene Sicherung ist unter Android die alleinige Backup-Lösung. Abgrenzung zur iOS-Entscheidung in ADR 0011 (dort bleiben eigene Sicherungen bewusst im Gerätebackup — auf iOS ist die Plattform das Hauptziel und der Ausschlussmechanismus anders motiviert; die ADRs widersprechen sich nicht, sondern treffen plattformspezifische Entscheidungen).
- **Verworfene Alternative:** `dataExtractionRules` + `fullBackupContent` mit Exclude-Listen — höherer Pflegeaufwand (Konstanten-Duplikation), löst das SecureStorage-Problem nicht (Datei wandert weiter unlesbar mit), einziger Mehrwert wäre `tankbook.selectedVehicleId`.
- **SecureStorage-Verhalten unter Android (dokumentierte Festlegung):** Keystore-verschlüsselte SharedPreferences-Datei; wandert wegen `allowBackup="false"` nicht mit. Nach Neuinstallation/Gerätewechsel: `tankatlas.routing-api-key` und `tankatlas.routing-enabled` neu eingeben; `tankatlas.price-api-key` wird durch den Build-Schlüssel automatisch neu geschrieben (`ApiKeyProvider`, `ApiKeys.cs:117–128`). Unlesbare Einträge aus Fremd-Transfers werden von den Aufrufern toleriert (`ApiKeys.cs:101–114`, `RouteSearchAccess.cs:132–145, 227–237`; vgl. Schnittstellenkommentar `RouteSearchAccess.cs:10–12`).
- **Folgen:** Kein automatischer Datenabfluss in Google-Backup/D2D-Transfer; Verlust der Daten beim Gerätewechsel ohne eigene Sicherung (muss die Hilfe klar nennen); Regressionsschutz über den neuen Manifest-Test; Revision des ADR nötig, falls Android produktiv unterstützt und System-Backup gewünscht wird.

### Schritt 4: Hilfe-Dokumentation ergänzen

- `docs/help/Sicherung/beschreibung.md` — Abschnitt „Datenschutz und Sicherheit“ (Z. ~52–54): Satz ergänzen, dass unter **Android** die App-Daten **nicht** Teil des Geräte-Backups sind (`allowBackup` deaktiviert); die eigene Sicherung ist dort der einzige Weg, Daten auf ein neues Gerät zu übertragen — anders als auf dem iPhone.
- `docs/help/Datenschutz/beschreibung.md` — Abschnitt „Schlüssel in der Keychain“ (Z. ~31–37): Android-Absatz ergänzen: Schlüssel liegen dort in einer mit dem Geräte-Keystore verschlüsselten Einstellungsdatei; sie sind nicht in der App-Sicherung und wandern wegen deaktiviertem Auto-Backup auch nicht über ein System-Backup auf ein neues Gerät — auf einem neuen Gerät sind Routing-Schlüssel und Schalter neu einzugeben.
- `changes.log` — Eintrag im Muster `## YYYY-MM-DD — Titel (Issue #66)` mit Detailaufzählung (Manifest-Änderung, Test, ADR, Hilfe).
- `docs/RELEASE_NOTES.md` — **entfällt**: keine anwendersichtbare Änderung auf einer ausgelieferten Plattform (Android wird nicht gebaut/ausgeliefert); wird Android später produktiv, gehört der Hinweis in die dortigen Release Notes.
- Kein Eintrag in `docs/help/index.md` nötig (keine neue Hilfeseite, nur Ergänzungen).

### Schritt 5: Prüfung

- `dotnet test src/Tankradar.Tests.Unit --filter "FullyQualifiedName~AndroidManifestTests"` lokal ausführen (grün).
- Optional Gesamtlauf `dotnet test src/Tankradar.Tests.Unit` zur Absicherung, dass keine bestehenden Tests betroffen sind (Manifest-Textänderung kann keine anderen Tests berühren, da bisher kein Test das Android-Manifest liest).
- Manuelle Gegenprobe: Test temporär gegen `"true"` laufen lassen (Rot-Prüfung lokal gedacht, nicht committen) bzw. Code-Review der Assertion.
- **Nicht durchführbar / nicht nötig:** `dotnet build -f net11.0-android` (kein Android-Workload-Vorhandensein voraussetzbar; CI baut Android nicht — die XML-Änderung ist attributwert-beschränkt und birgt kein Build-Risiko; der gültige Attributwert `false` ist durch den Unit-Test verifiziert).

## Akzeptanzszenarien

| # | Szenario | Prüfung |
|---|----------|---------|
| A1 | Die Backup-Entscheidung ist im Manifest umgesetzt | `AndroidManifest.xml` enthält `android:allowBackup="false"` am `<application>`-Element; Git-Diff zeigt genau diese Attributänderung plus Doku-Kommentar; kein `Resources/xml/`-Ordner entstanden |
| A2 | Manifest-Regressionstest verhindert Rücknahme | `dotnet test src/Tankradar.Tests.Unit --filter "FullyQualifiedName~AndroidManifestTests"` ist grün; der Test schlägt (a) bei `allowBackup="true"`, (b) bei fehlendem Attribut, (c) bei zusätzlich deklariertem `dataExtractionRules`/`fullBackupContent`/`backupAgent`/`restoreAnyVersion` |
| A3 | Datenbank, Belegfotos, eigene Sicherungen sind nicht Teil von Auto-Backup/Gerätetransfer | Durch `allowBackup="false"` per Plattformsemantik abgedeckt (gilt für `files/` und `shared_prefs/` vollständig); statisch prüfbar über A1/A2 — gerätetestfrei, dokumentiert im ADR |
| A4 | SecureStorage-Verhalten nach Wiederherstellung ist definiert und dokumentiert | ADR 0022 legt fest: keine Migration der SecureStorage-Datei, Schlüssel sind auf neuem Gerät neu einzugeben (Ausnahme: Build-Schlüssel schreibt sich selbst), Fehlertoleranz der Aufrufer dokumentiert; `docs/help/Datenschutz/beschreibung.md` enthält den Android-Absatz |
| A5 | Entscheidung ist dokumentiert | `docs/adr/0022-android-backup.md` existiert mit Kontext/Alternativen/Folgen; `changes.log` enthält den Issue-#66-Eintrag; Hilfeabschnitte „Sicherung“ und „Datenschutz“ nennen das Android-Verhalten |

## Offene Punkte

Keine.
