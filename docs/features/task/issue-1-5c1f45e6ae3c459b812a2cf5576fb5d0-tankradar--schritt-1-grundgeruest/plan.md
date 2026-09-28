# Umsetzungsplan: App-Grundgerüst, Navigation und Design-System

**Erstellt:** 2026-09-28  
**Anforderung:** `requirement.md` (Übersetzte Anforderung: App-Grundgerüst, Navigation und Design-System)  
**Bestandsaufnahme:** `inventory.md` (Ausgangszustand: Initialzustand, keine Implementierung)

## Übersicht

Schritt 1 etabliert das Projektgrundgerüst der MAUI-App „Tankradar" mit einer gemeinsamen Codebasis für iOS (Mindestversion iOS 16) und Windows (Entwicklung, Debugging, automatisierte Tests). Die App folgt einer MVVM-Architektur und zeigt beim Start eine Hauptnavigation mit vier Bereichen (Favoriten, Karte, Tankbuch, Optionen), die zunächst leer, aber designgerecht gestaltet sind. Das Design-System wird als zentrale, wiederverwendbare Gestaltungsgrundlage mit Farbpalette (Teal, Emerald, Amber, Rot), Schriftarten (Inter, JetBrains Mono), Abstände, Eckenradien, Schatten, Mindestgrößen (44×44 px) und Light/Dark-Mode etabliert. Zusätzlich werden Testinfrastruktur (Unit, Integration, E2E mit FlaUI), eine Skript-Infrastruktur für Windows-Zwischenstände und Dokumentation von Designabweichungen (keine Strompreis-Elemente in v1.0) aufgebaut.

**Betroffene Bereiche:** Projektstruktur, Navigation, Design-System, MVVM-Infrastruktur, Testinfrastruktur, Konfiguration, Versionsverwaltung, Dokumentation.

