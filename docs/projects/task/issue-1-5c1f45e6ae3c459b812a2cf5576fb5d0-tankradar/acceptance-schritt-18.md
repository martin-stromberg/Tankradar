# Abnahme Schritt 18 – iOS-Build, Signierung und TestFlight-Auslieferung

- **Datum:** 2026-10-25
- **Branch:** `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-18-ios-auslieferung` (HEAD `14372c9`)
- **Geprüfter Umfang:** `git diff b0a8646..HEAD` (59 Dateien, 13 Commits)
- **Maßstab:** Abschnitt „Schritt 18“ im `project-plan.md` einschließlich „Zusatz aus der Abnahme von Schritt 17a“ und „Zusatz aus der Abnahme von Schritt 17“, `requirement.md`, Entscheidungstabelle, die drei Zusatzaufträge (0xC0000005, Sortiertests, wackeliger Sicherungstest)

## Gesamturteil

**Abgenommen mit Hinweisen**

Alle Pflichtteile sind umgesetzt, Build, Formatprüfung, Unit-, Integrations- und die gezielten E2E-Tests sind grün. Die drei Zusatzaufträge sind sauber bearbeitet: Der Absturz 0xC0000005 ist richtig der App beim Beenden zugeordnet und begründet dokumentiert. Die Sortierung ist durch ViewModel-Tests abgesichert. Für den wackeligen Sicherungstest ist die Ursache belegt und vollständig behoben.

Es gibt keine kritischen oder hohen Befunde. Zwei mittlere Befunde sollten vor der nächsten Auslieferung korrigiert werden:

- Der Warnpfad im neuen CI-Schritt *Validate iOS package* bricht ohne Meldung ab (M1).
- Die Datenschutzangaben verschweigen, dass Tankstellen-Kennungen an Tankerkönig gehen (M2).

Beide sind klein und klar abgegrenzt.

## Anforderungstabelle

