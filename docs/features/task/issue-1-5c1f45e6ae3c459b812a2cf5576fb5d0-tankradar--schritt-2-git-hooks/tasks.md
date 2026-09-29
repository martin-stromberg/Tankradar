# Tasks: Nachbesserung Lokale Git-Hooks zur Qualitätssicherung (Schritt 2)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Konfiguration | CS1591 in Tankradar.MAUI.csproj als Fehler konfigurieren | Offen | — |
| 2 | Quellcode | XML-Dokumentation in `src/Tankradar.MAUI/Platforms/Android/MainApplication.cs` ergänzen | Offen | — |
| 3 | Quellcode | XML-Dokumentation in `src/Tankradar.MAUI/Platforms/MacCatalyst/AppDelegate.cs` ergänzen | Offen | — |
| 4 | Quellcode | XML-Dokumentation in `src/Tankradar.MAUI/Platforms/MacCatalyst/Program.cs` ergänzen | Offen | — |
| 5 | Quellcode | XML-Dokumentation in `src/Tankradar.MAUI/Platforms/Windows/App.xaml.cs` ergänzen | Offen | — |
| 6 | Quellcode | XML-Dokumentation in `src/Tankradar.MAUI/Platforms/iOS/AppDelegate.cs` ergänzen | Offen | — |
| 7 | Quellcode | XML-Dokumentation in `src/Tankradar.MAUI/Platforms/iOS/Program.cs` ergänzen | Offen | — |
| 8 | Code-Formatierung | Code-Formatierungsfehler mit `dotnet format` beheben | Offen | `dotnet format --verify-no-changes` erfolgreich |
| 9 | Code-Formatierung | `.editorconfig` anlegen oder anpassen (optional) | Offen | — |
| 10 | Check-Skript | `ALLOWED_TYPES` in `conventional-commits-check.py` um `ci`, `build`, `style`, `revert` erweitern | Offen | `test_conventional_commits_allows_ci_build_style_revert_types` |
| 11 | Check-Skript | Regex-Pattern in `conventional-commits-check.py` für Breaking-Change-Notation (`feat!:`, `fix!:`) anpassen | Offen | `test_conventional_commits_breaking_change_notation_feat`, `test_conventional_commits_breaking_change_notation_fix` |
| 12 | Check-Skript | `check_breaking_change()` Funktion in `conventional-commits-check.py` hinzufügen | Offen | `test_conventional_commits_breaking_change_in_body` |
| 13 | Check-Skript | `commits_since_divergence()` in `conventional-commits-check.py` für `--base <ref>`-Parameter anpassen | Offen | — |
| 14 | Check-Skript | `BLOCKED_EXTENSIONS` in `forbidden-patterns-check.py` um `.sql`, `.mobileprovision`, `.p8` erweitern | Offen | `test_forbidden_patterns_detects_sql_dump`, `test_forbidden_patterns_detects_mobileprovision`, `test_forbidden_patterns_detects_p8_file` |
| 15 | Check-Skript | `LOG_SIZE_LIMIT_BYTES` in `forbidden-patterns-check.py` auf `0` setzen | Offen | `test_forbidden_patterns_blocks_other_log_files` |
| 16 | Check-Skript | `ALLOWED_LOG_FILES` Konstante in `forbidden-patterns-check.py` hinzufügen mit `{'changes.log'}` | Offen | `test_forbidden_patterns_allows_changes_log` |
| 17 | Check-Skript | `check_api_keys()` in `forbidden-patterns-check.py` für JSON-Notation erweitern | Offen | `test_forbidden_patterns_detects_api_key_json_notation`, `test_forbidden_patterns_detects_generic_json_api_keys` |
| 18 | Check-Skript | Logdatei-Prüfungslogik in `forbidden-patterns-check.py` für `ALLOWED_LOG_FILES` anpassen | Offen | `test_forbidden_patterns_allows_changes_log` |
| 19 | Hook-Skript | `.githooks/pre-commit` anpassen für blockierende `format-code-style-check.py --strict` | Offen | Manueller Hook-Test: Formatierungsfehler blockieren |
| 20 | Hook-Skript | `.githooks/pre-commit` anpassen für blockierende `forbidden-patterns-check.py --strict` | Offen | Manueller Hook-Test: Verbotene Muster blockieren |
| 21 | Hook-Skript | `.githooks/pre-commit` prüfen auf blockierende Aufrufe für `csproj-xmldoc-check.py` und `translation-check.py` | Offen | Manueller Hook-Test: XML-Doku-Fehler blockieren |
| 22 | Hook-Skript | `.githooks/pre-push` für stdin-Ref-Parsing anpassen | Offen | Manueller Hook-Test: Push mit stdin-Refs funktioniert |
| 23 | Hook-Skript | `.githooks/pre-push` anpassen für `conventional-commits-check.py --base <remote_sha>` | Offen | Manueller Hook-Test: Nur gepushte Commits geprüft |
| 24 | Hook-Skript | `.githooks/pre-push` anpassen für `format-code-style-check.py --strict` | Offen | Manueller Hook-Test: Formatierungsfehler blockieren |
| 25 | Git-Index | Ausführungsrechte (100755) für `.githooks/pre-commit` setzen | Offen | `git ls-files -s | grep pre-commit` zeigt 100755 |
| 26 | Git-Index | Ausführungsrechte (100755) für `.githooks/pre-push` setzen | Offen | `git ls-files -s | grep pre-push` zeigt 100755 |
| 27 | Git-Index | Ausführungsrechte (100755) für alle `.py` Dateien in `.githooks/` setzen | Offen | `git ls-files -s | grep .githooks` zeigt 100755 |
| 28 | Tests | `test_conventional_commits_breaking_change_notation_feat` hinzufügen | Offen | Test bestehen |
| 29 | Tests | `test_conventional_commits_breaking_change_notation_fix` hinzufügen | Offen | Test bestehen |
| 30 | Tests | `test_conventional_commits_breaking_change_in_body` hinzufügen | Offen | Test bestehen |
| 31 | Tests | `test_conventional_commits_allows_ci_build_style_revert_types` hinzufügen | Offen | Test bestehen |
| 32 | Tests | `test_forbidden_patterns_detects_api_key_json_notation` hinzufügen | Offen | Test bestehen |
| 33 | Tests | `test_forbidden_patterns_detects_generic_json_api_keys` hinzufügen | Offen | Test bestehen |
| 34 | Tests | `test_forbidden_patterns_detects_mobileprovision` hinzufügen | Offen | Test bestehen |
| 35 | Tests | `test_forbidden_patterns_detects_p8_file` hinzufügen | Offen | Test bestehen |
| 36 | Tests | `test_forbidden_patterns_detects_sql_dump` hinzufügen | Offen | Test bestehen |
| 37 | Tests | `test_forbidden_patterns_allows_changes_log` hinzufügen | Offen | Test bestehen |
| 38 | Tests | `test_forbidden_patterns_blocks_other_log_files` hinzufügen | Offen | Test bestehen |
| 39 | Tests | Alle Test-Suites ausführen und verifizieren | Offen | Alle Tests bestehen, Exit-Code 0 |
| 40 | Verifikation | Pre-Commit Hook manuell testen: Formatierungsfehler blockieren | Offen | Commit wird blockiert |
| 41 | Verifikation | Pre-Commit Hook manuell testen: Gültige Commits akzeptieren | Offen | Commit wird akzeptiert |
| 42 | Verifikation | Pre-Commit Hook manuell testen: XML-Doku-Fehler blockieren | Offen | Commit wird blockiert |
| 43 | Verifikation | Pre-Push Hook manuell testen: Ungültige Commit-Types blockieren | Offen | Push wird blockiert |
| 44 | Verifikation | Pre-Push Hook manuell testen: Breaking-Change-Notation akzeptieren | Offen | Push wird akzeptiert |
| 45 | Verifikation | Pre-Push Hook manuell testen: JSON-API-Keys blockieren | Offen | Push wird blockiert |
| 46 | Verifikation | Pre-Push Hook manuell testen: `changes.log` akzeptieren | Offen | Push wird akzeptiert |
| 47 | Verifikation | Pre-Push Hook manuell testen: Andere Logdateien blockieren | Offen | Push wird blockiert |
