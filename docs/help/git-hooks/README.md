# Git-Hooks — Übersicht

Tankradar verwendet lokale Git-Hooks zur Qualitätssicherung: Sie laufen automatisch vor jedem
Commit (`pre-commit`) und vor jedem Push (`pre-push`) und prüfen Dinge, die sonst erst spät
auffallen würden — nicht mehr aktuelle Übersetzungen, fehlende XML-Dokumentation,
Platzhalter-Implementierungen, unvollständige Enum-Testabdeckung, Formatierungsabweichungen,
versehentlich committete Secrets, falsches Commit-Nachrichten-Format und fehlgeschlagene Tests.

Die Hook-Dateien liegen versioniert unter [`.githooks/`](../../../.githooks/) im
Repository-Root und werden über `git config core.hooksPath` aktiviert — es müssen keine Dateien
nach `.git/hooks/` kopiert werden.

## Schnelleinstieg

```powershell
# Windows
.\.githooks\install-hooks.cmd
```

```bash
# Unix/macOS/Linux
./.githooks/install-hooks.sh
```

Validieren, dass die Hooks aktiv sind:

```bash
git config --local core.hooksPath
# Erwartete Ausgabe: .githooks
```

Details zur Installation: [`installation.md`](installation.md).
Details zu allen einzelnen Prüfungen: [`checks.md`](checks.md).

## Was passiert wann?

- **`pre-commit`** (vor jedem `git commit`): blockiert direkte Commits auf `main`/`staging` sowie
  Verstöße gegen Code-Formatierung, XML-Dokumentation, Übersetzungs-Konsistenz und verbotene
  Muster. Enum-Testabdeckung und Platzhalter-Implementierungen laufen im **Warn-Modus** — sie geben
  eine Meldung aus, blockieren den Commit aber nicht. So bleibt Work-in-Progress möglich.
- **`pre-push`** (vor jedem `git push`): blockiert direkte Pushes auf `main`/`staging` sowie
  Pushes, wenn Verstöße gegen verbotene Muster, Code-Formatierung, XML-Dokumentation,
  Übersetzungs-Konsistenz, Enum-Testabdeckung, Platzhalter-Implementierungen, das
  Commit-Nachrichten-Format oder fehlschlagende Tests gefunden werden (jeweils für das gesamte
  Repository im strikten Modus).

## Notfall-Fallback

Wenn ein Hook fälschlicherweise blockiert oder ein Notfall eine sofortige Aktion erfordert:

```bash
SKIP_HOOKS=1 git commit -m "..."
SKIP_HOOKS=1 git push
```

Dies überspringt **alle** Hook-Prüfungen und gibt dabei eine gut sichtbare Warnung auf stderr
aus (damit ein Übergehen nicht unbemerkt bleibt). Alternativ funktionieren auch die
Git-Standardmechanismen `git commit --no-verify` und `git push --no-verify` bzw.
`git push --force` — diese sind in der lokalen Shell-Historie nachvollziehbar.

Für die Testausführung in `pre-push` gibt es zusätzlich den gezielten Fallback
`HOOK_SKIP_TESTS=1` (siehe [`checks.md`](checks.md#test-execution-checkpy)).

## FAQ

**"python3: command not found" beim Commit/Push**
Python 3.x ist nicht im `PATH`. Installieren und sicherstellen, dass `python3` (oder unter
Windows alternativ `python`) aus einer normalen Shell heraus aufrufbar ist. Siehe
[`installation.md`](installation.md).

**Der erste Commit nach dem Klonen schlägt sofort mit einem Branch-Protection-Fehler fehl**
Das ist gewollt, wenn direkt auf `main` oder `staging` gearbeitet wird. Einen Feature-Branch
anlegen (`git checkout -b feature/...`) und dort committen.

**`dotnet format` meldet Formatierungsabweichungen**
Das blockiert den Commit. Mit `dotnet format Tankradar.sln` lokal beheben.

**Warum blockiert `pre-push`, obwohl `pre-commit` durchgelaufen ist?**
`pre-commit` prüft die meisten Checks nur für gestaffelte Dateien (die Formatprüfung läuft
immer über die gesamte Solution); `pre-push` prüft das gesamte Repository im
strikten Modus (`--all --strict`) und validiert zusätzlich die Commit-Nachrichten
(Merge-Commits müssen mit `merge: …` beginnen, „Merge branch …“ wird abgelehnt). Enum-Testabdeckung und Platzhalter-Implementierungen laufen in
`pre-commit` als Warnung und blockieren nicht, werden aber in `pre-push` blockiert — das ist
beabsichtigt und gibt Zeit, Work-in-Progress schrittweise fertigzustellen, bevor sie das
lokale Repository verlässt.

**Wie deaktiviere ich die Hooks dauerhaft?**
`git config --local --unset core.hooksPath` im Repository ausführen. Dies wird nicht empfohlen,
da die Hooks Teil der Qualitätssicherung dieses Projekts sind.

## Weiterführende Dokumentation

- [`installation.md`](installation.md) — Schritt-für-Schritt-Installation, Systemanforderungen,
  Troubleshooting
- [`checks.md`](checks.md) — Alle Prüfungen im Detail: wann sie laufen, was sie prüfen, wie
  Verstöße behoben werden