| # | Anforderung | Ergebnis | Fundstelle / Nachweis |
|---|-------------|----------|------------------------|
| 1 | Skript-Aktionen Build, Simulator, Gerät, Store-Upload mit automatischer Buildnummer und Upload vorhandener Pakete, nutzbar von Windows über einen gekoppelten Mac | erfüllt (seit Schritt 3, hier geprüft) | `scripts/iOS-Deployment.ps1` (`ValidateSet` build/simulator/device/store/upload/list/menu, `Update-BuildNumber`, `Invoke-Upload` mit `-IpaPath`, Pair-to-Mac/SSH). Ohne Mac nur über `test-ios-deployment.ps1` prüfbar |
| 2 | Mindestversion iOS 16 | erfüllt | csproj Z. 55 `SupportedOSPlatformVersion` 16.0 (unverändert); IPA-Prüfung `iOS-Deployment.ps1` Z. 794–799; CI-Schritt `action.yml` Z. 286–289; Skripttest. Die vorhandene IPA vom 04.10. enthält `MinimumOSVersion = 16.0` |
| 3 | Bundle-ID `de.martinstromberg.tankradar`; Team-ID, Signierungs- und App-Store-Connect-Daten nur außerhalb des Repos | erfüllt | Die vorhandene IPA zeigt `CFBundleIdentifier = de.martinstromberg.tankradar`. Die Action liest die Werte nur aus Inputs (Secrets). Keine Geheimnisse im Diff |
| 4 | Fehlende Werte werden erkannt und verständlich gemeldet, ohne andere Aktionen zu blockieren | erfüllt (mit Hinweis N2) | `Get-MissingDeployValues`/`Write-MissingDeployValues` (Z. 391–431): je Aktion gesammelt, mit Parameter, Umgebungsvariable und Herkunft. Getestet für store/upload/device/build/simulator/list/menu. Ausgegeben werden nur Namen, keine Werte. `upload` fragt aber noch interaktiv nach der Signierung (N2) |
| 5 | Nur benötigte Rechte: Standort (sinngemäß „Tankstellen in der Nähe“), Kamera/Fotos nur für Belegfotos | erfüllt | `Info.plist` Z. 31–36: Der Standorttext nennt jetzt auch den Routenstart. `IosProjectFilesTests` erlaubt genau drei `NS…UsageDescription`-Schlüssel. Zu `NSPhotoLibraryUsageDescription` siehe Hinweis H3 |
| 6 | Datenbank und Belegfotos über Data Protection geschützt | erfüllt | `DatabaseFileProtector` (`CompleteUntilFirstUserAuthentication`, unverändert). Neu: Exportdateien (`TankbookExportService.cs` Z. 95, Test `Create_ProtectsFinishedFile`). Sicherungen waren bereits geschützt (`BackupService.cs` Z. 103/302) |
| 7 | CI erzeugt bei hinterlegten Signierungsdaten bei Releases einen signierten iOS-Build | erfüllt (Befund M1 im neuen Prüfschritt) | `release.yml` Z. 134 und `staging-ci.yml` Z. 282 mit `signing-enabled: vars.IOS_SIGNING_ENABLED`. Neuer Schritt *Validate iOS package* vor dem Upload (`action.yml` Z. 269–298) |
| 8 | Datenschutzangaben für die App-Store-Prüfung: Standort, Weitergabe von Adress-/Routeneingaben an Geokodierung und Routing, keine Weitergabe von Fahrzeug-/Tankbuchdaten | teilweise erfüllt | `docs/help/Datenschutz/` (index, beschreibung, ablauf-technisch) und ADR 0015 sind vollständig gegliedert. Der Datenfluss zu Tankerkönig ist aber unvollständig beschrieben (M2) |
| 9 | Hilfsfunktionen des Skripts ohne Mac automatisiert getestet | erfüllt | `scripts/test-ios-deployment.ps1`: 89 Prüfungen grün, Teil von `local-ci.ps1` und des CI-Schritts *iOS deployment script checks* |
| 10 | Nutzung des Skripts dokumentiert | erfüllt | `docs/help/ci-cd/ios-deployment.md`: neuer Abschnitt „Mindestversion und fehlende Werte“ mit Tabelle je Aktion, erweiterte Prüfliste |
| 17a-1 | Windows-ICO mit mehreren Größen (16–256 px) | erfüllt | `MSBuild/WindowsAppIcon.targets`, Import nur für das Windows-Zielframework. Das gebaute `appicon.ico` enthält 16, 24, 32, 48 und 256 px (PNG, 32 bit), selbst ausgelesen |
| 17a-2 | 1024-px-App-Store-Icon auf Größe und Deckung prüfen | teilweise erfüllt | Größe: `assetutil`-Prüfung im Skript und in der CI (Befund M1 im Warnpfad). Deckung: nur auf SVG-Ebene (`AppIconResourceTests_Xml.MarketingIcon_SourceIsSquareAndFullyCovered`) und als Encoding-Warnung im Skript, nicht in der CI. Ohne Mac nicht vollständig prüfbar |
| 17a-3 | Ersten TestFlight-Upload mit neuem Icon auf Annahme prüfen | offen | Seit 17a gab es keinen TestFlight-Upload: Im Pre-Release-Lauf 40178004723 wurden *ios prerelease* und *prerelease* übersprungen, weil der wackelige Test scheiterte. Erst nach dem Merge dieses Schritts möglich (Hinweis H1) |
| 17a-4 | Icon-Prüfung in `create-review-version.ps1` gegen veraltete Dateien | erfüllt | Der Publish-Ordner wird vor dem Build geleert. Danach werden Zeitstempel gegen den Buildstart und die SVG-Quellen geprüft, außerdem die ICO-Größen 16/32/48/256 |
| 17a-5 | Unit-Tests der Icon-Ressourcen per XML-Auswertung | erfüllt | `AppIconResourceTests_Xml` (`XDocument`). `AppIconResourceTests_Motif` behält nur die Motivprüfung |
| 17a-6 | „Kacheln/Startmenü“ in `changes.log` korrigieren | erfüllt | Eintrag 2026-10-20 korrigiert (unverpackt, kein Startmenü- oder Kacheleintrag) |
| 17-1 | Abfragetakt mit kleiner Reserve bzw. Reservierung ab dem Absenden | teilweise erfüllt | 250 ms Reserve (`RouteSearchOptions.cs` Z. 34, `EffectiveQueryInterval`, Tests). Eine Verzögerung durch `RequestThrottle` (bis 1 s) deckt das nicht; die Doku behauptet das Gegenteil (N1) |
| 17-2 | Grenzmeldung abrunden; bei 1 km keinen kleineren Abstand vorschlagen | erfüllt | `SearchTexts.FormatRouteLimit` (`Math.Floor`, Vorschlag nur bei `DistanceKm > 1`), `SearchTextsTests_Route` |
| 17-3 | „gerätelokal“ für die Keychain präzisieren, auch in den Datenschutzangaben | erfüllt | Kommentare (`MauiProgram`, `SecureStorageApiKeyStore`, `CredentialLockerApiKeyStore`, `RouteSearchAccess`), README, ADR 0014/0015, Hilfe, Datenschutztext. Im Code und in der Hilfe kommt die Altformulierung nicht mehr vor |
| 17-4 | E2E-Test „Suche abbrechen“ | erfüllt | `RouteE2ETests_Cancel` (nur UIA-Invoke, kein Fokus, keine Maus). Selbst ausgeführt und grün |
| 17-5 | Abfragegrenze nach Tankerkönig-Antwort anpassen (ADR 0014) | nicht anwendbar | Es liegt keine Antwort vor. Das ist in ADR 0014 Z. 54 und ADR 0015 vermerkt, die Grenze bleibt unverändert |
| Z1 | Befund 0xC0000005 untersuchen und beheben oder begründet dokumentieren | erfüllt | Selbst im Log von Lauf 40175344480 geprüft: Der Stack (`Marshal.Release` ← `IObjectReference.DisposeTrackerSource/Finalize` ← `GC.RunFinalizers`) erscheint während `BackupE2ETests_TamperedSchema.MigrationLockRow…`. Der Testlauf läuft danach weiter und endet mit 147/147 grün; der Absturz liegt also im Kindprozess der App, nicht im `testhost`. Maßnahmen (Teardown wartet, Handler-Abmeldung) und Begründung stehen in ADR 0015 Teil B. Die Wirksamkeit ist nur über CI-Läufe belegbar (H2) |
| Z2 | Sortierung (Name/Preis/Entfernung, Seitenreset) per ViewModel-Tests statt E2E | erfüllt | `MapViewModelTests_Sort` (4 Tests): paarweise verschiedene Reihenfolgen, Wechsel ohne neue Abfrage, Seitenreset nach „Weitere anzeigen“ (25 Einträge, Zähler „35 übrig“), fehlende Werte am Ende, Routenergebnis. Der E2E-Test bleibt unverändert |
| Z3 | Wackeliger Test `Restore_WithPooledConnectionHoldingWal_Succeeds`: Ursache finden, beheben, Prüfabsicht erhalten | erfüllt | Ursache belegt (siehe „Prüfung der Collection-Lösung“). Alle 13 `BackupServiceTests_*`-Klassen liegen in der Sammlung `BackupCollection`, der Testkörper ist unverändert. 20 Wiederholungen grün |
| Rand | `MtouchInterpreter=all` und `TrimmerRootAssembly` unverändert; `WindowsAppIcon.targets` bricht iOS-Build und CI nicht | erfüllt | csproj Z. 70 und 75–79 unverändert, der Diff der csproj ergänzt nur den Import. Release-Build mit iOS-Ziel (`net10.0-ios`, iossimulator-x64) und Windows-Ziel ohne Warnungen. Das Ziel ist nur für Windows importiert |

