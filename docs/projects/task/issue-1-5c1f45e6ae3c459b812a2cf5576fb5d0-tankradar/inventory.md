# Bestandsaufnahme: Tankradar – Mobiles Preis- und Ladestations-Tracking

Analysiert am: 2026-09-28 (Commit: 2aebc74)

## Zusammenfassung

Das Repository befindet sich im **Initialzustand**. Die Anforderungsanalyse ist abgeschlossen, aber die Implementierung der Anwendung hat noch nicht begonnen.

### Vorhanden

- ✅ Detaillierte Anforderungsspezifikation (`requirement.md`)
- ✅ Designentwurf (`design-draft/`)
- ✅ Repository-Struktur mit Git-Tracking
- ✅ .gitignore konfiguriert
- ✅ Grundlegende Projekt-Dokumentation

### Nicht vorhanden

- ❌ Keine Solution- oder Project-Dateien (.sln, .csproj)
- ❌ Keine Quellcode-Dateien (Domain Models, Services, ViewModels, Views)
- ❌ Keine Datenbank-Layer (EF Core DbContext, Migrations)
- ❌ Keine Tests (Unit, Integration, E2E)
- ❌ Keine DI-Konfiguration oder App-Startup-Code
- ❌ Keine CI/CD-Workflows

### Test-Ausgangszustand

**Keine Tests vorhanden.** Das Projekt hat keine Test-Projekte, Test-Runner oder Test-Artefakte. Dies ist erwartungsgemäß für einen Initialzustand. Eine vollständige Übersicht des Test-Ausgangszustands und der geplanten Test-Suiten findet sich in [tests.md](inventory/tests.md).

### Lokale Toolchain-Bereitschaft

**Umgebung vollständig bereit für Entwicklung.** Die lokale Entwicklungsumgebung ist optimal für MAUI-Entwicklung konfiguriert:

- ✅ **.NET 10.0.401 SDK** – Neueste Version mit vollständiger MAUI-Unterstützung
- ✅ **MAUI Workloads** – iOS, Android, Windows vollständig installiert
- ✅ **Windows Development** – Alle Tools für Windows-Desktop-Entwicklung bereit
- ✅ **GitHub CLI** – Version 2.92.0, funktionsfähig für Repository-Management
- ⚠️ **IDE** – Visual Studio oder JetBrains Rider empfohlen (nicht installiert)
- ⚠️ **iOS Device Builds** – Xcode auf macOS erforderlich für echte Devices

Siehe [toolchain.md](inventory/toolchain.md) für Details.

### Geplante Phasierung (aus Anforderung)

Die Implementierung ist in sieben Phasen unterteilt:

1. **Grundstruktur & Datenmodell** – MAUI-Projekt, Domain Models, EF Core, DI
2. **Basis-Services und API-Integration** – LocationService, FuelPriceService, FavoriteGroupService, etc.
3. **UI und ViewModels** – HomePage, SearchPage, StationDetailPage, FavoritesPage, SettingsPage
4. **Tankbuch und Statistiken** – LogbookPage, FuelEntryService, StatisticsService, Charts
5. **Sicherheit und Offline-Modus** – SecureStorageService, BackupService, Encryption
6. **Testing und QA** – Unit Tests, Integration Tests, E2E Tests
7. **Deployment und CI/CD** – GitHub Actions, iOS Build, Windows Installer

## Details

Detaillierte Untersuchungen für individuelle Bereiche sind nicht notwendig, da keine Implementierung vorhanden ist. Die Bestandsaufnahme beschränkt sich auf die verfügbaren Artefakte und externe Vorgaben:

- [Test-Ausgangszustand und geplante Test-Suiten](inventory/tests.md)
- [Design-System: E-Mobility & Fuel Navigator](inventory/design-system.md)
- [Lokale Toolchain: Entwicklungsumgebung](inventory/toolchain.md)
- [Externe Vorgaben und Standards](inventory/external-standards.md)

### Projektverzeichnis-Struktur (aktuell)

