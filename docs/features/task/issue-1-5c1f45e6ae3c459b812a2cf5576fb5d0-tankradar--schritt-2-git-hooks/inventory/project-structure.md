# Projektstruktur: Tankradar MAUI-Projekt

Dokumentation der Tankradar-Projektstruktur, die für die Implementierung von Git-Hooks relevant ist.

## Überblick

**Projekttyp:** .NET 10.0 MAUI Single-Project-Anwendung  
**Zielplattformen:** iOS 16+, Windows 10.0.19041+  
**Repository-Root:** `D:\Repositories\softwareschmiede\5c1f45e6-ae3c-459b-812a-2cf5576fb5d0`

## Verzeichnisstruktur

```
Repository-Root/
├── .git/
│   └── hooks/              # Nur Sample-Dateien, wird durch core.hooksPath überschrieben
├── .githooks/              # ← WIRD ERSTELLT: Versionierte Hook-Dateien (Schritt 2)
├── .gitignore              # Standard-VisualStudio + .env, *.env.*
├── README.md               # Projektdokumentation
├── Tankradar.sln          # Solution-Datei
├── .claude/               # Claude Code Workspace-Konfiguration (untracked)
├── docs/
│   ├── adr/                # Architekturentscheidungen
│   ├── features/           # Feature-/Requirements-Dokumentation
│   │   └── task/issue-1-5c1f45e6ae3c459b812a-2cf5576fb5d0-tankradar--schritt-2-git-hooks/
│   │       ├── requirement.md
│   │       ├── inventory.md              # ← WIRD ERSTELLT
│   │       └── inventory/                # ← WIRD ERSTELLT
│   │           ├── tests.md
│   │           ├── external-sources.md
│   │           ├── project-structure.md
│   │           └── test-results/
│   │               └── dotnet-test-baseline.log
│   └── projects/           # Allgemeine Projektinventur (read-only)
│       └── task/issue-1-5c1f45e6ae3c459b812a-2cf5576fb5d0-tankradar/
│           └── inventory/
│               └── external-sources/     # Template Git-Hooks (schreibgeschützt)
├── scripts/
│   └── create-review-version.ps1        # PowerShell-Skript für Review-Versionen
├── src/
│   ├── Tankradar.MAUI/                  # Hauptanwendung (Single Project)
│   │   ├── Tankradar.MAUI.csproj
│   │   ├── Models/                      # Datenmodelle (aktuell leer)
│   │   ├── ViewModels/                  # MVVM-ViewModels
│   │   │   ├── BaseViewModel.cs
│   │   │   ├── FavoritesViewModel.cs
│   │   │   ├── MapViewModel.cs
│   │   │   ├── FuelLogViewModel.cs
│   │   │   └── SettingsViewModel.cs
│   │   ├── Views/                       # XAML-UI-Seiten
│   │   │   ├── FavoritesPage.xaml
│   │   │   ├── MapPage.xaml
│   │   │   ├── FuelLogPage.xaml
│   │   │   └── SettingsPage.xaml
│   │   ├── Services/
│   │   │   └── AppConfiguration.cs
│   │   │   └── AppDataPathProvider.cs   # Datenverzeichnis-Resolver (TEST_DATA_PATH support)
│   │   ├── Resources/
│   │   │   ├── DesignSystem.xaml        # Zentrales Design-System (Farben, Typografie, Spacing)
│   │   │   ├── Fonts/                   # Embedded Fonts (Inter, JetBrains Mono, OpenSans)
│   │   │   │   ├── Inter-*.ttf
│   │   │   │   ├── JetBrainsMono-*.ttf
│   │   │   │   ├── OFL-Inter.txt
│   │   │   │   └── OFL-JetBrainsMono.txt
│   │   │   ├── Raw/
│   │   │   │   └── appsettings.json     # Bundle-ID-Konfiguration
│   │   │   └── ...
│   │   ├── Platforms/                   # Plattformspezifischer Code
│   │   │   ├── iOS/
│   │   │   ├── Windows/
│   │   │   ├── Android/
│   │   │   └── MacCatalyst/
│   │   ├── App.xaml(.cs)
│   │   ├── MauiProgram.cs
│   │   └── ...
│   ├── Tankradar.Tests.Unit/            # Unit-Tests (xUnit)
│   │   ├── Tankradar.Tests.Unit.csproj
│   │   ├── Unit/
│   │   │   ├── AppDataPathProviderTests_DataDirectoryResolution.cs
│   │   │   └── BaseViewModelTests_PropertyBinding.cs
│   │   └── BaseTest.cs                  # Test-Basisklasse
│   ├── Tankradar.Tests.Integration/     # Integration-Tests (xUnit)
│   │   ├── Tankradar.Tests.Integration.csproj
│   │   ├── Integration/
│   │   │   └── TestDataContextTests_Lifecycle.cs
│   │   └── ...
│   ├── Tankradar.Tests.E2E/             # End-to-End-Tests (xUnit + FlaUI)
│   │   ├── Tankradar.Tests.E2E.csproj
│   │   ├── E2E/
│   │   │   └── FlaUI/
│   │   │       └── NavigationE2ETests.cs
│   │   └── ...
│   └── TestSupport/                     # Gemeinsame Test-Utilities
│       └── ... (Hilfsmethoden, Test-Fixtures)
└── design-draft/                        # Design-Entwürfe (Archivierung)
```