## Befunde

### M1 (mittel) – CI-Schritt *Validate iOS package*: Warnpfad der Icon-Prüfung nicht erreichbar, Abbruch ohne Meldung

- **Fundstelle:** `.github/actions/package-ios/action.yml` Z. 291–292 (unter `set -euo pipefail` aus Z. 273)
- **Problem:** `ICON_W=$(echo "$ICON_BLOCK" | grep -i '"PixelWidth"' | head -1 | tr -dc '0-9')` liefert unter `pipefail` den Exit-Code 1 von `grep`, wenn der Block leer ist. Das passiert, wenn `assetutil` fehlschlägt oder die Ausgabe anders aussieht. Die Zuweisung löst dann `set -e` aus, und der Schritt endet mit Exit 1 ohne `::error::` und ohne die vorgesehene Warnung. Damit blockiert er den TestFlight-Upload, obwohl ADR 0015 sagt: „ist sie nicht lesbar, gibt es nur eine Warnung“. Im Skript (`Invoke-IpaValidation`, nur `set -e`) tritt das nicht auf, die beiden Varianten verhalten sich also unterschiedlich.
- **Nachweis:**
  - Simulation des aus der YAML extrahierten Schritts mit Attrappen für `ditto`, `plutil` und `xcrun`: Im Normalfall Exit 0 („IPA-Prüfung bestanden“), bei nicht lesbarem Icon Exit 1 ohne jede Ausgabe.
  - Der Normalpfad funktioniert: Die vorhandene IPA vom 04.10. enthält im `Assets.car` die Rendition `appiconItunesArtwork.png`. Der Fehler wirkt also nur, wenn `assetutil` versagt oder sein Format ändert.
