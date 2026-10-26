# Abnahme Korrektur – MAUI 10.0.120 und Fotoberechtigungen („Foto aufnehmen“ auf dem iPhone)

- **Datum:** 2026-10-25
- **Branch:** `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--maui-update` (HEAD `d5eeef5`)
- **Geprüfter Umfang:** `git diff f772d7d..HEAD` (22 Dateien, Commits `2f24b45` und `d5eeef5`)
- **Maßstab:** Anforderungen 1–4 des Korrekturauftrags (Anlass: TestFlight rc.12, „Foto aufnehmen“ endet nach der Kamera-Freigabe mit „Das Foto konnte nicht übernommen werden.“)

## Gesamturteil

**Abgenommen mit Hinweisen**

Die Korrektur trifft die Ursache. In MAUI 10.0.120 verlangt `CapturePhotoAsync` auf iOS nur noch die Kamera-Berechtigung, und `PickPhotosAsync` fragt gar keine Berechtigung ab. Das habe ich im Quelltext des Tags selbst geprüft.

`MauiVersion` 10.0.120 ist eine unterstützte und konsistente Konfiguration: In jedem Zielframework und in jedem Job kommen alle `Microsoft.Maui.*`-Pakete einschließlich Build-Tasks, XamlC und Resizetizer in 10.0.120 aus NuGet. Die Mindestprüfung MA003 in `Microsoft.Maui.Core` 10.0.120 verlangt nur eine Workload ab 10.0.0. Diese Bedingung erfüllen alle Umgebungen.

Selbst ausgeführt und grün:
- Release-Build mit Warnungen als Fehlern (Windows und `net10.0-ios`)
- Formatprüfung
- alle Unit-Tests (1921) und Integrationstests (291)
- 12 gezielte E2E-Tests (off-screen)
- `test-ios-deployment.ps1` und `validate-workflows.py`

Es gibt keine kritischen und keine hohen Befunde. Zwei mittlere Befunde sollten vor bzw. mit der nächsten TestFlight-Auslieferung erledigt werden:

- **M1:** Ein aufgenommenes Foto wird auf iOS als PNG in voller Auflösung geliefert und kann die 20-MB-Grenze überschreiten. Dann scheitert „Foto aufnehmen“ erneut, diesmal mit „Das Foto ist zu groß“.
- **M2:** ADR 0016, README und changes.log beschreiben den bisherigen Stand falsch. rc.12 wurde auf iOS mit MAUI **10.0.0** gebaut, das Windows-Paket der CI mit **10.0.110**, lokal lief 10.0.20. Daraus folgen eine zu enge Auswertung der Release-Notes und eine falsche Abhängigkeitsaussage.

## Anforderungstabelle

