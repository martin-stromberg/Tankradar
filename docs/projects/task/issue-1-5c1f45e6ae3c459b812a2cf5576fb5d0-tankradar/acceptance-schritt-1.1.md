# Abnahmeprüfung – Entwicklungsschritt 1

## Ergebnis

**Status:** Abweichungen gefunden

## Abweichungen

- [ ] **Schriften Inter und JetBrains Mono fehlen.** Die Anforderung verlangt ausdrücklich die Schriften Inter und JetBrains Mono als Teil des Design-Systems. Der Code bindet sie nicht ein: `src/Tankradar.MAUI/Resources/Fonts/` enthält nur `OpenSans-Regular.ttf` und `OpenSans-Semibold.ttf`, `MauiProgram.cs` registriert nur diese beiden Schriften, und `label-code` in `Resources/DesignSystem.xaml` verwendet Consolas (Windows) bzw. Menlo (iOS). Außerdem setzen die Textstile (`headline-*`, `body-*`, `price-hero`) gar keine `FontFamily`. Sie nutzen also nicht einmal den dokumentierten OpenSans-Ersatz, sondern die Systemschrift der Plattform. Die Begründung „kein Internetzugriff" trägt nicht: Die Release-Seiten https://github.com/rsms/inter/releases (aktuell 4.1) und https://github.com/JetBrains/JetBrainsMono/releases (aktuell v2.304) waren bei der Prüfung erreichbar. Beide Schriften stehen unter der SIL Open Font License und dürfen mit der App ausgeliefert werden. Die Einschränkung ist in README.md („Bekannte Einschränkungen") als TODO dokumentiert, erfüllt die Anforderung aber nicht.
- [ ] **Die Hauptnavigation ist nicht nach dem Designentwurf gestaltet.** Laut Anforderung zeigt die App die Hauptnavigation „wie im Designentwurf vorgesehen", und die Bereiche sind „bereits im Design gestaltet". Im Entwurf (`startseite_favoriten_gruppen/code.html`, `<nav>`) hat die untere Navigationsleiste einen hellen bzw. dunklen Surface-Hintergrund mit Schatten. Der aktive Eintrag ist Teal (`text-primary`, halbfett), die inaktiven Einträge haben die Farbe `on-surface-variant`. `AppShell.xaml` definiert nur die `TabBar` mit Titeln und Icons. Es gibt keine Shell-Farbwerte (`Shell.TabBarBackgroundColor`, `Shell.TabBarForegroundColor`, `Shell.TabBarTitleColor`, `Shell.TabBarUnselectedColor` o. Ä.) und keinen Light/Dark-Bezug. `App.xaml` bindet nur `DesignSystem.xaml` ein (kein `Styles.xaml`). Die Navigation sieht daher aus wie der Plattform-Standard, nicht wie das Design-System (Teal, Hell- und Dunkelmodus).
- [ ] **E2E-Testdatenverzeichnis wirkt nicht in der App.** Die Anforderung verlangt, dass E2E-Tests ein Datenverzeichnis verwenden, das vom Entwicklungs- und Echtbetrieb getrennt ist. `E2ETestBase` legt zwar ein temporäres Verzeichnis an und übergibt es per Umgebungsvariable `TEST_DATA_PATH` an den App-Prozess. Die App (`src/Tankradar.MAUI`) wertet diese Variable aber nirgends aus (nur `TestDataContext` im Integrationstestprojekt liest sie). Die App hat keinen Mechanismus, ihr Datenverzeichnis umzulenken. Sobald sie Daten schreibt (ab Schritt 4), würden E2E-Tests dasselbe Verzeichnis wie der Entwicklungsbetrieb nutzen. Die Grundlage für die geforderte Trennung ist also auf App-Seite nicht vorhanden.

## Hinweise

- **Erfüllt:**
  - MAUI-Projekt mit iOS (`SupportedOSPlatformVersion` 16.0) und Windows
  - MVVM-Struktur (`BaseViewModel`, vier ViewModels, per Dependency Injection registriert)
  - vier Tabs „Favoriten", „Karte", „Tankbuch", „Optionen" mit Favoriten als Startseite
  - leere Seiten mit Design-Token-Farben und `AppThemeBinding` für Hell- und Dunkelmodus
  - Design-Tokens für Farben (Teal, Emerald, Amber, Rot, Neutral), Abstände, Eckenradien, Schatten 0–3 und `MinimumTouchTarget` 44
  - Designabweichung Strom dokumentiert (`docs/adr/0001-no-electricity-prices-in-v1.md`, README)
  - Bundle-ID als Platzhalter `com.softwareschmiede.tankradar.dev` (csproj `ApplicationId`, `AppConfiguration`, `appsettings.json`, Umgebungsvariable `TANKRADAR_BUNDLE_ID`)
  - Windows ohne Installation startbar (`WindowsPackageType=None`, `WindowsAppSDKSelfContained=true`)
  - drei Testprojekte (Unit, Integration, E2E mit FlaUI)
  - Skript `scripts/create-review-version.ps1`, in der README dokumentiert
  - `review-versions/` per `.gitignore` ausgeschlossen
  - Zwischenstand `review-versions/0.1.0_2026-09-28/` mit `CHANGELOG.md` und `bin/Tankradar.MAUI.exe` liegt vor
- **Verifikation:** `dotnet build` (Windows-Ziel) ohne Warnungen und Fehler. `dotnet test Tankradar.sln`: Unit 4/4, Integration 3/3 und E2E 1/1 erfolgreich.
- **Der E2E-Test ist schwach:** Nach dem Klick sucht er ein Element mit demselben Namen wie der Tab. Diese Prüfung findet auch den Tab selbst und beweist deshalb nicht, dass die Seite gewechselt hat. Stabiler wäre eine eindeutige `AutomationId` auf der Seitenüberschrift.
- **Design-Tokens werden nur definiert, nicht angewendet:** Es gibt keine impliziten Styles, z. B. für Buttons mit `MinimumHeightRequest` 44 oder Primärfarbe Teal. Die 44×44-Mindestgröße wird also nicht zentral durchgesetzt. Für die leeren Seiten ist das noch unkritisch, sollte aber vor den fachlichen Schritten ergänzt werden.
- **Typografie weicht ab:** Bei `headline-md` (600 statt Bold) und `price-hero` (800) stimmen die Schriftgewichte nicht mit dem Entwurf überein. Die negativen Buchstabenabstände der Überschriften fehlen, und `title-sm`, `price-unit` und `label-pill` sind nicht definiert.
- **Icon und Splash nutzen noch Vorlagenwerte:** App-Icon und Splash-Screen verwenden die MAUI-Vorlagenfarbe `#512BD4` (Lila), und `dotnet_bot.png` ist noch enthalten. Der Entwurf enthält ein eigenes Logo (`tank_lade_navigator_logo`). Diese Abweichung ist nicht dokumentiert.
- **Die Bundle-ID ist doppelt gepflegt:** Zur Build-Zeit ist sie als `ApplicationId` im csproj fest eingetragen (per `-p:ApplicationId=` überschreibbar). Die Laufzeitkonfiguration in `AppConfiguration` hat darauf keinen Einfluss. Das ist zulässig, sollte aber in Schritt 3 (CI, Secrets) vereinheitlicht werden.