- **Korrektur:** In beiden Zeilen `|| true` ergänzen, z. B. `ICON_W=$(echo "$ICON_BLOCK" | grep -i '"PixelWidth"' | head -1 | tr -dc '0-9' || true)`, ebenso für `ICON_H`. Im Skripttest prüfen, dass beide Zuweisungen im CI-Schritt `|| true` tragen. Besser noch: den Schritt mit Attrappen ausführen wie oben.

### M2 (mittel) – Datenschutzangaben: Übermittlung von Tankstellen-Kennungen an Tankerkönig fehlt, „Favoriten“ fälschlich als nicht übertragen genannt

- **Fundstellen:**
  - `docs/help/Datenschutz/beschreibung.md` Z. 13: Die Tankerkönig-Zeile nennt nur Koordinaten und Radius.
  - `docs/help/Datenschutz/beschreibung.md` Z. 18: „**Nicht** übertragen werden: … Favoriten …“
  - `docs/help/Datenschutz/index.md` Z. 3: „nur, wenn für eine Suche …“
- **Problem:** Die App überträgt nicht nur bei Suchen Daten an Tankerkönig:
  - Die Startseite fragt die Preise der Favoriten über deren Kennungen ab: `FuelPriceService.RefreshStationsAsync` → `TankerkoenigClient.cs` Z. 165, `prices.php?ids=…`.
  - Die Detailansicht ruft `detail.php?id=…` ab (Z. 130).
  - `docs/help/Preisdaten/beschreibung.md` beschreibt das bereits korrekt.

  Gruppennamen, Notizen und Prioritäten verlassen das Gerät nicht, die Kennungen der favorisierten Tankstellen aber schon, zusammen mit der IP-Adresse. Der Text dient laut Dokument als Grundlage der Datenschutzerklärung und muss den tatsächlichen Datenfluss vollständig wiedergeben.
- **Korrektur:**
  - Die Tankerkönig-Zeile um „Kennungen der Tankstellen (Preise der Favoriten auf der Startseite, Detailansicht)“ ergänzen und den Zweck „Preise der Favoriten, Tankstellendetails“ aufnehmen.
  - In der Liste „Nicht übertragen“ „Favoriten“ ersetzen durch „Favoritengruppen, Namen, Notizen und Prioritäten (nur die Kennungen der Tankstellen gehen zur Preisabfrage an Tankerkönig)“.
  - In `index.md` „wenn für eine Suche“ durch „wenn für eine Suche oder die Preise der Favoriten“ ersetzen.
  - Die App-Store-Vorlage bleibt unverändert (die Daten werden nicht vom Entwickler gesammelt).

### N1 (niedrig) – Abfragereserve deckt die Verzögerung durch `RequestThrottle` nicht; die Doku verspricht es

- **Fundstellen:**
  - `src/Tankradar.MAUI/Services/Routing/RouteSearchOptions.cs` Z. 34 (250 ms)
  - `src/Tankradar.MAUI/Services/Pricing/Throttling.cs` (`RequestThrottle.WaitAsync`, `MinRequestInterval` 1 s)
  - `docs/adr/0014-route-search.md` Z. 53 („… deckt die Verzögerung … plus die allgemeine Drosselung von 1 s …; der Abstand … fällt so nicht unter 10 s“)
  - `docs/adr/0015-ios-delivery-and-privacy.md` Z. 40