## Projektdateien und Konfiguration

### Hauptprojekt: `Tankradar.MAUI.csproj`
**Pfad:** `src/Tankradar.MAUI/Tankradar.MAUI.csproj`

**Zielframeworks:**
- `net10.0-ios` (iOS 16.0+)
- `net10.0-windows10.0.19041.0` (Windows)
- `net10.0-android` (wird gebaut, aber nicht primär unterstützt)
- `net10.0-maccatalyst` (wird gebaut, aber nicht primär unterstützt)

**Besonderheiten:**
- Single-Project-Ansatz (MAUI)
- Keine XML-Dokumentation erzwungen (wird über Hooks prüfbar)
- `GenerateDocumentationFile` unkonfiguriert (Requirement: soll durch Hooks prüfbar sein)

### Test-Projekte
**Framework:** .NET 10.0 (Windows 10.0.19041.0)  
**Test-Framework:** xUnit mit VSTest-Adapter  
**FlaUI:** Nur in E2E-Tests für UI-Automation

### .gitignore
**Pfad:** `.gitignore`

**Wichtige Ausschlüsse für Git-Hooks:**
- `*.env` — Umgebungsvariablen mit Secrets
- `bin/`, `obj/`, `TestResults/` — Build-Artefakte (ausgeschlossen in Hook-Scans)
- `review-versions/` — Lokale Review-Builds
- `.vs/` — Visual Studio-Temporärdateien

**Für Hooks relevant:**
- Secrets-Pattern nicht explizit definiert (Requirement: `forbidden-patterns-check.py` nötig)
- DB-Dumps, Logdateien nicht explizit aufgelistet

## Dependency-Management

### NuGet-Packages (Beispiele aus Test-Projekten)
- **xUnit:** `xunit` + `xunit.runner.visualstudio`
- **FlaUI:** `FlaUI.Core`, `FlaUI.UIA3` (nur E2E)
- **MAUI:** `Microsoft.Maui.Controls`, `Microsoft.Maui.Essentials`
- **Logging:** (unklar, zu prüfen)

### Python-Dependencies
Aktuell für Template-Hooks:
- Standard Library (`re`, `subprocess`, `pathlib`, `argparse`)
- XML-Parsing: `xml.etree.ElementTree` (Standard)
- Weitere: unklar, zu dokumentieren

## Testdaten und Isolation

### TEST_DATA_PATH-Mechanik
**Komponente:** `AppDataPathProvider` in `Services/AppDataPathProvider.cs`

**Funktionsweise:**
- E2E-Tests setzen Umgebungsvariable `TEST_DATA_PATH` auf isoliertes Verzeichnis
- `IAppDataPathProvider.GetDataDirectory()` prüft `TEST_DATA_PATH`, nutzt es falls gesetzt
- Ansonsten: `FileSystem.AppDataDirectory` (Plattform-Standard)
- Nach Test-Lauf: Verzeichnis wird gelöscht

