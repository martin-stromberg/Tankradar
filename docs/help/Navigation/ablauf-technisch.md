← [Zurück zur Übersicht](index.md)

# Navigation und Design-System — Technischer Ablauf

## Übersicht

Die App-Navigation basiert auf dem MAUI-Shell-Modell mit einer Bottom-TabBar. Beim Start wird die Dependency-Injection aufgebaut, die MainPage wird auf `AppShell` gesetzt, und die erste Route (Favoriten) wird angezeigt. Das Design-System ist als zentrale `ResourceDictionary` implementiert und wird über `AppThemeBinding` zwischen Hell- und Dunkelmodus umgeschaltet.

## Ablauf

### 1. App-Start und MauiProgram-Initialisierung

Die App wird über `App.xaml.cs` gestartet:

1. `App.xaml.cs` aufgerufen
2. `MauiProgram.CreateMauiApp()` erzeugt die MAUI-App-Instanz
3. `MauiProgram.cs` registriert alle Services in der Dependency Injection:
   - `FavoritesViewModel`, `MapViewModel`, `TankbookViewModel`, `SettingsViewModel`
   - `AppConfiguration` (Singleton)
   - `IAppDataPathProvider` → `AppDataPathProvider` (Singleton, siehe Schritt 6 „App-Datenverzeichnis-Auflösung")
   - Schriftarten via `ConfigureFonts(...)` und `.AddFont(...)`: `OpenSansRegular`/`OpenSansSemibold` (Fallback), `InterRegular`/`InterSemibold` (Fließtext und Überschriften) sowie `JetBrainsMonoRegular`/`JetBrainsMonoSemibold` (Code-Darstellung; `JetBrainsMonoSemibold` ist registriert, aktuell aber an keinem Style verdrahtet)
4. Alle ViewModels werden als Transient registriert

Beteiligte Komponenten:
- `App.xaml.cs` — Konstruktor, MainPage-Setzung
- `App.xaml` — Ressourcen-Verweis auf `DesignSystem.xaml`
- `MauiProgram.cs` — Service-Registrierung, DI-Setup, Font-Registrierung
- `AppConfiguration.cs` — Konfigurationsklasse mit Bundle-ID
- `IAppDataPathProvider` / `AppDataPathProvider.cs` — Ermittlung des App-Datenverzeichnisses (Details siehe Schritt 6)

### 2. Shell-Navigation aufbauen

Nach der MauiProgram-Initialisierung:

1. `App.xaml.cs` setzt `MainPage = new AppShell()`
2. `AppShell.xaml` wird geladen:
   - `<TabBar>` mit vier `<ShellContent>`-Elementen
   - Jeder `ShellContent` hat eine `Route` (z. B. `Route="favorites"`)
   - Jeder verweist auf eine `ContentTemplate` mit einer Page-Klasse
   - Auf dem `<Shell>`-Root sind vier Farbattribute gesetzt, die MAUI beim Rendern der nativen TabBar anwendet:
     - `Shell.TabBarBackgroundColor` → `AppThemeBinding` auf `ColorSurfaceLight`/`ColorSurfaceDark`
     - `Shell.TabBarForegroundColor` und `Shell.TabBarTitleColor` → `ColorPrimaryTeal` (aktiver Tab)
     - `Shell.TabBarUnselectedColor` → `AppThemeBinding` auf `ColorTextSecondaryLight`/`ColorTextSecondaryDark` (inaktive Tabs)
3. MAUI Shell registriert die Routes
4. Bei App-Start wird automatisch die erste Route (Favoriten) angezeigt

Beteiligte Komponenten:
- `AppShell.xaml` — TabBar-Definition mit vier Tabs sowie `Shell.TabBar*`-Farbattributen
- `AppShell.xaml.cs` — Code-Behind (ggf. für zukünftige Anpassungen)
- `FavoritesPage`, `MapPage`, `TankbookPage`, `SettingsPage` — ContentTemplate-Seiten
- `Resources/DesignSystem.xaml` — Quelle der referenzierten Farb-Tokens
- MAUI Shell-Navigation (built-in)

Hinweis: In der aktuell verwendeten MAUI-Version (`Microsoft.Maui.Controls` 10.0.20) existieren keine `Shell.TabBar*Shadow*`- oder `Shell.TabBar*Bold*`-Attached-Properties. Der im Designentwurf gezeigte Schatten der TabBar und die Halbfett-Schrift des aktiven Tab-Titels sind daher mit Bordmitteln nicht nachbildbar und wurden als bekannte, akzeptierte Abweichung nicht umgesetzt.

Jede der vier Views trägt außerdem auf ihrem Headline-`Label` (Style `headline-lg`) eine feste `AutomationId` (`FavoritesPage.Headline`, `MapPage.Headline`, `TankbookPage.Headline`, `SettingsPage.Headline`). `NavigationE2ETests.AppStartsAndNavigatesThroughAllTabs()` (`Tankradar.Tests.E2E`) sucht nach jedem Tab-Klick gezielt nach der zur jeweiligen Seite gehörenden `AutomationId` statt nach dem — auch am Tab selbst treffenden — Tab-Namen, und weist damit einen tatsächlichen Seitenwechsel nach statt nur eines Namenstreffers.

### 3. ViewModel-Bindung und Lifecycle

Beim Wechsel zu einem Tab:

1. MAUI Shell lädt die entsprechende Page (via ContentTemplate)
2. Page wird instanziiert (z. B. `FavoritesPage`)
3. Page-Code-Behind (`FavoritesPage.xaml.cs`) setzt `BindingContext = new FavoritesViewModel()` (per DI)
4. Das ViewModel erbt von `BaseViewModel` und implementiert `INotifyPropertyChanged`
5. Falls vorhanden, wird `OnAppearing()` des ViewModels aufgerufen (nicht in Schritt 1 implementiert, aber vorbereitet)

Beteiligte Komponenten:
- `BaseViewModel.cs` — Basis-Klasse mit `INotifyPropertyChanged`, `Title`, `IsBusy`
- `FavoritesViewModel`, `MapViewModel`, `TankbookViewModel`, `SettingsViewModel` — konkrete ViewModels
- `TankradarContentPage.cs` — Custom Page-Klasse (optional, für gemeinsame Page-Logik)

### 4. Design-System-Ressourcen laden

Beim App-Start und bei jedem Laden einer Page:

1. `App.xaml` referenziert `<ResourceDictionary Source="Resources/DesignSystem.xaml" />`
2. `DesignSystem.xaml` wird geladen und alle Ressourcen registriert:
   - Farb-Tokens: `ColorPrimaryTeal`, `ColorBackgroundLight`, `ColorTextPrimaryDark`, etc.
   - Spacing-Tokens: `space-xs` (4px), `space-md` (16px), etc.
   - Border-Radius-Tokens: `rounded`, `rounded-lg`, etc.
   - Shadow-Tokens: `ShadowLevel0` bis `ShadowLevel3`
   - Typografie-Stile: `headline-lg`, `body-md`, `price-hero`, etc. — jeder Stil referenziert über `FontFamily` explizit einen in Schritt 1 registrierten Font-Alias, keiner fällt mehr implizit auf die Systemschrift zurück: `headline-xl`/`headline-lg`/`headline-md`/`price-hero` → `InterSemibold`, `body-lg`/`body-md`/`body-sm` → `InterRegular`, `label-code` → `JetBrainsMonoRegular`
3. Pages und Controls referenzieren diese Ressourcen via `StaticResource` oder `DynamicResource`

Beteiligte Komponenten:
- `App.xaml` — ResourceDictionary-Verweis
- `Resources/DesignSystem.xaml` — zentrale Ressourcen-Definition
- `FavoritesPage.xaml` und andere — Verwendung der Ressourcen

### 5. Theme-Wechsel (Light/Dark Mode)

Beim Wechsel des Geräte-Themes (z. B. von Light zu Dark):

1. MAUI erkennt `AppThemeChanged` (automatisch oder über `Application.Current.RequestedTheme`)
2. Alle `AppThemeBinding`-Bindings in XAML werden neu ausgewertet
3. `AppThemeBinding Light={...} Dark={...}` wählt die passende Ressource aus
4. Farben, Hintergrund und Text werden sofort aktualisiert

Beispiel aus `FavoritesPage.xaml`:
```xml
BackgroundColor="{AppThemeBinding Light={StaticResource ColorBackgroundLight}, 
                                  Dark={StaticResource ColorBackgroundDark}}"
TextColor="{AppThemeBinding Light={StaticResource ColorTextPrimaryLight}, 
                            Dark={StaticResource ColorTextPrimaryDark}}"
```

Beteiligte Komponenten:
- `DesignSystem.xaml` — Farb-Tokens für Light und Dark
- `App.xaml.cs` — Optional: Event-Handler für `AppThemeChanged` (vorbereitet, aktuell nicht implementiert)
- Alle Pages (XAML) — `AppThemeBinding` für Dynamic Color-Selection
- MAUI Application-Klasse (built-in) — Theme-Verwaltung

### 6. App-Datenverzeichnis-Auflösung (Testisolation)

Unabhängig vom sichtbaren Navigations- und Theme-Ablauf stellt `MauiProgram.CreateMauiApp()` bereits einen zentralen Mechanismus bereit, über den künftige Datenzugriffe (ab Entwicklungsschritt 4) das App-Datenverzeichnis ermitteln. In diesem Schritt hat der Dienst noch keinen produktiven Aufrufer, wird aber vollständig bereitgestellt und per Unit-Test abgesichert:

1. Ein Aufrufer lässt sich `IAppDataPathProvider` per Dependency Injection injizieren und ruft `GetDataDirectory()` auf.
2. `AppDataPathProvider.GetDataDirectory()` liest die Umgebungsvariable `TestDataPaths.TestDataPathEnvironmentVariable` (Wert: `TANKATLAS_TEST_DATA_PATH`) über `Environment.GetEnvironmentVariable(...)`.
3. Ist der Wert gesetzt und nicht leer, wird genau dieser Pfad zurückgegeben.
4. Andernfalls wird eine injizierte `Func<string>`-Factory für das Standardverzeichnis aufgerufen; im produktiven Konstruktor ist das `() => FileSystem.AppDataDirectory`.

`E2ETestBase` setzt `TANKATLAS_TEST_DATA_PATH` bereits vor dem App-Start, sodass FlaUI-gesteuerte E2E-Testläufe automatisch ein isoliertes Testdatenverzeichnis erhalten und nie das Datenverzeichnis der Entwicklungs- oder Produktionsinstallation berühren.

```mermaid
flowchart TD
    A["GetDataDirectory() aufgerufen"] --> B{"TANKATLAS_TEST_DATA_PATH gesetzt und nicht leer?"}
    B -- Ja --> C["Wert der Umgebungsvariable zurückgeben"]
    B -- Nein --> D["_defaultDirectoryFactory() aufrufen"]
    D --> E["Produktiv: FileSystem.AppDataDirectory"]
```

Beteiligte Komponenten:
- `IAppDataPathProvider` — Abstraktion (`GetDataDirectory()`), Namespace `Tankradar.MAUI.Services`
- `AppDataPathProvider` — Implementierung mit zwei Konstruktoren (produktiver Parameterloser Konstruktor vs. testbare Überladung mit `Func<string> defaultDirectoryFactory`)
- `TestDataPaths.TestDataPathEnvironmentVariable` (`src/TestSupport/TestDataPaths.cs`) — gemeinsame Konstante für den Variablennamen `TANKATLAS_TEST_DATA_PATH`, auch vom Integrationstestprojekt (`TestDataContext`) genutzt
- `MauiProgram.cs` — registriert `IAppDataPathProvider` → `AppDataPathProvider` als Singleton
- `AppDataPathProviderTests_DataDirectoryResolution` (`Tankradar.Tests.Unit`) — deckt beide Verzweigungen ab (Variable gesetzt / nicht gesetzt)

## Diagramm

```mermaid
flowchart TD
    A["App-Start"] --> B["MauiProgram.CreateMauiApp"]
    B --> C["Services registrieren"]
    C --> D["AppShell instanziieren"]
    D --> E["Shell-TabBar laden"]
    E --> F["Erste Route anzeigen"]
    F --> G["FavoritesPage laden"]
    G --> H["ViewModel binding"]
    H --> I["Design-System Ressourcen anwenden"]
    I --> J["App angezeigt"]
    
    J -->|Nutzer tippt Tab| K["ShellContent Route ändern"]
    K --> L["Neue Page instanziieren"]
    L --> H
    
    J -->|Geräte-Theme ändert| M["AppThemeBinding neu auswertet"]
    M --> N["Farben wechseln"]
    N --> O["Seite aktualisiert"]
```

## Fehlerbehandlung

### Font-Loading-Fehler

Alle betroffenen Typografie-Stile referenzieren seit dieser Nachbesserung explizit einen registrierten Font-Alias (siehe Schritt 4) statt implizit auf die Systemschrift zurückzufallen. Falls eine Schriftdatei dennoch nicht geladen werden kann (z. B. fehlende TTF-Datei im Build):
- MAUI verwendet automatisch eine System-Schriftart als Fallback
- Visuell ist kein großer Unterschied sichtbar
- Alle Text-Elemente bleiben lesbar

Beteiligte Komponenten:
- `MauiProgram.cs` — `.AddFont()` Aufrufe (Fehler sind still, Fallback ist automatisch)

### AppDataPathProvider ohne gesetzte Umgebungsvariable

Ist `TANKATLAS_TEST_DATA_PATH` nicht gesetzt (Normalfall außerhalb von E2E-Testläufen), liefert `AppDataPathProvider.GetDataDirectory()` deterministisch das Ergebnis der injizierten Standardverzeichnis-Factory (produktiv `FileSystem.AppDataDirectory`) zurück — es wird kein Fehler geworfen, keine leere Zeichenkette akzeptiert (auch bei nur aus Leerzeichen bestehendem Variablenwert greift der Fallback).

Beteiligte Komponenten:
- `AppDataPathProvider.GetDataDirectory()` — `string.IsNullOrWhiteSpace(...)`-Prüfung vor Verwendung des Umgebungsvariablen-Werts

### Theme-Binding bei älteren MAUI-Versionen

Falls MAUI älter als 8.0 ist (was in diesem Projekt nicht der Fall ist):
- `AppThemeBinding` funktioniert nicht automatisch
- Workaround: Manueller Event-Handler auf `AppThemeChanged` erforderlich

Aktuelles Projekt nutzt .NET 10.0.401 und MAUI 8.0+, daher kein Problem.

### ViewModel-Injection-Fehler

Falls ein ViewModel nicht registriert ist:
- DI wirft eine Exception beim Instanziieren der Page
- Die Page wird nicht angezeigt
- Exception wird in Debug-Ausgabe protokolliert

Schutzmaßnahme:
- Alle vier ViewModels sind in `MauiProgram.cs` registriert
- Bei neuen Pages muss das ViewModel hinzugefügt werden

### AppShell-Route-Konflikt

Falls zwei Routes denselben Namen haben:
- MAUI navigiert zur ersten registrierten Route
- Nachfolgende Routes mit gleichem Namen sind unerreichbar

Schutzmaßnahme:
- Route-Namen in `AppShell.xaml` sind eindeutig (`"favorites"`, `"map"`, `"tankbook"`, `"settings"`)