| # | Anforderung | Ergebnis | Fundstelle / Nachweis |
|---|-------------|----------|------------------------|
| 1a | MAUI auf neueste stabile 10.0.x mit dotnet/maui#34287 | erfüllt | `Tankradar.MAUI.csproj` Z. 31 `<MauiVersion>10.0.120</MauiVersion>`. Laut `gh api repos/dotnet/maui/releases` ist 10.0.120 (21.10.2026) die neueste stabile 10.0.x. #34287 ist enthalten, und zwar schon **ab 10.0.60** (siehe N1) |
| 1b | Konsistent für alle `Microsoft.Maui.*`-Pakete | erfüllt | `obj/project.assets.json` für `net10.0-ios` und `net10.0-windows10.0.19041.0`: Controls, Controls.Core, Controls.Xaml, Controls.Build.Tasks, Core, Essentials, Graphics, Resizetizer (Windows zusätzlich Graphics.Win2D.WinUI.Desktop) alle **10.0.120**. Keine Mischversionen und keine Restore-Meldungen (`logs` leer, kein NU1605/NU1608). `nuget.g.props/.targets` importieren Build-Tasks und Resizetizer aus 10.0.120. Die Testprojekte erben 10.0.120 transitiv |
| 1c | Konsistent in der CI (Windows- und macOS/iOS-Jobs) | erfüllt | Die Workload trägt nur `Microsoft.Maui.Sdk` bei. `BundledVersions.targets` setzt `MauiVersion` nur, wenn sie leer ist, und erzeugt die impliziten Referenzen mit `$(MauiVersion)`; geprüft für 10.0.20 lokal und für Tag 10.0.0. Workload-Stand je Umgebung: Windows-CI 10.0.110 (Log Job 121182547232), macOS-Job 10.0.0 (Log Job 121189094257), lokal 10.0.20. Alle liegen über dem MA003-Minimum `MauiMinimumSupportedWorkloadVersion` = 10.0.0. Die iOS-Assemblies von 10.0.120 referenzieren `Microsoft.iOS` 26.0.0.0, gepinnt ist 26.5.10318 (Ref 26.5.0.0). Mono-Runtime-Packs und ILLink (10.0.13) kommen aus SDK/Workload und hängen nicht von MAUI ab. `MtouchInterpreter=all` ist unverändert. Keine Caches oder `global.json` im Repo, die eine andere Version ziehen. Einen Bruch auf dem macOS-Runner erwarte ich nicht (Restrisiko: Hinweis H2) |
| 2a | `NSPhotoLibraryUsageDescription` aus der iOS-Info.plist entfernt | erfüllt | `Platforms/iOS/Info.plist`: Es bleiben nur Kamera und Standort. MacCatalyst ist begründet unverändert. In der App braucht sonst nichts die Fotomediathek: `Share` nur für ZIP/CSV/PDF, keine `PHPhotoLibrary`-, `Permissions.Photos`- oder `UIImageWriteToSavedPhotosAlbum`-Aufrufe |
| 2b | Tests, Doku und Datenschutz angepasst | erfüllt mit Hinweis | `IosProjectFilesTests`, `PlatformManifestTests_Photos` und `test-ios-deployment.ps1` sind angepasst und grün. Datenschutz-Beschreibung und -Ablauf sind angepasst. Eine Tabellenzeile ist veraltet (N2) |
| 3a | Verständliche Meldung bei verweigerter Berechtigung bzw. nicht unterstützter Funktion, mit Hinweis auf die Einstellungen | erfüllt | `MauiReceiptPhotoPicker.TranslateAsync` (Z. 42–56) übersetzt `PermissionException` → `PermissionDenied` und `FeatureNotSupportedException` → `NotSupported`. `TankbookEntryTexts.GetPhotoAccessError` (Z. 230–240) liefert vier Texte. Zum Stil siehe N3 |
| 3b | Sonstige Fehler wie bisher; Abbruch ohne Meldung; Ansage bleibt; Protokoll nur Ausnahmetyp | erfüllt | ViewModel Z. 1080–1092: Der spezifische catch steht vor dem allgemeinen, `OperationCanceledException` bleibt ausgenommen. Bei Abbruch (`null`) gibt es keine Meldung. `_announcer.Announce` gilt für beide Zweige. Protokolliert werden nur Typ und Grund (Enum), Test `Capture_PermissionDenied_ShowsSettingsHint` prüft „geheimer Pfad“ nicht im Log |
| 3c | Unit-Tests | erfüllt mit Hinweis | `TankbookEntryFormViewModelTests_PhotoAccess` (5 Tests), `TankbookEntryTextsTests_Messages` (+4 Asserts). Die Abbildung im Adapter und der Abbruchpfad sind nicht getestet (N4) |
| 4 | changes.log, Hilfe, ADR | erfüllt mit Befund | Vorhanden: `changes.log`, `docs/help/Tankbuch/*`, `docs/help/Datenschutz/*`, ADR 0016 neu, ADR 0015/0009 nachgezogen, README. Inhaltliche Fehler: M2, N1, N2 |

## Befunde

### M1 (mittel) – Aufgenommenes Foto wird auf iOS als PNG in voller Auflösung geliefert und kann die 20-MB-Grenze überschreiten

