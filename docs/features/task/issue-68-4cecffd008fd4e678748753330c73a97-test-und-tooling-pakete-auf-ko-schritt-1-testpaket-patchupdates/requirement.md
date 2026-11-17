# Anforderung (Schritt 1, Issue #68)

Kompatible Patch-/Minor-Updates ohne Breaking Changes in den drei Testprojekten
anwenden — mindestens `Microsoft.NET.Test.Sdk` 17.14.1 → 17.14.2. Vorab den
tatsächlichen Update-Stand ermitteln; Major-Updates (`Microsoft.NET.Test.Sdk`
18.x, `xunit.runner.visualstudio` 4.x, `coverlet.collector` 10.x) bewusst nicht
anwenden und die Zurückstellung kurz dokumentieren.

## Akzeptanzkriterien

- Alle drei Testprojekte verwenden konsistente, aktualisierte Patchstände.
- `dotnet test` für Unit- und Integrationstests erfolgreich; E2E-Tests laufen
  (lokal soweit möglich, verbindlich in der PR-CI).
- Zurückgestellte Major-Updates sind kurz begründet dokumentiert.

## Ermittelter Update-Stand (NuGet, 2026-11-17)

- `Microsoft.NET.Test.Sdk`: 17.14.1 → **17.14.2** (Patch; 18.x zurückgestellt)
- `xunit.runner.visualstudio`: 3.1.4 → **3.1.5** (Patch; 4.x zurückgestellt)
- `coverlet.collector`: 6.0.4 ist neuestes 6.x (8.x/10.x zurückgestellt)
- `xunit` 2.9.3, `FlaUI.Core/UIA3` 5.0.0: bereits aktuell
- `Microsoft.Data.Sqlite` 10.0.15, `SQLitePCLRaw.bundle_e_sqlite3` 2.1.14:
  bereits aktuell (2.1.14 ist zudem bewusster Pin gegen GHSA-2m69-gcr7-jv3q)
