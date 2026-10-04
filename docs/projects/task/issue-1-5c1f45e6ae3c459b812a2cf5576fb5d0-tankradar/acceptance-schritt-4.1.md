# Abnahmeprüfung – Entwicklungsschritt 4

## Ergebnis

**Status:** Abweichungen gefunden

## Abweichungen

- [ ] **FlaUI-Tests für das Ändern von Einstellungen sind nicht zuverlässig grün.** Die Anforderung verlangt, dass Tests per FlaUI das Ändern von Einstellungen in der Windows-App abdecken (einschließlich „Änderungen bleiben nach einem Neustart erhalten“). Die Tests dazu gibt es (`SettingsE2ETests_Defaults`, `_FuelTypes`, `_Persistence`), sie schlagen aber sporadisch fehl. Von vier Läufen (1× `scripts/local-ci.ps1`, 3× `dotnet test src/Tankradar.Tests.E2E`) war nur einer vollständig grün. In den anderen drei Läufen scheiterten jeweils 2 von 9 Tests, und zwar abwechselnd verschiedene: `Settings_ProvideAllControls_WithoutElectricOptions`, `LastSelectedFuelType_CannotBeDeselected_ShowsMessage`, zweimal hintereinander `ChangedChoices_SurviveRestart` und `FuelTypeSelectionAndOrder_SurviveRestart`. Die Ursache ist jedes Mal dieselbe: `SettingsE2ETestBase.OpenSettings()` klickt den Reiter „Optionen“ einmalig per `tab.Click()` (Zeile 25). Die Seite wechselt danach nicht, und `Settings.FuelType.SuperE5.Switch` wird nicht gefunden (Zeile 26). Laut Screenshot und UI-Baum in `e2e-diagnostics/` bleibt die App auf „Favoriten“. In der Pipeline ist E2E nur „best-effort“, deshalb meldet `local-ci.ps1` trotzdem Erfolg. Die automatisierte FlaUI-Absicherung der Persistenz ist damit nicht belastbar. Damit ist auch Befund 1 aus `review-code.1.md` (Stabilität/Flakiness der Einstellungs-E2E-Tests) nur teilweise behoben: Die feste Wartezeit ist durch das Signal `Settings.Loaded` ersetzt, aber die Navigation zur Seite bleibt instabil. Die Funktion der App selbst ist dagegen in Ordnung, siehe manuelle Prüfung unter „Hinweise“.

## Hinweise

**Fachliche Umsetzung (im Code nachvollzogen):**

- **Eine SQLite-Datenbank:** Es gibt genau eine Datenbank, `tankatlas.db` im App-Datenverzeichnis über `IAppDataPathProvider`, mit EF Core (`TankradarDbContext`, Tabellen `UserSettings` und `FuelTypeSettings`).
- **Erster Start und Updates:**
  - Beim Start legt `App.InitializeDatabaseAsync` → `DatabaseInitializer.RunAsync` das Verzeichnis an und ruft `MigrateAsync()` auf. `EnsureCreated` wird nicht verwendet.
  - Die zwei Migrationen (`InitialCreate`, `AddFuelTypeSettings`) sind rein additiv.
  - `SettingsService` stellt vor jedem Zugriff die Initialisierung sicher. Nach einem Fehler ist ein neuer Versuch möglich.
- **Dateischutz unter iOS:** `DatabaseFileProtector` setzt unter `#if IOS` `NSFileProtectionCompleteUntilFirstUserAuthentication` auf Verzeichnis, `.db`, `-wal` und `-shm`. Fehlschläge werden protokolliert. Eine zusätzliche Verschlüsselung gibt es nicht.
- **Optionen:**
  - Spritsorten (Super E5, Super E10, Diesel) mit Schalter und ▲/▼ für die Reihenfolge; die letzte ausgewählte Sorte lässt sich nicht abwählen.
  - GPS-Nutzung „Immer“ / „Nur bei Nutzung“ / „Nie“, Ansicht Liste/Karte, Sortierung Preis/Entfernung/Name.
  - Es gibt keine Strom- oder Ladetyp-Optionen.
  - Die Texte stehen zentral in `SettingsTexts`.
- **Standardwerte:** „Nur bei Nutzung“, Liste, Preis, alle Spritsorten aktiv. Das entspricht „sicher statt bequem“.
- **Speichern und Fehler:** Änderungen werden sofort gespeichert. Lade- und Speicherfehler werden protokolliert und als Statusmeldung angezeigt.
- **Projektdatei:** In der MAUI-Projektdatei ist `<Product>Tankatlas</Product>` gesetzt. Die EXE im Zwischenstand zeigt `ProductName = Tankatlas`, und Assembly- und Dateiname bleiben `Tankradar.MAUI`.
- **README:** Der Satz „befindet sich in Entwicklungsschritt 1“ ist durch einen Satz zum Stand nach Schritt 4 ersetzt.