```
.
├── .claude/                    # Claude Code Konfiguration
├── .git/                       # Git Repository
├── .gitignore                  # Git-Ignore Konfiguration
├── README.md                   # Projekt-Beschreibung (minimal)
├── issue.md                    # Original-Anforderung (englisch/deutsch)
├── design-draft/               # Design-Entwurf (ZIP)
├── docs/
│   └── projects/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar/
│       ├── requirement.md      # Anforderungsübersetzung (detailliert)
│       ├── todo.md             # TODO-Liste
│       └── inventory/          # Diese Bestandsaufnahme
│           ├── tests.md
│           └── test-results/
└── review-versions/            # Zwischenstände (laut Stakeholder-Entscheidung)
```

### Fehlende Verzeichnisse (in Phase 1 zu erstellen)

```
Tankradar/
├── Tankradar.App/
├── Tankradar.App.iOS/
├── Tankradar.App.Windows/
├── Tankradar.Core/
├── Tankradar.Services/
├── Tankradar.Data/
├── Tankradar.Api/
├── Tankradar.UI/
├── Tankradar.Security/
├── Tankradar.Tests/
├── Tankradar.IntegrationTests/
├── Tankradar.E2ETests/
└── Tankradar.Infrastructure/
```

## Nächste Schritte

1. **Phase 1: Grundstruktur & Datenmodell**
   - MAUI-Projekt-Scaffold erstellen
   - Domain Model-Klassen implementieren
   - EF Core DbContext und Migrations konfigurieren
   - DI-Container in MauiProgram.cs einrichten

2. **Vor der Implementierung prüfen:**
   - .NET-Version und MAUI-Workload-Installation
   - iOS-Deployment-Anforderungen (CocoaPods, provisioning profiles)
   - Windows-Build-Anforderungen (MSIX SDK, etc.)

## Anhang: Verfügbare Ressourcen

### Design-System

Die verbindliche Grundlage für die UI-Implementierung ist unter `design-draft/stitch_smart_fuel_charge_tracker.zip` vorhanden. Das Design-System ist dokumentiert unter [design-system.md](inventory/design-system.md) und umfasst:

- **Farbpalette:** Primary (Teal), Secondary (Emerald), Tertiary (Amber), Error (Rot)
- **Typografie:** Inter (Hauptschrift), JetBrains Mono (technische Daten)
- **Komponenten:** Buttons, Badges, Stationskarten, Map-Pins, Eingabefelder
- **Screens:** HomePage, SearchPage, StationDetailPage, LogbookPage (Light & Dark Mode)
- **Layout:** Responsive Grid (Mobile, Tablet, Desktop)

### Externe Standards und Vorgaben

Die folgenden externen Repositories definieren verbindliche Standards für Architektur, Workflows und Deployment:

- **CI/CD-Workflows:** https://github.com/martin-stromberg/Pattern-Collection/blob/main/CI-Workflows/instructions.md
- **Git-Hooks:** https://github.com/martin-stromberg/Pattern-Collection/blob/main/Git-Hooks/readme.md
- **iOS-Deployment:** https://github.com/martin-stromberg/VideoPlayer-App/blob/staging/scripts/iOS-Deployment.ps1

Eine detaillierte Analyse dieser Standards und deren Relevanz für Tankradar findet sich in [external-standards.md](inventory/external-standards.md).

### Stakeholder-Entscheidungen (aus Anforderung)

- **Ablage funktionierender Windows-Zwischenstände:** `review-versions/` (git-ignored), je Version ein benannter Unterordner (z.B. `review-versions/0.1.0_2026-09-28/`)
- **Test-Abdeckung:** Angestrebte Ziele in Phase 6 (Anforderung nicht spezifiziert, Standard: >70% Core-Logic, >50% overall)

---

**Bestandsaufnahme erstellt:** 2026-09-28  
**Status:** Projekt initialisiert, Anforderung vollständig analysiert, Implementierung beginnt in Phase 1
