# Abnahmeprüfung – Entwicklungsschritt 4

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

**Abweichung aus `acceptance-schritt-4.1.md` (instabile FlaUI-Tests der Optionen) ist behoben:**

- **Neue Navigationshilfe:** `E2ETestBase.NavigateToTab(tabTitle, pageAutomationId)` wartet zuerst bis zu 30 s auf den Reiter. Dann wählt sie ihn in bis zu 5 Versuchen aus, abwechselnd per `SelectionItemPattern.Select()` und per Klick. Nach jedem Versuch wartet sie bis zu 5 s auf die seitenspezifische AutomationId.
  - `SettingsE2ETestBase.OpenSettings()` wartet auf `SettingsPage.Headline` und danach wie bisher auf `Settings.FuelType.SuperE5.Switch` und `Settings.Loaded`.
  - `NavigationE2ETests` nutzt dieselbe Hilfe. Die doppelte `FindTab`-Logik dort ist entfernt.
- **Praktischer Nachweis: 5 Läufe hintereinander** mit `dotnet test src/Tankradar.Tests.E2E` (Debug-Build des aktuellen Stands `26e98f4`), alle vollständig grün:

  | Lauf | Start | Ergebnis | Dauer |
  |------|-------|----------|-------|
  | 1 | 21:18:33 | 9/9 bestanden, 0 Fehler | 28 s |
  | 2 | 21:19:03 | 9/9 bestanden, 0 Fehler | 26 s |
  | 3 | 21:19:31 | 9/9 bestanden, 0 Fehler | 26 s |
  | 4 | 21:19:59 | 9/9 bestanden, 0 Fehler | 25 s |
  | 5 | 21:20:26 | 9/9 bestanden, 0 Fehler | 22 s |

  Dazu kommt der E2E-Lauf in `scripts/local-ci.ps1` (Release): ebenfalls 9/9 grün. Das sind 6 grüne Läufe in Folge, ohne Fehlschlag und ohne neue Diagnosedaten unter `e2e-diagnostics/`.
- **Regression:** Keine festgestellt. Die Navigationstests (alle vier Reiter und zurück) und alle Einstellungstests (Standardwerte, Spritsorten, Persistenz nach Neustart) sind grün.

**Kompilierte Bindungen (`IChoiceOption`) und Funktion der Optionen-Seite:**

- **Bindungen im Code:** `SettingsPage.xaml` hat `x:DataType="vm:SettingsViewModel"`. Die Vorlage der Spritsorten verwendet `vm:FuelTypeItemViewModel`, die drei Auswahlgruppen (GPS, Ansicht, Sortierung) verwenden das neue, nicht generische Interface `vm:IChoiceOption` (`Label`, `AutomationKey`, `IsSelected`). `ChoiceOptionViewModel<TValue>` implementiert dieses Interface.
  - Den Hintergrund nennt der Umsetzer: Generische Typen lassen sich in `x:DataType` nicht ausdrücken.
  - Das Interface erweitert `INotifyPropertyChanged` nicht. Laut manueller Prüfung (unten) werden Änderungen trotzdem korrekt angezeigt, weil das Laufzeitobjekt über `BaseViewModel` Änderungen meldet. Kein Mangel.
- **Manuelle Prüfung des Zwischenstands `review-versions/0.1.4_2026-10-04/bin/Tankradar.MAUI.exe`:**
  - **Start:** mit `TEST_DATA_PATH=%TEMP%\tankatlas-acceptance-step4-r2`, außerhalb des Repos.
  - **Bedienung:** per Windows UI Automation (Skript im Scratchpad).
  - **Ablauf:**
    - Fenstertitel „Tankatlas“. Reiter „Optionen“ geöffnet: Die Seite lädt vollständig, und alle Auswahlelemente sind vorhanden und sichtbar.
      - drei Spritsorten-Schalter mit ▲/▼
      - GPS „Immer“ / „Nur bei Nutzung“ / „Nie“
      - Ansicht „Liste“ / „Karte“
      - Sortierung „Preis“ / „Entfernung“ / „Name“
    - **Standardwerte:** alle Spritsorten aktiv in der Reihenfolge Super E5, Super E10, Diesel; „Nur bei Nutzung“, „Liste“, „Preis“. Das entspricht „sicher statt bequem“. ▲ ist beim ersten Eintrag gesperrt, ▼ beim letzten.
    - **Geändert:** Super E10 abgewählt, Diesel zweimal nach oben verschoben, GPS „Nie“, Ansicht „Karte“, Sortierung „Name“. Ergebnis sofort sichtbar: Reihenfolge Diesel, Super E5, Super E10 (aus); die neuen Optionen ausgewählt.
    - **Letzte Sorte:** Nach dem Abwählen von Super E5 sollte auch Diesel abgewählt werden. Der Schalter blieb an, und es erschien die Meldung „Mindestens eine Spritsorte muss ausgewählt bleiben.“ Nachdem Super E5 wieder eingeschaltet wurde, verschwand die Meldung.
    - **Neustart:** App geschlossen (der Prozess endete sauber) und neu gestartet. Alle Änderungen waren erhalten: Reihenfolge Diesel, Super E5 (an), Super E10 (aus); „Nie“, „Karte“, „Name“.
    - **Datenverzeichnis:** Es enthielt ausschließlich `tankatlas.db`.
  - **Aufräumen:** Das temporäre Verzeichnis ist entfernt, es läuft kein `Tankradar.MAUI`-Prozess mehr.
