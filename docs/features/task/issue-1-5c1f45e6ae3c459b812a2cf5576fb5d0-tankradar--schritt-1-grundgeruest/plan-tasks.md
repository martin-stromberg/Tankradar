# Tasks: App-Grundgerüst, Navigation und Design-System

**Erstellt:** 2026-09-28  
**Zugehöriger Plan:** `docs/features/task/issue-1-5c1f45e6ae3c459b-812a-2cf5576fb5d0-tankradar--schritt-1-grundgeruest/plan.md`

Alle Zeilen starten mit Status `Offen` und Testnachweis `—`. Aktualisierung erfolgt durch `/review-plan` oder manuelle Verfolgung.

## Projektstruktur und Infrastruktur

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Projektstruktur | `.sln` Datei `Tankradar.sln` erstellen | Offen | — |
| 2 | Projektstruktur | Hauptprojekt `Tankradar.MAUI.csproj` mit iOS 16 und Windows-Zielen konfigurieren | Offen | — |
| 3 | Projektstruktur | Verzeichnisse `src/Models/`, `src/ViewModels/`, `src/Views/`, `src/Resources/`, `src/Platforms/` anlegen | Offen | — |
| 4 | Projektstruktur | `.csproj` Zielplattformen und Mindestversionen einstellen: iOS 16.0, Windows 10.0.19041 | Offen | — |
| 5 | Projektstruktur | `.gitignore` überprüfen: `review-versions/` muss ausgeschlossen sein | Offen | — |

## Konfiguration und Abhängigkeiten

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 6 | Konfiguration | Klasse `AppConfiguration.cs` mit `BundleId`-Property (Standardwert: `com.softwareschmiede.tankradar.dev`) implementieren | Offen | — |
| 7 | Konfiguration | `appsettings.json` mit AppConfiguration-Struktur anlegen | Offen | — |
| 8 | Konfiguration | `MauiProgram.cs` mit `.AddMauiApp<App>()` und DI-Registrierung anlegen | Offen | — |
| 9 | Konfiguration | Font-Registrierung in `MauiProgram.cs` vorbereiten (`.AddFont()` Aufrufe) | Offen | — |
| 10 | Konfiguration | `AppConfiguration` in DI registrieren: `.AddSingleton<AppConfiguration>()` | Offen | — |
| 11 | Konfiguration | Plattformspezifische Konfiguration in `MauiProgram.cs` vorbereiten | Offen | — |

## MVVM-Infrastruktur

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 12 | MVVM | Klasse `BaseViewModel.cs` (abstract) mit `INotifyPropertyChanged` implementieren | Offen | — |
| 13 | MVVM | Property `Title` (string) in `BaseViewModel` hinzufügen | Offen | — |
| 14 | MVVM | Property `IsBusy` (bool) in `BaseViewModel` hinzufügen | Offen | — |
| 15 | MVVM | Property `IsNotBusy` (bool) in `BaseViewModel` hinzufügen (Binding-Konverter) | Offen | — |
| 16 | MVVM | Hilfsmethode `SetProperty<T>()` in `BaseViewModel` implementieren | Offen | — |
| 17 | MVVM | Virtuelle Methode `OnAppearing()` in `BaseViewModel` hinzufügen | Offen | — |
| 18 | MVVM | ViewModel `FavoritesViewModel.cs` (erbt von `BaseViewModel`) anlegen | Offen | — |
| 19 | MVVM | ViewModel `MapPageViewModel.cs` (erbt von `BaseViewModel`) anlegen | Offen | — |
| 20 | MVVM | ViewModel `TankbookViewModel.cs` (erbt von `BaseViewModel`) anlegen | Offen | — |
| 21 | MVVM | ViewModel `SettingsViewModel.cs` (erbt von `BaseViewModel`) anlegen | Offen | — |
| 22 | MVVM | Alle ViewModels in `MauiProgram.cs` registrieren | Offen | — |

