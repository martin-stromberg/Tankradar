← [Zurück zur Übersicht](index.md)

# Versionierung

Versionen werden aus den Commit-Nachrichten (Conventional Commits) abgeleitet; es gibt keine von Hand
gepflegte Versionsnummer und kein handgeschriebenes Changelog. Die Release-Notes erzeugt GitHub
(`--generate-notes`).

## Regeln

| Situation | Ergebnis |
|---|---|
| Noch kein Release-Tag vorhanden | Erste Version ist **0.1.0**. |
| `feat:` | Minor-Anhebung (`0.1.0` → `0.2.0`). |
| `fix:`, `perf:` | Patch-Anhebung (`0.2.0` → `0.2.1`). |
| Breaking Change (`feat!:` oder Footer `BREAKING CHANGE:`) bei Hauptversion 0 | Nur Minor-Anhebung — **keine automatische Anhebung auf 1.0.0**. |
| `docs:`, `chore:`, `ci:`, `test:`, `refactor:`, `style:`, `build:`, `plan:`, `merge:` | Löst keine neue Version aus. |
| Hauptversion ab 1 | Klassisches SemVer (Breaking → major). |

Version 1.0.0 setzt der Projektleiter bewusst per manuell gepushtem Tag `v1.0.0`; der
`Release`-Workflow unterstützt manuelle Tags als vollwertigen Weg.

## Pre-Releases

Jeder Push auf `staging` mit freigabefähigen Commits erzeugt `v<Version>-rc.<n>` (kleines `rc`, `n` ab 1,
gezählt über die vorhandenen Tags dieser Version). Beispiel: Letztes Release `v0.1.0`, ein `feat:` →
`v0.2.0-rc.1`, der nächste Push vor dem Release → `v0.2.0-rc.2`. Die RC-Version (mit Suffix, ohne `v`)
wird als `Version` des Pakets und in `update.json` geschrieben.

## Voraussetzung

Die Berechnung benötigt den letzten Release-Tag im Verlauf von `staging`. Deshalb den Backmerge-PR
`main` → `staging` immer mit **„Create a merge commit“** mergen.

## Lokal ausprobieren

```powershell
node scripts/determine-next-version.mjs --rc
```

Die Ausgabe zeigt `changed`, `version` und die RC-Werte für den aktuellen Stand des Repositories.
