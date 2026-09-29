# Bestandsaufnahme: Nachbesserung Lokale Git-Hooks (Schritt 2)

Analyse des bestehenden Code-Standes für die Nachbesserung der Git-Hooks zur Qualitätssicherung (Schritt 2, Runde 1). Der Fokus liegt auf den Hook-Dateien (`.githooks/pre-commit`, `.githooks/pre-push`), den Python-Check-Skripten und ihren Tests.

## Zusammenfassung

### Vorhandener Code
- **Hook-Dateien:** Beide `.githooks/pre-commit` und `.githooks/pre-push` existieren und sind funktionsfähig
  - `pre-commit` läuft mit Warn-Modus (nicht blockierend) auf den meisten Checks, blockiert aber direkte Commits auf `main`/`staging`
  - `pre-push` blockiert direkte Pushes auf `main`/`staging` und führt strikte Checks aus
  - Beide nutzen Python-Interpreter-Fallback (python3 bevorzugt, sonst python)

- **Check-Skripte (Python):** 9 Check-Skripte sind vorhanden:
  - `conventional-commits-check.py` — validiert Commit-Nachrichten nach Conventional-Commits-Format
  - `forbidden-patterns-check.py` — prüft auf verbotene Muster (Secrets, Zertifikate, DB-Dumps, Logdateien)
  - `format-code-style-check.py` — prüft Code-Formatierung via `dotnet format --verify-no-changes`
  - `csproj-xmldoc-check.py` — validiert XML-Dokumentation in .csproj und .cs Dateien
  - `translation-check.py` — prüft Lokalisierungs-Konsistenz über .resx Dateien
  - `enum-coverage-check.py` — überprüft Testabdeckung von Enums
  - `no-notimplemented-check.py` — blockt Platzhalter-Implementierungen (NotImplementedException)
  - `test-execution-check.py` — führt Tests aus und validiert deren Erfolg
  - `_hook_common.py` — gemeinsame Hilfsfunktionen

- **Test-Abdeckung:** 4 Test-Suites mit 7–30 Tests pro Suite vorhanden (insgesamt ca. 70+ Tests)

### Bekannte Defizite (aus Anforderung)
- **Conventional-Commits-Typen:** `ALLOWED_TYPES` enthält nur `{'feat', 'fix', 'docs', 'test', 'refactor', 'chore', 'perf', 'plan', 'merge'}` — fehlen: `ci`, `build`, `style`, `revert`
- **Breaking-Change-Notation:** Noch nicht unterstützt (`feat!:`, `fix!:`, `BREAKING CHANGE:` im Body)
- **Pre-Push Commit-Bereich:** Nutzt hardcodierte `main..HEAD` statt stdin-basierte Refs
- **Forbidden-Patterns:** 
  - API-Schlüssel nur in `=`-Notation, nicht in JSON-Notation (`"key": "..."`)
  - iOS-Signierungsdateien (.mobileprovision, .p8) nicht geprüft
  - SQL-Dumps (.sql) nicht geprüft
  - Logdateien mit 1-MB-Größenlimit, nicht unbegrenzt
  - `changes.log` Ausnahme nicht implementiert
- **XML-Dokumentation:** CS1591 ist in Tankradar.MAUI.csproj nicht als Fehler konfiguriert
- **Hook-Ausführungsrechte:** Alle Hook- und Check-Dateien haben Modus `100644` im Git-Index, sollten aber `100755` sein

### Test-Ausgangszustand
Zeitpunkt: 2026-09-29 04:00 UTC  
Branch: `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-2-git-hooks`  
Commit: `9323ff361087432ce3e03d3818bc5cf308e25990`  
Uncommittete Änderungen: 2 Dateien (docs/features/, docs/projects/...)  

**Alle Tests bestehen erfolgreich.** Für Details siehe [Tests](inventory/tests.md).

## Details

- [Hook-Dateien](inventory/hooks.md)
- [Check-Skripte](inventory/checks.md)
- [Tests](inventory/tests.md)