- **Fundstelle:** `src/Tankradar.MAUI/Services/Tankbook/MauiReceiptPhotoPicker.cs` Z. 37 (`MediaPicker.Default.CapturePhotoAsync()` ohne Optionen) zusammen mit `TankbookEntryLimits.MaxReceiptPhotoBytes` = 20 MB.
- **Problem:** In 10.0.120 wandelt `DictionaryToMediaFile` das Kamerabild (`UIImagePickerController.OriginalImage`) in ein `CompressedUIImageFileResult` um, ohne Optionen mit `compressionQuality = 100`. `ShouldUsePngFormat()` liefert dann `true` (Qualität ≥ 90 und keine Maximalgröße), und `PlatformOpenReadAsync` schreibt `AsPNG()` in voller Sensorauflösung. `NormalizeOrientation` zeichnet zudem mit Alphakanal neu. Das war in 10.0.0/10.0.20 genauso, wurde aber nie erreicht, weil die Aufnahme vorher an der Berechtigung scheiterte. Ein 12-MP-Foto ergibt als PNG typischerweise 10–25 MB. Liegt es über 20 MB, meldet das Formular „Das Foto ist zu groß (höchstens 20 MB).“, und die reparierte Funktion bleibt für viele Belege unbenutzbar. Dazu kommen ein hoher Speicherbedarf (ca. 48 MB Bitmap plus PNG-Daten) und eine spürbare Kodierzeit.
- **Nachweis:** Quelltext `MediaPicker.ios.cs` im Tag 10.0.120, Z. 478–489 (OriginalImage → `CompressedUIImageFileResult`), Z. 1040–1056 (Formatwahl) und Z. 1079–1085 (`AsPNG`). Der Belegtest auf dem Gerät steht noch aus (ADR 0016 „Offen (Gerät)“).
- **Korrektur:**
  1. In `CaptureAsync` Optionen übergeben, die JPEG erzwingen, z. B. `CapturePhotoAsync(new MediaPickerOptions { CompressionQuality = 85 })`. Bei < 90 wird `AsJPEG(0.85)` verwendet, ein 12-MP-Beleg wird etwa 2–4 MB groß und bleibt gut lesbar. Alternativ `MaximumWidth`/`MaximumHeight` (z. B. 3000) setzen.
  2. Die Aussage „Das Foto wird nicht verkleinert oder neu kodiert“ im Klassenkommentar sowie „unverändert“ in ADR 0009, ADR 0016 und `docs/help/Tankbuch/beschreibung.md` für die Aufnahme präzisieren: Die Auswahl übernimmt das Original, die Aufnahme liefert ein von MAUI kodiertes JPEG.
  3. Im Gerätetest Größe und Format eines aufgenommenen Belegs prüfen.

### M2 (mittel) – Bisherige MAUI-Version und Abhängigkeitsänderungen falsch dokumentiert

- **Fundstellen:**
  - `docs/adr/0016-maui-version-and-photo-permissions.md` Z. 9, 13, 14, 23
  - `README.md` Z. 13
  - `changes.log` Z. 3 und 4
- **Problem:**
  1. ADR, README und changes.log nennen 10.0.20 als bisherige Version („Standardwert aus der installierten Workload“). Das traf nur auf den lokalen Rechner zu (Workload-Set 10.0.401 → MAUI 10.0.20). Das iPhone-Paket rc.12 entstand im macOS-Job: SDK 10.0.113, `--skip-manifest-update`, Log „Installing pack Microsoft.Maui.Sdk version 10.0.0“, also mit **10.0.0**. Das Windows-Paket rc.12 entstand mit Workload-Set 10.0.402.1, Log „Microsoft.Maui.Sdk.Msi.x64 (10.0.110)“, also mit **10.0.110**. Vor der Korrektur liefen damit drei verschiedene MAUI-Versionen. Das stützt die Entscheidung, die Version festzulegen, wird aber nicht erwähnt.
  2. Folge für die Auswertung der Release-Notes (ADR Z. 14): Für iOS reicht der Sprung von 10.0.0 bis 10.0.120. Die Korrekturen aus 10.0.1–10.0.20 fehlen in der Bewertung, etwa „MediaPicker and FilePicker returning null on cancellation“ (10.0.20); die App fängt das über `results?.FirstOrDefault()` ab.
  3. „Build-Werkzeuge der Workload 10.0.20, Bibliotheken 10.0.120“ (ADR Z. 23) stimmt nicht. XamlC, SourceGen und Resizetizer kommen aus den NuGet-Paketen `Microsoft.Maui.Controls.Build.Tasks` und `Microsoft.Maui.Resizetizer` 10.0.120. Die Workload liefert nur `Microsoft.Maui.Sdk` (Vorgabewerte, implizite Referenzen, MA003-Prüfung), im macOS-Job in 10.0.0.
  4. „Keine weitere neue oder aktualisierte Abhängigkeit“ (changes.log Z. 4) stimmt nicht. Gegenüber dem lokalen Stand ändern sich transitiv `Microsoft.WindowsAppSDK` 1.7.250909003 → 1.8.260529003 (neu mit den Komponenten AI/ML, darunter `onnxruntime.dll` 21,7 MB und `DirectML.dll` 18,7 MB), `Microsoft.Graphics.Win2D` 1.3.2 → 1.4.0, `Microsoft.Web.WebView2` 1.0.2903.40 → 1.0.4258.31 und `Microsoft.Windows.SDK.BuildTools` 10.0.22621.756 → 10.0.26100.8249. Der Zwischenstand wächst dadurch von 149 MB (`review-versions/0.1.25_2026-10-25`, WinAppRuntime 1.7) auf 193 MB (`0.1.26_2026-10-25`, WinAppRuntime 1.8). Die CI-Pakete enthielten das wegen 10.0.110 vermutlich schon (`release-win-x64.zip` rc.4–rc.12 ca. 119–122 MB gegenüber 92 MB lokal am 04.10.). Für das ausgelieferte Windows-Paket ist der Sprung also klein.