**Implikation für Git-Hooks:**
- Hooks sollten keine Test-Datenverzeichnisse committen (ohnehin in `.gitignore`)
- Test-Execution-Validierung (`test-execution-check.py`) könnte diese Umgebungsvariable setzen

## Solutionstruktur: `Tankradar.sln`

**Projekte:**
1. `Tankradar.MAUI` (Hauptanwendung)
2. `Tankradar.Tests.Unit` (Unit-Tests)
3. `Tankradar.Tests.Integration` (Integration-Tests)
4. `Tankradar.Tests.E2E` (E2E-Tests)
5. `TestSupport` (Test-Utilities, gemeinsam genutzt)

**Konfigurationen:** `Debug|Any CPU` (Standard)

## CI/Build-Integration

**Aktueller Zustand:**
- Keine GitHub Actions oder CI-Workflows erkannt (nur lokales PowerShell-Skript)
- README empfiehlt `dotnet test Tankradar.sln`

**Zu prüfen:**
- Existiert ein `.github/workflows/`-Verzeichnis?
- Wird CI-Setup im Requirement erwähnt?

## Git-Hooks-Integration (Ziel Schritt 2)

### Geplante Struktur

```
.githooks/                                  # ← Wird versioniert
├── pre-commit                              # Haupthook vor Commit
├── pre-push                                # Haupthook vor Push
├── install-hooks.cmd                       # Installation (Windows)
├── install-hooks.sh                        # Installation (Unix)
├── csproj-xmldoc-check.py                  # C#-Dokumentation
├── no-notimplemented-check.py              # Stub-Prüfung
├── enum-coverage-check.py                  # Enum-Testabdeckung
├── translation-check.py                    # Lokalisierungs-Prüfung
├── format-code-style-check.py              # NEU: Code-Formatierung (Anforderung)
├── forbidden-patterns-check.py             # NEU: Secrets/Patterns (Anforderung)
├── conventional-commits-check.py           # NEU: Commit-Message-Format (Anforderung)
└── test-execution-check.py                 # NEU: Test-Validierung vor Push (Anforderung)
```

### Aktivierung nach Schritt 2

```bash
# Windows:
.githooks\install-hooks.cmd

# Unix/macOS:
.githooks/install-hooks.sh

# Ergebnis: git config --local core.hooksPath .githooks
```

## Besonderheiten für MAUI/XAML

### Razor-Checks entfallen
Template enthält Razor-spezifische Checks:
- `razor-l10n-check.py` — Nicht anwendbar (XAML statt Razor)
- `razor-usage-check.py` — Nicht anwendbar (XAML statt Razor)

**Äquivalente für XAML unklar:** XAML-Ressourcen-Lokalisierung und Komponenten-Nutzung müssen neu konzipiert werden.

### Android/iOS-Build-Hinweise

Template-pre-commit sucht dynamisch nach `.sln` und optional nach `SecretScan.csproj`, `MarkdownLinkCheck.csproj`. Diese Projekte existieren in Tankradar nicht (noch).

**Auswirkung:**
- Hooks werden trotzdem funktionieren (optionale Projekte)
- Gegebenenfalls müssen `SecretScan` oder `MarkdownLinkCheck` später hinzugefügt werden

## Zusammenfassung für Hook-Implementierung

| Aspekt | Status | Notiz |
|--------|--------|-------|
| Projekttyp | MAUI Single Project | xUnit-Tests vorhanden, vierteiliger Build (iOS/Android/Windows/macOS) |
| C#-Struktur | Modular (ViewModels, Services, Resources) | XML-Doc-Prüfung direkt anwendbar |
| Enums | Zu prüfen | Coverage-Check soll verfügbar sein |
| Ressourcen/Lokalisierung | XAML | Razor-Checks entfallen, XAML-Äquivalent nötig |
| Tests | 3 Suites, 11 Tests bestehen | Baseline vor Hook-Änderungen erfasst |
| .gitignore | Standard + .env | Hooks-Checks für Secrets müssen Muster definieren |
| Secrets/Config | appsettings.json (Bundle-ID) | Mögliches Ziel für `forbidden-patterns-check.py` |
| Test-Daten | Isoliert via TEST_DATA_PATH | Hook-Mechanik muss berücksichtigen |
| CI-Integration | Nicht erkannt | Nur lokale PowerShell-Skripte |
