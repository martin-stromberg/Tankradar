# Offene Aufgaben

Erstellt am: 2026-09-29
Abbruchgrund: Kein Fortschritt zwischen den letzten zwei Iterationen

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine. `review.md` meldet Status „Vollständig umgesetzt".

## Code-Review-Befunde

- [ ] `.githooks/pre-commit` und `.githooks/pre-push` rufen Python ausschließlich über den hartcodierten Befehl `python3` auf (pre-commit:34,48; pre-push:32,39,46,54,62), ohne Fallback auf `python`. Das widerspricht der eigenen Dokumentation (`docs/help/git-hooks/installation.md:10`, FAQ in `docs/help/git-hooks/README.md`, `install-hooks.cmd` prüft explizit beide Varianten). Auf einer laut eigener Dokumentation unterstützten Windows-Umgebung mit nur `python` im PATH (kein `python3`-Shim) würde `python3` mit „command not found" fehlschlagen — in `pre-commit` nur eine irreführende Warnung, in `pre-push` aber eine fälschliche Blockade jedes Pushes mit irreführender Fehlermeldung. Empfehlung: Python-Interpreter einmalig ermitteln (z. B. `PYTHON=python3; command -v "$PYTHON" >/dev/null 2>&1 || PYTHON=python`, mit klarer Fehlermeldung falls auch `python` fehlt) und danach überall `"$PYTHON"` statt des fest codierten `python3` verwenden.
- [ ] `.githooks/_hook_common.py` (gemeinsames Modul mit `run`, `repo_root`, `staged_files`, `find_solution`) hat keine eigene Testdatei. `repo_root()` und `find_solution()` sind in keinem Test (auch nicht indirekt) mit ihrem realen Verhalten inkl. Fehlerfällen („kein Git-Repository", „keine .sln/.slnx-Datei gefunden") abgedeckt. Empfehlung: `.githooks/test_hook_common.py` mit gezielten Unit-Tests ergänzen, analog zum bestehenden Testmuster der anderen `test_*.py`-Dateien in `.githooks/`.

## Fehlgeschlagene Tests

Keine. Alle 11 .NET-Tests und alle 20 Python-Hook-Unit-Tests sind bestanden (siehe `test-results.md`).
