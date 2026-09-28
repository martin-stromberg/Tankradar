# Umsetzungsplan: Nachbesserung Entwicklungsschritt 1, Runde 1 – Tankradar Schritt 1 Grundgerüst

## Übersicht

Drei fachliche Nachbesserungen am bereits committeten MAUI-Grundgerüst werden umgesetzt: (1) tatsächliche Einbindung der Schriften Inter und JetBrains Mono statt OpenSans-/Systemschrift-Fallback, (2) Styling der `AppShell`-Tab-Leiste nach Designentwurf (Teal-Akzent, Surface-Hintergrund, Light-/Dark-Mode-Unterstützung) über die bereits vorhandenen Design-Token, und (3) ein zentraler, per Unit-Test abgesicherter `IAppDataPathProvider`-Service, der das App-Datenverzeichnis anhand der bestehenden Umgebungsvariable `TEST_DATA_PATH` (Konstante `TestDataPaths.TestDataPathEnvironmentVariable`) umlenkt. Ergänzend wird der bestehende E2E-Navigationstest um belastbare, `AutomationId`-basierte Assertions gehärtet und Icon/Splash-Screen werden von der MAUI-Vorlagenfarbe auf die Teal-Primärfarbe umgestellt.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Farb-Token für inaktive Nav-Einträge (`on-surface-variant`) | Wiederverwendung von `ColorTextSecondaryLight`/`ColorTextSecondaryDark`; **kein** neuer Token | `DesignSystem.xaml` hat keinen Token namens `on-surface-variant`; `ColorTextSecondary*` bildet denselben gedämpften Textzweck ab. Ein wortgleicher neuer Token wäre reine Namensverdopplung ohne fachlichen Mehrwert und widerspricht dem Grundsatz, bestehende Token wiederzuverwenden. |
| `Shell.*`-Bindable-Properties für die TabBar | `Shell.TabBarBackgroundColor`, `Shell.TabBarForegroundColor`, `Shell.TabBarTitleColor`, `Shell.TabBarUnselectedColor` | Per Binäranalyse von `Microsoft.Maui.Controls.dll` Version `10.0.20` (exakt die im Projekt referenzierte Version, siehe `project.assets.json`) verifiziert: Es existieren genau `TabBarBackgroundColor`, `TabBarBackgroundColorProperty`, `TabBarDisabledColor`, `TabBarForegroundColor`, `TabBarIsVisible`, `TabBarTitleColor`, `TabBarUnselectedColor` (jeweils mit `...Property`-Pendant). Es existiert **keine** `Shell.TabBar*Shadow*`- oder `Shell.TabBar*Font*`/`*Bold*`-Property in dieser Version. Der im Entwurf gezeigte Schatten und die Halbfett-Schrift des aktiven Tab-Titels sind mit den vorhandenen Shell-Attached-Properties **nicht** nachbildbar; ein Custom-Renderer/-Handler dafür ist für das Grundgerüst außerhalb des Anforderungsumfangs (siehe Seiteneffekte/Risiken) und wird bewusst **nicht** umgesetzt. Farblich (Hintergrund, aktiver/inaktiver Text- und Icon-Ton) wird der Entwurf vollständig über die vier genannten Properties abgebildet. |
| JetBrains-Mono-Schriftschnitte | Beide Schnitte (Regular + SemiBold) registrieren; `label-code` referenziert weiterhin nur `JetBrainsMonoRegular` | Die Anforderung selbst schreibt in „Betroffene Klassen" bereits vor, zwei `AddFont`-Aufrufe für JetBrains Mono (Regular, Medium/SemiBold) anzulegen. Da aktuell nur ein einziger Style (`label-code`) diese Schriftfamilie nutzt und keine hervorgehobene Code-Variante im Entwurf ersichtlich ist, wird SemiBold nur **registriert** (für spätere Schritte verfügbar), aber in diesem Schritt an keinem Style verdrahtet – vermeidet spekulative neue Styles ohne aktuellen Bedarf. |
| Ablage der OFL-Lizenztexte | Zwei separate Dateien `OFL-Inter.txt` und `OFL-JetBrainsMono.txt` direkt in `src/Tankradar.MAUI/Resources/Fonts/`; keine zusätzliche README-Lizenzsektion | Separate Dateien je Schriftfamilie sind eindeutig zuordenbar und vermeiden eine zusammengeführte Datei mit unklarer Attribution. Der Anforderungstext verlangt im Abschnitt „Dokumentation" ausschließlich das Entfernen des bestehenden Font-TODOs in `README.md` – eine zusätzliche Lizenzsektion ist nicht gefordert und wird nicht ergänzt, um den Umfang nicht auszuweiten. |
| Testbarkeit des Standard-Datenverzeichnisses (`FileSystem.AppDataDirectory`) ohne MAUI-Host | `AppDataPathProvider` erhält per Konstruktor-Injektion eine `Func<string>`-Factory für das Standardverzeichnis (Produktionsdefault: `() => FileSystem.AppDataDirectory`), die **nur** aufgerufen wird, wenn `TEST_DATA_PATH` nicht gesetzt ist | `FileSystem.AppDataDirectory` (Microsoft.Maui.Essentials) setzt einen initialisierten MAUI-Host voraus und ist in reinen xUnit-Läufen ohne laufende App nicht sicher aufrufbar. Durch Injektion einer austauschbaren Factory (Gateway-Pattern mit ersetzbarer Strategie für den Default-Pfad) kann der Unit-Test die Verzweigungslogik vollständig prüfen (inkl. „nicht gesetzt"-Fall), ohne den echten MAUI-Essentials-Aufruf auszulösen. In `MauiProgram.cs` wird der parameterlose Produktionskonstruktor verwendet, der intern den echten `FileSystem.AppDataDirectory`-Aufruf kapselt. |
| Namenskonvention `AutomationId` | `{PageName}.Headline`, z. B. `FavoritesPage.Headline`, `MapPage.Headline`, `TankbookPage.Headline`, `SettingsPage.Headline` | Vom Anforderungstext selbst als Beispiel vorgeschlagen; konsistent, kollisionsfrei über alle vier Views und sofort verständlich für künftige E2E-Tests weiterer Elemente (`{PageName}.{Elementrolle}`). |
| Kompilierung von `TestDataPaths.cs` in `Tankradar.MAUI` | `<Compile Include="..\TestSupport\TestDataPaths.cs" Link="TestSupport\TestDataPaths.cs" />` in `Tankradar.MAUI.csproj`, analog zu `Tankradar.Tests.Integration.csproj`/`Tankradar.Tests.E2E.csproj` | `TestSupport` ist kein eigenständiges Projekt, sondern eine lose Datei, die von den Testprojekten bereits per `Compile Include`-Verlinkung eingebunden wird. Dasselbe etablierte Muster wird für `Tankradar.MAUI` übernommen, statt ein neues `TestSupport`-Projekt/eine `ProjectReference` einzuführen oder den Variablennamen zu duplizieren. |

## Programmabläufe

### Ermittlung des App-Datenverzeichnisses

1. `MauiProgram.CreateMauiApp()` registriert `IAppDataPathProvider` → `AppDataPathProvider` als Singleton im DI-Container.
2. Ein Aufrufer (ab Entwicklungsschritt 4) lässt sich `IAppDataPathProvider` injizieren und ruft `GetDataDirectory()` auf.
3. `AppDataPathProvider.GetDataDirectory()` liest `Environment.GetEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable)`.
4. Ist der Wert gesetzt und nicht leer, wird genau dieser Pfad zurückgegeben.
5. Andernfalls wird die injizierte `_defaultDirectoryFactory` (Produktionsdefault: `() => FileSystem.AppDataDirectory`) aufgerufen und deren Ergebnis zurückgegeben.

Beteiligte Klassen/Komponenten: `IAppDataPathProvider`, `AppDataPathProvider`, `TestDataPaths`, `MauiProgram`.

Hinweis: `E2ETestBase` setzt `TEST_DATA_PATH` bereits heute vor App-Start (unverändert durch diesen Schritt); mit diesem Ablauf liest `Tankradar.MAUI` diese Variable erstmals selbst.

### Schriftregistrierung und -anwendung

1. `MauiProgram.CreateMauiApp()` registriert in `ConfigureFonts(...)` zusätzlich zu den bestehenden zwei OpenSans-Aufrufen vier weitere Aufrufe: `fonts.AddFont("Inter-Regular.ttf", "InterRegular")`, `fonts.AddFont("Inter-SemiBold.ttf", "InterSemibold")`, `fonts.AddFont("JetBrainsMono-Regular.ttf", "JetBrainsMonoRegular")`, `fonts.AddFont("JetBrainsMono-SemiBold.ttf", "JetBrainsMonoSemibold")`.
2. `DesignSystem.xaml` referenziert je Style den passenden Alias über `FontFamily` (siehe Tabelle unten); `FontAttributes` (z. B. `Bold` bei den Headline-Styles) bleibt unverändert bestehen.
3. Beim Rendern eines `Label` mit einem der betroffenen Styles löst MAUI den Alias über die in Schritt 1 registrierte Zuordnung zur physischen TTF-Datei auf.

| Style | Neuer `FontFamily`-Alias |
|-------|---------------------------|
| `headline-xl`, `headline-lg`, `headline-md` | `InterSemibold` |
| `body-lg`, `body-md`, `body-sm` | `InterRegular` |
| `price-hero` | `InterSemibold` |
| `label-code` | `JetBrainsMonoRegular` (ersetzt den bisherigen `OnPlatform`-Block) |

Beteiligte Klassen/Komponenten: `MauiProgram`, `DesignSystem.xaml`.

### Navigations-Styling (AppShell)

1. `AppShell.xaml` setzt auf dem `<Shell>`-Root vier zusätzliche Attribute mit `AppThemeBinding`:
   - `Shell.TabBarBackgroundColor` → `ColorSurfaceLight` / `ColorSurfaceDark`
   - `Shell.TabBarForegroundColor` → `ColorPrimaryTeal` (kein Light/Dark-Unterschied, da der Token einwertig ist)
   - `Shell.TabBarTitleColor` → `ColorPrimaryTeal`
   - `Shell.TabBarUnselectedColor` → `ColorTextSecondaryLight` / `ColorTextSecondaryDark`
2. Die MAUI-Shell wendet diese Werte plattformspezifisch auf den nativen Tab-Bar-Renderer an (auf Windows: WinUI-`NavigationView`-Ableitung).

Beteiligte Klassen/Komponenten: `AppShell.xaml`, `DesignSystem.xaml` (Token-Quelle).

### Seitenspezifische E2E-Navigationsprüfung

1. `NavigationE2ETests.AppStartsAndNavigatesThroughAllTabs()` klickt wie bisher nacheinander jeden Tab über `FindTab(tabTitle)`.
2. Statt `MainWindow.FindFirstDescendant(cf => cf.ByName(tabTitle))` sucht der Test nach jedem Klick gezielt `MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(expectedAutomationId))`, wobei `expectedAutomationId` aus einer Zuordnungstabelle `tabTitle → "{PageName}.Headline"` stammt.
3. Der Test schlägt fehl, wenn nach dem Tab-Klick kein Element mit der erwarteten `AutomationId` gefunden wird — beweist damit einen tatsächlichen Seitenwechsel statt nur eine Namensübereinstimmung.

Beteiligte Klassen/Komponenten: `NavigationE2ETests`, `FavoritesPage.xaml`, `MapPage.xaml`, `TankbookPage.xaml`, `SettingsPage.xaml`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `IAppDataPathProvider` | Interface | Abstraktion zur Ermittlung des aktuell gültigen App-Datenverzeichnisses; wird per DI injiziert, damit Aufrufer nicht selbst `Environment.GetEnvironmentVariable` aufrufen und die Testisolation umgehen können. |
| `AppDataPathProvider` | Klasse (Service), Namespace `Tankradar.MAUI.Services` | Implementiert `IAppDataPathProvider`; liest `TestDataPaths.TestDataPathEnvironmentVariable`, fällt bei nicht gesetzter Variable auf eine injizierbare `Func<string>`-Factory (Produktionsdefault `FileSystem.AppDataDirectory`) zurück. |

## Änderungen an bestehenden Klassen

### `MauiProgram` (statische Klasse)

- **Geänderte Methoden:** `CreateMauiApp()` — `ConfigureFonts(...)`-Block um vier `fonts.AddFont(...)`-Aufrufe für Inter (Regular, SemiBold) und JetBrains Mono (Regular, SemiBold) erweitert; der darüberstehende Erklärkommentar zum Fallback-Zustand wird entfernt bzw. durch eine kurze Beschreibung der jetzt vollständigen Font-Registrierung ersetzt. Zusätzlich neue Zeile `builder.Services.AddSingleton<IAppDataPathProvider, AppDataPathProvider>();` im bestehenden Registrierungsblock (Muster wie `AddSingleton<AppConfiguration>()`).

### `Tankradar.MAUI.csproj`

- `<MauiIcon ... Color="#512BD4" />` → `Color="#0F766E"` (Wert von `ColorPrimaryTeal`).
- `<MauiSplashScreen ... Color="#512BD4" ... />` → `Color="#0F766E"`.
- Zeile `<MauiImage Update="Resources\Images\dotnet_bot.png" Resize="True" BaseSize="300,185" />` wird entfernt.
- Neue Zeile `<Compile Include="..\TestSupport\TestDataPaths.cs" Link="TestSupport\TestDataPaths.cs" />` in einer `<ItemGroup>`, analog zu den bestehenden Test-Projekten.

### `src/Tankradar.MAUI/AppShell.xaml`

- Vier neue Attribute auf dem `<Shell>`-Root (siehe Programmablauf „Navigations-Styling“ oben). Keine Änderung an `AppShell.xaml.cs`.

### `src/Tankradar.MAUI/Resources/DesignSystem.xaml`

- Sieben Styles (`headline-xl`, `headline-lg`, `headline-md`, `body-lg`, `body-md`, `body-sm`, `price-hero`) erhalten je einen neuen `FontFamily`-Setter (siehe Tabelle im Programmablauf „Schriftregistrierung und -anwendung“).
- `label-code`: bisheriger `FontFamily`-Setter mit `OnPlatform`-Block (Consolas/Menlo) wird durch `<Setter Property="FontFamily" Value="JetBrainsMonoRegular" />` ersetzt.

### `src/Tankradar.MAUI/Views/FavoritesPage.xaml`, `MapPage.xaml`, `TankbookPage.xaml`, `SettingsPage.xaml`

- Jeweils das Headline-`Label` (Style `headline-lg`) erhält ein neues Attribut `AutomationId="{PageName}.Headline"` (z. B. `AutomationId="FavoritesPage.Headline"`).

### `src/Tankradar.MAUI/Resources/Images/`

- Datei `dotnet_bot.png` wird gelöscht.

### `src/Tankradar.Tests.E2E/E2E/FlaUI/NavigationE2ETests.cs`

- **Geänderte Methoden:** `AppStartsAndNavigatesThroughAllTabs()` — statt `MainWindow.FindFirstDescendant(cf => cf.ByName(tabTitle))` wird nach jedem Tab-Klick `MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(expectedAutomationId))` verwendet; eine neue private, statische Zuordnung (z. B. `Dictionary<string, string>` oder parallele Arrays) `tabTitle → automationId` wird ergänzt (`"Favoriten" → "FavoritesPage.Headline"` usw.).

### `README.md`

- Abschnitt „Bekannte Einschränkungen“: erster Punkt (Zeilen 117–124, Font-TODO) wird vollständig entfernt; die beiden übrigen Punkte (iOS-Build, Android/MacCatalyst) bleiben unverändert.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine neuen Konfigurationseinträge. `TEST_DATA_PATH` (Konstante `TestDataPaths.TestDataPathEnvironmentVariable`) ist bereits vorhanden und wird von `Tankradar.MAUI` erstmals gelesen, ohne selbst neu eingeführt zu werden.

## Seiteneffekte und Risiken

- **Layout/Textumbruch durch Font-Wechsel:** Inter und JetBrains Mono haben andere Zeichenmetriken als OpenSans/Systemschrift bzw. Consolas/Menlo. Auf den vier aktuell rein platzhalterhaften Views ist das Risiko gering, sollte aber im manuellen Review-Zwischenstand (Schritt „Review-Zwischenstand“ in der Umsetzungsreihenfolge) visuell geprüft werden.
- **Plattformabhängige Wirkung der `Shell.TabBar*`-Properties:** Ob `TabBarForegroundColor`/`TabBarTitleColor`/`TabBarUnselectedColor` auf der Windows-Zielplattform (primäres Testziel laut `E2ETestBase`) exakt wie im Entwurf sichtbar werden, lässt sich nicht automatisiert (FlaUI prüft keine Farben) nachweisen — Verifikation erfolgt visuell über den Windows-Review-Zwischenstand, nicht per Test.
- **Fehlende Schatten-/Fettschrift-Nachbildung:** Der im Designentwurf sichtbare Schatten der TabBar und die Halbfett-Darstellung des aktiven Tab-Titels sind mit den in MAUI 10.0.20 vorhandenen Shell-Attached-Properties nicht abbildbar (siehe Designentscheidungen) und werden als bekannte, akzeptierte Abweichung nicht umgesetzt.
- **Neue `Compile Include`-Verlinkung in `Tankradar.MAUI.csproj`:** Bringt den Namespace `Tankradar.TestSupport` in die Haupt-App-Assembly ein; da `TestDataPaths` ausschließlich eine Konstante enthält, ist das Kollisionsrisiko mit bestehenden Typen in `Tankradar.MAUI.Services` minimal.
- **`Tankradar.Tests.Unit` benötigt keine Projektanpassung:** Die bestehende `ProjectReference` auf `Tankradar.MAUI.csproj` macht `IAppDataPathProvider`/`AppDataPathProvider` automatisch für Unit-Tests verfügbar.

## Umsetzungsreihenfolge

1. **`TestDataPaths.cs` in `Tankradar.MAUI.csproj` verlinken**
   - Voraussetzungen: Keine.
   - Beschreibung: `<Compile Include="..\TestSupport\TestDataPaths.cs" Link="TestSupport\TestDataPaths.cs" />` ergänzen, analog zu den bestehenden Test-Projekten.

2. **`IAppDataPathProvider` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: Interface mit Methode `string GetDataDirectory()` im Namespace `Tankradar.MAUI.Services` erstellen, vollständig per XML-Doc-Kommentar dokumentiert.

3. **`AppDataPathProvider` implementieren**
   - Voraussetzungen: Schritt 1 (Zugriff auf `TestDataPaths`), Schritt 2 (Interface).
   - Beschreibung: Klasse mit zwei Konstruktoren — parameterlos (Produktionsdefault `() => FileSystem.AppDataDirectory`) und einer testbaren Überladung mit `Func<string> defaultDirectoryFactory` — implementiert `GetDataDirectory()` gemäß Programmablauf oben.

4. **DI-Registrierung in `MauiProgram.cs`**
   - Voraussetzungen: Schritt 3.
   - Beschreibung: `builder.Services.AddSingleton<IAppDataPathProvider, AppDataPathProvider>();` ergänzen.

5. **Unit-Testklasse für `AppDataPathProvider` schreiben**
   - Voraussetzungen: Schritt 3 (Klasse muss existieren); `Tankradar.Tests.Unit` referenziert `Tankradar.MAUI` bereits über `ProjectReference`.
   - Beschreibung: Testklasse nach Namensschema `AppDataPathProviderTests_DataDirectoryResolution` mit den zwei in „Tests“ beschriebenen Fällen.

6. **Font-Dateien und Lizenztexte ablegen**
   - Voraussetzungen: Keine.
   - Beschreibung: `Inter-Regular.ttf`, `Inter-SemiBold.ttf`, `JetBrainsMono-Regular.ttf`, `JetBrainsMono-SemiBold.ttf` sowie `OFL-Inter.txt` und `OFL-JetBrainsMono.txt` nach `src/Tankradar.MAUI/Resources/Fonts/` legen; `<MauiFont Include="Resources\Fonts\*" />` erfasst sie automatisch, keine `.csproj`-Änderung nötig.

7. **`MauiProgram.ConfigureFonts(...)` erweitern**
   - Voraussetzungen: Schritt 6 (TTF-Dateien müssen vorhanden sein).
   - Beschreibung: Vier `fonts.AddFont(...)`-Aufrufe ergänzen, Erklärkommentar über dem Block anpassen.

8. **`DesignSystem.xaml` `FontFamily`-Setter ergänzen**
   - Voraussetzungen: Schritt 7 (Alias-Namen müssen registriert sein).
   - Beschreibung: Sieben Styles um `FontFamily`-Setter ergänzen, `label-code` von `OnPlatform` auf `JetBrainsMonoRegular` umstellen.

9. **`AppShell.xaml` Shell-Farbattribute ergänzen**
   - Voraussetzungen: Keine (Token bereits vorhanden).
   - Beschreibung: Vier `Shell.TabBar*`-Attribute mit `AppThemeBinding` gemäß Programmablauf „Navigations-Styling“ setzen.

10. **Icon/Splash-Farbe und Vorlagenbild bereinigen**
    - Voraussetzungen: Keine.
    - Beschreibung: `Color`-Attribute in `Tankradar.MAUI.csproj` auf `#0F766E` ändern, `MauiImage Update`-Zeile für `dotnet_bot.png` entfernen, Datei `dotnet_bot.png` löschen.

11. **`AutomationId` auf Headline-Labels setzen**
    - Voraussetzungen: Keine.
    - Beschreibung: In den vier Views je Headline-`Label` `AutomationId="{PageName}.Headline"` ergänzen.

12. **`NavigationE2ETests` härten**
    - Voraussetzungen: Schritt 11 (AutomationIds müssen existieren).
    - Beschreibung: Assertion auf `ByAutomationId(expectedAutomationId)` umstellen, Zuordnungstabelle `tabTitle → automationId` ergänzen.

13. **README-TODO entfernen**
    - Voraussetzungen: Schritte 6–8 abgeschlossen (Fonts tatsächlich eingebunden).
    - Beschreibung: Font-TODO-Absatz aus „Bekannte Einschränkungen“ entfernen.

14. **Review-Zwischenstand aktualisieren**
    - Voraussetzungen: Alle vorherigen Schritte abgeschlossen.
    - Beschreibung: `scripts/create-review-version.ps1` erneut ausführen (Ergebnis nicht committen).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `GetDataDirectory_WithTestDataPathSet_ReturnsEnvironmentVariableValue` | `AppDataPathProviderTests_DataDirectoryResolution` (`Tankradar.Tests.Unit`) | Setzt `TEST_DATA_PATH` per `Environment.SetEnvironmentVariable`, prüft, dass `GetDataDirectory()` exakt diesen Wert liefert; Rücksetzen der Variable in `finally`. |
| `GetDataDirectory_WithoutTestDataPathSet_ReturnsDefaultDirectoryFactoryResult` | `AppDataPathProviderTests_DataDirectoryResolution` (`Tankradar.Tests.Unit`) | Stellt sicher, dass `TEST_DATA_PATH` nicht gesetzt ist, injiziert eine fest verdrahtete Test-`Func<string>` (z. B. `() => "C:\\FakeDefault"`) über die testbare Konstruktor-Überladung, prüft, dass genau dieser Wert zurückgegeben wird — ohne den echten `FileSystem.AppDataDirectory`-Aufruf auszulösen. |

### Betroffene bestehende Tests

Keine.

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | App startet, jeder Tab-Klick führt zu einem seitenspezifischen Element (`AutomationId`) statt nur einem namensgleichen Treffer | `NavigationE2ETests.AppStartsAndNavigatesThroughAllTabs` (`src/Tankradar.Tests.E2E/E2E/FlaUI/NavigationE2ETests.cs`) | Belastbarer Nachweis eines tatsächlichen Seitenwechsels bei Tab-Navigation | Nur ein laufender, per FlaUI gesteuerter Windows-Prozess kann beweisen, dass die native Shell-Navigation tatsächlich die richtige Seite anzeigt; ein Unit-/Integrationstest kann UI-Rendering und native Steuerelement-Bäume nicht prüfen. |

Welche bestehenden E2E-Tests müssen angepasst werden?

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `NavigationE2ETests.AppStartsAndNavigatesThroughAllTabs` | Ersetzt die schwache `ByName(tabTitle)`-Assertion durch eine `ByAutomationId(...)`-Assertion auf dem seitenspezifischen Headline-Label, wie in der Anforderung gefordert. |

Kein zusätzlicher E2E-Test für Font-/Farb-Styling und den `IAppDataPathProvider`-Service geplant: Schriftart- und Farbwerte sind über FlaUI nicht zuverlässig automatisiert prüfbar (kein Farbvergleich, keine Font-Introspektion in der verwendeten FlaUI-Version) — visuelle Korrektheit wird stattdessen über den manuellen Windows-Review-Zwischenstand (`scripts/create-review-version.ps1`) sichergestellt. Der `IAppDataPathProvider`-Service hat in diesem Schritt noch keinen produktiven Aufrufer (erste Nutzung erst ab Entwicklungsschritt 4) und erzeugt daher keinen über die UI beobachtbaren, E2E-testbaren Effekt; seine Korrektheit wird vollständig durch die zwei neuen Unit-Tests abgesichert.

## Offene Punkte

Keine.
