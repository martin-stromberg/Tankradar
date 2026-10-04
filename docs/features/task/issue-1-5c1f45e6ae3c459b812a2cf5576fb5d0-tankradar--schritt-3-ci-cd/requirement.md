# Anforderung: CI/CD-Pipeline mit Pre-Releases und Releases (Tankradar, Schritt 3)

## Ziel
Das GitHub-Repository erhält eine CI/CD-Pipeline (GitHub Actions), die Build, Tests und
Release-Prozess automatisiert. Grundlage ist die Vorlage `ci-workflows-instructions.md`
(alle sieben Workflows, beide gemeinsamen Bausteine, Versionsermittlung, Troubleshooting,
Checkliste), angepasst an die MAUI-App.

## Funktionale Anforderungen
1. Branch-Modell `main` (stabile Releases), `staging` (Pre-Releases), Entwicklungszweige; PRs nach `main` nur von `staging`.
2. Build der Windows- und der iOS-Variante (iOS 16 Mindestversion).
3. Prüfungen: Formatprüfung, statische Analyse, Abhängigkeits-Schwachstellenprüfung, Unit-, Integrations- und FlaUI-E2E-Tests (Windows), Mindest-Testabdeckung der Vorlage (70 %) erzwingen.
4. Push auf `staging` erzeugt automatisch Pre-Releases (RC), Push auf `main` finale Releases.
5. Release-Artefakt der Windows-App ist ein gezipptes, nach dem Entpacken ohne Installation startbares Build; kein MSIX/Setup.
6. Versionierung nach Vorlage: erste Version 0.1.0, bis 1.0 keine automatische Anhebung auf 1.0.
7. Geheimnisse/umgebungsspezifische Werte nur über GitHub Secrets/Repository-Variablen (API-Schlüssel Kraftstoffpreis/Routing, Signierung, Bundle-ID, Apple-Team-ID). Fehlen Signierung/Apple-Kennungen, wird iOS unsigniert gebaut, ohne Fehlschlag.
8. Gleichwertiger lokaler Prüflauf (Billing-Limit-Fallback), Nutzung dokumentiert.
9. Einmalige Repository-Einstellungen (Branches, Branch-Schutz, Labels, Secrets, Variablen) als Checkliste in der Projektdokumentation.
10. Nachweis: erfolgreicher lokaler Prüflauf und syntaktisch gültige Workflows.

## Restpunkte aus Abnahme Schritt 2
- `.githooks/install-hooks.sh` im Index mit Ausführungsrecht (`git update-index --chmod=+x`).
- `docs/help/git-hooks/checks.md`, Docstring `.githooks/format-code-style-check.py` und `changes.log`-Eintrag zu Schritt 2 korrekt beschreiben: Formatprüfung beim Commit über die gesamte Solution und blockierend; Commit-Nachrichten werden beim Push geprüft; Merge-Commits müssen `merge: …` heißen, „Merge branch …“ wird abgelehnt.

## Rahmenbedingungen
Nichts pushen; keine GitHub-Secrets/Branches/Einstellungen anlegen; `docs/projects/` unverändert; Hooks bleiben grün.
