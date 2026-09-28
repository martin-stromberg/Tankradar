# Test-Ausgangszustand für Schritt 1

Dieses Dokument erfasst den Test-Ausgangszustand vor der Umsetzung von Schritt 1 („App-Grundgerüst, Navigation und Design-System").

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-09-28 20:30 UTC+02:00 (CEST)
- **Branch:** `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-1-grundgeruest`
- **Commit-ID:** (Branch-Start vor Implementierung)
- **Uncommittete Änderungen:** Keine
- **Testumgebung:**
  - .NET SDK: 10.0.401
  - MAUI Workloads: iOS, Windows (installiert)
  - Platform: Windows 11 Pro 10.0.26200
  - Shell: PowerShell (primär), Bash verfügbar
- **Ermittelte Testsuiten:** Keine Test-Projekte vorhanden

## Testläufe

### Ergebnis

**Keine Tests vorhanden.** Das Projekt hat keine Test-Assemblies, Test-Projekte oder Test-Quelldateien. Dies ist der erwartete Zustand für einen Initialzustand vor Schritt 1.

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Anmerkung |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| N/A  | N/A (keine Tests) | D:\Repositories\softwareschmiede\5c1f45e6-ae3c-459b-812a2cf5576fb5d0 | N/A | 0 | 0 | 0 | Keine Test-Infrastruktur vorhanden |

## Nachgewiesene bestehende Testfehler

**Keine Testfehler nachgewiesen.** Es gibt keine Tests zum Ausführen.

## Testlücken und Ausführungsprobleme

### Keine Test-Projekte

Die folgenden Test-Projekte, die gemäß Anforderung aufgebaut werden müssen, sind noch nicht vorhanden:

- `Tankradar.Tests.Unit` – Unit-Tests (xUnit/NUnit)
- `Tankradar.Tests.Integration` – Integrationstests
- `Tankradar.Tests.E2E` – End-to-End-Tests (FlaUI gegen Windows-App)

### Keine Testdateien

Keine der geplanten Testdateien existiert:

- `Tankradar.Tests.Unit/Navigation/NavigationSmokeTests.cs`
- `Tankradar.Tests.E2E/FlaUI/NavigationE2ETests.cs` (startet App, navigiert durch alle 4 Bereiche)
- Test-Basis-Klassen und TestDataContext

### Erwartete Tests in Schritt 1 (gemäß Anforderung)

Nach Abschluss von Schritt 1 werden folgende Tests erwartet:

1. **E2E-Navigations-Smoke-Test:** App startet, navigiert durch alle vier Navigationsbereiche (Favoriten, Karte, Tankbuch, Optionen) und kehrt korrekt zurück
2. **Unit-Tests für ViewModels:** Grundlegende ViewModel-Initialisierung und Property-Bindungen
3. **Integrationstests für Navigation:** Korrekte Weitergabe von Parametern zwischen Pages

## Testklassen

(Keine Testklassen vorhanden)

## Hilfsmethoden

(Keine Test-Hilfsmethoden vorhanden)

---

**Baseline-Erfassung:** 2026-09-28  
**Status:** Initialzustand - Implementierung beginnt mit Schritt 1
