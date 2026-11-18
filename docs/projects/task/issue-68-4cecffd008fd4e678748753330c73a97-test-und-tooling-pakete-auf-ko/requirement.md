# Anforderung

Quelle: Issue-Kommentar zu martin-stromberg/Tankradar#68 (IssueComment-Event).
Issues: #68, #69, #70, #71 — alle Folge-Items des Architektur- und Sicherheitsreviews #62.

## Ziel

Vier Review-Folgeissues in einem Projekt umsetzen und gemeinsam als PR gegen `staging`
liefern. Aufgabenstatus in den jeweiligen Issues kommentieren. Vor Start: laufendes
Release abwarten und den dadurch entstehenden Backmerge nach staging einholen;
Release-Fehler zählen zum Aufgabenumfang.

## Fachliche Anforderungen

### Issue #68 — Test- und Tooling-Pakete auf kompatible Patchstände aktualisieren

- Kompatible Patchupdates in den Testprojekten anwenden (konkret genannt:
  `Microsoft.NET.Test.Sdk` 17.14.1 → 17.14.2).
- Größere Major-Updates (`Microsoft.NET.Test.Sdk` 18.x, `xunit.runner.visualstudio` 4.x,
  `coverlet.collector` 10.x) nur nach gesonderter Breaking-Change-Prüfung — hier bewusst
  zurückstellen und dokumentieren.
- Nach den Updates Unit-, Integrations- und E2E-Tests ausführen.
- Akzeptanz: konsistente Paketversionen über alle Testprojekte; `dotnet test` läuft;
  zurückgestellte Major-Updates sind kurz begründet oder in Folge-Issues dokumentiert.

### Issue #69 — Paket- und GitHub-Actions-Governance vereinheitlichen

- Zentrale Paketverwaltung über `Directory.Packages.props` prüfen/einführen.
- Dependabot oder Renovate für NuGet- und GitHub-Actions-Updates einrichten.
- Sicherheitskritische externe Actions perspektivisch auf Commit-SHAs pinnen
  (Risiko veränderbarer Tags reduzieren oder bewusst dokumentieren).
- `global.json` mit `rollForward: latestPatch` einführen oder Abweichung dokumentieren.
- Akzeptanz: nachvollziehbar konsistente Paketverwaltung; automatisierte Update-
  Vorschläge; definiertes .NET-SDK-Band für lokale Builds und CI.

### Issue #70 — Opt-in-Diagnosepaket für produktive Fehleranalyse einführen

- Optionales, lokal erzeugtes Diagnosepaket, das der Anwender explizit freigibt.
- Enthält begrenzte Laufzeit-/Fehlerinformationen; konsequente Redaktion: keine
  API-Schlüssel, Koordinaten, Stationsnamen, Suchbegriffe, Dateipfade, Belegfotos.
- Retention und Umfang begrenzt.
- Akzeptanz: Erzeugung/Teilen nur nach ausdrücklicher Benutzeraktion; Umfang und Zweck
  vor dem Teilen sichtbar; Redaktionsregeln durch Tests abgesichert; keine
  unaufgeforderte Telemetrie oder Übertragung.

### Issue #71 — EF-Core-Compiled-Model und iOS-AOT-/Trimming-Verbesserungen evaluieren

- EF-Core-Compiled-Model evaluieren.
- Aktuelle EF-Core- und .NET-MAUI-Hinweise zu iOS-AOT, Trimming und Reflection prüfen.
- Prototypisch Startzeit, Paketgröße und relevante Datenbankpfade messen
  (soweit in dieser Umgebung möglich, sonst dokumentierte Risikoabwägung).
- Ergebnis als ADR dokumentieren (`docs/adr/`), auch bei Beibehaltung der aktuellen
  Konfiguration.
- Akzeptanz: begründete Entscheidung mit Messwerten oder dokumentierter
  Risikoabwägung; bei Umstellung bleiben Migrationen/Restore/DB-Integrität durch Tests
  abgesichert; sonst begründete Dokumentation der bestehenden
  Interpreter-/Trimmer-Konfiguration.

## Betroffene Bereiche

- `src/Tankradar.Tests.Unit|Integration|E2E` (Paketversionen)
- `src/PagesScreenshots`, `src/Tankradar.MAUI` (Paketversionen, Diagnosefeature)
- `Directory.Packages.props`, `global.json`, `.github/dependabot.yml` (neu)
- `.github/workflows/` (Action-Pinning-Entscheidung)
- `src/Tankradar.MAUI` Settings/Diagnose (Opt-in-Diagnosepaket)
- `docs/adr/` (ADR zu Issue #71), `docs/help/` (Doku Diagnosepaket)

## Explizite Nicht-Ziele

- Major-Updates der Testpakete (18.x/4.x/10.x) — separat zu bewerten.
- Kein automatischer Versand von Diagnosedaten (kein Telemetrie-Endpunkt).
- Kein PR gegen `main` — Zielbranch ist `staging`.

## Offene Fragen

- Keine — die Issues tragen begründete Empfehlungen und Akzeptanzkriterien;
  verbleibende Einzelentscheidungen (z. B. SHA-Pinning-Tiefe, Umfang der Messungen
  für #71) werden im Projektplan als behandelte Annahmen dokumentiert.

## Vorgegebener Ablauf (aus dem Auftragskommentar)

1. Laufendes Release abwarten; bei Fehlern mitkorrigieren.
2. Backmerge nach staging einholen.
3. Issues #68–#71 gemeinsam umsetzen (project-Skill).
4. Aufgabenstatus in den jeweiligen Issues kommentieren.
5. Ein gemeinsamer PR gegen `staging` mit Closing-Tags für #68–#71.
