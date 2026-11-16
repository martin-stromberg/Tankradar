# Bestandsaufnahme (IST-Zustand) — November-2026-Sicherheitsupdates

Ermittelt am 2026-11-16 auf Branch `task/issue-63-b4ccabf7a28d446fba80fc4f6f6c183c-november-2026-sicherheitsupdat`.
Anforderung: [requirement.md](requirement.md) (Issue #63, P1).

## Kernergebnis in Kürze

- **Alle 6 zu aktualisierenden Paketreferenzen liegen in genau 2 Dateien:**
  `src/Tankradar.MAUI/Tankradar.MAUI.csproj` (5 Referenzen) und
  `src/Tankradar.Tests.E2E/Tankradar.Tests.E2E.csproj` (2 Referenzen).
  Details: [inventory/packages.md](inventory/packages.md).
- **Kein zentrales Paketmanagement:** keine `Directory.Packages.props`, keine
  `packages.lock.json`, keine `nuget.config`, kein `global.json`, kein
  `Directory.Build.props/targets`. Versionen stehen ausschließlich inline in den
  `.csproj`-Dateien.
- **`Microsoft.Maui.Controls` ist indirekt gepinnt:** die Referenz nutzt
  `Version="$(MauiVersion)"`; die eigentliche Änderung erfolgt am
  `<MauiVersion>10.0.120</MauiVersion>`-Property (`Tankradar.MAUI.csproj:31`).
- **Transitives Pinning:** `SQLitePCLRaw.bundle_e_sqlite3` `2.1.12` wurde bewusst
  als direkte Referenz gesetzt, um die transitiv gezogene, als anfällig gemeldete
  2.1.11 (GHSA-2m69-gcr7-jv3q) anzuheben — in **beiden** Projekten (App und E2E).
  Kein Pinning von `SQLitePCLRaw.core` oder `Microsoft.EntityFrameworkCore` (ohne
  Suffix) vorhanden.
- **SDK/Runtime:** installiertes SDK `10.0.403` (Requirement nennt .NET SDK
  10.0.401+ in README), Workload-Set `10.0.401`, Workload `maui-windows`
  10.0.20/10.0.100 vorhanden (plus `android`, `ios`, `maccatalyst` lokal). Die CI
  installiert Workloads per `dotnet workload restore Tankradar.sln`.
- **Release-Weg:** Push auf `staging` → `staging-ci.yml` („Pre-Release") baut bei
  Versionsänderung automatisch ein Windows-ZIP als GitHub-**Pre-Release**; Merge
  des Promotion-PRs `staging` → `main` löst `release.yml` aus, das
  `release-win-x64.zip` + `update.json` als finales Release veröffentlicht
  (self-contained `win-x64`, `scripts/package-windows.ps1`). Alternativ manueller
  Tag `v*.*.*`. Für das Akzeptanzkriterium „neuer Build für ausgelieferte
  Artefakte" reicht der normale Weg über staging → main (bzw. der RC-Build aus
  `staging-ci`). Details: [inventory/workflows.md](inventory/workflows.md).
- **Tests:** 3 Testprojekte (Unit ~350 Dateien, Integration ~68, E2E ~113);
  E2E-Tests sind FlaUI-/UIA3-Tests, die die gebaute `Tankradar.MAUI.exe` aus
  `src/Tankradar.MAUI/bin/<Config>/net10.0-windows10.0.19041.0/win-x64/` starten —
  sie benötigen einen vorherigen `dotnet build` und eine Windows-Desktop-Sitzung.
  Details: [inventory/tests.md](inventory/tests.md).
- **Doku mit Versionsnennungen, die nach dem Update veralten:**
  `docs/help/Einstellungen/ablauf-technisch.md:7` (EF Core `10.0.10`),
  `docs/help/Navigation/ablauf-technisch.md:54` (`Microsoft.Maui.Controls`
  `10.0.120`), `docs/adr/0016` (historisch, MAUI 10.0.120 — Entscheidungsdokument,
  Aktualisierung ggf. nicht nötig). Details: [inventory/docs.md](inventory/docs.md).

## Detaildokumente

| Datei | Inhalt |
|---|---|
| [inventory/packages.md](inventory/packages.md) | Alle Fundstellen der betroffenen Pakete mit Zeilennummern und Ist-Versionen; transitive Pinning-Situation; komplette Paketliste aller Projekte |
| [inventory/project.md](inventory/project.md) | Solution-Mitglieder, Zielframeworks, `Include*Target`-Schalter, SDK/Workload-Stand, `TestSupport`-Shared-Code-Modell |
| [inventory/workflows.md](inventory/workflows.md) | PR-CI (`pr-staging-ci.yml`), Staging-CI/Pre-Release, Release-Workflow, Security-Scan, Review-Workflows, lokaler Prüflauf |
| [inventory/tests.md](inventory/tests.md) | Testprojekte, Ausführungskommandos (CI und lokal), Coverage-Gate 70 %, E2E-Besonderheiten (FlaUI, Off-Screen, Pre-Push-Hook) |
| [inventory/docs.md](inventory/docs.md) | Dokumentationsstellen mit konkreten Paketversions-Nennungen |

## Hinweise für Planung/Implementierung

1. Die Versionserhöhung ist ein reines `.csproj`-Edit in zwei Dateien — die
   `MauiVersion`-Erhöhung zieht transitiv u. a. `Microsoft.Maui.Core`,
   `Microsoft.WindowsAppSDK`, `Microsoft.Graphics.Win2D`, `Microsoft.Web.WebView2`
   mit (vgl. ADR 0016, Abschnitt „Abhängigkeiten" für den Sprung auf 10.0.120;
   für 10.0.130 erneut prüfen).
2. `Microsoft.EntityFrameworkCore.Design` hat `PrivateAssets=all` — wirkt nur
   design-time (`dotnet ef migrations`), kein Einfluss aufs ausgelieferte Paket.
3. Nach dem Edit: `dotnet restore Tankradar.sln`, dann
   `node scripts/check-vulnerabilities.mjs --run Tankradar.sln` (Akzeptanzkriterium),
   danach Build + alle drei Teststufen; lokal komplett über `scripts/local-ci.ps1`.
4. Der PR geht gegen `staging` (`verify-pr-source.yml` erlaubt nur `staging` →
   `main`); die PR-CI führt Scan/Build/Tests/iOS-Compile selbst aus.
5. Git-Hooks (`.githooks/pre-push`) bauen die Solution und führen Unit- +
   Integrationstests aus (E2E nur mit `PRE_PUSH_E2E=1`) — ein Push ohne lokalen
   Testlauf scheitert am Hook.