- **Eigenschaften der EXE (0.1.4):** `ProductName = Tankatlas`, `FileDescription = Tankatlas` (die Nachbesserung `AssemblyTitle` wirkt), `FileVersion = 0.1.4.0`. Datei- und Assemblyname bleiben `Tankradar.MAUI`, wie in der Vorgehensentscheidung „App-Anzeigename“ verlangt.

**XC0022:**

- Ein vollständiger Neu-Build (`--no-incremental`) der iOS-Compile-Prüfung lief mit den Parametern aus `local-ci.ps1`: `net10.0-ios`, `iossimulator-arm64`, `TreatWarningsAsErrors`, `MauiXamlInflator=XamlC`. Ergebnis: Exit 0, **0 XC0022-Warnungen** (vorher 33). Es blieb nur die bekannte, nicht projektbezogene Hinweiswarnung MAUI1001 zum explizit gesetzten XamlC-Inflator.
- Danach wurde der Windows-Restore wiederhergestellt, wie es `local-ci.ps1` vorsieht.

**Pipeline-Skripte:**

- **`scripts/local-ci.ps1`:** Exit 0. Alle Schritte OK.
  - Node-Tests, Workflow-Validierung, iOS-Deployment-Skript, Restore, Format, Sicherheitsprüfung, statische Analyse mit Warnungen als Fehler.
  - Unit-Tests 82/82, Integrationstests 19/19, Zeilenabdeckung 92,5 % (Schwelle 70 %).
  - FlaUI-E2E 9/9, iOS-Compile-Prüfung OK.
  - Das Windows-Paket wurde wie vorgesehen übersprungen.
- **`scripts/test-ios-deployment.ps1`:** Exit 0, „iOS-Deployment-Prüfung erfolgreich.“

**Gesamtanforderung erneut im Code nachvollzogen:**

- **Eine Datenbank:** `tankatlas.db` mit EF Core / SQLite.
  - `DatabaseInitializer` legt das Verzeichnis an und ruft `MigrateAsync()` auf. Bei einem Fehler wird das Ergebnis verworfen, sodass ein neuer Versuch möglich ist.
  - Die Migrationen `InitialCreate` und `AddFuelTypeSettings` sind rein additiv.
  - Das Upgrade ohne Datenverlust ist durch den Integrationstest `DatabaseInitializerTests_Upgrade` abgedeckt.
- **iOS-Dateischutz:** `NSFileProtectionCompleteUntilFirstUserAuthentication` auf Verzeichnis, `.db`, `-wal` und `-shm`. Es gibt keine zusätzliche Verschlüsselung.
- **Optionen:** Spritsorten (Auswahl und Reihenfolge), GPS, Ansicht und Sortierung. Es gibt keine Strom- oder Ladetyp-Einstellungen. Die Texte stehen zentral in `SettingsTexts`.
- **Speichern:** `SettingsViewModel` serialisiert Laden und Speichern über ein `SemaphoreSlim`. Schnelle Mehrfachänderungen konkurrieren deshalb nicht um die Datenbank. `SettingsService` aktualisiert vorhandene Zeilen (Schlüssel `FuelTypeKey`) und legt fehlende neu an.
- **Tests:** Speichern/Laden (Unit und Integration), Anlegen/Aktualisieren der Datenbank (Integration) und FlaUI-Tests für das Ändern von Einstellungen in der Windows-App sind vorhanden und grün.
- **Projektdatei:** `<Product>Tankatlas</Product>` und `<AssemblyTitle>Tankatlas</AssemblyTitle>` sind gesetzt.
- **README:** Der Satz zum Entwicklungsstand ist aktualisiert („Entwicklungsschritt 4 abgeschlossen: …“), die Verzeichnisübersicht um `Models/` und `Data/` ergänzt.
- **Technische Mängel im neuen Code:** In der Durchsicht des Fix-Commits `26e98f4` und der Kernklassen (`SettingsService`, `DatabaseInitializer`, `DatabaseFileProtector`, `App`, `MauiProgram`, `SettingsPage.xaml`) fanden sich keine.

**Kleinere Beobachtungen (keine Abweichung):**

- `ProductVersion` der EXE im Zwischenstand 0.1.4 lautet `0.1.0+3f73f8ab…`. Der Build entstand aus dem Arbeitsstand vor dem Fix-Commit (EXE 21:14:47, Commit 21:15:04); der Funktionsumfang entspricht jedoch dem geprüften Stand. `FileVersion` ist korrekt `0.1.4.0`.
- Die `CHANGELOG.md` im Zwischenstand 0.1.4 ist sehr knapp und technisch formuliert („stabile E2E-Navigation, AssemblyTitle Tankatlas, XC0022 behoben“). Für Prüfer aus Anwendersicht wäre ein Hinweis auf die Optionen hilfreicher.
- In `NavigateToTab` wird die erste Klickvariante (SelectionItemPattern) bevorzugt. Ein Klickverlust wird dadurch abgefangen, statt ihn in der App zu beheben. Für einen E2E-Test ist das angemessen; ein Fehlverhalten der App selbst wurde bei der manuellen Bedienung nicht beobachtet.