---

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| **Navigation (AppShell)** | Bottom TabBar mit vier Tabs, jeder Tab eine separate Seite mit eigenem ViewModel (ShellContentRoute-Muster) | Design-Entwurf zeigt Bottom-TabBar; MAUI AppShell bietet natives Tab-Verhalten auf iOS und Windows; einfache Implementierung, native Plattformverhalten |
| **MVVM-Framework** | MAUI-native Dependency Injection + MAUI-BaseViewModel Klasse; keine externe Bibliothek erforderlich | .NET SDK 10.0.401 und MAUI enthalten ausreichend DI und Binding-Infrastruktur; Einfachheit für Schritt 1; optional CommunityToolkit.Mvvm in späteren Schritten |
| **Design-System (ResourceDictionary)** | Zentrale `Resources/DesignSystem.xaml` mit allen Farben, Typografie, Spacing, Border-Radii und Schatten als XAML-Ressourcen + separate App-Theme-Wechsel per `App.xaml.cs` | XAML ResourceDictionary ist standard in MAUI; Light/Dark-Mode über `AppTheme`-Binding; alle Designtoken zentral gepflegt; wiederverwendbar auf allen Seiten |
| **Schriftart-Einbindung** | Inter und JetBrains Mono als TTF-Dateien in `Assets/Fonts/`; Plattformspezifische Registrierung in `MauiProgram.cs` für Windows, in `Info.plist` für iOS | MAUI unterstützt benutzerdefinierten Schriftarten über `AddFont()` in MauiProgram; Fallback auf System-Schriften, falls Laden fehlschlägt; gängiges MAUI-Muster |
| **Bundle-ID-Management** | Klasse `AppConfiguration` mit Property `BundleId`, Wert aus Umgebungsvariable oder `appsettings.json` lesbar, Placeholder-Standard `com.softwareschmiede.tankradar.dev` | Konfigurationsklasse ermöglicht laufzeitflexible Übergabe ohne Blockade durch fehlende Werte; CI/CD-Integration möglich; Standardwert blockiert Entwicklung nicht |
| **Windows-Standalone-Ausführung** | Unpackaged MAUI-Build (executable läuft direkt ohne MSIX/Installer); wird durch `MauiProgram` und Standard-.csproj-Konfiguration für Windows erreicht | MAUI unterstützt standardmäßig unpackaged Ausführung; keine zusätzliche Toolchain nötig; erfüllt Anforderung der portablen Testbarkeit |
| **Test-Datenisolation** | Separates `TestDataDirectory` pro Testlauf (Umgebungsvariable `TEST_DATA_PATH` oder Default in User-Profile/Tests); E2E-Tests verwenden zusätzlich einen eigenen `TestAppDataContext` | Verhindert Datenvermischung zwischen Produktiv-, Entwicklungs- und Testbetrieb; FlaUI-Tests gegen isolierte lokale SQLite-Datenbank; Reproduzierbarkeit gewährleistet |
| **E2E-Test-Framework** | FlaUI (Open Source) gegen Windows-App; erste Test-Klasse `NavigationE2ETests` mit Rauchtest „App startet und navigiert durch alle vier Bereiche" | FlaUI ist plattformunabhängig und einfach zu nutzen; ermöglicht UI-Tests ohne Quellcode-Zugriff; für Windows die beste Wahl; erlaubt Funktionsprüfung auf echter App |
| **Version-Skript (Windows-Zwischenstände)** | PowerShell-Skript `scripts/create-review-version.ps1` mit Parametern `[-Version <version>] [-ChangelogFile <path>]`; kopiert Release-Build in `review-versions/<Version>_<JJJJ-MM-TT>/` | PowerShell natürlich auf Windows; einfach von CI und lokal aufrufbar; Datumsangabe ermöglicht mehrere Versionen desselben Release-Tags; `.gitignore`-Eintrag für `review-versions/` vorhanden (laut Bestandsaufnahme) |
| **Testdaten und Mocks** | E2E-Tests verwenden separate lokale Testdatenbank; für künftige Schritte (API-Mocks) per Umgebungsvariable `MOCK_API_MODE=true` konfigurierbar | Trennung von Produktiv- und Testumgebung; ermöglicht reproduzierbare Tests ohne externe Dependencies; vorbereitet für Mock-Services in Schritten 5+ |
| **Designabweichung (Strompreise)** | Keine Implementierung von Strompreis-Elementen (Umschalter „Kraftstoff/Laden", Ladestecker-Kennzeichnungen) in Schritt 1; explizit dokumentiert als bewusste Abweichung im Projektplan und in neuer ADR-Dokumentation | Stakeholder-Entscheidung: Keine offizielle, frei nutzbare Strompreis-API vorhanden; Designentwurf enthält diese Elemente, aber v1.0 umfasst sie nicht; Abweichung muss klar dokumentiert sein |

---

## Programmabläufe

### Anwendungsstart und AppShell-Initialisierung

1. `App.xaml.cs` wird geladen, ruft `MauiProgram.CreateMauiApp()` auf.
2. `MauiProgram.CreateMauiApp()` registriert alle Services (ViewModels, Services, etc.) in der Dependency Injection (DI) per `.ConfigureServices()`.
3. AppShell wird als Hauptseite instanziiert; enthält vier ShellContent-Elemente (Favoriten, Karte, Tankbuch, Optionen).
4. Jeder ShellContent verbindet sich mit einem ViewModel über `BindingContext`.
5. Beim Start wird die erste Route (Favoriten) angezeigt.

Beteiligte Klassen/Komponenten: `App.xaml.cs`, `App.xaml`, `MauiProgram.cs`, `AppShell.xaml`, `AppShell.xaml.cs`, `FavoritesPage`, `FavoritesViewModel`, `MapPage`, `MapPageViewModel`, `TankbookPage`, `TankbookViewModel`, `SettingsPage`, `SettingsViewModel`, `AppConfiguration`

### Tab-Navigation zwischen den vier Bereichen

1. Nutzer tippt einen Tab in der Bottom-TabBar an.
2. `AppShell` registriert den Tab-Wechsel über seine `Navigation`-Infrastruktur.
3. Entsprechende Page wird geladen, deren `BindingContext` auf die zugehörige ViewModel verweist.
4. ViewModel wird durch DI mit ihrer `OnAppearing()` aufgerufen (falls überschrieben).

Beteiligte Klassen/Komponenten: `AppShell.xaml`, MAUI `Shell` und `ShellContent` Navigation, ViewModels

### Theme-Wechsel (Light/Dark-Mode)

1. Nutzer ändert Systemeinstellung oder zukünftig App-Einstellung für Theme.
2. `App.xaml.cs` lauscht auf `AppThemeChanged` Event oder wird von SettingsPage angerufen.
3. `AppSheme` wird auf `Light` oder `Dark` gesetzt.
4. XAML-Binding aktualisiert alle Farben über ResourceDictionary.

Beteiligte Klassen/Komponenten: `App.xaml.cs`, `AppShell.xaml`, `Resources/DesignSystem.xaml`, MAUI `Application.Current.UserAppTheme`

### Design-System Ressourcenaktualisierung

1. Eine Page oder Control referenziert eine Ressource wie `{StaticResource ColorPrimaryTeal}` oder `{DynamicResource ColorPrimaryTeal}`.
2. Bei `DynamicResource` wird die Ressource bei Theme-Wechsel automatisch aktualisiert.
3. Bei `StaticResource` bleibt sie unverändert (nur zur Compile-Zeit aufgelöst).

Beteiligte Klassen/Komponenten: `Resources/DesignSystem.xaml`, MAUI `ResourceDictionary`, `DynamicResource` Markup Extension

---

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `AppShell` | Page | Hauptnavigations-Container mit vier Tabs (ShellContent-Elemente) für Favoriten, Karte, Tankbuch, Optionen |
| `FavoritesPage` | Page | Seite für den Bereich „Favoriten" (Startseite) — initial leer, später mit Favoritengruppen |
| `FavoritesViewModel` | ViewModel | Datenlogik und Befehle für FavoritesPage — initial leer, später mit Favoritengruppen-Bindung |
| `MapPage` | Page | Seite für den Bereich „Karte" (Suche) — initial leer, später mit Suchlogik und Kartenansicht |
| `MapPageViewModel` | ViewModel | Datenlogik und Befehle für MapPage — initial leer, später mit Such-Bindungen |
| `TankbookPage` | Page | Seite für den Bereich „Tankbuch" — initial leer, später mit Fahrzeugen und Tankvorgängen |
| `TankbookViewModel` | ViewModel | Datenlogik und Befehle für TankbookPage — initial leer, später mit Tankbuch-Bindungen |
| `SettingsPage` | Page | Seite für den Bereich „Optionen" (Einstellungen) — initial leer, später mit Einstellungsformularen |
| `SettingsViewModel` | ViewModel | Datenlogik und Befehle für SettingsPage — initial leer, später mit Einstellungs-Bindungen |
| `BaseViewModel` | Klasse (abstract) | Basisklasse für alle ViewModels mit `INotifyPropertyChanged`-Unterstützung, `Title`-Property, `IsBusy`-Flag und Befehls-Infrastruktur |
| `AppConfiguration` | Klasse | Verwaltung der App-Konfiguration (Bundle-ID, Theme, Datenbankpfade) mit Lesezugriff auf Umgebungsvariablen/`appsettings.json` |
| `DesignSystem.xaml` | ResourceDictionary | Zentrale Definition aller Design-Token: Farben (Primary, Secondary, Tertiary, Error, Neutral), Typografie (Größen, Gewichte, Zeilenhöhen), Spacing (4px, 8px, 16px, 24px, 40px), Border-Radii (4px, 8px, 12px, 16px, 24px, 9999px), Schatten-Ebenen |

---

## Änderungen an bestehenden Klassen

Keine bestehenden Klassen vorhanden; das Projekt befindet sich in einem Initialzustand. Alle Klassen sind neu.

---

## Datenbankmigrationen

Für Schritt 1 sind **keine Datenbankmigrationen** erforderlich. Die SQLite-Datenbank wird in Schritt 4 (Lokale Datenhaltung und Einstellungen) initialisiert. 

Allerdings muss die Infrastruktur für zukünftige Migrationen vorbereitet werden:

| Vorbereitung | Betroffene Komponente | Beschreibung der Vorbereitung |
|--------------|----------------------|------------------------------|
| **Datenbank-Initialisierer** | `Services/DatabaseService.cs` (später) | Klasse wird in Schritt 4 angelegt; muss Migrations-Geschichtslogik unterstützen (z. B. ein `_AppVersion`-Flag in der Datenbank für Versions-Tracking) |
| **Migrationsverzeichnis** | `src/Data/Migrations/` | Verzeichnis wird angelegt und unter `.gitignore` konfiguriert (später Datenbankdateien dort) |

---

## Validierungsregeln

Für Schritt 1 sind **keine Validierungsregeln** erforderlich. Alle vier Hauptseiten sind initial leer. Validierungen werden ab Schritt 4 (Einstellungen) und später (Fahrzeuge, Tankvorgänge) hinzugefügt.

---

## Konfigurationsänderungen

| Eintrag | Ort | Typ | Standardwert | Zweck |
|---------|-----|-----|--------------|-------|
| `BundleId` | `AppConfiguration` Klasse oder `appsettings.json` | string | `com.softwareschmiede.tankradar.dev` | Eindeutige App-Identifikation für iOS und später Android; wird von Build-Pipeline aus GitHub Secrets überschrieben |
| `MinimumWindowsTargetPlatformVersion` | `.csproj` / `csproj`-Datei | Zielversion | `10.0.19041` (Windows 10 Build 19041+) | Windows-Mindestversion für Deployment; MAUI-Standard |
| `MinimumIOSVersion` | `.csproj` | Zielversion | `16.0` | iOS-Mindestversion gemäß Anforderung und Stakeholder-Entscheidung |
| `TEST_DATA_PATH` | Umgebungsvariable (lokal) / Test-Konfiguration | string | `{UserProfile}/AppData/Local/Tankradar.Tests/` | Verzeichnis für isolierte Test-Datenbank und Test-Dateien |
| `MOCK_API_MODE` | Umgebungsvariable (E2E-Tests, Schritt 5+) | bool | `false` | Aktiviert Mock-Services statt echter API-Aufrufe (vorbereitet) |

---

## Seiteneffekte und Risiken

**Keine bekannten direkten Seiteneffekte**, da das Projekt in einem Initialzustand ist. Folgende **zukünftige Abhängigkeiten** müssen berücksichtigt werden:

- **Font-Fallback auf Plattformen:** Falls Inter oder JetBrains Mono nicht geladen werden, erfolgt automatischer Fallback auf System-Schriften (z. B. Helvetica auf iOS, Segoe UI auf Windows). Design-Konsistenz wird dadurch nicht beeinträchtigt, Lesbarkeit bleibt gewährleistet.
  
- **AppTheme-Binding bei älteren MAUI-Versionen:** Der automatische Theme-Wechsel über `AppTheme`-Binding funktioniert nur in MAUI 8.0+. Bei älteren Versionen muss manuell auf `AppThemeChanged`-Event gehört werden. Kontrolliert wird dies durch die `.csproj` MAUI-Version (10.0.401 ist aktuell und unterstützt es).

- **Windows-Standalone vs. MSIX:** Unpackaged Builds unter Windows können UAC-Probleme verursachen, wenn die App Systemverzeichnisse/Registry-Zugriff benötigt. Für Schritt 1 (nur Navigation, keine Systemzugriffe) kein Problem; ab Schritt 4 (Datenbankzugriff auf Benutzerdaten) weiterhin kein Problem.

- **Git-Hook-Integration (ab Schritt 2):** Schritt 1 muss ohne Git-Hooks lauffähig sein; die Hooks werden in Schritt 2 integriert. Bis dahin können alle Code-Commits erfolgen.

- **CI/CD-Integration (ab Schritt 3):** Schritt 1 muss ohne CI lauffähig sein; Build-Artefakte werden erst ab Schritt 3 durch GitHub Actions erzeugt.

---

## Umsetzungsreihenfolge

1. **Projektstruktur und .csproj-Konfiguration anlegen**
   - Voraussetzungen: .NET SDK 10.0.401 installiert, MAUI-Workloads (iOS, Windows) installiert
   - Beschreibung: 
     - Neue MAUI-Solution `Tankradar.sln` erstellen
     - Hauptprojekt `Tankradar.MAUI.csproj` mit iOS 16 und Windows-Zielen konfigurieren
     - Verzeichnisstruktur anlegen: `src/Models/`, `src/ViewModels/`, `src/Views/`, `src/Resources/`, `src/Platforms/`
     - Zielplattformen in `.csproj`: `ios;windows` mit Mindestversionen iOS 16.0, Windows 10.0.19041
     - `.gitignore` überprüfen: `review-versions/` ist bereits ausgeschlossen (laut Bestandsaufnahme)

2. **AppConfiguration-Klasse und appsettings.json anlegen**
   - Voraussetzungen: Projektstruktur vorhanden
   - Beschreibung:
     - `Services/AppConfiguration.cs` erstellen mit Property `BundleId` (string, lesbar aus Umgebungsvariable oder `appsettings.json`, Standardwert `com.softwareschmiede.tankradar.dev`)
     - `appsettings.json` im Wurzelverzeichnis oder unter `Assets/` mit Struktur `{ "AppConfiguration": { "BundleId": "com.softwareschmiede.tankradar.dev" } }` anlegen
     - `MauiProgram.cs` vorbereiten für DI-Registrierung

3. **MauiProgram.cs und Dependency Injection konfigurieren**
   - Voraussetzungen: Projektstruktur vorhanden, AppConfiguration vorhanden
   - Beschreibung:
     - `MauiProgram.cs` anlegen mit `.AddMauiApp<App>()`
     - `.ConfigureServices()` mit Registrierung aller ViewModels und Services (ab nächsten Schritten)
     - `.AddFont("Inter-Regular.ttf", "InterRegular")` und weitere Inter/JetBrains Mono Schriften registrieren (Dateien noch nicht vorhanden, werden in Schritt 4 hinzugefügt)
     - `AppConfiguration` registrieren: `.AddSingleton<AppConfiguration>()`
     - Plattformspezifische Konfiguration vorbereiten

4. **BaseViewModel-Klasse implementieren**
   - Voraussetzungen: MauiProgram konfiguriert
   - Beschreibung:
     - `ViewModels/BaseViewModel.cs` erstellen als abstract Klasse
     - `INotifyPropertyChanged` implementieren
     - Properties: `Title` (string), `IsBusy` (bool), `IsNotBusy` (bool, über Binding-Konverter)
     - Hilfsmethode `SetProperty<T>(ref T field, T value, string propertyName)` für MVVM Binding
     - Virtuelle Methode `OnAppearing()` für Lifecycle-Hooks (wird bei Page-Sichtbarkeit aufgerufen)
     - `RelayCommand` oder simpler `Command`-Support für Befehle vorbereiten (Verwendung ab späteren Schritten)

5. **Views und ViewModels für vier Hauptseiten anlegen (leer)**
   - Voraussetzungen: BaseViewModel implementiert, MauiProgram konfiguriert, Projektstruktur
   - Beschreibung:
     - `Views/FavoritesPage.xaml` und `Views/FavoritesPage.xaml.cs` anlegen (initial leer, nur Grundstruktur)
     - `Views/FavoritesPage.xaml.cs` bindet `FavoritesViewModel` als BindingContext
     - `ViewModels/FavoritesViewModel.cs` erstellen, erbt von `BaseViewModel`
     - Dasselbe für `MapPage`/`MapPageViewModel`, `TankbookPage`/`TankbookViewModel`, `SettingsPage`/`SettingsViewModel`
     - Alle ViewModels im MauiProgram registrieren
     - Alle Pages haben placeholder-Text oder leeren Container (z. B. `<VerticalStackLayout>`), um später Inhalte einfach hinzuzufügen

6. **Design-System ResourceDictionary anlegen (DesignSystem.xaml)**
   - Voraussetzungen: Views angelegt, `Resources/` Verzeichnis vorhanden
   - Beschreibung:
     - `Resources/DesignSystem.xaml` als `ResourceDictionary` erstellen
     - Alle Farbtokens definieren:
       - `ColorPrimaryTeal`: `#0F766E`
       - `ColorSecondaryEmerald`: `#10B981`
       - `ColorTertiaryAmber`: `#F59E0B`
       - `ColorErrorRed`: `#EF4444`
       - Zusätzliche Neutral-, Surface- und Text-Farben (Light und Dark Mode)
     - Typografie-Stile definieren: `headline-xl`, `headline-lg`, `headline-md`, `body-lg`, `body-md`, `body-sm`, `price-hero`, `label-code` mit Größen, Gewichten, Zeilenhöhen
     - Spacing-Tokens: `space-xs` (4px), `space-sm` (8px), `space-md` (16px), `space-lg` (24px), `space-xl` (40px)
     - Border-Radius-Tokens: `rounded-sm` (4px), `rounded` (8px), `rounded-md` (12px), `rounded-lg` (16px), `rounded-xl` (24px), `rounded-full` (pill)
     - Schatten-Definitionen für Ebenen 0–3
     - Light/Dark Mode Unterstützung durch Conditional XAML oder Runtime-Themes

7. **AppShell mit Navigation anlegen**
   - Voraussetzungen: Design-System vorhanden, alle vier Pages/ViewModels vorhanden
   - Beschreibung:
     - `AppShell.xaml` erstellen mit `<Shell>` Element
     - Vier `<ShellContent>`-Elemente für:
       - `Route="favorites"` → `FavoritesPage`
       - `Route="map"` → `MapPage`
       - `Route="tankbook"` → `TankbookPage`
       - `Route="settings"` → `SettingsPage`
     - Bottom TabBar mit Icons definieren (Icons später hinzufügen oder Platzhalter verwenden)
     - `AppShell.xaml.cs` mit `Title` und `Icon`-Bindungen implementieren
     - First Route (Favoriten) als Default setzen

8. **App.xaml und App.xaml.cs mit Theme-Support konfigurieren**
   - Voraussetzungen: AppShell vorhanden, Design-System vorhanden
   - Beschreibung:
     - `App.xaml` erstellen mit `<Application>` Element
     - `App.xaml.Resources` mit Verweis auf `DesignSystem.xaml` konfigurieren: `<ResourceDictionary Source="Resources/DesignSystem.xaml" />`
     - `App.xaml.cs` implementieren:
       - `App(MauiProgram mauiProgram, AppConfiguration appConfig)` Constructor mit DI
       - `MainPage = new AppShell()` setzen
       - Event-Handler für `AppThemeChanged` vorbereiten (Theme-Wechsel zwischen Light/Dark)
       - `RequestedTheme = OSAppTheme.Unspecified` setzen (System-Theme folgen)

9. **Testprojekte anlegen und konfigurieren**
   - Voraussetzungen: Hauptprojekt vorhanden, NuGet-Pakete verfügbar (xUnit, NUnit, FlaUI)
   - Beschreibung:
     - Drei neue Test-Projekte in Solution hinzufügen:
       - `Tankradar.Tests.Unit` (.NET 8+ Classlib mit xUnit 2.6+)
       - `Tankradar.Tests.Integration` (.NET 8+ Classlib mit xUnit 2.6+)
       - `Tankradar.Tests.E2E` (.NET 8+ Classlib mit xUnit 2.6+ und FlaUI 4.0+)
     - Jedes Testprojekt referenziert `Tankradar.MAUI` (außer E2E, das über Prozessautomatisierung läuft)
     - NuGet-Abhängigkeiten hinzufügen:
       - xUnit, xUnit.Runner.VisualStudio
       - NUnit (optional für Unit-Tests, aber nicht erforderlich)
       - FlaUI (nur in E2E)
       - Moq oder NSubstitute für Mocking (vorbereitet, nicht sofort nötig)
     - Verzeichnisstruktur in jedem Testprojekt: `Unit/`, `Integration/`, `E2E/`

10. **BaseTest-Klasse und TestDataContext anlegen**
    - Voraussetzungen: Testprojekte angelegt
    - Beschreibung:
      - `Tankradar.Tests.Unit/BaseTest.cs` erstellen als abstract Klasse mit:
        - Konstruktor-basierte DI-Setup
        - `IDisposable` für Cleanup
        - Hilfsmethoden für Mock-Erstellung (später erweitert)
      - `Tankradar.Tests.Integration/TestDataContext.cs` implementieren:
        - Isoliertes Datenbankverzeichnis (per `TEST_DATA_PATH` Umgebungsvariable oder Default-Pfad)
        - Datenbankinitialisierung (wird in Schritt 4 mit Migrations angelegt)
        - `IDisposable` für Cleanup (Verzeichnis nach Tests löschen)
      - `Tankradar.Tests.E2E/E2ETestBase.cs` erstellen für FlaUI-Tests (wird in nächsten Schritten gefüllt)

11. **Erster E2E-Test: NavigationSmokeTest anlegen**
    - Voraussetzungen: E2E-Testprojekt vorhanden, App buildbar
    - Beschreibung:
      - `Tankradar.Tests.E2E/FlaUI/NavigationE2ETests.cs` erstellen
      - Testmethode `AppStartsAndNavigatesThroughAllTabs()`:
        - Startet Windows-App (Release-Build)
        - Prüft, dass AppShell mit vier Tabs sichtbar ist
        - Navigiert zu jedem Tab und prüft, dass die Seite/Page lädt und sichtbar wird
        - Kehrt zur Startseite zurück
        - Schließt App
      - Nutzt FlaUI `WindowsDriver` und Accessibility-Automation
      - Test lädt parallel zur manuellen Testbarkeit

12. **Schriftartdateien beschaffen und einbinden (Assets/Fonts)**
    - Voraussetzungen: Projektstruktur vorhanden
    - Beschreibung:
      - Inter-Familie TTF-Dateien beschaffen (Inter-Regular.ttf, Inter-Bold.ttf, Inter-SemiBold.ttf, etc.)
      - JetBrains Mono TTF-Dateien beschaffen (JetBrains Mono-Regular.ttf, JetBrains Mono-Bold.ttf)
      - In `Assets/Fonts/` ablegen
      - `.csproj`-Datei konfigurieren:
        - Für iOS: `<MauiFont Include="Assets/Fonts/Inter-Regular.ttf" Alias="InterRegular" />` etc.
        - MauiProgram.cs `.AddFont()` Aufrufe (bereits in Schritt 3 vorbereitet)
      - Fallback-Schriftart angeben für Fehlerfall

13. **Dokumentation: Designabweichungen und Strompreise (ADR oder Design-Dokumentation)**
    - Voraussetzungen: Projektstruktur vorhanden
    - Beschreibung:
      - Datei `docs/adr/0001-no-electricity-prices-in-v1.md` oder ähnlich anlegen:
        - Titel: „Keine Strompreis-Elemente in Version 1.0"
        - Kontext: Designentwurf enthält Schalter „Kraftstoff/Laden" und Ladestecker-Kennzeichnungen
        - Entscheidung: Diese Elemente werden in v1.0 nicht implementiert (Stakeholder-Entscheidung vom 2026-09-28)
        - Begründung: Keine offizielle, frei nutzbare Strompreis-API vorhanden (siehe Projektplan, Grobe Vorgehensentscheidungen)
        - Konsequenzen: Entwurf-Abweichung dokumentiert; Benutzer sehen nur Kraftstoffpreise in v1.0; Ladevorgänge im Tankbuch sind möglich (manuell eingegeben), aber keine Ladestations-API
      - Alternativ: Eintrag in README.md unter „Design-Entscheidungen" oder in `docs/help/design-deviations.md`
      - Referenz auf diese Dokumentation im Projektplan eintragen

14. **Prozess-Skript für Windows-Zwischenstände anlegen (create-review-version.ps1)**
    - Voraussetzungen: Projektstruktur vorhanden, `review-versions/` Verzeichnis per `.gitignore` ausgeschlossen
    - Beschreibung:
      - PowerShell-Skript `scripts/create-review-version.ps1` anlegen
      - Parameter: `[-Version <version>]` (z. B. „0.1.0"), optional `[-ChangelogFile <path>]`
      - Ablauf:
        1. Windows-Release-Build ausführen (`dotnet publish -c Release -f net8.0-windows`)
        2. Build-Ausgabe in `review-versions/<Version>_<JJJJ-MM-TT>/bin/` kopieren
        3. Changelog-Datei erstellen: `review-versions/<Version>_<JJJJ-MM-TT>/CHANGELOG.md`
        4. Ausgabe: Pfad zum neuen Verzeichnis und Erfolgs-Status
      - Skript sollte idempotent sein (mehrfacher Aufruf mit gleicher Version überschreibt)
      - Validierung: Version muss im Format `X.Y.Z` sein
      - Fehlerbehandlung: Aussagekräftige Fehlermeldungen, falls Build fehlschlägt

15. **README.md aktualisieren oder erstellen**
    - Voraussetzungen: Projektstruktur vorhanden
    - Beschreibung:
      - README.md im Repository-Root anlegen (oder existierende aktualisieren):
        - Projekt-Übersicht: „Tankradar — MAUI-App für Kraftstoffpreise, Favoriten und Tankbuch"
        - Anforderungen: .NET SDK 10.0.401, MAUI-Workloads iOS/Windows
        - Setup-Anleitung: `dotnet restore`, `dotnet build`, `dotnet run -f net8.0-windows`
        - Testausführung: `dotnet test`
        - Windows-Zwischenstände: `.\scripts\create-review-version.ps1 -Version 0.1.0`
        - Verzeichnisstruktur: Kurze Erklärung von `src/`, `tests/`, `scripts/`, `docs/`
        - Design-System und Designabweichungen: Verweis auf `docs/adr/` oder `docs/projects/…/design-system.md`

16. **Lokale Testausführung validieren**
    - Voraussetzungen: Testprojekte angelegt, erster E2E-Test vorhanden
    - Beschreibung:
      - Lokale Tests ausführen: `dotnet test`
      - Prüfung:
        - Unit-Tests (aktuell keine — sollten erfolgreich sein, da keine vorhanden)
        - Integrationstests (aktuell keine — sollten erfolgreich sein, da keine vorhanden)
        - E2E-Tests: `NavigationSmokeTest` sollte erfolgreich sein
      - Falls Fehler: Debuggen und Fehlerbehebung
      - Exit-Code 0 bei Erfolg, Logausgabe dokumentieren

17. **Versionsnummer in `.csproj` setzen und Windows-Zwischenstand Version 0.1.0 ablegen**
    - Voraussetzungen: Alle Schritte 1–16 abgeschlossen, Skript `create-review-version.ps1` vorhanden, Tests bestehen
    - Beschreibung:
      - `Tankradar.MAUI.csproj` Element `<Version>0.1.0</Version>` hinzufügen oder aktualisieren
      - `<InformationalVersion>0.1.0</InformationalVersion>` optional hinzufügen (für CI/CD)
      - Skript ausführen: `.\scripts\create-review-version.ps1 -Version 0.1.0 -ChangelogFile "Initial foundation release"`
      - Verzeichnis `review-versions/0.1.0_2026-09-28/` sollte erstellt werden mit executable App
      - Changelog-Datei sollte ausfüllbar sein mit initiaem Eintrag
      - **Nicht committen**: `.gitignore` verhindert Commit von `review-versions/`
      - Dokumentation aktualisieren: Anleitung zum Abrufen und Ausführen des Zwischenstands

---

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `AppStartsAndNavigatesThroughAllTabs()` | `NavigationE2ETests` | E2E-Rauchtest: App startet, alle vier Navigationsbereiche sind per FlaUI erreichbar und funktionstüchtig |
| `BaseTest` Konstruktor und Hilfsmethoden | `BaseTest` (abstract) | DI-Setup für Unit-Tests, Mock-Erstellung, Basis-Fixtures |
| `TestDataContext.Initialize()` | `TestDataContext` | Isoliertes Datenbankverzeichnis für Integrationstests wird erstellt |
| `TestDataContext.Cleanup()` | `TestDataContext` | Test-Datenbank und Dateien werden nach Tests gelöscht |
| `BaseViewModel.SetProperty<T>()` | (Komponente, nicht eigentlicher Test) | Hilfsmethode für MVVM Property-Binding (wird in Unit-Tests für ViewModels verwendet) |

### Betroffene bestehende Tests

Keine. Das Projekt ist in einem Initialzustand; es existieren keine vorherigen Tests.

### E2E-Tests (primärer Funktionsnachweis)

Die Anforderung nennt explizit einen E2E-Test: „Ein erster E2E-Test startet die App und wechselt durch alle vier Navigationsbereiche." Dies ist der primäre Funktionsnachweis, dass die Navigation und AppShell-Infrastruktur tatsächlich funktioniert — Unit-Tests für ViewModels können Property-Bindung nicht ausreichend prüfen, da sie keine UI-Interaktion simulieren.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| **Pflicht** | App startet und zeigt AppShell mit vier Tabs | `Tankradar.Tests.E2E/FlaUI/NavigationE2ETests.cs` → `AppStartsAndNavigatesThroughAllTabs()` | App startet ohne Fehler, AppShell wird angezeigt, vier Navigationsbereiche sind verfügbar, Wechsel zwischen Tabs funktioniert | Nur E2E-Test kann tatsächliche UI-Navigation und Platform-Integration (MAUI AppShell auf Windows) prüfen; Unit-Tests können nicht validieren, dass Pages tatsächlich sichtbar sind |
| **Pflicht** | Jeder Tab zeigt die erwartete Page | `NavigationE2ETests.cs` (Teil von `AppStartsAndNavigatesThroughAllTabs()`) | Jeder Tab navigiert zur korrekten Page (Favoriten, Karte, Tankbuch, Optionen); Page ist nicht blank, sondern enthält mindestens ein visuelles Element | Unit-Tests können XAML-Struktur nicht prüfen; nur E2E kann verifizieren, dass das richtige XAML gerendert wird |
| **Empfohlen** | App lädt beide Themes (Light/Dark) ohne Fehler | `NavigationE2ETests.cs` (optional zusätzliche Testmethode `AppSupportsThemeSwitching()`) | App-Theme kann per MAUI `AppTheme` gewechselt werden, alle Designtokens laden in beiden Themes | E2E prüft, dass ResourceDictionary und Theme-Binding tatsächlich funktionieren; Unit-Tests können XAML-Ressourcen nicht prüfen |

**Welche bestehenden E2E-Tests müssen angepasst werden?**

Keine. Das Projekt ist in einem Initialzustand; es existieren keine vorherigen Tests.

---

## Offene Punkte

Keine.

Alle in der Anforderung genannten Fragen wurden bereits während der Planung entschieden und sind oben im Abschnitt „Designentscheidungen" sowie in der Umsetzungsreihenfolge eingearbeitet (u. a. Schriftart-Fallback, Dark-Mode-Standard, Test-Datenisolation, Skriptsprache, CI/CD-Timing, 44×44-Validierung, Datenbankschema-Zeitpunkt, AppShell-Navigation). Es bestehen keine technischen Blocker für Schritt 1.

---

## Anhang: Struktur der Projektdateien nach Schritt 1

```
Tankradar/
├── Tankradar.sln
├── src/
│   ├── Tankradar.MAUI/
│   │   ├── Tankradar.MAUI.csproj
│   │   ├── App.xaml
│   │   ├── App.xaml.cs
│   │   ├── MauiProgram.cs
│   │   ├── AppShell.xaml
│   │   ├── AppShell.xaml.cs
│   │   ├── appsettings.json
│   │   ├── Assets/
│   │   │   ├── Fonts/
│   │   │   │   ├── Inter-Regular.ttf
│   │   │   │   ├── Inter-Bold.ttf
│   │   │   │   ├── JetBrainsMono-Regular.ttf
│   │   │   │   └── ...
│   │   ├── Views/
│   │   │   ├── FavoritesPage.xaml
│   │   │   ├── FavoritesPage.xaml.cs
│   │   │   ├── MapPage.xaml
│   │   │   ├── MapPage.xaml.cs
│   │   │   ├── TankbookPage.xaml
│   │   │   ├── TankbookPage.xaml.cs
│   │   │   ├── SettingsPage.xaml
│   │   │   └── SettingsPage.xaml.cs
│   │   ├── ViewModels/
│   │   │   ├── BaseViewModel.cs
│   │   │   ├── FavoritesViewModel.cs
│   │   │   ├── MapPageViewModel.cs
│   │   │   ├── TankbookViewModel.cs
│   │   │   └── SettingsViewModel.cs
│   │   ├── Services/
│   │   │   ├── AppConfiguration.cs
│   │   │   └── DatabaseService.cs (Skelett)
│   │   ├── Resources/
│   │   │   └── DesignSystem.xaml
│   │   └── Platforms/
│   │       ├── iOS/
│   │       │   ├── Info.plist (Font-Registrierung)
│   │       │   └── ...
│   │       └── Windows/
│   │           └── ...
│   ├── Tankradar.Tests.Unit/
│   │   ├── Tankradar.Tests.Unit.csproj
│   │   ├── BaseTest.cs
│   │   └── Unit/ (später gefüllt)
│   ├── Tankradar.Tests.Integration/
│   │   ├── Tankradar.Tests.Integration.csproj
│   │   ├── TestDataContext.cs
│   │   └── Integration/ (später gefüllt)
│   └── Tankradar.Tests.E2E/
│       ├── Tankradar.Tests.E2E.csproj
│       ├── E2ETestBase.cs
│       └── FlaUI/
│           └── NavigationE2ETests.cs
├── scripts/
│   └── create-review-version.ps1
├── docs/
│   ├── adr/
│   │   └── 0001-no-electricity-prices-in-v1.md
│   └── (projects/, features/ sind extern)
├── README.md
└── .gitignore (review-versions/ bereits ausgeschlossen)
```

---

**Plan erstellt:** 2026-09-28  
**Gültig für:** Schritt 1 des Tankradar-Projekts (Grundgerüst)  
**Status:** Vollständig, keine offenen technischen Punkte
