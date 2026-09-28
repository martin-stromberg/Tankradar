# Lokale Toolchain: Tankradar Entwicklungsumgebung

Dokumentiert am: 2026-09-28

## .NET SDK und Runtimes

### SDK-Version

```
.NET SDK Version:     10.0.401
Commit:               e34a38d2ae
Workload Version:     10.0.401
MSBuild Version:      18.9.11+e34a38d2a
Installation:         C:\Program Files\dotnet\sdk\10.0.401\
```

**Bewertung:** ✅ Excellente MAUI-Unterstützung. .NET 10.0 enthält neueste MAUI-Releases mit allen notwendigen Features.

### Installierte Runtimes

| Runtime | Versionen | Status |
|---------|-----------|--------|
| **.NETCore.App** | 8.0.31, 9.0.20, 10.0.12 | ✅ Multiple Versions |
| **AspNetCore.App** | 8.0.31, 9.0.20, 10.0.12 | ✅ Verfügbar |
| **WindowsDesktop.App** | 8.0.31, 9.0.20, 10.0.12 | ✅ Verfügbar |

**Bewertung:** ✅ Umfassende Runtime-Auswahl. Ermöglicht Cross-Target-Entwicklung.

### Architecture Support

```
Primary:   x64 (Windows 11 Pro x64)
Secondary: x86 available at C:\Program Files (x86)\dotnet\
```

**Bewertung:** ✅ x64 primär, x86 als Fallback vorhanden.

## MAUI Workloads

### Installierte Workloads

| Workload | Manifestversion | SDK-Quelle | Installationstyp | Status |
|----------|-----------------|-----------|-----------------|--------|
| **ios** | 26.5.10318/10.0.100 | SDK 10.0.400 | MSI | ✅ Installiert |
| **android** | 36.1.69/10.0.100 | SDK 10.0.400 | MSI | ✅ Installiert |
| **maccatalyst** | 26.5.10318/10.0.100 | SDK 10.0.400 | MSI | ✅ Installiert |
| **maui-windows** | 10.0.20/10.0.100 | SDK 10.0.400 | MSI | ✅ Installiert |

**Bewertung:** ✅ Alle erforderlichen MAUI-Workloads installiert. iOS und Windows Entwicklung möglich.

### Workload-Details

#### iOS Workload (`ios` 26.5.10318)
- **Enthält:** Xamarin.iOS SDK, CocoaPods Integration, Xcode Binding
- **Zielversion:** iOS 12+ (Manifest unterstützt iOS 14+)
- **Status für Tankradar:** ✅ Bereit
- **Hinweis:** macOS + Xcode wird für physische iOS-Geräte-Builds benötigt (nicht auf Windows)

#### Android Workload (`android` 36.1.69)
- **Enthält:** Android SDK, NDK, Build Tools
- **Zielversion:** Android 9+ (API Level 28+)
- **Status für Tankradar:** ✅ Bereit (optional, primär iOS/Windows)

#### MAUI Windows Workload (`maui-windows` 10.0.20)
- **Enthält:** Windows App SDK, WinUI 3 Binding, Deployment Tools
- **Zielversion:** Windows 10+ Build 18362 (20H2+)
- **Status für Tankradar:** ✅ Bereit
- **Hinweis:** Erforderlich für Windows Desktop Development & E2E Tests (FlaUI)

## GitHub CLI (gh)

### Version

```
gh version 2.92.0
Release:  2026-04-28
```

**Bewertung:** ✅ Aktuell und funktionsfähig.

### Verfügbare Funktionen

- ✅ `gh pr create` – Pull Requests erstellen
- ✅ `gh issue list/create` – Issues verwalten
- ✅ `gh workflow run` – CI/CD-Workflows triggern
- ✅ `gh repo clone` – Repositories klonen
- ✅ `gh auth` – Authentication verwalten

**Bewertung:** ✅ Alle relevanten GitHub-Integration für CI/CD und Repository-Management verfügbar.

## Betriebssystem