- **Problem:** Der Pacer reserviert ab der Reservierung. Liegt kurz vor Abfrage 1 eine andere Preisanfrage, etwa die Preisaktualisierung der Startseite bei einem Reiterwechsel, verzögert `RequestThrottle` Abfrage 1 um bis zu 1 s, Abfrage 2 aber nicht. Der tatsächliche Abstand kann dann auf etwa 9,25 s fallen. Genau dieses Szenario stand im Zusatz aus Schritt 17. Die Auswirkung ist gering, weil die Härtung optional ist und der Abstand weiterhin über 9 s liegt. Die Aussage in ADR 0014 ist aber falsch.
- **Korrektur:** Entweder die Reserve mindestens auf `PriceApiOptions.MinRequestInterval` (1 s) zuzüglich Messlatenz setzen, oder den nächsten Slot erst nach dem Durchlaufen von `RequestThrottle` festlegen („ab dem tatsächlichen Absenden“). Alternativ ADR 0014/0015 korrigieren und das Restrisiko benennen. Ein Unit-Test mit vorausgehender Throttle-Verzögerung sollte das absichern.

### N2 (niedrig) – Aktion `upload` fragt trotzdem interaktiv nach Signaturzertifikat und Profil

- **Fundstelle:** `scripts/iOS-Deployment.ps1` Z. 315–321 (`Resolve-CodesigningInteractive`, `$isStore = $Action -in @('store', 'upload')`), aufgerufen aus `Assert-StorePrerequisites` Z. 471–473 für `upload`.
- **Problem:** `Get-MissingDeployValues` verlangt für `upload` korrekt keine Signierung. Trotzdem holt das Skript bei konfiguriertem Mac Identitäten und Profile vom Mac und zeigt Auswahldialoge. Das widerspricht der Doku-Tabelle in `ios-deployment.md` („`upload`: nur `-ApiKeyPath`, `-ApiKeyId`, `-ApiIssuerId`“), blockiert aber nichts.
- **Korrektur:** In `Resolve-CodesigningInteractive` bei `upload` sofort zurückkehren (`$isStore` nur für `store`) und einen Test in `test-ios-deployment.ps1` ergänzen.

### N3 (niedrig) – `Test-MinimumOsVersion` wird vom Skript nicht verwendet

- **Fundstelle:** `scripts/iOS-Deployment.ps1` Z. 433–438. Die eigentliche Prüfung läuft im Bash-Teil von `Invoke-IpaValidation` (Z. 794–799).
- **Problem:** Die Funktion ist nur Testgegenstand. Ihre Tests (15.0/16.0/17.2/leer/Text) sichern nicht die tatsächlich ausgeführte Logik ab.
- **Korrektur:** Die Funktion entfernen und den Bash-Teil wie unter M1 per Attrappe mit 15.0/16.0/leer prüfen, oder die Funktion tatsächlich verwenden.

## Prüfung der Collection-Lösung (Zusatzauftrag 3)

- **Code-Suche nach allen `ClearAllPools`/`ClearPool`-Aufrufen in `src`** (per `grep`; das Suchwerkzeug hatte `RestoreSwap.cs` zunächst nicht gelistet):
  - App: `RestoreSwap.MoveWithRetry` (Z. 246). Erreicht über `BackupService.RestoreAsync` und `RestoreRecovery`, Letzteres nur bei vorhandenem Journal.
  - Tests: `BackupServiceTests_Rebuild` (4×), `BackupServiceTests_CustomName`, `BackupServiceTests_Versions`.
