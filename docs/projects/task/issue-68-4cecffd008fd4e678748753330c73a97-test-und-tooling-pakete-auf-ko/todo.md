# Projekt-Todo

Basisbranch: `task/issue-68-4cecffd008fd4e678748753330c73a97-test-und-tooling-pakete-auf-ko`
Issues: #68, #69, #70, #71
Anmerkung: In dieser Umgebung stehen keine delegierbaren Codex-Unteragenten zur Verfügung;
die Arbeitsaufträge (Übersetzung, Bestandsaufnahme, Planung, Implementierung, Reviews,
Tests, Dokumentation) werden daher direkt ausgeführt (documentierte Abweichung gemäß
project-Skill, Abschnitt „Ausführung in Codex").

## Checkliste

- [x] Branchprüfung (kein Hauptbranch, kein detached HEAD)
- [x] Verzeichnis/Tracking angelegt (`docs/projects/{branchname}/`)
- [x] Einstiegspunkt bestimmt (Neues Projekt, keine Artefakte vorhanden)
- [x] Vorbedingung: Release abwarten und Backmerge nach staging einholen
- [x] Anforderung aufbereiten (`requirement.md`)
- [x] Bestandsaufnahme (`inventory.md`)
- [ ] Grobe Planung (`project-plan.md`)
- [ ] Offene Punkte klären
- [ ] Planprüfung (`project-plan-check.md`)
- [ ] Planungscommit
- [ ] Entwicklungsschritte-Tracking (`steps.md`, `status.html`)
- [ ] Entwicklungsschleife (Lifecycle je Schritt, Abnahme, Merge)
- [ ] Issue-Statuskommentare (#68, #69, #70, #71)
- [ ] PR gegen staging erstellen und Checks überwachen
- [ ] Aufräumen und Abschlusscommit