- **Korrektur:**
  - ADR 0016 Kontext: „rc.12 wurde ohne festgelegte Version gebaut: iOS-Job 10.0.0, Windows-Job 10.0.110, lokal 10.0.20“.
  - Release-Notes-Abschnitt auf 10.0.0–10.0.120 (iOS) bzw. 10.0.110–10.0.120 (Windows-CI) erweitern.
  - Folgen-Satz zu den Build-Werkzeugen berichtigen.
  - In changes.log die transitiven Windows-Abhängigkeiten nennen.
  - README Z. 13 „10.0.20“ durch „10.0.0 (iOS-Build der CI)“ ersetzen.
  - Optional: Größe von `release-win-x64.zip` mit `local-ci.ps1 -Package` gegen rc.12 prüfen.

### N1 (niedrig) – Die Korrektur #34287 ist seit 10.0.60 enthalten, nicht erst „ab 10.0.90“

- **Fundstellen:**
  - `Tankradar.MAUI.csproj` Z. 30 („Ab 10.0.90 …“)
  - `changes.log` Z. 3 („(ab 10.0.90)“)
  - `PlatformManifestTests_Photos.cs` Z. 21 („ab .NET MAUI 10.0.90“)
- **Nachweis:**
  - PR #34287: Meilenstein „.NET 10 SR6“, gemergt am 22.03.2026, im Abschnitt „Mediapicker“ der Release-Notes 10.0.60 aufgeführt.
  - `PhotosAddOnly` in `MediaPicker.ios.cs`: vorhanden in 10.0.0, 10.0.20, 10.0.50 und 10.0.51; nicht mehr vorhanden in 10.0.60, 10.0.70, 10.0.71, 10.0.80, 10.0.90, 10.0.110 und 10.0.120.
- **Korrektur:** In den drei Stellen „ab 10.0.60“ schreiben. Die Entscheidung für 10.0.120 bleibt richtig.

### N2 (niedrig) – Zwei veraltete Dokumentstellen

- `docs/help/Datenschutz/ablauf-technisch.md` Z. 9: „Berechtigungstexte (nur Kamera, Fotos, Standort bei Nutzung)“ → „(nur Kamera und Standort bei Nutzung)“.
- `docs/help/Navigation/ablauf-technisch.md` Z. 54: „aktuell verwendete MAUI-Version (`Microsoft.Maui.Controls` 10.0.20)“ → 10.0.120. Inhaltlich gilt die Aussage weiter: `Shell` hat in 10.0.120 nur `TabBarBackgroundColor`, `-DisabledColor`, `-ForegroundColor`, `-IsVisible`, `-TitleColor` und `-UnselectedColor`, also keinen Schatten und kein Bold.
- Optional: `docs/help/Datenschutz/index.md` Z. 10 zusätzlich auf ADR 0016 verweisen.

### N3 (niedrig) – Meldungstext weicht vom vorhandenen Berechtigungstext ab