- **Aufrufer im Integrationsprojekt:** Wiederherstellungen bzw. `BackupService` nutzen nur die 13 `BackupServiceTests_*`-Klassen und `BackupTestEnvironment`, das wiederum nur von diesen Klassen verwendet wird. `RestoreRecovery` wird in `DatabaseInitializer` nur mit ausdrücklich übergebenem Objekt genutzt (Tests: nur Atomicity/Resilience).
- **Gepoolte Verbindungen:** Alle übrigen Integrationstests arbeiten mit `Pooling=False` (`TestDatabase`). Eine `-wal`-Annahme gibt es nur im betroffenen Test. Es gibt keine `xunit.runner.json`; ohne Sammlung wäre jede Klasse eine eigene, parallel laufende Collection.
- **Ergebnis:** Alle 13 Klassen tragen `[Collection(BackupCollection.Name)]`. Außerhalb der Sammlung ruft keine Klasse im selben Testprozess `ClearAllPools` auf. Die Lösung ist ursächlich und vollständig; die Prüfabsicht bleibt erhalten, denn der Testkörper ist unverändert.
- **Mechanismus experimentell bestätigt:** Ein eigenes Konsolenprogramm im Scratchpad nutzt dieselben `Microsoft.Data.Sqlite`-/`e_sqlite3`-Binärdateien. Ablauf: gepoolte Verbindung im WAL-Modus, nach `Dispose` ist `-wal` vorhanden. Danach ruft ein anderer Thread auf einer anderen Datenbank `ClearAllPools` auf, und `-wal` ist verschwunden. Das entspricht genau der fehlgeschlagenen Vorbedingung in Z. 243.
- **Wiederholungen:** 10× der Einzeltest und 10× alle `BackupServiceTests_*` (je 118 Tests) gemeinsam, alle grün. Dazu kommt der vollständige Integrationslauf.

## Prüfung der E2E-Teardown-Änderungen

- `CloseApplicationAndWait` (`E2ETestBase.cs`) holt den Prozess ausschließlich über `Application.ProcessId` der selbst gestarteten App, schließt per `Application.Close()` (WM_CLOSE, kein Fokuswechsel) und wartet 15 s. Erst danach folgt `Kill(entireProcessTree: true)` auf genau diesen Prozess und dessen Kinder. Es gibt keine Namens- oder Breitensuche und kein Beenden fremder Prozesse.
- Ohne gestartete App (abgebrochener Konstruktor) kehrt die Methode sofort zurück (`StartFailureCleanupE2ETests` grün). Der Exit-Code 0xC0000005 wird mit PID protokolliert, Ausnahmen beim Schließen werden nur gemeldet.
- `OffscreenWindow` meldet `Activated`, `SizeChanged` und `Closed` beim `Closed`-Ereignis ab; die Änderung ist risikoarm.
- Kleiner Hinweis: Ist der Prozess bei `GetProcessById` bereits beendet, entfällt `Application.Close()`. Das ist unschädlich.

## Ausgeführte Prüfungen

