# Übersetzte Anforderung – Issue #58: Automatisierung wiederkehrender Architektur-, Sicherheits- und Compliance-Reviews

## Fachliche Zusammenfassung

Die CI/CD-Infrastruktur des Repositories wird um drei eigenständige, zeitgesteuerte GitHub-Actions-Workflows erweitert, die jeweils automatisch ein GitHub-Issue mit fest vorgegebenem deutschem Prüfauftrag erzeugen. Jedes Issue erwähnt im Body zu Beginn den Account `@softwarewerkstatt`, wodurch die automatische Analyse und Bearbeitung durch diesen Account angestoßen wird. Vor jeder Issue-Erzeugung prüft der Workflow, ob bereits ein offenes Issue desselben Prüftyps existiert, und überspringt die Erstellung in diesem Fall, um Duplikate zu vermeiden.

## Fachlicher Scope

- **Verarbeitungseinheit:** Ein „Review-Zyklus" je Prüftyp. Es existieren drei disjunkte Prüftypen mit eigenem Rhythmus und eigenem Issue-Text:
  1. **Architektur- und Sicherheitsreview** (monatlich, jeweils am 1. Tag des Monats)
  2. **NuGet-Paketprüfung** (wöchentlich)
  3. **Compliance- und Copyright-Review** (jährlich, am 1. Januar)
- **Auslösende Ereignisse:** Zeitgesteuerter `schedule`-Trigger (Cron) pro Workflow. Ergänzend soll – entsprechend der bestehenden Konvention (z. B. `security-scan.yml`) – ein `workflow_dispatch`-Trigger für manuelle Auslösung vorgesehen werden.
- **Erwartete Wirkung:** Ein neues, offenes GitHub-Issue im Repository `martin-stromberg/Tankradar` mit aussagekräftigem Titel und dem im Issue #58 wörtlich vorgegebenen Body-Text, beginnend mit der Zeile `@softwarewerkstatt`. Das Issue ist der sichtbare Auslöser für die nachgelagerte automatische Bearbeitung.
- **Umgang mit fehlenden Objekten:**
  - **Offenes Issue desselben Prüftyps:** Vor der Erzeugung ist zu prüfen (`gh issue list --state open`), ob ein solches Issue existiert. Existiert eines, wird kein neues erzeugt und der Lauf endet erfolgreich ohne Aktion. Das Dedup-Kriterium (empfohlen: ein Label je Prüftyp, analog zum bestehenden Label `automated-promotion`; alternativ Titelsuche) ist festzulegen.
  - **Labels:** Falls die Deduplizierung labelbasiert erfolgt, sind die Labels idempotent im Workflow anzulegen (`gh label create ... --force`), wie in `staging-to-main-promotion.yml` vorgeführt.
  - **Berechtigung:** Die Workflows benötigen `permissions: issues: write` (sowie `contents: read`, falls ein Checkout erforderlich ist); das Token wird wie üblich über `GH_TOKEN: ${{ github.token }}` bereitgestellt.
- **Scope-Ausschlüsse:**
  - Die Workflows führen **keine** Reviews, Analysen oder Paket-Updates selbst durch – sie erzeugen ausschließlich die Issues. Die eigentliche Prüfung (inkl. PR-Erstellung für unbedenkliche NuGet-Updates) obliegt dem durch die Erwähnung getriggerten Account `@softwarewerkstatt`.
  - Kein automatisches Schließen, Eskalieren oder Nachfassen bei unbearbeiteten Issues; existiert noch ein offenes Issue, wird schlicht kein neues erzeugt.
  - Keine Änderung am bestehenden wöchentlichen `security-scan.yml` (reine Schwachstellensuche im Code, kein Issue-Mechanismus).
  - Kein konfigurierbarer Intervall-Mechanismus – die drei Rhythmen sind fest in den Cron-Ausdrücken verdrahtet.

## Betroffene Klassen und Komponenten

Es werden keine Anwendungsklassen (Datenmodell, Services, Interfaces, UI) angelegt oder geändert – das Feature liegt vollständig auf Repository-/CI-Ebene:

- **Neu:** drei Workflow-Dateien unter `.github/workflows/` (Vorschlag):
  - `review-architecture-security.yml` – Cron monatlich am 1. (z. B. `0 6 1 * *`)
  - `review-nuget-packages.yml` – Cron wöchentlich (Wochentag/Uhrzeit zu bestimmen; bestehender `security-scan.yml` läuft Montag 04:00 UTC)
  - `review-compliance.yml` – Cron jährlich am 1. Januar (z. B. `0 6 1 1 *`)
- **Dokumentation (anzupassen):** `docs/help/ci-cd/workflows.md` (Tabelle „Die sieben Workflows" wird um die drei neuen Einträge erweitert), ggf. Querverweise in `docs/help/ci-cd/index.md` / `einrichtung.md`.
- **Zu beachten:** `scripts/validate-workflows.py` prüft alle Workflows strukturell; die neuen Dateien müssen diese Prüfung bestehen (u. a. keine verbotenen `gh release`-/`.ipa`-Muster – hier nicht relevant).
- **Tests:** Keine Anwendungstests; die Korrektheit der Workflows wird über `validate-workflows.py` und den manuellen `workflow_dispatch`-Lauf verifiziert.

## Implementierungsansatz

- Pro Prüftyp ein eigener Workflow mit `on: schedule: - cron: '...'` plus `workflow_dispatch`.
- Job auf `ubuntu-latest` (wie `staging-to-main-promotion.yml`), `shell: bash`, kein Checkout nötig, da der Issue-Text im Workflow selbst liegt (Annahme – alternativ Texte als Dateien im Repo, siehe Offene Fragen).
- Schritte je Workflow:
  1. Ggf. Label je Prüftyp idempotent anlegen (`gh label create ... --force`).
  2. Dedup-Prüfung: `gh issue list --state open` gefiltert nach Label bzw. Titel; bei Treffer Workflow ohne Issue-Erzeugung beenden.
  3. `gh issue create --title "<aussagekräftiger Titel>" --body "<vorgegebener Text inkl. @softwarewerkstatt>"` (ggf. mit `--label`).
- Vorgegebene Issue-Texte werden **wörtlich** aus Issue #58 übernommen (Checklisten pro Prüftyp).
- Hinweis: `schedule`-Trigger wirken erst, sobald die Workflows auf dem Default-Branch (`main`) liegen – im Staging-Workflow also erst nach der Promotion `staging → main`.
- Bekannte GitHub-Einschränkung: Scheduled Workflows können bei hoher Last verzögert starten und werden nach 60 Tagen Repository-Inaktivität deaktiviert (Plattformverhalten, kein Umsetzungspunkt).

## Konfiguration

- Intervalle sind nicht konfigurierbar, sondern fest über die Cron-Ausdrücke definiert (Anforderung). Wartung erfolgt durch Editieren der Workflow-Dateien.
- Manuelle Auslösung über `workflow_dispatch` (Repository-Konvention).

## Offene Fragen

1. **Dedup-Kriterium:** Labelbasiert (empfohlen, analog `automated-promotion`) oder Titel-/Textsuche? Bei Label-Ansatz: Welche Label-Namen (z. B. `review-architecture`, `review-nuget`, `review-compliance`)? Bereits offene Issues ohne Label würden nicht erkannt.
2. **Zeitpunkt des wöchentlichen Laufs:** Welcher Wochentag/welche Uhrzeit? Abstimmung mit `security-scan.yml` (Mo 04:00 UTC) zweckmäßig, z. B. ein anderer Tag oder versetzte Stunde.
3. **Issue-Titel:** Konkrete Titel je Prüftyp festlegen (Vorschlag: „Monatliches Architektur- und Sicherheitsreview – <Monat Jahr>" usw., ggf. mit Datum für Eindeutigkeit).
4. **Zuordnung:** Reicht die Erwähnung `@softwarewerkstatt` im Body (Anforderung) oder soll das Issue zusätzlich per `--assignee` zugewiesen werden?
5. **Ablage der Issue-Texte:** Inline im Workflow (einfachster Weg) oder als separate Template-Dateien im Repo?
