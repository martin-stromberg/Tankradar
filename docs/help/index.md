# Dokumentation — Tankatlas

Übersicht über alle dokumentierten Funktionsbereiche und Entwickler-Ressourcen von Tankatlas.

## Benutzung der App

- [Navigation und Design-System](Navigation/index.md) — Die vier Hauptbereiche von Tankatlas und wie Sie zwischen ihnen navigieren. Die App passt sich automatisch an Ihre bevorzugte Darstellung (Hell-/Dunkelmodus) an.
- [Einstellungen (Optionen)](Einstellungen/index.md) — Spritsorten auswählen und ordnen, Standortnutzung, Standardansicht und Standardsortierung festlegen. Änderungen werden sofort lokal gespeichert und überstehen Neustart und App-Updates.

## Lokale Entwicklung

- [Git-Hooks zur Qualitätssicherung](git-hooks/index.md) — Automatisierte Qualitätsprüfungen beim Commit und Push. Prüft Übersetzungen, XML-Dokumentation, Platzhalter-Implementierungen, Enum-Testabdeckung, Code-Formatierung, Secrets und das Commit-Nachrichten-Format. Installationsanleitung und Troubleshooting enthalten.
- [CI/CD-Pipeline mit Pre-Releases und Releases](ci-cd/index.md) — Automatischer Build, Prüfungen und Veröffentlichung über GitHub Actions: Branch-Modell `main`/`staging`, Pre-Releases bei jedem Push auf `staging`, Releases bei jedem Push auf `main`, Versionierung ab 0.1.0, lokaler Prüflauf als Ersatz für Actions und Einrichtungs-Checkliste für GitHub.
