# Übersetzte Anforderung: App-Grundgerüst, Navigation und Design-System

## Fachliche Zusammenfassung

Es wird ein .NET-MAUI-Projektgerüst für die App „Tankradar" etabliert, das eine gemeinsame Codebasis für iOS (Mindestversion iOS 16, primäre Zielplattform) und Windows (Entwicklung, Debugging, automatisierte Tests) bereitstellt. Die App folgt einer MVVM-Architektur und präsentiert nach dem Start eine Hauptnavigation mit vier Bereichen (Favoriten, Karte, Tankbuch, Optionen), die zunächst leer, aber designgerecht gestaltet sind. Das Design-System wird als zentrale, wiederverwendbare Grundlage mit standardisierten Farben (Teal, Emerald, Amber, Rot), Schriftarten (Inter, JetBrains Mono), Abstände, Eckenradien, Schatten, Mindestgrößen und Hell-/Dunkelmodus etabliert.

## Betroffene Klassen und Komponenten

### Projektstruktur
- **Hauptprojekt:** `Tankradar.MAUI` (.NET MAUI shared codebase)
- **Plattformspezifische Einträge:** `Tankradar.MAUI.iOS` (iOS 16+), `Tankradar.MAUI.Windows` (standalone executable)
- **Verzeichnisstruktur:** 
  - `src/Models/` – Datenmodelle (anfangs leer)
  - `src/ViewModels/` – MVVM ViewModels für die vier Hauptbereiche
  - `src/Views/` – XAML-Ansichten (Pages/Controls)
  - `src/Resources/` – Schriftarten, Design-System-Ressourcen (Farben, Spacing, Typography)
  - `src/Platforms/` – Plattformspezifische Implementierungen

### MVVM-Komponenten (initial leer, strukturiert bereit)
- **Views (Pages):**
  - `FavoritesPage.xaml` / `FavoritesViewModel.cs` (Startseite)
  - `MapPage.xaml` / `MapPageViewModel.cs` (Suche/Karte)
  - `TankbookPage.xaml` / `TankbookViewModel.cs` (Tankbuch)
  - `SettingsPage.xaml` / `SettingsViewModel.cs` (Optionen)
  - `MainPage.xaml` / `AppShell.xaml` (Hauptnavigation mit Tab-Structure)

