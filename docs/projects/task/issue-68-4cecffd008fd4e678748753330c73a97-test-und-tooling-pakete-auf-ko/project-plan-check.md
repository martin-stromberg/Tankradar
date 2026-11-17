# Projektplan-Prüfung

## Ergebnis

**Status:** Projektplan vollständig

## Abgleich Anforderung ↔ Entwicklungsschritte

| Anforderung | Abgedeckt durch |
|-------------|-----------------|
| #68: Kompatible Patchupdates in Testprojekten | Schritt 1 |
| #68: Major-Updates bewusst zurückstellen + begründen | Schritt 1 |
| #68: `dotnet test` Unit/Integration/E2E erfolgreich | Schritt 1 (lokal + PR-CI) |
| #69: Zentrale Paketverwaltung (Directory.Packages.props) | Schritt 2 |
| #69: Dependabot/Renovate für NuGet + Actions | Schritt 2 |
| #69: Action-Tag-Risiko reduziert oder dokumentiert | Schritt 2 (dokumentierte Entscheidung) |
| #69: SDK-Band via global.json | Schritt 2 |
| #70: Opt-in-Diagnosepaket, nur explizite Benutzeraktion | Schritt 3 |
| #70: Umfang/Zweck vor Teilen sichtbar | Schritt 3 (Vorschau im Settings-Fluss) |
| #70: Redaktionsregeln testabgesichert | Schritt 3 (Unit-Tests) |
| #70: Keine unaufgeforderte Telemetrie | Schritt 3 (lokale Datei, kein Endpoint) |
| #71: Compiled-Model-Evaluation + AOT/Trimming-Prüfung | Schritt 4 |
| #71: Messwerte oder dokumentierte Risikoabwägung | Schritt 4 (ADR) |
| #71: Bei Umstellung Tests für Migrationen/Restore | Schritt 4 |
| Auftrag: Status in Issues kommentieren | Projekt-Abschlussphase |
| Auftrag: PR gegen staging | Projekt-Abschlussphase |
| Auftrag: Release abwarten + Backmerge | Vorbedingung (bereits in todo.md) |

## Abhängigkeitsprüfung

- Schritt 2 → 1, Schritt 3 → 2, Schritt 4 → 3: zyklenfrei, verweisen jeweils auf
  frühere Schritte. Lineare Kette vermeidet Konflikte in
  `Directory.Packages.props`/`.csproj`.
- Jede Schrittbeschreibung ist eigenständig als Lifecycle-Eingabe verwendbar.

## Fehlende oder unvollständige Punkte

Keine.

## Hinweise

- Schritt 1 und 2 berühren bewusst keine App-Funktionalität; Schritt 3 enthält
  die einzige UI-Erweiterung (Settings) und benötigt Usability-/E2E-Betrachtung
  im Lifecycle.
- Schritt 4 endet primär in einem ADR; eine Code-Umstellung ist konditional und
  risikobegrenzt.
