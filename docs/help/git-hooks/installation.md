← [Zurück zur Übersicht](README.md)

# Git-Hooks — Installation

## Systemanforderungen

| Voraussetzung | Version | Zweck |
|---|---|---|
| Git | 2.9 oder neuer | `core.hooksPath` (versionierte Hooks außerhalb von `.git/hooks/`) |
| Python | 3.x (`python3`, unter Windows alternativ `python`) | Alle Prüfskripte unter `.githooks/*.py` |
| .NET SDK | 10.0 oder neuer | `dotnet format`, `dotnet test` (Formatierungs- und Testprüfung) |
| Bash/sh | — | Ausführung der Hook-Skripte selbst (unter Windows über Git Bash, das mit Git for Windows mitgeliefert wird) |

Alle vier Voraussetzungen sind auf einer normalen Tankatlas-Entwicklungsumgebung ohnehin
vorhanden (siehe Haupt-[`README.md`](../../../README.md) für .NET/MAUI-Setup).

## Installation unter Windows

Aus dem Repository-Root ausführen:

```powershell
.\.githooks\install-hooks.cmd
```

Das Skript:

1. prüft, ob `git` im `PATH` verfügbar ist,
2. setzt `git config --local core.hooksPath .githooks`,
3. prüft, ob `python3` (oder ersatzweise `python`) im `PATH` verfügbar ist, und gibt bei Fehlen
   eine Warnung aus,
4. gibt eine Erfolgsmeldung mit Verweis auf diese Dokumentation aus.

## Installation unter Unix/macOS/Linux

Aus dem Repository-Root ausführen:

```bash
./.githooks/install-hooks.sh
```

Das Skript:

1. prüft, ob `git` im `PATH` verfügbar ist,
2. setzt `git config --local core.hooksPath .githooks`,
3. macht `pre-commit`, `pre-push` und `install-hooks.sh` ausführbar (`chmod +x`),
4. prüft, ob `python3` im `PATH` verfügbar ist, und gibt bei Fehlen eine Warnung aus,
5. gibt eine Erfolgsmeldung mit Verweis auf diese Dokumentation aus.

## Manuelle Installation (Fallback)

Falls die Installationsskripte aus irgendeinem Grund nicht funktionieren, genügt ein einzelner
Git-Befehl:

```bash
git config --local core.hooksPath .githooks
```

Unter Unix müssen die Hook-Dateien zusätzlich ausführbar gemacht werden:

```bash
chmod +x .githooks/pre-commit .githooks/pre-push
```

## Validierung

Prüfen, ob die Konfiguration gesetzt ist:

```bash
git config --local core.hooksPath
# Erwartete Ausgabe: .githooks
```

Einen Testcommit auf einem Feature-Branch durchführen, um `pre-commit` zu prüfen:

```bash
git checkout -b test/hooks-check
git commit --allow-empty -m "test: Hook-Installation überprüfen"
```

Die Ausgabe sollte die einzelnen Prüfungen auflisten und mit "Pre-Commit-Prüfungen
abgeschlossen" enden. Den Test-Branch danach wieder löschen:

```bash
git checkout -
git branch -D test/hooks-check
```

## Troubleshooting

| Problem | Ursache | Lösung |
|---|---|---|
| `python3: command not found` bzw. `'python3' is not recognized` | Python nicht im `PATH` | Python 3.x installieren; unter Windows ist `python` als Alternative ebenfalls ausreichend |
| Hook-Fehler direkt beim ersten Commit nach dem Klonen | Hooks noch nicht aktiviert | `.githooks/install-hooks.cmd` bzw. `.githooks/install-hooks.sh` ausführen |
| `dotnet: command not found` bei `pre-push` | .NET SDK nicht im `PATH` | .NET SDK 10.0+ installieren |
| Commit/Push wird trotz Feature-Branch blockiert | Branchname ist zufällig `main` oder `staging` | Feature-Branch mit anderem Namen verwenden |
| Hook scheint gar nicht zu laufen | `core.hooksPath` nicht gesetzt (z. B. nach einem frischen Klon ohne Installation) | Installationsskript erneut ausführen; mit `git config --local core.hooksPath` prüfen |
| Pre-Push hängt sehr lange | Testlauf (`dotnet test`) dauert ungewöhnlich lange | Bis zu 5 Minuten warten (Standard-Timeout); im Ausnahmefall `HOOK_SKIP_TESTS=1` setzen (siehe [`checks.md`](checks.md#test-execution-checkpy)) |

Weitere Details zu den einzelnen Prüfungen: [`checks.md`](checks.md).
