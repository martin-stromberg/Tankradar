# Nachbesserung Entwicklungsschritt 1, Runde 1 – Tankradar Schritt 1 Grundgerüst

## Fachliche Zusammenfassung

Aus der Abnahme von Entwicklungsschritt 1 („App-Grundgerüst, Navigation und Design-System") ergeben sich drei fachliche Nachbesserungen am bereits committeten MAUI-Grundgerüst: (1) Das Design-System muss die vorgeschriebenen Schriften Inter und JetBrains Mono tatsächlich einbinden und auf alle betroffenen Textstile anwenden, statt auf OpenSans bzw. die Plattform-Systemschrift zurückzufallen. (2) Die Hauptnavigation (`AppShell`) muss optisch dem Designentwurf entsprechen (Teal-Akzent für den aktiven Tab, Surface-Hintergrund, Unterstützung für Hell- und Dunkelmodus) statt den Plattform-Standard-Look zu zeigen. (3) Die App braucht einen zentralen, testbaren Mechanismus, der ihr Datenverzeichnis anhand der bereits vom E2E-Testprojekt gesetzten Umgebungsvariable `TEST_DATA_PATH` umlenkt, damit E2E-Tests nachweislich nicht das Datenverzeichnis des Entwicklungs- oder Echtbetriebs verwenden. Ergänzend wird der E2E-Navigationstest um belastbare, seitenspezifische Assertions erweitert und App-Icon/Splash-Screen werden von der MAUI-Vorlagenfarbe auf die Teal-Primärfarbe des Design-Systems umgestellt, inklusive Entfernung des ungenutzten Vorlagenbilds.

## Betroffene Klassen und Komponenten

### Datenmodellklassen
- Keine neuen Modellklassen erforderlich.

### Logikklassen / Services
- **Neuer Service zur Datenverzeichnis-Auflösung** (Arbeitstitel `AppDataPathProvider`, Namespace `Tankradar.MAUI.Services`): liest die Umgebungsvariable `TestDataPaths.TestDataPathEnvironmentVariable` (`TEST_DATA_PATH`, definiert in `src/TestSupport/TestDataPaths.cs`, bereits vom bestehenden Integrationstestprojekt über `TestDataContext` genutzt) aus; ist sie gesetzt, wird dieses Verzeichnis als App-Datenverzeichnis verwendet, andernfalls das reguläre Plattform-Datenverzeichnis (`FileSystem.AppDataDirectory`). Dieser Service ist die zentrale Stelle, über die alle künftigen Datenzugriffe ab Entwicklungsschritt 4 das Datenverzeichnis ermitteln müssen.
- **`MauiProgram.CreateMauiApp()`** (Erweiterung, `src/Tankradar.MAUI/MauiProgram.cs`):
  - `ConfigureFonts(...)`: zwei zusätzliche `fonts.AddFont(...)`-Aufrufe für Inter (Regular, SemiBold) und zwei für JetBrains Mono (Regular, Medium/SemiBold), mit Alias-Namen analog zum bestehenden Muster (`OpenSansRegular`, `OpenSansSemibold`).
  - `builder.Services`: Registrierung des neuen Datenverzeichnis-Services (Singleton).
  - Der erklärende Kommentar über dem `ConfigureFonts`-Block, der den aktuellen Fallback-Zustand beschreibt, muss entsprechend angepasst/entfernt werden.

### Interfaces
- **Abstraktion für die Datenverzeichnis-Auflösung** (Arbeitstitel `IAppDataPathProvider`): eine Methode/Property zur Ermittlung des aktuell gültigen App-Datenverzeichnisses (z. B. `string GetDataDirectory()`). Wird über DI injiziert, damit Aufrufer (ab Schritt 4) nicht selbst `Environment.GetEnvironmentVariable` aufrufen und damit die Testisolation umgehen können.

### Enums
- Keine betroffen.

### UI-Komponenten / Ressourcen
- **`src/Tankradar.MAUI/AppShell.xaml`**: Ergänzung von Shell-Farbwerten auf dem Wurzelelement `<Shell>`, jeweils mit `AppThemeBinding` auf die vorhandenen Design-Token, u. a.:
  - Hintergrund der TabBar → `ColorSurfaceLight` / `ColorSurfaceDark`
  - aktiver Tab (Titel/Icon) → `ColorPrimaryTeal` (Halbfett gemäß Entwurf)
  - inaktive Tabs → `ColorTextSecondaryLight` / `ColorTextSecondaryDark` (im Entwurf `on-surface-variant`; im vorhandenen Farbsystem gibt es keinen eigenen Token dieses Namens, `ColorTextSecondary*` ist die nächstliegende vorhandene Entsprechung – siehe „Offene Fragen")
  
  Hinweis: Die exakten Shell-Attribute (`Shell.TabBarBackgroundColor`, `Shell.TabBarForegroundColor`, `Shell.TabBarTitleColor`, `Shell.TabBarUnselectedColor`, ggf. weitere) sind beim Implementieren gegen die aktuelle .NET-MAUI-Version zu verifizieren.
- **`src/Tankradar.MAUI/Resources/DesignSystem.xaml`** (Überarbeitung):
  - Style `label-code`: `FontFamily` von der plattformabhängigen `OnPlatform`-Umschaltung (Consolas/Menlo) auf den registrierten JetBrains-Mono-Alias ändern.
  - Styles `headline-xl`, `headline-lg`, `headline-md`, `body-lg`, `body-md`, `body-sm`, `price-hero`: bislang **ohne** `FontFamily`-Setter (Systemschrift) – jeweils `FontFamily` explizit auf den passenden Inter-Alias setzen (Regular für Fließtext, SemiBold für Headlines/`price-hero`, konsistent mit dem bereits für OpenSans etablierten Namensschema).
- **`src/Tankradar.MAUI/Resources/Fonts/`**: vier neue TTF-Dateien (Inter Regular + SemiBold v4.1, JetBrains Mono Regular + SemiBold/Medium v2.304) plus Lizenzdatei(en) der SIL Open Font License für beide Schriftfamilien. Die vorhandene projektweite Einbindung `<MauiFont Include="Resources\Fonts\*" />` (`Tankradar.MAUI.csproj`) erfasst neue Dateien im Ordner automatisch, ohne dass die `.csproj` geändert werden muss.
- **`src/Tankradar.MAUI/Tankradar.MAUI.csproj`**: Farbattribut `Color="#512BD4"` bei `<MauiIcon ...>` und `<MauiSplashScreen ...>` auf den Hex-Wert der Teal-Primärfarbe (`ColorPrimaryTeal`, `#0F766E`) ändern; Zeile `<MauiImage Update="Resources\Images\dotnet_bot.png" ...>` entfernen.
- **`src/Tankradar.MAUI/Resources/Images/dotnet_bot.png`**: Datei löschen (ungenutztes MAUI-Vorlagenbild).

### Tests
- **Neue Unit-Testklasse** für den Datenverzeichnis-Service (Projekt `src/Tankradar.Tests.Unit`, Namensschema analog `BaseViewModelTests_PropertyBinding`):
  - Fall: `TEST_DATA_PATH` gesetzt → Service liefert genau diesen Pfad.
  - Fall: `TEST_DATA_PATH` nicht gesetzt → Service liefert das Standard-Plattformverzeichnis (bzw. verhält sich dokumentiert deterministisch, siehe „Offene Fragen" zur Testbarkeit von `FileSystem.AppDataDirectory` außerhalb eines laufenden MAUI-Hosts).
- **`src/Tankradar.Tests.E2E/E2E/FlaUI/NavigationE2ETests.cs`** (Überarbeitung): Die bestehende Assertion `MainWindow.FindFirstDescendant(cf => cf.ByName(tabTitle))` nach jedem Tab-Wechsel ist laut Abnahme schwach, da sie auch das Tab-Element selbst treffen kann und damit einen tatsächlichen Seitenwechsel nicht beweist. Stattdessen: nach jedem Klick auf einen Tab ein seitenspezifisches Element über `AutomationId` suchen (z. B. die Headline-`Label` je Seite). Voraussetzung: Die vier Views (`FavoritesPage.xaml`, `MapPage.xaml`, `TankbookPage.xaml`, `SettingsPage.xaml`) haben aktuell **keine** `AutomationId`-Werte gesetzt; diese müssen je Seite ergänzt werden (z. B. auf dem Headline-`Label`), damit der Test sie eindeutig referenzieren kann.

### Dokumentation
- **`README.md`**, Abschnitt „Bekannte Einschränkungen": Der TODO-Absatz zu fehlenden Inter-/JetBrains-Mono-Schriften ist nach Umsetzung zu entfernen (Hinweis auf Consolas/Menlo-Fallback und fehlenden Internetzugriff entfällt damit ebenfalls).

## Implementierungsansatz

1. **Schriften beschaffen und einbinden**: Inter v4.1 (Regular, SemiBold) von `github.com/rsms/inter/releases` sowie JetBrains Mono v2.304 (Regular, SemiBold/Medium) von `github.com/JetBrains/JetBrainsMono/releases` laden, TTF-Dateien nach `src/Tankradar.MAUI/Resources/Fonts/` legen, zugehörige OFL-Lizenztexte mit ablegen. Fonts in `MauiProgram.CreateMauiApp()` per `fonts.AddFont(...)` mit Alias registrieren. `DesignSystem.xaml` so anpassen, dass jeder betroffene Textstil (`headline-*`, `body-*`, `price-hero`, `label-code`) explizit den passenden Font-Alias über `FontFamily` referenziert – Konsistenzprüfung: kein Textstil darf mehr implizit auf die Systemschrift zurückfallen.
2. **Navigation stylen**: In `AppShell.xaml` Shell-Farbattribute ergänzen und über `AppThemeBinding` an die vorhandenen Light-/Dark-Farbtoken aus `DesignSystem.xaml` binden (`ColorSurfaceLight/Dark`, `ColorPrimaryTeal`, `ColorTextSecondaryLight/Dark` bzw. den in den „Offenen Fragen" zu klärenden Ersatz für `on-surface-variant`). Optisches Ziel: aktiver Tab in Teal/halbfett, inaktive Tabs gedämpft, Hintergrund mit Surface-Farbe und Schatten (sofern von der MAUI-Shell auf der Zielplattform unterstützt), konsistent zu Licht-/Dunkelmodus.
3. **Datenverzeichnis zentralisieren**: Neues Interface + Implementierung erstellen, die `TestDataPaths.TestDataPathEnvironmentVariable` auswertet (Wiederverwendung der bestehenden Konstante aus `src/TestSupport/TestDataPaths.cs`, die bereits vom Integrationstestprojekt genutzt wird – keine Dopplung des Variablennamens). Registrierung im DI-Container von `MauiProgram.cs`. Da `src/Tankradar.MAUI` aktuell noch keine eigenen Datenzugriffe hat (diese kommen erst ab Schritt 4), besteht die Aufgabe in diesem Schritt darin, den Mechanismus bereitzustellen und per Unit-Test abzusichern – nicht darin, bestehenden Code umzustellen, da keiner existiert.
4. **E2E-Test härten**: `AutomationId` auf den seitenspezifischen Headline-Labels der vier Views setzen; `NavigationE2ETests.AppStartsAndNavigatesThroughAllTabs()` so erweitern, dass nach jedem Tab-Klick gezielt nach dieser `AutomationId` gesucht wird statt nach dem (mehrdeutigen) Tab-Titel-Namen.
5. **Icon/Splash umstellen**: `Color`-Attribute in der `.csproj` auf die Teal-Primärfarbe ändern, `dotnet_bot.png`-Referenz und -Datei entfernen.
6. **Dokumentation nachziehen**: README-TODO-Eintrag entfernen.
7. **Review-Zwischenstand**: Nach Abschluss aller Punkte `scripts/create-review-version.ps1` erneut ausführen, um einen aktualisierten startfähigen Windows-Zwischenstand unter `review-versions/` abzulegen (nicht committen, siehe Rahmenbedingungen).

## Konfiguration

- **Umgebungsvariable `TEST_DATA_PATH`** (Konstante `TestDataPaths.TestDataPathEnvironmentVariable`): bleibt eine laufzeitseitige, nicht persistierte Konfiguration, die ausschließlich zur Testisolation dient. Sie wird von `E2ETestBase` vor App-Start gesetzt und muss vom neuen App-seitigen Service gelesen werden. Kein UI- oder Anwendungseinstellungs-Bezug notwendig.
- **Farb-/Font-Token**: Keine neue Konfigurationsebene nötig; alle Werte bleiben statische Design-Token in `DesignSystem.xaml` bzw. Attribute in der `.csproj` (Icon/Splash-Farbe), wie bereits im bestehenden Design-System etabliert.

## Offene Fragen

1. **Fehlender Farb-Token für `on-surface-variant`**: `DesignSystem.xaml` definiert aktuell `ColorTextSecondaryLight`/`ColorTextSecondaryDark`, aber keinen expliziten `on-surface-variant`-Token wie im HTML-Designentwurf. Reicht `ColorTextSecondary*` als Entsprechung für die inaktiven Navigationseinträge, oder soll ein neuer, wörtlich benannter Token ergänzt werden?
2. **Exakte MAUI-Shell-API**: Welche `Shell.*`-Bindable-Properties (`TabBarBackgroundColor`, `TabBarForegroundColor`, `TabBarTitleColor`, `TabBarUnselectedColor`, ggf. `TabBarShadow`/`TabBarDisabledColor`) sind in der im Projekt verwendeten .NET-MAUI-Version tatsächlich verfügbar und wie weit lässt sich damit der im Entwurf gezeigte Schatten auf der Windows-Zielplattform (primäres Build-/Testziel laut `E2ETestBase`) überhaupt nachbilden?
3. **Schriftgewichte JetBrains Mono**: Reicht „Regular" für `label-code`, oder wird zusätzlich ein Medium/SemiBold-Schnitt für hervorgehobenen Code-Text benötigt? (Bisher nutzt `label-code` nur einen Schnitt.)
4. **Lizenzablage**: Sollen die OFL-Lizenztexte beider Schriften jeweils separat neben den Fonts abgelegt werden (z. B. `OFL-Inter.txt`, `OFL-JetBrainsMono.txt`) oder zusammengeführt, und soll README/Projektdokumentation einen Hinweis auf die Lizenzpflicht erhalten?
5. **Testbarkeit von `FileSystem.AppDataDirectory` in Unit-Tests**: Dieser MAUI-Essentials-Aufruf setzt typischerweise eine initialisierte Plattform voraus und lässt sich in reinen Unit-Tests (`Tankradar.Tests.Unit`, kein MAUI-Host) unter Umständen nicht direkt aufrufen. Muss der Default-Pfad hinter einer weiteren, im Unit-Test mockbaren Abstraktion liegen, oder wird der „Kein `TEST_DATA_PATH` gesetzt"-Fall nur indirekt (z. B. über die reine Verzweigungslogik ohne echten Aufruf von `FileSystem.AppDataDirectory`) abgesichert?
6. **AutomationId-Namenskonvention**: Gibt es eine projektweite Namenskonvention für `AutomationId`-Werte (z. B. `FavoritesPage.Headline`), oder ist sie für dieses Nachbesserungsticket frei wählbar?