- **Fundstelle:** `TankbookEntryTexts.cs` Z. 234–235 im Vergleich zu `SearchTexts.LocationPermissionDenied` (Z. 139: „… Bitte erlaube den Standortzugriff in den Systemeinstellungen.“).
- **Problem:** Anrede (du) und Deutsch sind korrekt. Form und Ort weichen aber ab: „Erlaube den Zugriff in den Einstellungen unter Tankatlas.“ ohne „Bitte“, dazu „Einstellungen unter Tankatlas“ statt „Systemeinstellungen“. Ohne Anführungszeichen wirkt der App-Name im Satz wie ein Ort.
- **Korrektur (Vorschlag):** „Kein Zugriff auf die Kamera. Bitte erlaube den Kamerazugriff in den Einstellungen unter „Tankatlas“.“, entsprechend für die Fotos. Test, Hilfe (`docs/help/Tankbuch/beschreibung.md` Z. 105), ADR und changes.log mitziehen.

### N4 (niedrig) – Testlücken und Kleinigkeiten in den Tests

- Die Abbildung in `MauiReceiptPhotoPicker.TranslateAsync` ist ungetestet: `PermissionException` → `PermissionDenied`, `FeatureNotSupportedException` → `NotSupported`, andere Ausnahmen und `OperationCanceledException` unverändert. Gerade sie ist der Kern von Anforderung 3. **Korrektur:** `IMediaPicker` per Konstruktor injizieren (Standard `MediaPicker.Default`) und mit einem Fake im Unit-Test prüfen; die Essentials-Ausnahmetypen sind unter `net10.0-windows` verfügbar. Alternativ die Übersetzung als statische Methode auslagern und direkt testen.
- Kein Test belegt „Abbruch ⇒ keine Meldung“ ausdrücklich. `Capture_HiddenWhenNotSupported` prüft nur den unveränderten Dateinamen. **Korrektur:** Test mit `NextExtension = null` bzw. `NextException = new OperationCanceledException()` ergänzen: `PhotoError` leer, `Announcer.Announcements` leer, Befehle wieder ausführbar.
- `OpenFormAsync()` steht wortgleich in `TankbookEntryFormViewModelTests_Photo` und `_PhotoAccess`. Besser in `TankbookEntriesViewModelTestBase` verschieben.
- `FakeReceiptPhotoPicker.NextException` ist als „Die Ausnahme, die der nächste Aufruf auslöst“ dokumentiert, wirkt aber bis zum Zurücksetzen. Kommentar anpassen oder nach dem Auslösen zurücksetzen.

## Ausgeführte Prüfungen

Umgebung: Windows 11, SDK 10.0.402, Workload-Set 10.0.401 (maui-windows 10.0.20, ios 26.5.10318). In der Shell `IncludeAndroidTarget=false`, `IncludeMacCatalystTarget=false`, `FUEL_PRICE_API_KEY`/`TANKRADAR_FUEL_PRICE_API_KEY` entfernt, MSBuild-Knotenwiederverwendung und Compilerserver aus.