| Prüfung | Befehl / Vorgehen | Ergebnis |
|---------|-------------------|----------|
| Release-Build, Warnungen als Fehler | `dotnet build Tankradar.sln -c Release -p:TreatWarningsAsErrors=true` mit `IncludeAndroidTarget=false`, `IncludeMacCatalystTarget=false`, ohne Geheimnis-Variablen; einmal inkrementell, einmal `--no-incremental` | grün, 0 Warnungen, 0 Fehler; gebaut wurden `net10.0-windows10.0.19041.0` und `net10.0-ios` (iossimulator-x64); das ICO-Ziel lief („Windows-Icon mit 5 Größen geschrieben“) |
| ICO-Inhalt | ICO-Kopf von `bin/Release/…/win-x64/appicon.ico` ausgelesen | 5 Einträge: 16, 24, 32, 48, 256 px, PNG, 32 bit |
| Formatprüfung | `dotnet format Tankradar.sln --verify-no-changes --no-restore --severity error` (wie `local-ci.ps1`) | grün |
| Unit-Tests | `dotnet test src/Tankradar.Tests.Unit -c Release --no-build` | 1916/1916 grün |
| Integrationstests | `dotnet test src/Tankradar.Tests.Integration -c Release --no-build` | 291/291 grün |
| Wackeliger Test | 10× Einzeltest, 10× Filter `BackupServiceTests_` (118 Tests) | 20/20 Läufe grün |
| Mechanismus WAL/Pool | Konsolenexperiment im Scratchpad | bestätigt (siehe oben) |
| Gezielte E2E-Tests (Off-Screen-Standard, `TANKRADAR_E2E_WINDOW` nicht gesetzt) | Filter: `RouteE2ETests_Cancel`, `RouteE2ETests_Errors`, `RouteE2ETests_NotPersisted`, `RouteE2ETests_Accessibility`, `StartFailureCleanupE2ETests` | 13/13 grün in 1,2 min (darunter `CancelRouteSearch_WhileRunning…`, die Grenzmeldung „bis 131 km“, der Neustartpfad und `Dispose_AfterFailedStart_DoesNotThrow`); kein 0xC0000005-Hinweis im Protokoll |
| iOS-Skripttest | `pwsh -NoProfile -File scripts/test-ios-deployment.ps1` | grün, 89 Prüfungen |
| Workflow-Validierung | `python scripts/validate-workflows.py`, `python -m unittest scripts/test_validate_workflows.py` | grün (7 Workflows und alle lokalen Actions; 14 Tests); `actionlint` ist nicht installiert |
| CI-Schritt *Validate iOS package* | aus YAML extrahiert, `bash -n`, Simulation mit Attrappen | Syntax ok; Normalfall grün; Warnpfad defekt (M1) |
| Bundle-Inhalte | vorhandene IPA `bin/Release/net10.0-ios/ios-arm64/publish/Tankradar.MAUI.ipa` (04.10.) nur gelesen | `MinimumOSVersion` 16.0, `UIDeviceFamily` [1, 2], `ITSAppUsesNonExemptEncryption` false, `PrivacyInfo.xcprivacy` im Bundle-Root, Rendition `appiconItunesArtwork.png` in `Assets.car` |
| CI-Protokolle (nur lesend) | `gh run view 40175344480 --log`, `gh run view 40178004723 --json jobs`, `gh run list`, `gh release list` | 0xC0000005 einmal im E2E-Schritt, Lauf danach 147/147 grün; Pre-Release 40178004723: iOS-Jobs übersprungen |
| Privacy-Manifest / Info.plist | XML-Auswertung (Tests) und Sichtprüfung | gültiges plist-XML. Reason-Codes passen: `C617.1` (App-Container), `3B52.1` (vom Anwender per `FilePicker` gewählte Sicherungsdatei), `CA92.1` (nur vorsorglich, harmlos), `35F9.1`/`E174.1` (Laufzeit). Kein Tracking |
| Datenschutztexte | Abgleich mit dem Code (`TankerkoenigClient`, `NominatimGeocodingService`, `OpenRouteServiceClient`, Kacheln, `SecureStorage`) | Keychain/AfterFirstUnlock korrekt, ebenso Nominatim/ORS/Kacheln; Tankerkönig unvollständig (M2) |
| Prozesse | `tasklist` vor und nach den Läufen | Vor und nach den Läufen kein `Tankradar.MAUI.exe` und kein `testhost.exe`. Die noch laufenden MSBuild-/VBCSCompiler-Knoten wurden um 11:31–11:33 gestartet, also vor meinem ersten Aufruf um 11:47. Sie gehören nicht zu meinen Läufen und wurden nicht beendet. Eigene Restprozesse gab es keine |

Keine Aktion wurde vom Berechtigungssystem abgelehnt. Es wurden keine Geheimnisse gelesen; alle Läufe liefen ohne `FUEL_PRICE_API_KEY`/`TANKRADAR_FUEL_PRICE_API_KEY`. Kein Test rief Produktivendpunkte auf (Mock-Server).

## Weitere Beobachtungen (ohne Befundcharakter)

- `RouteE2ETests_Cancel` wartet mit fester Pause (`Thread.Sleep(4 s)`, Z. 35). Gezielter und kürzer wäre es, auf die eingegangene, abgebrochene Routing-Anfrage am Mock zu warten.
- `IosProjectFilesTests` weicht von der Namenskonvention `Klasse_Thema` ab, die Methodennamen nutzen `Info_plist_…`. Inhaltlich ist die Klasse klein und themenbezogen. Die übrigen neuen Testklassen (`MapViewModelTests_Sort`, `AppIconResourceTests_Xml`, `RouteE2ETests_Cancel`) sind klein und themenbezogen.
- `Platforms/MacCatalyst/Info.plist` behält den alten Standorttext. `PlatformManifestTests_Location` prüft deshalb nur noch den Teilstring „Ermittlung von Tankstellen in “. MacCatalyst ist kein Auslieferungsziel.
- Der Kommentar in `PrivacyInfo.xcprivacy` ist in ASCII-Umschrift geschrieben („Zusaetzliche“, „Begruendung“), obwohl die Datei UTF-8 ist. Die deutsche Dokumentation unter `docs/` verwendet durchgehend Umlaute.
- Das unversionierte Verzeichnis `docs/features/` (Arbeitsstände früherer Schritte) liegt weiterhin im Arbeitsverzeichnis. Es gehört nicht zu diesem Schritt.

