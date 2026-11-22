# Blaupause: `martin-stromberg/Softwareschmiede` PR #432

Gemergter PR „Flaky-Test-Härtung: OsInterface-Tagging, Deadline-Angleichung, Cleanup,
Flaky-Retry (Issue #428)". Relevante Dateien des PRs (Remote-Stand `main`, eingesehen
per `gh api`):

## Bausteine der Blaupause vs. Stand in Tankradar

| Blaupause (Softwareschmiede) | Zweck | Stand in Tankradar |
|------------------------------|-------|--------------------|
| `Infrastructure/Testing/TestCategories.cs` — `OsInterface`, `Flaky` (Konstanten, Wert = Trait-`Category`) | zentrale Filterstrings | **fehlt** (keine Trait-Nutzung) |
| `Infrastructure/Testing/OsInterfaceAttribute.cs` | `Attribute, ITraitAttribute` → `Category=OsInterface` | **fehlt**; Tankradar nutzt **xUnit 2.9.3** (Blaupause `Xunit.v3` mit `ITraitAttribute.GetTraits()`); in xUnit v2 ist `TraitAttribute` (Klasse, `Xunit`-Namespace) bzw. `ITraitDiscoverer` der Mechanismus — 1:1-Portierung nicht möglich |
| `Infrastructure/Testing/OsInterfaceFactAttribute.cs`/`OsInterfaceTheoryAttribute.cs` | Fact/Theory mit eingebauter Kategorie | **fehlt** (v3-spezifisch) |
| `Infrastructure/Testing/FlakyAttribute.cs` | `Category=Flaky`, nur mit `OsInterface` | **fehlt** |
| `Infrastructure/Testing/FlakyConventionTests.cs` | Reflexions-Meta-Test: jedes `Category=Flaky` braucht `Category=OsInterface` | **fehlt**; Muster übertragbar (in v2 `TraitAttribute`-Instanzen + `TraitAttribute`-Reflexion oder `ITraitDiscoverer`) |
| `Helpers/TempDirectoryCleanup.cs` | `DeleteBestEffort(root, processesToKill, normalizeAttributes)`: Prozessbaum-Kill + `WaitForExit(10 s)`, `Directory.Delete` mit 5 Versuchen/linearer Backoff (100 ms × n), Restfehler nur `Console.Error` | **fehlt** — vorhandene Löschungen: `E2ETestBase` (nur `IOException`), `TestDataContext` (kein Catch), `ApiKeyBuildRunner` (IOException/UnauthorizedAccess), `TempDirectory` (nur IOException) |
| `Helpers/TempDirectoryCleanupTests.cs`, `TempDirectory.cs`, `TestTempDirectoryFixture.cs` | Tests/Fixture des Helfers | **fehlt** |
| `scripts/Retry-FlakyTests.ps1` | genau ein Re-Run der `Flaky`-markierten Fehlschläge aus der TRX | **fehlt** |
| `scripts/Analyze-FlakyTestResults.ps1` | Flake-Rate (Fehlschlag → Re-Run grün) je Test, Schwellwert → Quarantäne-Issue | **fehlt** |
| `scripts/lib/TestDiscovery.ps1`, `scripts/lib/Trx.ps1` | Testdiscovery-/TRX-Parsing-Bibliotheken | **fehlt** |
| `scripts/Run-AllTestsIndividually.ps1` | Einzeltest-Läufer (Diagnose) | **fehlt** |
| `docs/help/stabilitaet/os-interface-tests.md` | Konventions-Doku | **fehlt** (`docs/help/stabilitaet/` existiert nicht) |
| Workflows: best-effort-OS-Job + `Category!=OsInterface`-Filter in der blockierenden Lane | Lane-Trennung | **fehlt** — beide Workflows haben keinerlei `--filter`; E2E läuft dokumentiert **blockierend** (bewusste Abweichung, `docs/help/ci-cd/workflows.md` „E2E-Tests als Auslieferungs-Gate") |
| Quarantäne-Issue-Erzeugung | `gh issue create` mit Dedup | kein eigener Mechanismus; Referenzmuster vorhanden: `.github/actions/create-review-issue` (Label + Dedup + `gh issue create`) |

## Übertragbarkeits-Notizen (Fakten, keine Planung)

- Blaupause-Zielplattform: Windows-Runner, .NET, xUnit **v3** mit `ITraitAttribute`.
  Tankradar: xUnit **2.9.3** (`Directory.Packages.props`), `xunit.runner.visualstudio` 3.1.5.
- Die Softwareschmiede trennt innerhalb eines Testprojekts per `--filter "Category=OsInterface"`;
  Tankradar trennt bereits nach **Projekten** (Unit/Integration/E2E) — die Lane-Trennung kann
  entweder projektweit (E2E ganz → OS-Lane) oder feingranular per Trait erfolgen
  (fachliche Entscheidung, siehe Offene Frage 1 der Anforderung).
- Die Blaupause dokumentiert `gh run rerun --job <id>` nur für den OS-Job; Tankradar hat
  derzeit keine dokumentierte Rerun-Regel.
- `docs/help/ci-cd/workflows.md` Abschnitte „E2E-Tests als Auslieferungs-Gate" und
  „Robuster App-Start der E2E-Tests" dokumentieren den heutigen blockierenden Stand
  inkl. `TransientRetry`-Begründung — Fakt, der bei der Lane-Entscheidung anzupassen wäre.
