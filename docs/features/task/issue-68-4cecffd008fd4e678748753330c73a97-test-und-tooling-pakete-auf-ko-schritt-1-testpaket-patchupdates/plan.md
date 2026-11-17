# Plan (Schritt 1)

1. In `src/Tankradar.Tests.Unit`, `src/Tankradar.Tests.Integration` und
   `src/Tankradar.Tests.E2E` jeweils:
   - `Microsoft.NET.Test.Sdk` 17.14.1 → 17.14.2
   - `xunit.runner.visualstudio` 3.1.4 → 3.1.5
2. `dotnet restore` + `dotnet test` (Unit, Integration) ausführen; E2E lokal
   versuchen, verbindlich über PR-CI.
3. Zurückgestellte Major-Updates dokumentieren (hier und im PR).

## Scope-Klärung

- Ausschluss (bestätigt durch Issue-Text #68, „Größere Sprünge ... separat
  bewertet werden"): keine Major-Updates in diesem Schritt.
- Keine UI-/Funktionsänderung → kein Usability-Review, keine E2E-Pflichtänderung.

## Offene Punkte

Keine.
