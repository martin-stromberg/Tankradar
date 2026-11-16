# November-2026-Sicherheitsupdates für .NET-/NuGet-Pakete anwenden

**Quelle:** GitHub Issue #63 · **Label:** bug · **Priorität:** P1

## Kontext / Hintergrund

Die Anforderung ergibt sich aus dem Architektur- und Sicherheitsreview (Issue #62).
Die installierte .NET-Runtime bzw. das SDK ist aktuell; der
NuGet-Vulnerability-Scan (`--include-transitive`) war ohne Befund. Dennoch liegen
mehrere direkte Paketreferenzen unter dem Patchstand vom November 2026: Das am
10.11.2026 veröffentlichte .NET 10.0.15 enthält Sicherheitsfixes, die in den
referenzierten Paketen berücksichtigt werden sollten.

Da das Windows-Paket der Anwendung self-contained ausgeliefert wird, reicht eine
Versionserhöhung im Projekt nicht aus — es sind Rebuild und Neuveröffentlichung
des Release-Pakets erforderlich.

## Ziel

Alle direkten Paketreferenzen auf den November-2026-Patchstand (oder einen
neueren, kompatiblen Stand) innerhalb der .NET-10-Linie bzw. des vorhandenen
SQLitePCLRaw-2.x-Zweigs anheben, die Solution vollständig verifizieren (Restore,
Vulnerability-Scan, Unit-/Integrations-/E2E-Tests) und ein neues Release-Paket
mit den aktualisierten Abhängigkeiten erzeugen.

## Änderungsanforderungen

Folgende Paketversionen sind zu aktualisieren (von → nach):

- `Microsoft.EntityFrameworkCore.Sqlite`: `10.0.10` → `10.0.15`
- `Microsoft.EntityFrameworkCore.Design`: `10.0.10` → `10.0.15`
- `Microsoft.Data.Sqlite` (in den E2E-Tests): `10.0.10` → `10.0.15`
- `Microsoft.Extensions.Logging.Debug`: `10.0.0` → `10.0.15`
- `Microsoft.Maui.Controls`: `10.0.120` → `10.0.130`
- `SQLitePCLRaw.bundle_e_sqlite3`: `2.1.12` → `2.1.14`

Nach der Aktualisierung sind Restore und Tests auszuführen sowie das
Release-Paket (self-contained Windows-Build) neu zu bauen.

## Akzeptanzkriterien

- [ ] Alle genannten Pakete stehen auf den genannten oder neueren kompatiblen Patchständen.
- [ ] `dotnet list Tankradar.sln package --vulnerable --include-transitive` liefert keinen Befund.
- [ ] Unit-, Integrations- und E2E-Tests laufen erfolgreich durch.
- [ ] Für ausgelieferte Artefakte wurde ein neuer Build mit den aktualisierten Paketen erzeugt.

## Nicht-Ziele

- Kein Umstieg auf .NET 11 (gesondert zu bewerten).
- Kein Umstieg auf SQLitePCLRaw 3.x — Aktualisierung erfolgt innerhalb des bestehenden 2.x-Zweigs.
- Keine Änderungen am Funktionsumfang oder Verhalten der Anwendung über die Paketaktualisierung hinaus.