| Prüfung | Ergebnis |
|---------|----------|
| Diff-Review `f772d7d..HEAD` (Code, Tests, Skript, Info.plist, Doku) | siehe Befunde |
| `dotnet restore Tankradar.sln -p:Configuration=Release` | erfolgreich, keine NU-Warnungen. `project.assets.json`: alle `Microsoft.Maui.*` 10.0.120 für `net10.0-ios` und `net10.0-windows10.0.19041.0`. Hinweis: Ein paralleler Restore (vermutlich Visual Studio) schrieb dieselbe Datei mit allen vier Zielframeworks neu, ebenfalls nur 10.0.120 |
| MSBuild-Auswertung je Zielframework | `MauiVersion` 10.0.120, `MauiWorkloadVersion` 10.0.20, iOS `TargetPlatformVersion` 26.5, `MtouchInterpreter` all, XamlC als Release-Standard (in 10.0.20 und 10.0.120 gleich) |
| `dotnet build Tankradar.sln -c Release --no-restore -p:TreatWarningsAsErrors=true` | **0 Warnungen, 0 Fehler**. `CoreCompile` und `XamlC` liefen für `net10.0-ios` (iossimulator-x64) und `net10.0-windows`. Das Windows-Icon-Ziel (Resizetizer-interne Eigenschaften in 10.0.120 unverändert) schrieb das ICO mit 5 Größen. Native iOS-Schritte (Link/AOT) sind nur auf dem Mac möglich |
| `dotnet format Tankradar.sln --verify-no-changes --no-restore --severity error` | sauber. Zusätzlich die geänderten Dateien mit Standardschwere: sauber |
| Unit-Tests (Release, vollständig) | **1921/1921** bestanden |
| Integrationstests (Release, vollständig; nur lokale Mock-Server) | **291/291** bestanden |
| E2E off-screen (`TANKRADAR_E2E_WINDOW` nicht gesetzt), Filter `TankbookEntriesE2ETests` / `BackupE2ETests_Photo` / `TankbookE2ETests_Flow` | **12/12** bestanden, darunter `ReceiptPhoto_IsStoredShownAndDeletedWithEntry`, `RestoreBringsBackEntriesPhotoAndStatistics`, Flow, Validation, EditDelete, Persistence, Partial, PartialHint, Electric und RestoreReload (2). Die App lief dabei mit MAUI 10.0.120 und WinAppSDK 1.8 |
| `scripts/test-ios-deployment.ps1` | 89 Prüfungen OK, Exit 0 (u. a. „Info.plist: nur Kamera und Standort (WhenInUse)“) |
| `python scripts/validate-workflows.py` | OK (7 Workflows); `actionlint` nicht installiert |
| Upstream-Quelltext `MediaPicker.ios.cs` (`gh api`, Tags 10.0.0–10.0.120) | `CapturePhotoAsync` → `PhotoAsync(photo: true, pickExisting: false)` verlangt nur `Permissions.Camera`. `PickPhotosAsync` (PHPicker, ab iOS 14) ruft kein `EnsureGrantedAsync` auf. `PhotosAddOnly` ist bis 10.0.51 vorhanden, ab 10.0.60 entfernt. Aufnahme-Ergebnis: PNG (M1) |
| Windows-Pfad `MediaPicker.windows.cs` 10.0.120 | Keine `Permissions`-Abfrage. Die Aufnahme liefert JPEG, eine `PermissionException` ist dort nicht zu erwarten |
| MAUI-SDK-Targets (`BundledVersions.targets` 10.0.20 lokal, Tag 10.0.0) und `Microsoft.Maui.Core.After.targets` 10.0.120 | `MauiVersion` überschreibt die Workload-Vorgabe; MA003 greift erst bei einer Workload unter 10.0.0 |
| CI-Definitionen (`.github/workflows/*`, `.github/actions/*`) und Job-Logs von Run 40197858684 | Windows: `setup-dotnet 10.0.x` + `dotnet workload restore` → Workload-Set 10.0.402.1, MAUI-SDK 10.0.110. macOS: SDK 10.0.113, iOS-Manifest 26.5.10318, `Microsoft.Maui.Sdk` 10.0.0. Keine Caches, keine eingecheckte `global.json` |
| Release-Notes 10.0.1–10.0.120 (`gh api …/releases/tags/…`) | MediaPicker/Permissions-Einträge ausgewertet (siehe M2 und H5) |
| Vergleich der Zwischenstände `review-versions/0.1.25` und `0.1.26` | MAUI 10.0.20 → 10.0.120, WinAppRuntime 1.7 → 1.8, `onnxruntime.dll` neu, 149 → 193 MB |
| Sicherheitsprüfung | Nicht selbst ausgeführt (Netzzugriff). Der Bericht `vulnerable-packages.json` des Umsetzers (16:08, nach dem MAUI-Update) enthält keine Funde |
| Prozesse | Vor- und nachher gelistet; von mir ist kein Prozess übrig geblieben. Die laufenden MSBuild-/VBCSCompiler-Prozesse stammen von vor der Prüfung und wurden nicht angefasst |

## Hinweise