| Parameter | Wert |
|-----------|------|
| **OS** | Windows 11 Pro |
| **Version** | 10.0.26200 |
| **Architecture** | x64 |
| **Build** | 26200 (aktuell) |

**Bewertung:** ✅ Windows 11 Pro ist vollständig unterstützt. Erfüllt alle Anforderungen für MAUI Windows Development.

## Empfehlenswerte Zusatztools (nicht installiert)

### Optional für Entwicklung

| Tool | Zweck | Installationsmethode |
|------|-------|----------------------|
| **Visual Studio** oder **Rider** | IDE für .NET/MAUI-Entwicklung | Windows Store, Installer |
| **Xcode** (macOS-Remote) | iOS-Builds und Simulator | macOS AppStore (für Remote-Mac) |
| **Android Studio** | Android Emulator und SDK-Manager | JetBrains Installer |
| **Postman** | API-Testing (optional) | Web/Desktop App |
| **Git Bash** oder **PowerShell 7** | Terminal mit erweiterter Scripting | Windows Store |

**Hinweis:** Für Windows Development sind diese optional, für iOS-Builds auf physischen Geräten ist macOS + Xcode erforderlich.

## Projektinitialisierung: empfehlenswerte dotnet-Befehle

```powershell
# MAUI-Projekt erstellen
dotnet new maui -n Tankradar

# Workloads prüfen
dotnet workload list

# Solution aus Projektordner erstellen
dotnet new sln -n Tankradar

# Class Library für Services hinzufügen
dotnet new classlib -n Tankradar.Services -f net10.0

# xUnit Test-Projekt hinzufügen
dotnet new xunit -n Tankradar.Tests -f net10.0

# Alle zum Solution hinzufügen
dotnet sln add Tankradar/Tankradar.csproj
dotnet sln add Tankradar.Services/Tankradar.Services.csproj
dotnet sln add Tankradar.Tests/Tankradar.Tests.csproj
```

## Windows-spezifische Anforderungen

### MSIX SDK (für App-Deployment)

**Status:** Wird als Teil der MAUI Windows Workload installiert.

```
Pfad: C:\Program Files (x86)\Windows Kits\10\bin\<version>\x64\
```

**Bewertung:** ✅ Vorhanden. Ermöglicht Windows Package-Erstellung.

### Windows Developer Mode

**Empfohlen für lokale Testing:**

```powershell
# Windows Developer Mode aktivieren (Einstellungen > Update & Sicherheit > Entwickleroptionen)
# oder via PowerShell:
reg add HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock /t REG_DWORD /v AllowDevelopmentWithoutDevLicense /d 1
```

**Status:** ❓ Nicht überprüft (wird für E2E Tests mit FlaUI benötigt)

## Zusammenfassung der Bereitschaft

| Aspekt | Status | Hinweis |
|--------|--------|---------|
| **.NET 10 SDK** | ✅ | Vollständig & aktuell |
| **MAUI Workloads** | ✅ | iOS, Android, Windows verfügbar |
| **Windows Development** | ✅ | Alles für Desktop MAUI bereit |
| **iOS Development** | ⚠️ | SDK vorhanden, aber Xcode auf macOS benötigt für echte Devices |
| **GitHub Integration** | ✅ | gh CLI funktionsfähig |
| **IDE** | ❌ | Visual Studio oder Rider empfohlen, nicht installiert |
| **E2E Testing (FlaUI)** | ⚠️ | SDK vorhanden, Developer Mode ggf. erforderlich |

## Nächste Schritte

1. **IDE installieren:** Visual Studio 2022+ oder JetBrains Rider (beide mit MAUI-Support)
2. **MAUI-Projekt erstellen:** `dotnet new maui -n Tankradar`
3. **Projektstruktur aufbauen:** Klassenverzeichnisse, Services, Tests hinzufügen
4. **EF Core & Database Setup:** Migrations konfigurieren
5. **DI-Container in MauiProgram.cs registrieren**

---

**Toolchain-Status dokumentiert:** 2026-09-28  
**Schlussfolgerung:** Umgebung ist bereit für Phase 1 Entwicklung (Grundstruktur & Datenmodell)
