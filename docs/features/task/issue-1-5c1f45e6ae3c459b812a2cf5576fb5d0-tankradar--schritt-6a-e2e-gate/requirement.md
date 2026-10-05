### Schritt 6a: Oberflächentests als Auslieferungs-Gate, ohne den Anwender zu stören

**Beschreibung:** Jede Auslieferung von Tankatlas (Pre-Release auf `staging`, Release auf `main`)
soll nur mit vollständig grünen Tests erfolgen, einschließlich der FlaUI-Oberflächentests unter
Windows. Bisher laufen die Oberflächentests in der CI nach der Vorlage nur als „best-effort“: Ein
Fehlschlag erzeugt eine Warnung, verhindert aber weder Merge noch Release. Künftig sind sie in der
PR-CI nach `staging` blockierend; dies wird als bewusste Abweichung von der CI-Vorlage dokumentiert.
Die vorhandenen Diagnose-Artefakte und die begrenzte Wiederholung bei UI-Automation-Timeouts
bleiben erhalten.

Gleichzeitig sollen lokale Testläufe den Anwender bei seiner Arbeit möglichst nicht stören. Der
`pre-push`-Hook führt die Oberflächentests standardmäßig nicht mehr aus (Unit- und
Integrationstests weiterhin); per Umgebungsvariable lassen sie sich dort einschalten. Der lokale
Prüflauf `scripts/local-ci.ps1` führt sie weiterhin aus. Im Testmodus startet die App ihr Fenster
außerhalb des sichtbaren Bildschirmbereichs und ohne sich in den Vordergrund zu holen; die Tests
bedienen die App dafür ausschließlich über UI-Automation-Muster (Auslösen, Auswählen, Werte setzen,
Scrollen) statt über Mausklicks. Diagnose-Screenshots bei Fehlschlägen werden direkt vom
App-Fenster aufgenommen. Diese Betriebsart wird nur übernommen, wenn sie nachweislich genauso
stabil ist wie der bisherige Betrieb im Vordergrund (mindestens fünf vollständig grüne Läufe
hintereinander, lokal und in der CI); andernfalls bleibt der bisherige Vordergrundbetrieb bestehen,
und nur die übrigen Einschränkungen gelten. Die Entscheidung und ihr Nachweis werden in der
Projektdokumentation festgehalten.

**Abhängigkeiten:** 3, 6

**Betroffene Bereiche:** E2E-Tests, CI, Git-Hooks, Entwicklungsprozess