- **H1 – Gerätetest vor der Freigabe:**
  - „Foto aufnehmen“ nach erstmaliger Freigabe: Größe und Format prüfen (M1).
  - Nach Ablehnung: Die Meldung erscheint und wird von VoiceOver angesagt.
  - Nach dem Erlauben in den Einstellungen funktioniert die Aufnahme wieder.
  - „Foto auswählen“ ohne Abfrage, auch mit HEIC.

  Weil das iPhone bisher MAUI 10.0.0 nutzte, kommen alle Korrekturen bis 10.0.120 zum ersten Mal auf das Gerät. Die Hauptabläufe (Suche, Karte, Tankbuch, Statistik, Sicherung) sollten in TestFlight kurz angetestet werden.
- **H2 – iOS-CI:** Ein Versionsbruch ist nicht zu erwarten (siehe Anforderung 1c). Der PR-Job baut aber nur für den Simulator. Linker, Interpreter und Signierung des Gerätepakets durchlaufen erst den Staging-Job *ios prerelease*. Dessen Ergebnis abwarten, bevor rc.13 als bestätigt gilt.
- **H3 – App-Store-Prüfung ohne `NSPhotoLibraryUsageDescription`:** Apples statische Prüfung (ITMS-90683) kann Foto-APIs anmahnen, die MAUI Essentials referenziert (`Permissions.Photos`, `PHAsset`). rc.12 wurde ohne `NSPhotoLibraryAddUsageDescription` angenommen; das spricht dafür, dass die verwalteten Referenzen nicht gemeldet werden. Trotzdem die Rückmeldung zum nächsten Upload (E-Mail von App Store Connect) beachten. Bei einer Beanstandung den Schlüssel wieder eintragen; er löst keine Abfrage aus.
- **H4 – Bedienkomfort (optional):** Bei `PermissionDenied` könnte ein Knopf „Einstellungen öffnen“ (`AppInfo.Current.ShowSettingsUI()`) den Weg abkürzen.
- **H5 – Weitere Release-Notes-Einträge:** Ohne Auswirkung auf die App, weil nur ein Foto und kein `RotateImage` genutzt wird:
  - „PickPhotosAsync“-Dateiname/Inhaltstyp (10.0.40)
  - HEIC-Anzeige (10.0.80)
  - `FullPath` für PHPicker (10.0.100)
  - `UIKitThreadAccessException` bei 2+ Elementen (10.0.110/120)
  - EXIF-Drehung unter Windows (10.0.120)

  Die Android-Einträge sind irrelevant. Ein `PermissionException` entsteht auch bei fehlendem Info.plist-Schlüssel; die Meldung würde dann fälschlich auf die Einstellungen verweisen. Den Kameraschlüssel sichern `IosProjectFilesTests` und `test-ios-deployment.ps1` ab.
- **H6 – Vorbestehend:** Beide catch-Zweige setzen `PhotoError`, ohne die Sitzung zu prüfen (`session != _session`). Praktisch ist das ohne Bedeutung, weil der Systemdialog modal ist.
- **H7 – Ablage (optional):** `ReceiptPhotoAccessProblem` liegt in `Services/Tankbook`, das vergleichbare `ReceiptPhotoError` in `Models/Tankbook`. `TankbookEntryTexts` importiert deshalb jetzt einen Service-Namensraum.
- **Abgelehnte Aktion:** Die Versionsliste von `api.nuget.org` per `curl` wurde vom Berechtigungssystem abgelehnt und nicht umgangen. Die Aktualität von 10.0.120 ist stattdessen über die GitHub-Releases von dotnet/maui belegt.

## Nachtrag: Korrekturen nach der Abnahme (Commits 942a500, 34646cb)

Alle Befunde wurden behoben: M1 (Aufnahme als JPEG, Qualität 85), M2 (ADR 0016, README, changes.log mit den tatsächlich genutzten Versionen und den aktualisierten Windows-Abhängigkeiten), N1–N4 (Versionsangabe ab 10.0.60, veraltete Hilfe, Meldungstexte im Stil des Standorttexts, Tests für den MediaPicker-Adapter und für den Abbruch, doppelter Helfer zusammengeführt).
WindowsAppSDK lässt sich nicht auf 1.7 halten: MAUI 10.0.120 verlangt ≥ 1.8 (NU1605 beim Downgrade).
Geprüft: Release-Build mit Warnungen als Fehler, Format, Unit 1931/1931, Integration 291/291, E2E Tankbuch/Belegfoto 11/11.
