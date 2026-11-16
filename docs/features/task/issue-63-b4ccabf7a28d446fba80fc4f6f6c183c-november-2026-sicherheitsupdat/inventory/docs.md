# Dokumentationsstellen mit Paketversions-Nennungen

Stellen, die konkrete Versionsnummern der zu aktualisierenden Pakete nennen
(gefunden via Repo-weiter Suche). Ob sie im Zuge des Updates angepasst werden,
ist eine Planungsentscheidung — ADR/changes.log sind historische Dokumente,
die `docs/help`-Dateien beschreiben den Ist-Zustand und veralten faktisch.

## Aktualisierung naheliegend (beschreiben Ist-Zustand)

| Datei | Zeile | Inhalt |
|---|---|---|
| `docs/help/Einstellungen/ablauf-technisch.md` | 7 | „… angesprochen über Entity Framework Core **10.0.10** (`Microsoft.EntityFrameworkCore.Sqlite`) …" — nennt die EF-Version explizit |
| `docs/help/Navigation/ablauf-technisch.md` | 54 | „In der aktuell verwendeten MAUI-Version (`Microsoft.Maui.Controls` **10.0.120**) existieren keine `Shell.TabBar*Shadow*`…" — nach Bump auf 10.0.130 veraltet; die fachliche Aussage (Properties existieren nicht) ggf. gegen 10.0.130 prüfen |

## Historisch / Entscheidungsdokumente (i. d. R. unverändert lassen)

| Datei | Bezug |
|---|---|
| `docs/adr/0016-maui-version-and-photo-permissions.md` | Begründet die Fixierung von `MauiVersion` (damals 10.0.120, Z. 13–16, 25). Enthält die relevante Regel: „Die MAUI-Version ist an einer Stelle festgelegt und bei Updates bewusst zu ändern." Bei größerem Sprung ggf. neuer ADR bzw. Eintrag in `changes.log` (Projektkonvention: `changes.log` Z. 97–102 dokumentierte den 10.0.120-Sprung inkl. transitiver Auswirkungen). |
| `docs/adr/0010-tankbook-statistics.md` | Z. 22: erwähnt EF Core / `Microsoft.Data.Sqlite` (Linker/Interpreter-Kontext), ohne Versionsnummer |
| `changes.log` | Z. 97–102 (Eintrag 2026-10-25): MAUI-10.0.120-Sprung mit transitiven Paketen — historisch |
| `issue.md` (Wurzel), `requirement.md` | Aufgabenbeschreibung mit Soll-Versionen — keine Änderung |

## Versionsfrei, aber prozessrelevant

- `README.md` Z. 39: „**.NET SDK 10.0.401** oder neuer" — SDK-Forderung, keine
  Paketversion; bleibt gültig (installiert: 10.0.403).
- `docs/help/ci-cd/workflows.md`: beschreibt alle Workflows und Bausteine
  (`security-scan`, `build-and-package`, `package-ios`) ohne Paketversionen.
- `docs/help/ci-cd/lokaler-pruefung.md` Z. 11: gleiche SDK-Angabe 10.0.401+.
- `docs/help/Einstellungen/ablauf-technisch.md` Z. 68 und
  `docs/help/Tankbuch/ablauf-technisch.md`: `dotnet ef migrations add`-Aufruf
  (Design-Paket betroffen, Kommando selbst versionsfrei; `dotnet-ef` ist **nicht**
  per `dotnet-tools.json` gepinnt — kein Manifest im Repo).
- `pages/README.md` Z. 31: Build-Kommando ohne Paketversionen.