## Views (Pages)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 23 | Views | Page `FavoritesPage.xaml` mit leerer Grundstruktur anlegen | Offen | — |
| 24 | Views | Code-Behind `FavoritesPage.xaml.cs` mit BindingContext-Setup implementieren | Offen | — |
| 25 | Views | Page `MapPage.xaml` mit leerer Grundstruktur anlegen | Offen | — |
| 26 | Views | Code-Behind `MapPage.xaml.cs` mit BindingContext-Setup implementieren | Offen | — |
| 27 | Views | Page `TankbookPage.xaml` mit leerer Grundstruktur anlegen | Offen | — |
| 28 | Views | Code-Behind `TankbookPage.xaml.cs` mit BindingContext-Setup implementieren | Offen | — |
| 29 | Views | Page `SettingsPage.xaml` mit leerer Grundstruktur anlegen | Offen | — |
| 30 | Views | Code-Behind `SettingsPage.xaml.cs` mit BindingContext-Setup implementieren | Offen | — |

## Design-System

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 31 | Design-System | `Resources/DesignSystem.xaml` als ResourceDictionary anlegen | Offen | — |
| 32 | Design-System | Farbtokens definieren: `ColorPrimaryTeal` (#0F766E) | Offen | — |
| 33 | Design-System | Farbtokens definieren: `ColorSecondaryEmerald` (#10B981) | Offen | — |
| 34 | Design-System | Farbtokens definieren: `ColorTertiaryAmber` (#F59E0B) | Offen | — |
| 35 | Design-System | Farbtokens definieren: `ColorErrorRed` (#EF4444) | Offen | — |
| 36 | Design-System | Neutral-, Surface- und Text-Farben definieren (Light Mode) | Offen | — |
| 37 | Design-System | Neutral-, Surface- und Text-Farben definieren (Dark Mode) | Offen | — |
| 38 | Design-System | Typografie-Stile definieren: `headline-xl`, `headline-lg`, `headline-md` | Offen | — |
| 39 | Design-System | Typografie-Stile definieren: `body-lg`, `body-md`, `body-sm` | Offen | — |
| 40 | Design-System | Typografie-Stile definieren: `price-hero`, `label-code` | Offen | — |
| 41 | Design-System | Spacing-Tokens definieren: `space-xs` (4px), `space-sm` (8px), `space-md` (16px) | Offen | — |
| 42 | Design-System | Spacing-Tokens definieren: `space-lg` (24px), `space-xl` (40px) | Offen | — |
| 43 | Design-System | Border-Radius-Tokens definieren: `rounded-sm` (4px) bis `rounded-full` | Offen | — |
| 44 | Design-System | Schatten-Definitionen für Ebenen 0–3 implementieren | Offen | — |
| 45 | Design-System | Light/Dark Mode Unterstützung in DesignSystem.xaml implementieren | Offen | — |

## Navigation (AppShell)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 46 | Navigation | `AppShell.xaml` mit `<Shell>` Element anlegen | Offen | — |
| 47 | Navigation | ShellContent für „Favoriten" (Route: `favorites`, Page: `FavoritesPage`) hinzufügen | Offen | — |
| 48 | Navigation | ShellContent für „Karte" (Route: `map`, Page: `MapPage`) hinzufügen | Offen | — |
| 49 | Navigation | ShellContent für „Tankbuch" (Route: `tankbook`, Page: `TankbookPage`) hinzufügen | Offen | — |
| 50 | Navigation | ShellContent für „Optionen" (Route: `settings`, Page: `SettingsPage`) hinzufügen | Offen | — |
| 51 | Navigation | Bottom TabBar mit Icons in AppShell konfigurieren | Offen | — |
| 52 | Navigation | Standardroute auf „Favoriten" setzen | Offen | — |
| 53 | Navigation | `AppShell.xaml.cs` mit Title und Icon-Bindungen implementieren | Offen | — |

## App-Einstiegspunkt

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 54 | App-Einstiegspunkt | `App.xaml` anlegen mit `<Application>` Element | Offen | — |
| 55 | App-Einstiegspunkt | `App.xaml.Resources` mit Verweis auf `DesignSystem.xaml` konfigurieren | Offen | — |
| 56 | App-Einstiegspunkt | `App.xaml.cs` mit Constructor und DI implementieren | Offen | — |
| 57 | App-Einstiegspunkt | `MainPage = new AppShell()` in `App.xaml.cs` setzen | Offen | — |
| 58 | App-Einstiegspunkt | Event-Handler für `AppThemeChanged` vorbereiten | Offen | — |
| 59 | App-Einstiegspunkt | `RequestedTheme = OSAppTheme.Unspecified` setzen (System-Theme folgen) | Offen | — |

## Schriftarten

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 60 | Schriftarten | Verzeichnis `Assets/Fonts/` anlegen | Offen | — |
| 61 | Schriftarten | Inter-Familie TTF-Dateien beschaffen (Regular, Bold, SemiBold) | Offen | — |
| 62 | Schriftarten | JetBrains Mono TTF-Dateien beschaffen (Regular, Bold) | Offen | — |
| 63 | Schriftarten | Schriftartdateien in `Assets/Fonts/` ablegen | Offen | — |
| 64 | Schriftarten | `.csproj` für iOS-Schriftart-Registrierung konfigurieren: `<MauiFont>` Elemente | Offen | — |
| 65 | Schriftarten | `Info.plist` für iOS-Schriftart-Registrierung aktualisieren (falls nötig) | Offen | — |
| 66 | Schriftarten | Fallback-Schriftart dokumentieren für Fehlerfall | Offen | — |

## Testinfrastruktur

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 67 | Tests | Testprojekt `Tankradar.Tests.Unit.csproj` anlegen | Offen | — |
| 68 | Tests | Testprojekt `Tankradar.Tests.Integration.csproj` anlegen | Offen | — |
| 69 | Tests | Testprojekt `Tankradar.Tests.E2E.csproj` anlegen | Offen | — |
| 70 | Tests | NuGet-Abhängigkeiten hinzufügen: xUnit 2.6+, xUnit.Runner.VisualStudio | Offen | — |
| 71 | Tests | NuGet-Abhängigkeiten hinzufügen: FlaUI 4.0+ (nur E2E) | Offen | — |
| 72 | Tests | NuGet-Abhängigkeiten hinzufügen: Moq oder NSubstitute (für Mocking) | Offen | — |
| 73 | Tests | Verzeichnisstruktur in Testprojekten anlegen: Unit/, Integration/, E2E/ | Offen | — |
| 74 | Tests | Klasse `BaseTest.cs` (abstract) in Unit-Testprojekt anlegen | Offen | — |
| 75 | Tests | Klasse `TestDataContext.cs` in Integration-Testprojekt anlegen | Offen | — |
| 76 | Tests | Klasse `E2ETestBase.cs` in E2E-Testprojekt anlegen | Offen | — |

## E2E-Tests (FlaUI)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 77 | E2E-Tests | Testdatei `Tankradar.Tests.E2E/FlaUI/NavigationE2ETests.cs` anlegen | Offen | — |
| 78 | E2E-Tests | Testmethode `AppStartsAndNavigatesThroughAllTabs()` implementieren (FlaUI-Automation) | Offen | — |
| 79 | E2E-Tests | App-Start und -Shutdown in E2E-Test implementieren | Offen | — |
| 80 | E2E-Tests | Navigation zu jedem Tab testen und Sichtbarkeit prüfen | Offen | — |
| 81 | E2E-Tests | Rückkehr zur Startseite (Favoriten) testen | Offen | — |

## Skripte und Tooling

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 82 | Skripte | Verzeichnis `scripts/` anlegen | Offen | — |
| 83 | Skripte | PowerShell-Skript `scripts/create-review-version.ps1` implementieren | Offen | — |
| 84 | Skripte | Skript-Parameter: `[-Version <version>]`, `[-ChangelogFile <path>]` | Offen | — |
| 85 | Skripte | Skript-Funktion: Windows-Release-Build ausführen | Offen | — |
| 86 | Skripte | Skript-Funktion: Build-Ausgabe in `review-versions/<Version>_<JJJJ-MM-TT>/bin/` kopieren | Offen | — |
| 87 | Skripte | Skript-Funktion: Changelog-Datei erstellen | Offen | — |
| 88 | Skripte | Skript-Fehlerbehandlung und Validierung implementieren | Offen | — |
| 89 | Skripte | Skript testen: Manuell aufrufen und Ausgabe prüfen | Offen | — |

## Dokumentation und Designabweichungen

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 90 | Dokumentation | Datei `docs/adr/0001-no-electricity-prices-in-v1.md` anlegen (oder Alternative in `docs/help/` oder README.md) | Offen | — |
| 91 | Dokumentation | ADR: Titel „Keine Strompreis-Elemente in Version 1.0" | Offen | — |
| 92 | Dokumentation | ADR: Kontext und Begründung dokumentieren (Stakeholder-Entscheidung, fehlende API) | Offen | — |
| 93 | Dokumentation | ADR: Konsequenzen für UI und Benutzer dokumentieren | Offen | — |
| 94 | Dokumentation | ADR oder README: Referenz auf Projektplan eintragen | Offen | — |
| 95 | Dokumentation | `README.md` anlegen oder aktualisieren | Offen | — |
| 96 | Dokumentation | README: Projekt-Übersicht (Tankradar, Zweck) | Offen | — |
| 97 | Dokumentation | README: Anforderungen (.NET SDK 10.0.401, MAUI-Workloads) | Offen | — |
| 98 | Dokumentation | README: Setup-Anleitung (`dotnet restore`, `dotnet build`, `dotnet run`) | Offen | — |
| 99 | Dokumentation | README: Testausführung (`dotnet test`) | Offen | — |
| 100 | Dokumentation | README: Windows-Zwischenstände (Skript-Aufruf) | Offen | — |
| 101 | Dokumentation | README: Verzeichnisstruktur kurz erklären | Offen | — |
| 102 | Dokumentation | README: Verweis auf Design-System und Designabweichungen | Offen | — |

## Validierung und lokale Tests

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 103 | Validierung | Projekt lokale Buildbarkeit testen: `dotnet build` | Offen | — |
| 104 | Validierung | Unit-Tests lokal ausführen: `dotnet test Tankradar.Tests.Unit` | Offen | — |
| 105 | Validierung | Integrationstests lokal ausführen: `dotnet test Tankradar.Tests.Integration` | Offen | — |
| 106 | Validierung | E2E-Tests lokal ausführen: `dotnet test Tankradar.Tests.E2E` | Offen | — |
| 107 | Validierung | App auf Windows manuell starten und testen: `dotnet run -f net8.0-windows` | Offen | — |
| 108 | Validierung | App: AppShell zeigt vier Tabs an | Offen | — |
| 109 | Validierung | App: Navigation zwischen Tabs funktioniert | Offen | — |
| 110 | Validierung | App: Systemtheme (Light/Dark) wird korrekt angewendet | Offen | — |

## Version 0.1.0 und Zwischenstand

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 111 | Versionierung | Versionsnummer in `.csproj` setzen: `<Version>0.1.0</Version>` | Offen | — |
| 112 | Versionierung | Optional: `<InformationalVersion>0.1.0</InformationalVersion>` hinzufügen | Offen | — |
| 113 | Versionierung | Skript ausführen: `.\scripts\create-review-version.ps1 -Version 0.1.0` | Offen | — |
| 114 | Versionierung | Verzeichnis `review-versions/0.1.0_2026-09-28/` wird erstellt | Offen | — |
| 115 | Versionierung | Executable App befindet sich in `review-versions/0.1.0_2026-09-28/bin/` | Offen | — |
| 116 | Versionierung | Changelog-Datei wird erstellt und gefüllt | Offen | — |
| 117 | Versionierung | `.gitignore` verhindert Commit von `review-versions/` | Offen | — |
| 118 | Versionierung | Dokumentation aktualisiert: Anleitung zur Nutzung des Zwischenstands | Offen | — |

---

**Gesamt-Taskanzahl:** 118 Aufgaben  
**Status der Nachverfolgung:** Alle Tasks starten mit Status `Offen`  
**Aktualisierung:** Erfolgt durch `/review-plan` oder während der Implementierung