## Hinweise für die Projektnachbereitung

- **H1 – erster TestFlight-Upload mit neuem Icon (offen aus 17a):**
  - Nach dem Merge im nächsten signierten Pre-Release prüfen, dass der Schritt *Validate iOS package* „IPA-Prüfung bestanden (MinimumOSVersion 16.0)“ meldet und keine Icon-Warnung erscheint.
  - Prüfen, dass der Upload in App Store Connect ohne ITMS-90717 (Transparenz), ITMS-91053 (Privacy-Manifest) oder Icon-Fehler verarbeitet wird (E-Mail von Apple, TestFlight-Status).
  - M1 vorher korrigieren, damit eine geänderte `assetutil`-Ausgabe nicht still den Upload verhindert.
- **H2 – 0xC0000005:** In den nächsten CI-E2E-Läufen nach „Hinweis: Die Tankatlas-App (PID …) endete mit 0xC0000005“ bzw. „Fatal error. 0xC0000005“ suchen. Bleibt die Meldung, ist sie laut ADR 0015 eine bekannte, folgenlose Meldung beim Beenden. Weitere Maßnahmen wären nur mit einem Speicherabbild der CI belastbar.
- **H3 – `NSPhotoLibraryUsageDescription`:** Die Foto-Auswahl über `MediaPicker` nutzt ab iOS 14 den systemseitigen Picker, der keine Mediathek-Berechtigung braucht. Der Schlüssel ist daher vermutlich entbehrlich. Auf einem Gerät prüfen und dann entfernen oder die Begründung in ADR 0015 bestätigen.
- **H4 – Datenschutzerklärung:** Die öffentliche URL stellt der Inhaber bereit. Grundlage ist `docs/help/Datenschutz/beschreibung.md`, nach Korrektur von M2.
- **H5 – Tankerkönig:** Die Antwort zur Abfragegrenze steht aus. Danach die Grenze und ADR 0014 anpassen, zusammen mit N1.
- **H6:** Die Korrekturen zu M1, M2 und N1–N3 sind klein und lassen sich in einer Korrekturrunde zu Schritt 18 erledigen. Nach M1 und N1 sind `test-ios-deployment.ps1`, `validate-workflows.py` sowie die Unit-Tests zu Pacer und Skript erneut auszuführen.

## Nachtrag: Korrekturen nach der Abnahme (Commit a536d0e)

Da Schritt 18 der letzte Schritt ist, wurden die Hinweise nicht übertragen, sondern direkt behoben:

- **M1:** Icon-Prüfung in `.github/actions/package-ios/action.yml` und `scripts/iOS-Deployment.ps1` mit `|| true` an beiden Zuweisungen; unter `set -euo pipefail` simuliert (leerer Block → Warnung statt Abbruch, 1024 wird gelesen).
- **M2:** `docs/help/Datenschutz/beschreibung.md` nennt die an Tankerkönig übertragenen Tankstellenkennungen (Favoritenpreise, Detailansicht); `index.md` enthielt keine abweichende Aussage.
- **N1:** Reserve `DefaultQueryIntervalReserve` auf 1,25 s (allgemeine Drosselung bis 1 s plus 250 ms), Tests und ADR 0014/0015, Hilfe und `changes.log` angepasst.
- **N2, N3:** offen gelassen (niedrig, ohne Auswirkung auf die Auslieferung).

Geprüft: Release-Build mit Warnungen als Fehler, `dotnet format --verify-no-changes`, Unit-Tests 1916/1916, Integrationstests `~Route` 37/37, `scripts/test-ios-deployment.ps1`, `scripts/validate-workflows.py` – alle grün.