**Code-Reviews:**

- Alle 5 Befunde aus `review-code.1.md` sind im Code behoben:
  - Logging in `SettingsViewModel`
  - gekapselte Initialisierung mit Logging in `App`
  - Auswertung von `SetAttributes` und Schutz des Verzeichnisses
  - gemeinsame Methode `AppSettings.NormalizeFuelTypes`
  - Aufteilung der Enum-Tests
- Einschränkung zum Thema E2E-Stabilität: siehe Abweichung.
- Der einzige Befund aus `review-code.md` (toter Code `IsAnyRadioChecked`) ist ebenfalls behoben; die Methode gibt es nicht mehr.

**Praktische Prüfung:**

- **`scripts/local-ci.ps1`:** Exit-Code 0.
  - Format, Sicherheitsprüfung, Build mit Warnungen als Fehler und iOS-Compile-Prüfung waren OK.
  - Unit-Tests: 82/82 grün. Integrationstests: 19/19 grün. Zeilenabdeckung 92,5 %.
  - E2E-Tests: 7/9 (Warnung, siehe Abweichung).
- **`scripts/test-ios-deployment.ps1`:** Exit-Code 0, alle Prüfungen OK.
- **Update-Sicherheit (Integrationstests, alle grün):**
  - `DatabaseInitializerTests_FirstStart`: Ein Stand ohne Datenbank legt Datei und Schema mit beiden Migrationen an.
  - `DatabaseInitializerTests_Upgrade.Initialize_FromOlderSchemaVersion_KeepsData`: Eine Datenbank nur mit `InitialCreate` und vorhandenen Einstellungen wird migriert, die Werte bleiben erhalten.
  - `Initialize_Twice_IsIdempotent` und `MigrationsTests_ModelSync`: Es gibt keine ausstehenden Modelländerungen.
  - Frühere Versionen haben keine Nutzerdaten gespeichert; `appsettings.json` enthält nur die gebündelte Bundle-ID. Deshalb müssen keine Altdaten übernommen werden.
- **Manuelle Prüfung des Zwischenstands `review-versions/0.1.3_2026-10-04`:**
  - Gestartet mit `TEST_DATA_PATH` auf `%TEMP%\tankatlas-acceptance-step4`.
  - Optionen geöffnet: Standardwerte korrekt.
  - GPS auf „Nie“ und Sortierung auf „Name“ gestellt, Super E10 abgewählt.
  - App beendet und neu gestartet: Alle drei Änderungen waren erhalten.
  - Im Datenverzeichnis lag ausschließlich `tankatlas.db`.
  - Das temporäre Verzeichnis wurde danach entfernt; es läuft kein App-Prozess mehr.

**Weitere Beobachtungen:**

- **Designentwurf:** Er enthält keinen eigenen Bildschirm „Optionen“, nur den Navigationseintrag „tune“/„einstellungen“. Die Seite verwendet die Design-Tokens des Projekts (Karten, Farben, Typografie, Hell-/Dunkelmodus, 44-px-Touch-Ziele). Mehr lässt sich gegen den Entwurf nicht prüfen.
- **EXE-Eigenschaften:**
  - `FileDescription` lautet weiterhin `Tankradar.MAUI`. Diesen Wert zeigen der Task-Manager und die Spalte „Dateibeschreibung“ im Explorer an. Gefordert war nur `<Product>`; eine zusätzliche `<AssemblyTitle>Tankatlas</AssemblyTitle>` würde den Namen auch dort zeigen.
  - `ProductVersion` des Zwischenstands ist `0.1.0+f6b28df…`, obwohl der Ordner `0.1.3` heißt. Der Build stammt aus dem Arbeitsstand vor dem Feature-Commit.
- **Build-Warnungen:** Der iOS-Build meldet 33 XamlC-Warnungen XC0022, alle in `SettingsPage.xaml` (fehlendes `x:DataType`, Bindungen nicht kompiliert). Sie blockieren nicht, sind aber leicht zu beheben.
- **Diagnosedaten:** Die E2E-Läufe haben Diagnosedaten unter `e2e-diagnostics/` und `TestResults/` hinterlassen. Beide Verzeichnisse sind gitignored.