### Design-System
- **ResourceDictionary:** `Resources/DesignSystem.xaml`
  - `ColorPrimaryTeal` (#0F766E)
  - `ColorSecondaryEmerald` (#10B981)
  - `ColorTertiaryAmber` (#F59E0B)
  - `ColorErrorRed` (#EF4444)
  - Zusätzliche Neutral-, Surface- und Text-Farben
  - Typographische Styles (headline-xl, headline-lg, body-lg, body-md, body-sm, price-hero, label-code)
  - Spacing-Skala (space-xs 4px, space-sm 8px, space-md 16px, space-lg 24px, space-xl 40px)
  - Border-Radius-Token (rounded 8px, rounded-lg 16px, rounded-full)
  - Schatten-Ebenen (0–3)
- **Schriftart-Ressourcen:**
  - `Assets/Fonts/Inter-Regular.ttf`, `Inter-Bold.ttf` etc.
  - `Assets/Fonts/JetBrainsMono-Regular.ttf`

### Test-Infrastruktur
- **Projekte:**
  - `Tankradar.Tests.Unit` – Unit-Tests (xUnit/NUnit)
  - `Tankradar.Tests.Integration` – Integrationstests
  - `Tankradar.Tests.E2E` – End-to-End-Tests (FlaUI gegen Windows-App)
- **Test-Basis:**
  - `TestDataContext` – Isolierte Test-Datenhaltung (separat vom Entwicklungs- und Echtbetrieb)
  - Initialer E2E-Test: `NavigationSmokeTest` (App starten, durch alle vier Navigationsbereiche navigieren)

### Konfiguration
- **Bundle-ID-Management:**
  - Klasse `AppConfiguration` mit `BundleId`-Property (default: `com.softwareschmiede.tankradar.dev`)
  - Wert aus Umgebungsvariable oder Konfigurationsdatei lesbar
  - Placeholder blockiert Entwicklung nicht
- **Plattformkonfiguration:**
  - `MauiProgram.cs` registriert Plattformspezifika
  - Windows-Standalone-Config: Executable läuft ohne Installation

### Versioning und Ablage
- **Versionierungs-Datei:** `Directory.Build.props` oder `.csproj` mit Versionsnummer `0.1.0`
- **Review-Versions-Verzeichnis:** `review-versions/0.1.0_JJJJ-MM-TT/` (per `.gitignore` ausgeschlossen)
- **Skript/Tool zur Versionserstellung:** z. B. PowerShell-Skript `scripts/create-review-version.ps1`
  - Kopiert startfähige Windows-Anwendung
  - Erzeugt Changelog-Datei
  - Strukturiert unter Versionsnummer und Datum

## Implementierungsansatz

### Projektaufbau
1. **MAUI-Projekt-Template initialisieren:** Nutzung von `.NET SDK 10.0.401` mit MAUI-Workloads (iOS, Windows)
2. **Plattformziele konfigurieren:**
   - iOS 16 als Mindestversion in `.csproj`
   - Windows Desktop Runtime (Standalone)
3. **MVVM-Framework integrieren:** MAUI-native Dependency Injection oder CommunityToolkit.Mvvm
4. **Navigation aufbauen:** `AppShell.xaml` mit vier Tab-Routes für die Bereiche

### Design-System-Integration
1. **Schriftarten einbinden:**
   - Inter und JetBrains Mono als App-Ressourcen
   - iOS: Einträge in `Info.plist`
   - Windows: Schriftart-Verzeichnis im Projekt
2. **Design-Ressourcen definieren:** Zentrales `Resources/DesignSystem.xaml` mit allen Farben, Spacing- und Typography-Token
3. **Light/Dark-Mode:** MAUI `AppTheme` nutzen, Farbwechsel über Conditional XAML oder Runtime-Binding
4. **Touch-Target-Validierung:** Alle interaktiven Elemente mindestens 44×44 px

### Test-Infrastruktur
1. **Test-Projekte anlegen:** Unit, Integration, E2E mit entsprechenden Verweisen auf Hauptproject
2. **Test-Datenhaltung:** Separate `TestDataContext` mit isoliertem Datenverzeichnis
3. **FlaUI-E2E-Setup:** Windows-App-Referenz, erste Navigations-E2E-Test
4. **Mock-/Test-Services:** Stubs für externe APIs (werden in späteren Schritten benötigt)

### Versionsverwaltung
1. **Semantic Versioning:** Start mit `0.1.0`, Regeln aus CI-Workflow-Vorlage
2. **Windows-Zwischenstand-Skript:** PowerShell-Skript, das
   - Den aktuellen Windows-Release-Build kopiert
   - Unter `review-versions/<Version>_<JJJJ-MM-TT>/` ablegt
   - Changelog-Template bereitstellt
   - Benutzerfreundlich aufrufbar ist (z. B. `./scripts/create-review-version.ps1 -Version 0.1.0`)

## Konfiguration

### Bundle-ID
- **Ebene:** Anwendungsebene
- **Speicherort:** `MauiProgram.cs` oder dedizierte `AppSettings.json` / Umgebungsvariable
- **Placeholder:** `com.softwareschmiede.tankradar.dev` (Entwicklung)
- **Produktiv:** Vom Anwender festgelegt, keine Blockade durch fehlenden Wert
- **Nutzung:** iOS Bundle-ID in `Info.plist`, Package Name in Android (später)

### Design-System-Konfiguration
- **Zentrale Verwaltung:** `Resources/DesignSystem.xaml`
- **Überschreiben auf View-Ebene:** Lokal zulässig für Spezialfälle, aber Konsistenz angestrebt
- **Theme-Umschaltung:** Global über App-Theme-Binding

### Test-Konfiguration
- **Test-Datenverzeichnis:** Umgebungsvariable `TEST_DATA_PATH` oder Default `{AppLocalData}/Tests/`
- **E2E-Mock-Services:** Umgebungsvariablen für Test-Endpunkte (werden später in Schritt 5 relevant)

## Offene Fragen

1. **AppShell vs. Navigation Stack:** Sollen die vier Bereiche als `TabBar`-Navigation (empfohlen nach Design) oder als Stack-Navigation mit Tab-Buttons oben umgesetzt werden? → Design deutet auf Bottom-TabBar hin.

2. **Schriftart-Fallback:** Was geschieht, wenn Inter oder JetBrains Mono auf iOS nicht geladen werden? → Standard-Fallback verwenden oder Error melden?

3. **Dark Mode Standard:** Soll die App standardmäßig im Light Mode starten oder das Systemthema folgen? → Empfehlung: Systemtheme folgen.

4. **Test-Datenverzeichnis Isolation:** Sollen Unit- und Integrationstests dieselbe isolierte Testdatenbank nutzen oder separate? → Empfehlung: Getrennt (Unit: In-Memory, Integration: echte SQLite im Test-Verzeichnis).

5. **Review-Versions-Skript-Sprache:** PowerShell nur (Windows-fokussiert) oder auch Bash für macOS/Linux-Kompatibilität? → Kontext deutet auf Windows-Fokus, aber bei Bedarf bash-Wrapper ergänzbar.

6. **CI/CD-Integration Timing:** Wird das Review-Versions-Skript durch die CI-Pipeline automatisch aufgerufen oder nur manuell? → Anforderung deutet auf Manuell, aber: Klärung mit Team.

7. **Mindestgröße 44×44 Validierung:** Soll eine automatische Lint-Regel (z. B. über GitHub Actions oder lokale Pre-Commit-Hooks) erzwungen werden? → Empfehlung: Erst nach Schritt 2 (Git-Hooks).

8. **Offline-Datenbank-Schema:** Wird bereits in Schritt 1 eine Datenbank-Grundstruktur angelegt, oder nur in Schritt 4? → Schritt 1 vorbereiten, Schritt 4 umsetzt (per Projektplan).

---

**Kontext:** Entwicklungsschritt 1 ("Grundgerüst") des Projekts "Tankradar"; folgt nach Projektplan und Design-System-Inventar; .NET SDK 10.0.401 und MAUI-Workloads lokal verfügbar.
