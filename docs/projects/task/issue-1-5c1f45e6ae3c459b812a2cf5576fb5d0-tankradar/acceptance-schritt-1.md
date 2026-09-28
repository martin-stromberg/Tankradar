# Abnahmeprüfung – Entwicklungsschritt 1

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

- **Die drei Abweichungen aus `acceptance-schritt-1.1.md` sind behoben:**
  1. **Schriften:** `Resources/Fonts/` enthält jetzt `Inter-Regular.ttf`, `Inter-SemiBold.ttf`, `JetBrainsMono-Regular.ttf` und `JetBrainsMono-SemiBold.ttf`. Alle vier sind gültige TrueType-Dateien (Signatur `00 01 00 00`), die OFL-Lizenztexte liegen bei. `MauiProgram.cs` registriert die Schriften als `InterRegular`, `InterSemibold`, `JetBrainsMonoRegular` und `JetBrainsMonoSemibold`. In `DesignSystem.xaml` setzen alle Textstile eine `FontFamily`: `headline-*` und `price-hero` nutzen Inter SemiBold, `body-*` Inter Regular und `label-code` JetBrains Mono. Die Schriften werden auch in den Review-Stand `0.1.1` ausgeliefert. In der README ist der Punkt nicht mehr als Einschränkung geführt.
  2. **Navigation im Design:** `AppShell.xaml` setzt `Shell.TabBarBackgroundColor` auf Surface Hell/Dunkel per `AppThemeBinding`. Aktiver Eintrag und Titel sind Teal (`TabBarForegroundColor`, `TabBarTitleColor`), inaktive Einträge haben die Sekundär-Textfarbe Hell/Dunkel (`TabBarUnselectedColor`). Das entspricht der unteren Navigationsleiste im Entwurf.
  3. **Testdatenverzeichnis:** Die App hat jetzt den Dienst `IAppDataPathProvider`/`AppDataPathProvider`, als Singleton in `MauiProgram.cs` registriert. Er wertet `TEST_DATA_PATH` aus (gemeinsame Konstante aus `src/TestSupport/TestDataPaths.cs`, im MAUI-Projekt verlinkt). Ohne die Variable fällt er auf `FileSystem.AppDataDirectory` zurück. `E2ETestBase` übergibt dem App-Prozess ein eigenes temporäres Verzeichnis. Drei Unit-Tests sichern die Auflösung ab.
- **Die übrige Anforderung ist weiterhin erfüllt:**
  - MAUI-Projekt für iOS 16 und Windows
  - MVVM mit `BaseViewModel`, vier ViewModels und Dependency Injection
  - vier Tabs „Favoriten" (Startseite), „Karte", „Tankbuch" und „Optionen"; die leeren Seiten sind mit Design-Tokens und Hell-/Dunkelmodus gestaltet
  - Design-Tokens für Farben (Teal, Emerald, Amber, Rot), Abstände, Eckenradien, Schatten 0–3 und `MinimumTouchTarget` 44
  - Designabweichung Strom als ADR `docs/adr/0001-no-electricity-prices-in-v1.md` und in der README
  - Bundle-ID als Konfigurationswert mit Platzhalter `com.softwareschmiede.tankradar.dev`
  - Windows-App ohne Installation startbar (`WindowsPackageType=None`)
  - drei Testprojekte (Unit, Integration, E2E mit FlaUI)
  - Skript `scripts/create-review-version.ps1`, in der README dokumentiert
  - `review-versions/` per `.gitignore` ausgeschlossen
  - Zwischenstände `0.1.0_2026-09-28` und `0.1.1_2026-09-28`, jeweils mit `CHANGELOG.md` und `bin/Tankradar.MAUI.exe`
- **Verifikation:** `dotnet build Tankradar.sln` ohne Warnungen und Fehler. `dotnet test Tankradar.sln`: Unit 7/7, Integration 3/3 und E2E 1/1 erfolgreich.
- **E2E-Test verbessert:** Der Navigationstest prüft nach jedem Tab-Wechsel jetzt eine seitenspezifische `AutomationId` (`*Page.Headline`). Die Schwäche aus der Vorprüfung ist damit behoben.
- **Icon und Splash:** Beide verwenden jetzt Teal (`#0F766E`), `dotnet_bot.png` ist entfernt. Das Logo aus dem Entwurf (`tank_lade_navigator_logo`) ist noch nicht übernommen, die Anforderung verlangt das aber nicht ausdrücklich.
- **Kleinere Designdetails (nicht abnahmerelevant):**
  - Die Überschriften kombinieren `FontFamily="InterSemibold"` mit `FontAttributes="Bold"`. Je nach Plattform kann das zu künstlichem Fettdruck führen. Ein eigener Bold-Schnitt oder der Verzicht auf `FontAttributes` wäre sauberer.
  - Der Schatten der Navigationsleiste aus dem Entwurf ist in der Shell nicht nachgebildet.
  - Die fehlenden Typostile aus der Vorprüfung (`title-sm`, `price-unit`, `label-pill`) sind noch nicht definiert.
  - Es gibt noch keine impliziten Styles, die die 44×44-Mindestgröße zentral durchsetzen.
  - Diese Punkte sollten spätestens in den fachlichen UI-Schritten nachgezogen werden.
- **Bundle-ID doppelt gepflegt:** Die Bundle-ID steht als `ApplicationId` im csproj und zusätzlich in der Laufzeitkonfiguration. Das ist unverändert zulässig; die Vereinheitlichung ist für Schritt 3 vorgesehen.
