# Bestandsaufnahme: Nachbesserung Entwicklungsschritt 1, Runde 1 – Tankradar Schritt 1 Grundgerüst

Analysiert wurde der bereits committete Code des MAUI-Grundgerüsts (`src/Tankradar.MAUI`,
`src/Tankradar.Tests.Unit`, `src/Tankradar.Tests.E2E`, `src/Tankradar.Tests.Integration`,
`src/TestSupport`) bezogen auf die drei geforderten Nachbesserungen: Font-Einbindung (Inter/JetBrains
Mono), Shell-Styling nach Designentwurf, und ein zentraler, testbarer Datenverzeichnis-Service.

## Zusammenfassung

- **Fonts (Inter/JetBrains Mono):** `MauiProgram.ConfigureFonts(...)` registriert aktuell nur
  `OpenSansRegular`/`OpenSansSemibold`; keine Inter-/JetBrains-Mono-Aufrufe. `Resources/Fonts/`
  enthält nur die beiden OpenSans-TTFs, keine Inter-/JetBrains-Mono-Dateien, keine Lizenztexte.
  In `DesignSystem.xaml` haben alle sieben `headline-*`/`body-*`/`price-hero`-Styles **keinen**
  `FontFamily`-Setter (Systemschrift-Fallback); `label-code` hat einen `FontFamily`-Setter, aber
  über `OnPlatform` auf `Consolas`/`Menlo-Regular`, nicht auf einen registrierten Font-Alias.
- **Shell-Styling:** `AppShell.xaml` setzt auf dem `<Shell>`-Root **keine** Farbattribute
  (`TabBarBackgroundColor`/-`ForegroundColor`/-`TitleColor`/-`UnselectedColor` fehlen komplett);
  die App zeigt den Plattform-Standard-Look. Alle benötigten Farb-Token (`ColorPrimaryTeal`,
  `ColorSurfaceLight/Dark`, `ColorTextSecondaryLight/Dark`) existieren bereits in
  `DesignSystem.xaml`; ein Token namens `on-surface-variant` (wie im HTML-Designentwurf) existiert
  nicht.
- **Datenverzeichnis-Service:** Es existiert **keine** Klasse `AppDataPathProvider` oder
  `IAppDataPathProvider` im Repository — weder im `Tankradar.MAUI`-Projekt noch anderswo. Die
  Konstante `TestDataPaths.TestDataPathEnvironmentVariable` (`"TEST_DATA_PATH"`) existiert bereits
  in `src/TestSupport/TestDataPaths.cs` und wird schon heute von `E2ETestBase` (setzt die
  Variable vor App-Start) und `TestDataContext` (Integrationstests) genutzt — aber `Tankradar.MAUI`
  selbst liest diese Variable noch nirgends.
- **E2E-Test-Härtung:** `NavigationE2ETests.AppStartsAndNavigatesThroughAllTabs` prüft nach jedem
  Tab-Klick aktuell nur `MainWindow.FindFirstDescendant(cf => cf.ByName(tabTitle))` — dieselbe
  Assertion, die laut Abnahme zu schwach ist. Keine der vier Views
  (`FavoritesPage.xaml`, `MapPage.xaml`, `TankbookPage.xaml`, `SettingsPage.xaml`) setzt aktuell
  eine `AutomationId` auf ihrem Headline-`Label` oder einem anderen Element.
- **Icon/Splash/README:** `Tankradar.MAUI.csproj` verwendet für `MauiIcon`/`MauiSplashScreen` noch
  `Color="#512BD4"` (MAUI-Standardvorlagenfarbe), nicht die Teal-Primärfarbe `#0F766E`;
  `<MauiImage Update="Resources\Images\dotnet_bot.png" .../>` ist vorhanden, die Datei
  `dotnet_bot.png` existiert im Ordner. `README.md` enthält im Abschnitt „Bekannte Einschränkungen“
  weiterhin den TODO-Absatz zu den fehlenden Schriften.
- **Designentwurf:** `design-draft/` enthält nur die ZIP `stitch_smart_fuel_charge_tracker.zip`
  (kein entpackter Ordner). Die enthaltene `startseite_favoriten_gruppen/code.html` (Light) und
  `_dark_mode`-Variante belegen die im Entwurf verwendeten Nav-Farben (u. a. `on-surface-variant`
  = `#3e4947` Light / `#9ca3af` Dark) — Details in [ui-resources.md](inventory/ui-resources.md).

**Test-Ausgangszustand:** Alle 8 im Projekt vorhandenen Tests (3 Integration, 4 Unit, 1 E2E) sind
im Ausgangslauf grün (`dotnet test Tankradar.sln`, Exit-Code 0, 0 Fehlschläge, 0 übersprungen).
Es gibt keine nachgewiesenen bestehenden Testfehler. Testlücke: Für den neuen
Datenverzeichnis-Service existiert erwartungsgemäß noch keine Testklasse, da der Service selbst
noch nicht existiert. Details und Nachweise: [tests.md](inventory/tests.md).

## Details

- [Logik (`MauiProgram`, `AppConfiguration`, `TestDataPaths`, fehlender Datenverzeichnis-Service)](inventory/logic.md)
- [UI-Ressourcen und Design-System (`AppShell.xaml`, `DesignSystem.xaml`, `.csproj`, Fonts/Images, README, Designentwurf)](inventory/ui-resources.md)
- [Tests (Ausgangszustand, Testklassen, Hilfsmethoden)](inventory/tests.md)

Kein Abschnitt für Datenmodell, Enums oder Interfaces: Die Anforderung nennt keine neuen oder
betroffenen Modellklassen/Enums, und es existiert aktuell kein Interface im Repository, das die
geplante `IAppDataPathProvider`-Abstraktion vorwegnimmt oder anderweitig betroffen wäre (siehe
[logic.md](inventory/logic.md), Abschnitt „Datenverzeichnis-Service“).
