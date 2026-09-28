# Tasks: Lokale Git-Hooks zur Qualitätssicherung (Schritt 2)

## Übersicht

Diese Tasks-Datei enthält alle Einzelaufgaben für die Umsetzung der Git-Hooks. Sie ergänzt den `plan.md` mit konkreten, abgrenzbaren Aufträgen. Jede Task ist unabhängig prüf- und abnahmefähig.

---

## Tasks nach Bereich

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Hook-Dateien / Vorbereitung | `.githooks/`-Verzeichnis im Repository-Root anlegen | Offen | — |
| 2 | Hook-Dateien / Vorbereitung | `pre-commit` Shell-Hook aus Template kopieren und anpassen (Razor-Checks entfernen) | Offen | — |
| 3 | Hook-Dateien / Vorbereitung | `pre-push` Shell-Hook aus Template kopieren und anpassen (Razor-Checks entfernen) | Offen | — |
| 4 | Hook-Dateien / Vorbereitung | `csproj-xmldoc-check.py` aus Template kopieren (unverändert) | Offen | — |
| 5 | Hook-Dateien / Vorbereitung | `no-notimplemented-check.py` aus Template kopieren (unverändert) | Offen | — |
| 6 | Hook-Dateien / Vorbereitung | `enum-coverage-check.py` aus Template kopieren (unverändert) | Offen | — |
| 7 | Hook-Dateien / Vorbereitung | `translation-check.py` aus Template kopieren und validieren für XAML-Ressourcen | Offen | — |
| 8 | Hook-Dateien / Vorbereitung | `install-hooks.cmd` aus Template kopieren | Offen | — |
| 9 | Hook-Dateien / Vorbereitung | `install-hooks.sh` aus Template kopieren | Offen | — |
| 10 | Hook-Dateien / Code-Formatierung | `format-code-style-check.py` implementieren: ruft `dotnet format --verify-no-changes` auf, gibt Warnungen aus (Exit 0) | Offen | Unit-Test für dotnet-format Aufruf |
| 11 | Hook-Dateien / Secrets-Prüfung | `forbidden-patterns-check.py` implementieren: Regex-Prüfung für API-Schlüssel (FUEL_API_KEY, ROUTING_KEY, etc.) | Offen | Unit-Test für API-Schlüssel-Pattern |
| 12 | Hook-Dateien / Secrets-Prüfung | `forbidden-patterns-check.py` erweitern: Regex-Prüfung für Zertifikate (.pfx, .p12, .keystore, .jks, private Keys) | Offen | Unit-Test für Zertifikat-Pattern |
| 13 | Hook-Dateien / Secrets-Prüfung | `forbidden-patterns-check.py` erweitern: Regex-Prüfung für DB-Dumps (.db, .sqlite*) | Offen | Unit-Test für DB-Dump-Pattern |
| 14 | Hook-Dateien / Secrets-Prüfung | `forbidden-patterns-check.py` erweitern: Prüfung für große Logdateien (>1 MB .log) | Offen | Unit-Test für Logdatei-Größen-Check |
| 15 | Hook-Dateien / Secrets-Prüfung | `forbidden-patterns-check.py` erweitern: `--all` und `--strict` Argumente für pre-push Nutzung | Offen | Unit-Test für Argument-Parsing |
| 16 | Hook-Dateien / Commit-Validierung | `conventional-commits-check.py` implementieren: Format-Validierung `type(scope): message` | Offen | Unit-Test für gültiges Format |
| 17 | Hook-Dateien / Commit-Validierung | `conventional-commits-check.py` erweitern: Type-Validierung (feat, fix, docs, test, refactor, chore, perf) | Offen | Unit-Test für Type-Validierung |
| 18 | Hook-Dateien / Commit-Validierung | `conventional-commits-check.py` erweitern: Scope optional, Validierung der Nachrichtenlänge (min. 10 Zeichen) | Offen | Unit-Test für Scope und Längenvaidierung |
| 19 | Hook-Dateien / Commit-Validierung | `conventional-commits-check.py` erweitern: Git-History Lesen (Commits seit Divergenz von `main`) | Offen | Unit-Test mit Git-Repo |
| 20 | Hook-Dateien / Test-Validierung | `test-execution-check.py` implementieren: Ruft `dotnet test Tankradar.sln` auf | Offen | Integration-Test mit Testlauf |
| 21 | Hook-Dateien / Test-Validierung | `test-execution-check.py` erweitern: Timeout nach 5 Minuten mit Fehlerbehandlung | Offen | Timeout-Test (könnte zu lange dauern) |
| 22 | Hook-Dateien / Test-Validierung | `test-execution-check.py` erweitern: Umgebungsvariable `HOOK_SKIP_TESTS=1` zum Überspringen | Offen | Unit-Test für ENV-Var-Prüfung |
| 23 | Hook-Dateien / Test-Validierung | `test-execution-check.py` erweitern: `TEST_TIMEOUT_SECONDS=...` für Timeout-Konfiguration | Offen | Unit-Test für Timeout-Override |
| 24 | Hook-Dateien / Installation | `install-hooks.cmd` überprüfen und testen: Setzt `git config --local core.hooksPath .githooks` | Offen | Manuelle Test unter Windows 11 |
| 25 | Hook-Dateien / Installation | `install-hooks.cmd` erweitern: Prüft Git-Verfügbarkeit (`git --version`) | Offen | Manuelle Test unter Windows |
| 26 | Hook-Dateien / Installation | `install-hooks.cmd` erweitern: Prüft Python 3 Verfügbarkeit | Offen | Manuelle Test unter Windows |
| 27 | Hook-Dateien / Installation | `install-hooks.cmd` anpassen: Gibt verständliche Erfolgsmeldung aus | Offen | Manuelle Überprüfung |
| 28 | Hook-Dateien / Installation | `install-hooks.sh` überprüfen und testen: Setzt `git config --local core.hooksPath .githooks` | Offen | Manuelle Test unter Linux/macOS |
| 29 | Hook-Dateien / Installation | `install-hooks.sh` erweitern: `chmod +x .githooks/pre-commit .githooks/pre-push` | Offen | Manuelle Test unter Linux/macOS |
| 30 | Hook-Dateien / Installation | `install-hooks.sh` erweitern: Prüft Git-Verfügbarkeit | Offen | Manuelle Test unter Linux/macOS |
| 31 | Hook-Dateien / Installation | `install-hooks.sh` erweitern: Prüft Python 3 Verfügbarkeit | Offen | Manuelle Test unter Linux/macOS |
| 32 | Hook-Dateien / Installation | `install-hooks.sh` anpassen: Gibt verständliche Erfolgsmeldung aus | Offen | Manuelle Überprüfung |
| 33 | Dokumentation | `docs/help/git-hooks/`-Verzeichnis anlegen | Offen | — |
| 34 | Dokumentation | `docs/help/git-hooks/README.md` schreiben: Übersicht und Quick-Start | Offen | Manuelle Review |
| 35 | Dokumentation | `docs/help/git-hooks/README.md` erweitern: FAQ und Troubleshooting-Tipps | Offen | Manuelle Review |
| 36 | Dokumentation | `docs/help/git-hooks/README.md` erweitern: Links zu `installation.md` und `checks.md` | Offen | Manuelle Review |
| 37 | Dokumentation | `docs/help/git-hooks/installation.md` schreiben: Schritt-für-Schritt Anleitung Windows | Offen | Manuelle Review und Test |
| 38 | Dokumentation | `docs/help/git-hooks/installation.md` erweitern: Schritt-für-Schritt Anleitung Unix/macOS | Offen | Manuelle Review und Test |
| 39 | Dokumentation | `docs/help/git-hooks/installation.md` erweitern: Systemanforderungen (Git 2.9+, Python 3.x, .NET SDK 10.0+) | Offen | Manuelle Review |
| 40 | Dokumentation | `docs/help/git-hooks/installation.md` erweitern: Validierungs-Kommando | Offen | Manuelle Review |
| 41 | Dokumentation | `docs/help/git-hooks/checks.md` schreiben: Tabelle aller Hook-Checks mit Zweck und Lösungen | Offen | Manuelle Review |
| 42 | Dokumentation | `docs/help/git-hooks/checks.md` erweitern: Beispiel-Fehlerausgaben für jeden Check | Offen | Manuelle Review |
| 43 | Dokumentation | `docs/help/git-hooks/checks.md` erweitern: Detaillierte Beschreibung `format-code-style-check.py` | Offen | Manuelle Review |
| 44 | Dokumentation | `docs/help/git-hooks/checks.md` erweitern: Detaillierte Beschreibung `forbidden-patterns-check.py` | Offen | Manuelle Review |
| 45 | Dokumentation | `docs/help/git-hooks/checks.md` erweitern: Detaillierte Beschreibung `conventional-commits-check.py` | Offen | Manuelle Review |
| 46 | Dokumentation | `docs/help/git-hooks/checks.md` erweitern: Detaillierte Beschreibung `test-execution-check.py` | Offen | Manuelle Review |
| 47 | Dokumentation | `README.md` aktualisieren: Neuer Abschnitt „Git-Hooks" mit Installationshinweis | Offen | Manuelle Review |
| 48 | Dokumentation | `README.md` Update: Link zu `docs/help/git-hooks/installation.md` hinzufügen | Offen | Manuelle Review |
| 49 | Repository-Struktur | `.gitignore` überprüfen: Sind `review-versions/`, `design-draft/`, `.claude/` ausgeschlossen? | Offen | Datei-Review |
| 50 | Repository-Struktur | `.gitignore` ergänzen (falls nötig): `review-versions/` | Offen | Commit-Validierung |
| 51 | Repository-Struktur | `.gitignore` ergänzen (falls nötig): `design-draft/` | Offen | Commit-Validierung |
| 52 | Repository-Struktur | `.gitignore` ergänzen (falls nötig): `.claude/` | Offen | Commit-Validierung |
| 53 | Repository-Struktur | `.gitignore` ergänzen (falls nötig): Große Logdateien (`*.log`) | Offen | Commit-Validierung |
| 54 | Repository-Struktur | `.gitignore` ergänzen (falls nötig): DB-Dumps (`*.db`, `*.sqlite*`) | Offen | Commit-Validierung |
| 55 | Tests / Unit | Unit-Test für `format-code-style-check.py`: dotnet-format Aufruf verifizieren | Offen | Test läuft, besteht |
| 56 | Tests / Unit | Unit-Test für `forbidden-patterns-check.py`: API-Schlüssel-Pattern testen | Offen | Test läuft, besteht |
| 57 | Tests / Unit | Unit-Test für `forbidden-patterns-check.py`: Zertifikat-Pattern testen | Offen | Test läuft, besteht |
| 58 | Tests / Unit | Unit-Test für `forbidden-patterns-check.py`: DB-Dump-Pattern testen | Offen | Test läuft, besteht |
| 59 | Tests / Unit | Unit-Test für `forbidden-patterns-check.py`: Logdatei-Größen-Check testen | Offen | Test läuft, besteht |
| 60 | Tests / Unit | Unit-Test für `conventional-commits-check.py`: Valides Format (`feat(scope): msg`) akzeptieren | Offen | Test läuft, besteht |
| 61 | Tests / Unit | Unit-Test für `conventional-commits-check.py`: Ungültiger Type ablehnen | Offen | Test läuft, besteht |
| 62 | Tests / Unit | Unit-Test für `conventional-commits-check.py`: Nachricht zu kurz ablehnen | Offen | Test läuft, besteht |
| 63 | Tests / Unit | Unit-Test für `test-execution-check.py`: Environment-Variable `HOOK_SKIP_TESTS=1` funktioniert | Offen | Test läuft, besteht |
| 64 | Tests / Unit | Unit-Test für `test-execution-check.py`: Timeout-Override via `TEST_TIMEOUT_SECONDS` funktioniert | Offen | Test läuft, besteht |
| 65 | Tests / Integration | Manueller Integration-Test: Commit auf Feature-Branch mit Hook durchführen | Offen | Commit erfolgreich, Hook durchlaufen |
| 66 | Tests / Integration | Manueller Integration-Test: Commit auf `main` blockiert durch Hook | Offen | Commit blockiert, Exit 1 |
| 67 | Tests / Integration | Manueller Integration-Test: Commit auf `staging` blockiert durch Hook | Offen | Commit blockiert, Exit 1 |
| 68 | Tests / Integration | Manueller Integration-Test: Push auf Feature-Branch mit Hook durchführen (Tests müssen bestehen) | Offen | Push erfolgreich, Tests laufen und bestehen |
| 69 | Tests / Integration | Manueller Integration-Test: Push auf `main` blockiert durch Hook | Offen | Push blockiert, Exit 1 |
| 70 | Tests / Integration | Manueller Integration-Test: Push auf `staging` blockiert durch Hook | Offen | Push blockiert, Exit 1 |
| 71 | Tests / Validierung | Hooks grün gegen aktuellen Code-Stand: Alle 11 bestehenden Tests bestehen nach Hook-Installation | Offen | `dotnet test Tankradar.sln` Exit 0 |
| 72 | Tests / Validierung | Hook-Fehlerbehandlung: Fehlgeschlagene Hooks werden korrekt gemeldet mit aussagekräftigen Fehlern | Offen | Fehlerausgabe reviewen |
| 73 | Tests / Validierung | Fallback-Mechanik testen: `SKIP_HOOKS=1` überspringt Hooks mit Warnung auf stderr | Offen | Stderr-Warnung sichtbar |
| 74 | Tests / Validierung | Hook-Performance: Pre-commit sollte < 10 Sekunden dauern, Pre-push < 5 Min (hauptsächlich Tests) | Offen | Zeit-Messung durchführen |

---

## Legende

- **Status:** Offen (keine Bearbeitung gestartet), In Bearbeitung, Abgeschlossen, Blockiert
- **Testnachweis:** Wie wird die erfolgreiche Umsetzung nachgewiesen? (Unit-Test, Manuelle Review, Datei-Review, Git-Validierung, etc.)
