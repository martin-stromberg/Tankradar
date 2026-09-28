# Umsetzungsplan: Lokale Git-Hooks zur Qualitätssicherung (Schritt 2)

## Übersicht

Das Repository erhält ein versioniertes System lokaler Git-Hooks unter `.githooks/`, das vor jedem Commit und Push automatisch Qualitätsprüfungen durchführt. Die Hooks übernehmen bewährte Checks aus einer Template-Vorlage (XML-Dokumentation, Stub-Prüfung, Enum-Coverage) und erweitern sie um projektspezifische Anforderungen (Formatierung, Secrets-Erkennung, Conventional Commits, Test-Validierung). Hooks und Installationsskripte werden versioniert im Repository abgelegt; Installation erfolgt durch Ausführung eines Skripts (Windows `.cmd` / Unix `.sh`). Installation und Troubleshooting werden in `docs/help/git-hooks/` dokumentiert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| **Hook-Pfad-Konfiguration** | `git config --local core.hooksPath .githooks` | Elegante Lösung (Git 2.9+), keine Symlinks/Kopien nötig, Hook-Dateien bleiben versioniert. Voraussetzung: Git 2.9+ (üblich in modernen IDEs und CI-Systemen). |
| **Razor-Checks Replacement** | `translation-check.py` bleibt unverändert (prüft XAML-Ressourcendateien), `razor-l10n-check.py` und `razor-usage-check.py` entfallen. | XAML nutzt andere Ressourcen-Struktur als Razor. Lokalisierungsprüfung via Ressourcen-Dateien (nicht XAML-Text) durchführbar. XAML-Komponenten-Verwendung wird nicht durch einen Hook prüfbar (technisch komplex, außerdem nicht in Anforderung explizit). |
| **Code-Formatierungs-Check** | `dotnet format --verify-no-changes` in Pre-Commit-Hook (Warn-Modus); blockiert nicht. | Standard-Tool, teil .NET SDK. Soll Entwickler hinweisen, aber Commit nicht blockieren (Work-in-Progress-Flexibilität). Mit `--verify-no-changes` wird nur geprüft, nicht automatisch repariert (saubere Audit-Trail). |
| **Forbidden Patterns** | Hardcodierte Liste mit Regex-Mustern für API-Schlüssel (Tankerkönig, OpenRouteService), Zertifikate (.pfx, .p12, .keystore), Signierungsdaten, DB-Dumps (.db, .sqlite), Logdateien (.log ab 1 MB). Blockiert in Pre-Push. | Projekt-spezifisch für Kraftstoff-/Routing-APIs, gängige Secret-Patterns (AWS, Azure, GitHub Tokens als Optionen). |
| **Conventional Commits Format** | Pflicht-Format: `type(scope): message`. Erlaubte Types: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`, `perf`. Scopes optional: `core`, `ui`, `api`, `hooks`, `ci`, etc. Blockiert in Pre-Push. | Standard Conventional Commits. Scopes halten sich an Projektbereiche (Core-Logik, UI-Komponenten, API-Integration, Hook-Wartung). Wird später für automatische Versionierung (Schritt 3) benötigt. |
| **Test-Validierung vor Push** | Hook ruft `dotnet test Tankradar.sln` auf; blockiert Push nur wenn Tests fehlschlagen (Exit-Code ≠ 0). Timeout: 5 Minuten; überschreitbar mit Umgebungsvariable `HOOK_SKIP_TESTS=1` (Notfall-Fallback). | Stellt sicher, dass nur arbeitender Code gepusht wird. 5 Min Timeout verhindert Hängenlassen (Entwickler können dann `--no-verify` nutzen, dies wird gelogged). Timeout-Überschreitung ist dokumentierter Ausfall-Modus. |
| **Hook-Deaktivierung (Notfall)** | Umgebungsvariable `SKIP_HOOKS=1` überspringt alle Hooks mit Warnung auf stderr. Alternativ (selten): `git commit --no-verify`, `git push --force` (Git-Standard, wird loggbar). | Gibt Entwicklern Notfall-Ausweg bei echten Blockierungen, erzeugt aber sichtbare Warnung. Verhindert stille Umgehung. |
| **Python-Umgebung** | System-Python 3.x (keine venv erforderlich). Hooks nutzen Shebang `#!/usr/bin/env python3`. Voraussetzung: `python3` im PATH. | Einfach, verbreitet. Virtual Environments komplizieren Installation und Wartung; nicht nötig da die Check-Skripte nur Standard-Library nutzen (oder minimal externe Abhängigkeiten). Dokumentation weist auf Anforderung hin. |
| **Installation als Requisite** | `install-hooks.cmd` (Windows) / `install-hooks.sh` (Unix) setzen nur `git config --local core.hooksPath .githooks`. Hook-Dateien sind bereits im Repo, müssen nicht kopiert werden. | Minimale Komplexität. Lokale Config reicht. Nach Pull ist Installation ein einmaliger Schritt pro Klon (nicht wiederholt bei jedem Pull). |

## Programmabläufe

### Pre-Commit-Hook Ablauf

1. **Ablauf-Beginn:** Git ruft `.githooks/pre-commit` auf vor Commit-Operation
2. **Branch-Protection:** Shell-Hook prüft aktuellen Branch mit `git rev-parse --abbrev-ref HEAD`
   - Wenn `main` oder `staging`: Echo Warnung, Exit 1 (Commit blockiert)
   - Sonst: Fortfahren
3. **Python-Checks (Warn-Modus, nicht blockierend):**
   - Repo-Root ermitteln: `git rev-parse --show-toplevel`
   - `csproj-xmldoc-check.py` (nur gestaffelte Dateien, kein `--all`)
   - `no-notimplemented-check.py` (nur gestaffelte Dateien)
   - `enum-coverage-check.py` (nur gestaffelte Dateien)
   - `translation-check.py` (nur gestaffelte Dateien)
4. **Formatierungs-Prüfung:** `dotnet format --verify-no-changes` (nur wenn `.sln` vorhanden)
   - Warnung ausgeben bei Verstoß, nicht blockieren
5. **.NET-Optionale Checks:** Falls `SecretScan.csproj` existiert, Optional-Projekt bauen (Warnung bei Fehler)
6. **Exit-Code:** 0 (Warnungen akzeptiert), nur Blockade bei Branch-Protection oder kritischem Fehler

Beteiligte Dateien/Klassen: `.githooks/pre-commit` (Shell), `csproj-xmldoc-check.py`, `no-notimplemented-check.py`, `enum-coverage-check.py`, `translation-check.py`, `format-code-style-check.py`

### Pre-Push-Hook Ablauf

1. **Ablauf-Beginn:** Git ruft `.githooks/pre-push` auf vor Push-Operation, stellt Ref-Information via stdin zur Verfügung
2. **Branch-Protection:** Liest stdin für Remote-Refs, prüft auf `refs/heads/main` oder `refs/heads/staging`
   - Wenn gepusht wird auf `main` oder `staging`: Echo Warnung, Exit 1 (Push blockiert)
   - Sonst: Fortfahren
3. **Strict Python-Checks (blockierend mit `--all --strict`):**
   - `no-notimplemented-check.py --all --strict`
   - `enum-coverage-check.py --all --strict`
4. **Conventional Commits Validierung:** `conventional-commits-check.py` validiert alle Commits des aktuellen Branches gegen `main`
   - Prüft Format `type(scope): message`
   - Blockiert bei Formatverstößen
5. **Test-Ausführungs-Validierung:** `test-execution-check.py` führt `dotnet test Tankradar.sln` aus
   - Timeout: 5 Minuten (überschreibbar via `HOOK_SKIP_TESTS=1`)
   - Blockiert bei Testfehlschlag
6. **Exit-Code:** 0 nur wenn alle Checks bestanden, ansonsten Exit 1 (Push blockiert)

Beteiligte Dateien/Klassen: `.githooks/pre-push` (Shell), `no-notimplemented-check.py`, `enum-coverage-check.py`, `conventional-commits-check.py`, `test-execution-check.py`

### Installation und Aktivierung

**Windows (`install-hooks.cmd`):**
1. Batchdatei prüft, ob Git verfügbar ist (`git --version`)
2. Setzt `git config --local core.hooksPath .githooks`
3. Versucht, Hook-Dateien ausführbar zu machen (über `attrib` oder `git`): `git config core.hooksPath .githooks` (Windows-spezifisch nicht immer nötig, aber für Git-Bash sauberer)
4. Prüft auf Python 3: `python3 --version` oder `python --version`
5. Gibt Erfolgsmeldung aus

**Unix/macOS/Linux (`install-hooks.sh`):**
1. Bash-Skript prüft Git-Verfügbarkeit
2. Setzt `git config --local core.hooksPath .githooks`
3. Macht Hook-Dateien ausführbar: `chmod +x .githooks/pre-commit .githooks/pre-push`
4. Macht Python-Skripte lesbar (bereits mit korrektem Shebang)
5. Prüft Python 3 Verfügbarkeit
6. Gibt Erfolgsmeldung aus

**Manuelle Installation (Fallback):**
```bash
git config --local core.hooksPath .githooks
```

Beteiligte Dateien/Klassen: `install-hooks.cmd`, `install-hooks.sh`

## Neue Dateien

| Datei | Typ | Zweck |
|-------|-----|-------|
| `.githooks/pre-commit` | Shell-Hook | Führt Branch-Protection und Warn-Checks vor Commit aus |
| `.githooks/pre-push` | Shell-Hook | Führt Branch-Protection und Strict-Checks vor Push aus |
| `.githooks/csproj-xmldoc-check.py` | Python-Prüfskript | Kopie aus Template: Prüfung XML-Dokumentation in C#-Code und .csproj |
| `.githooks/no-notimplemented-check.py` | Python-Prüfskript | Kopie aus Template: Blockade von NotImplementedException und Stubs |
| `.githooks/enum-coverage-check.py` | Python-Prüfskript | Kopie aus Template: Warnung/Blockade bei fehlender Enum-Testabdeckung |
| `.githooks/translation-check.py` | Python-Prüfskript | Kopie aus Template (angepasst): Prüfung fehlender Übersetzungen in Ressourcen |
| `.githooks/format-code-style-check.py` | Python-Prüfskript | Neu: Prüfung Code-Formatierung via `dotnet format --verify-no-changes` |
| `.githooks/forbidden-patterns-check.py` | Python-Prüfskript | Neu: Blockade von API-Schlüsseln, Zertifikaten, Signierungsdaten, DB-Dumps, Logdateien |
| `.githooks/conventional-commits-check.py` | Python-Prüfskript | Neu: Validierung Commit-Nachrichten im Conventional-Commits-Format |
| `.githooks/test-execution-check.py` | Python-Prüfskript | Neu: Validierung lokaler Test-Ausführung vor Push |
| `.githooks/install-hooks.cmd` | Windows-Batchdatei | Installation der Hooks unter Windows |
| `.githooks/install-hooks.sh` | Unix-Shellskript | Installation der Hooks unter Unix/macOS/Linux |
| `docs/help/git-hooks/README.md` | Dokumentation | Übersicht: Zweck, Aktivierung, Troubleshooting der Hooks |
| `docs/help/git-hooks/installation.md` | Dokumentation | Schritt-für-Schritt Installation, Systemanforderungen, Validierung |
| `docs/help/git-hooks/checks.md` | Dokumentation | Detaillierte Beschreibung aller Hook-Prüfungen, Fehlercodes, Behebung |

## Änderungen an bestehenden Dateien

### `README.md`

- **Neue Abschnitt:** „Git Hooks" mit kurzem Verweis auf Installation
- **Inhalt:** Nach dem Klonen oder beim Onboarding: `.githooks/install-hooks.cmd` (Windows) oder `.githooks/install-hooks.sh` (Unix) aufrufen
- **Link:** Verweist auf `docs/help/git-hooks/` für Details

### `.gitignore` (falls nötig)

- **Prüfung:** Ist `review-versions/`, `design-draft/`, `.claude/` bereits ausgeschlossen? (Anforderung verlangt explizit diese Ausschlüsse)
- **Änderung (wenn nötig):** Ergänze falls fehlend

### Template-Hooks-Kopien anpassen

**`csproj-xmldoc-check.py`:**
- Unkritisch, wird 1:1 kopiert
- Optional: Comment im Header aktualisieren, um auf Tankradar zu verweisen

**`no-notimplemented-check.py`:**
- Unkritisch, wird 1:1 kopiert

**`enum-coverage-check.py`:**
- Unkritisch, wird 1:1 kopiert
- Prüfung: Test-Verzeichnisse müssen korrekt erkannt werden (`Tankradar.Tests.*`)

**`translation-check.py`:**
- Unkritisch, wird 1:1 kopiert (nutzt generisches Ressourcen-Pattern)

**`pre-commit` und `pre-push`:**
- Werden 1:1 kopiert aus Template, aber:
  - `.razor`-Checks werden entfernt oder mit Kommentar markiert als „entfällt für MAUI/XAML"
  - Optionale Projekte (`SecretScan.csproj`, `MarkdownLinkCheck.csproj`) bleiben als optionale Checks erhalten (Hook läuft trotzdem, warnt aber nicht wenn nicht vorhanden)

## Datenbankmigrationen

Keine. Die Git-Hooks beeinflussen die Anwendungsdatenbank nicht.

## Validierungsregeln

Siehe Python-Prüfskripte:

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| **Commit auf `main` / `staging`** | Branch-Protection blockiert direkte Commits | Exit 1, Commit abgelehnt |
| **Push auf `main` / `staging`** | Branch-Protection blockiert direkten Push | Exit 1, Push abgelehnt |
| **XML-Dokumentation** (C#-Code) | Öffentliche/interne Typen und Member müssen `<summary>`, `<param>`, `<returns>`, `<typeparam>`, `<response>` haben | Warnung in pre-commit; blockiert nicht |
| **NotImplementedException** | Keine `throw new NotImplementedException(...)` oder `throw new Exception(...)` im finalen Code (Stubs) | Warnung in pre-commit, blockiert in pre-push |
| **Enum-Testabdeckung** | Alle öffentlichen/internen Enum-Werte müssen in Test-Dateien verwendet werden | Warnung in pre-commit, blockiert in pre-push |
| **Übersetzungen** | Alle lokalisierbaren Ressourcenschlüssel müssen über alle Sprachen konsistent sein | Warnung in pre-commit |
| **Code-Formatierung** | Code muss mit `dotnet format` Standard-Regeln erfüllen | Warnung in pre-commit |
| **Verbotene Muster** | Keine API-Schlüssel (Regex: `(FUEL_API_KEY\|ROUTING_KEY\|MAPBOX_TOKEN)`, etc.), Zertifikate (.pfx, .p12, .keystore, .jks, .pem private), Signierungsdaten, DB-Dumps (*.db*, *.sqlite*), große Logdateien (>1 MB .log) | Blockiert in pre-push |
| **Commit-Nachricht Format** | Format: `type(scope): message`. Type: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`, `perf`. Scope optional. Nachricht: min 10 Zeichen, keine Großbuchstaben-Start (außer Eigennamen) | Blockiert in pre-push |
| **Test-Ausführung** | `dotnet test Tankradar.sln` muss erfolgreich durchlaufen (Exit 0) vor Push | Blockiert in pre-push |

## Konfigurationsänderungen

Keine Änderungen an `appsettings.json` oder Projektdateien erforderlich. Git-Config wird durch Installationsskript gesetzt:

| Eintrag | Typ | Wert | Zweck |
|---------|-----|------|-------|
| `git config --local core.hooksPath` | Git-Konfiguration | `.githooks` | Gibt Git-Pfad für Hook-Dateien vor; lokal pro Repository |

## Seiteneffekte und Risiken

- **Code-Formatierungs-Warnung:** Bestehender Code könnte nicht den `dotnet format` Standards entsprechen. Diese Warnung ist nicht-blockierend, aber könnte Entwickler-Workflow beeinflussen. Risiko: **Niedrig** (Warnung nur, nicht blockierend).

- **XML-Dokumentation fehlend:** Wenn bestehender C#-Code nicht vollständig dokumentiert ist, erscheinen Warnungen. Risiko: **Mittel** — Größerer Bestand könnte viele Warnungen haben, aber auch hier nur Warnung, nicht blockierend in pre-commit. **KRITISCH:** Schritt 2 Anforderung sagt „Hooks müssen auf aktuellem Code-Stand grün durchlaufen". Das bedeutet: Entweder ist der aktuelle Code bereits dokumentiert (Schritt 1), oder die Hook-Regeln müssen angepasst werden. Muss im Testschritt (Punkt 8 der Umsetzungsreihenfolge) validiert werden.

- **NotImplementedException / Enum-Coverage in pre-push:** Strict-Checks in pre-push könnten Pull-Requests blockieren, wenn nicht alle Tests Enum-Werte abdecken oder es noch Stubs gibt. Risiko: **Mittel** — pre-push ist blockierend, wird aber hauptsächlich erst bei Push-Vorbereitung geprüft. Teamkommunikation über Standards erforderlich.

- **Test-Timeout (5 Min):** Wenn Test-Suite länger als 5 Minuten läuft, blockiert der Hook den Push. Risiko: **Niedrig** (aktuell nur 11 Tests, schnell). Umgebungsvariable `HOOK_SKIP_TESTS=1` bietet Fallback.

- **Bestehende Tests müssen weiterhin durchlaufen:** Aktuelle 11 Tests müssen nach Hook-Installation weiterhin grün sein (Exit 0). Risiko: **Hoch** — Wenn Hooks selbst einen Test zum Scheitern bringen, muss Hook-Logik angepasst werden. Mitigieren: Testlauf in Schritt 8 durchführen.

- **Conventional Commits Rückwärtskompatibilität:** Ältere Commits des Repo entsprechen möglicherweise nicht dem Format. Das ist OK — Hook validiert nur neue Commits beim Push. Rückwirkend müssen alte Commits nicht angepasst werden (Pre-Push-Hook prüft Commits des aktuellen Branches gegen upstream).

- **Razor-Checks entfallen:** Keine XAML-Äquivalente implementiert. Lokalisierungs-Prüfung erfolgt weiterhin via `translation-check.py` auf Ressourcen-Basis. XAML-Komponenten-Verwaisung wird nicht prüfbar. Risiko: **Niedrig** (war in Anforderung als Optional/Anpassung gekennzeichnet).

## Umsetzungsreihenfolge

1. **Vorbereitung: Vorlage-Dateien kopieren und strukturieren**
   - Voraussetzungen: Vorlage unter `docs/projects/.../external-sources/githooks/` vorhanden
   - Beschreibung: Kopiere aus Template-Verzeichnis folgende Dateien nach `.githooks/`:
     - `pre-commit`, `pre-push` (Shell-Hooks)
     - `csproj-xmldoc-check.py`, `no-notimplemented-check.py`, `enum-coverage-check.py`, `translation-check.py` (Python-Checks aus Template)
     - `install-hooks.cmd`, `install-hooks.sh` (Installationsskripte)
     - Diese Dateien sind 1:1-Kopien oder minimal angepasst

2. **Hook-Dateien anpassen**
   - Voraussetzungen: Dateien aus Schritt 1 kopiert
   - Beschreibung: 
     - `pre-commit` und `pre-push`: Entferne oder kommentiere `razor-l10n-check.py` und `razor-usage-check.py` Aufrufe aus Template
     - `translation-check.py`: Validiere, dass es XAML-Ressourcen (nicht nur Razor-Komponenten) findet
     - Sicherstelle, dass alle Hook-Dateien korrekt ausführbar sind (Shebang, Line-Endings)

3. **Neue Python-Checks implementieren: `format-code-style-check.py`**
   - Voraussetzungen: `.NET SDK 10.0+` verfügbar (wird auf Developer-Maschinen vorausgesetzt)
   - Beschreibung:
     - Skript prüft `dotnet format --verify-no-changes` auf der Lösung
     - Bei Formatverstößen: Warnung auf stderr mit Liste betroffener Dateien
     - Exit-Code: 0 (auch bei Verstößen, nur Warnung für pre-commit)
     - Argumente: optional `--strict` (für zukünftige Nutzung; nicht in pre-commit, nur wenn manuell aufgerufen)

4. **Neue Python-Checks implementieren: `forbidden-patterns-check.py`**
   - Voraussetzungen: `.gitignore` vorhanden; Regex-Muster-Liste festgelegt
   - Beschreibung:
     - Prüft gestaffelte Dateien (ohne `--all`) oder ganz Repo (mit `--all`)
     - Regex-Muster für Verbotenes:
       - API-Schlüssel: `(FUEL_API_KEY|ROUTING_KEY|MAPBOX_TOKEN|NOMINATIM_TOKEN)\s*=`, `(?:fuel|routing|api)[_-]?(?:key|token|secret)\s*=` (case-insensitive)
       - Zertifikate: Dateiendungen `.pfx`, `.p12`, `.keystore`, `.jks`, `.pem`, `.cer`; Dateiinhalt: `-----BEGIN RSA PRIVATE KEY-----`, `-----BEGIN CERTIFICATE-----`, `-----BEGIN PRIVATE KEY-----`
       - DB-Dumps: Dateiendungen `.db`, `.sqlite`, `.sqlite3`
       - Logdateien: Dateien mit `.log` über 1 MB Größe
       - Signierungsdaten: `<SigningKey>`, `<CertificateThumbprint>` in .csproj-Dateien mit echten Werten (nicht Platzhalter)
     - Exit-Code: 0 (Warnung in pre-commit), 1 (Fehler in pre-push mit `--strict`)
     - Output: Dateiname, Zeilennummer, betroffene Muster

5. **Neue Python-Checks implementieren: `conventional-commits-check.py`**
   - Voraussetzungen: Git verfügbar, Repo initialisiert
   - Beschreibung:
     - Liest alle Commits des aktuellen Branches seit Divergenz von `main`
     - Validiert jede Commit-Nachricht gegen Format: `type(scope): subject`
     - Types: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`, `perf`
     - Scope: optional (z.B. `(core)`, `(ui)`, `(api)`)
     - Subject: min. 10 Zeichen, keine Großbuchstaben-Start (Großbuchstaben bei Eigennamen OK)
     - Exit-Code: 0 (alle OK), 1 (Verstoß gefunden)
     - Output: Commit-SHA, Nachricht, Fehler

6. **Neue Python-Checks implementieren: `test-execution-check.py`**
   - Voraussetzungen: `.NET SDK 10.0+`, Lösung `Tankradar.sln` vorhanden
   - Beschreibung:
     - Führt `dotnet test Tankradar.sln --nologo --verbosity quiet` aus mit Timeout 5 Min
     - Timeout kann via `HOOK_SKIP_TESTS=1` oder `TEST_TIMEOUT_SECONDS=...` überschrieben werden
     - Exit-Code: 0 (Tests bestanden), 1 (Tests fehlgeschlagen oder Timeout)
     - Output: Test-Summary (bestanden/fehlgeschlagen) auf stderr
     - Bei Timeout: Meldung auf stderr, dass `HOOK_SKIP_TESTS=1` zum Überspringen gesetzt werden kann

7. **Installationsskripte anpassen und testen**
   - Voraussetzungen: `.githooks/`-Verzeichnis mit allen Dateien angelegt
   - Beschreibung:
     - `install-hooks.cmd`: Validiere Syntax, teste unter Windows 11
       - Prüfe Git-Verfügbarkeit (`git --version`)
       - Setze `git config --local core.hooksPath .githooks`
       - Prüfe Python 3 (`python3 --version` oder `python --version`)
       - Keine Hook-Dateien manuell kopieren (nur Config)
     - `install-hooks.sh`: Validiere Syntax, teste unter Bash/Linux/macOS
       - Prüfe Git-Verfügbarkeit
       - Setze `git config --local core.hooksPath .githooks`
       - `chmod +x .githooks/pre-commit .githooks/pre-push`
       - Prüfe Python 3
     - Beide Skripte geben Erfolgsmeldung mit Link zur Dokumentation aus

8. **Testschritt: Hooks grün gegen aktuellen Code-Stand**
   - Voraussetzungen: Alle Hooks und Python-Checks implementiert; `install-hooks.cmd`/`.sh` funktioniert
   - Beschreibung:
     - Installation durchführen: `install-hooks.cmd` (oder `.sh`)
     - Validiere, dass `git config --local core.hooksPath` auf `.githooks` gesetzt ist
     - Führe manuell einen Commit (z.B. auf Branch) durch, um pre-commit zu testen:
       - Pre-commit muss durchlaufen (Warnungen OK, Blockade nur bei Branch-Protection)
       - Alle 11 bestehenden Unit/Integration/E2E-Tests müssen weiterhin durchlaufen
     - Führe manuell einen Push durch, um pre-push zu testen:
       - Pre-push muss durchlaufen (alle Strict-Checks OK, Tests OK)
     - **KRITISCH:** Falls irgendein Hook oder Check auf aktuellem Code-Stand fehlschlägt:
       - Analysiere Fehlerursache
       - Option A: Behebe Verstoß im Code (z.B. fehlende XML-Doku, Formatierung)
       - Option B: Passe Hook-Regel an (z.B. `translation-check.py` zu streng), wenn sachlich begründet
       - Dokumentiere Entscheidung in Commit-Nachricht
     - Nach Anpassungen: Re-Test durchführen
     - **Erfolgs-Kriterium:** Hooks durchlaufen ohne Fehler-Exits, alle 11 Tests bestanden

9. **Dokumentation erstellen: `docs/help/git-hooks/README.md`**
   - Voraussetzungen: `.githooks/`-Verzeichnis vollständig, Hooks getestet
   - Beschreibung:
     - Übersicht: Was sind Git-Hooks und warum sind sie wichtig?
     - Links zu Installationsanleitung und detaillierten Check-Beschreibungen
     - Schnelleinstieg: Installation und Validierung (2-3 Zeilen)
     - FAQ: Häufige Fragen und Probleme
     - Fallback-Mechnik dokumentieren: `SKIP_HOOKS=1` für Notfall

10. **Dokumentation erstellen: `docs/help/git-hooks/installation.md`**
    - Voraussetzungen: Installationsskripte fertig
    - Beschreibung:
      - Schritt-für-Schritt für Windows und Unix
      - Systemanforderungen aufzählen: Git 2.9+, Python 3.x, .NET SDK 10.0+
      - Validierungs-Kommando: Manueller Test eines Commits
      - Troubleshooting-Tipps (z.B. „Python nicht gefunden", „Hook-Fehler beim ersten Commit")

11. **Dokumentation erstellen: `docs/help/git-hooks/checks.md`**
    - Voraussetzungen: Alle Checks implementiert
    - Beschreibung:
      - Tabelle aller Checks mit:
        - Name des Checks
        - Wann läuft er (pre-commit oder pre-push)
        - Modus (Warnung oder blockierend)
        - Was prüft er
        - Wie behebe ich Verstöße?
      - Beispiel-Fehlerausgaben und deren Lösungen
      - Tiefergehende Details für jedes Skript

12. **README.md Update**
    - Voraussetzungen: `docs/help/git-hooks/` fertig
    - Beschreibung:
      - Neuer Abschnitt „Git-Hooks zur Qualitätssicherung"
      - Text: „Nach dem Klonen des Repositories müssen die Git-Hooks installiert werden. Rufe dazu aus dem Repository-Verzeichnis aus: `./install-hooks.cmd` (Windows) oder `./install-hooks.sh` (Unix/macOS) auf. Details: siehe `docs/help/git-hooks/`"
      - Link zur Installationsdokumentation

13. **.gitignore Update (falls nötig)**
    - Voraussetzungen: `.gitignore` vorhanden
    - Beschreibung:
      - Prüfe, ob folgende Einträge vorhanden sind; ergänze falls nötig:
        - `review-versions/`
        - `design-draft/`
        - `.claude/`
        - `*.env` und `*.env.*` (für Secrets)
        - `*.log` (große Logdateien)
        - `*.db` / `*.sqlite*` (DB-Dumps)
      - Falls `.gitignore` diese noch nicht enthält: Add und Commit (ist Teil der Schritt-2-Anforderung)

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse / Datei | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `test_forbidden_patterns_detects_api_key` | `.githooks/test_forbidden_patterns_check.py` (Einheit-Test) | Dass Regex-Pattern für API-Schlüssel (`FUEL_API_KEY=...`) erkannt werden |
| `test_forbidden_patterns_detects_certificate` | `.githooks/test_forbidden_patterns_check.py` | Dass Dateiendungen .pfx, .p12 erkannt werden |
| `test_conventional_commits_valid_format` | `.githooks/test_conventional_commits_check.py` | Dass valide Nachricht `feat(core): Beschreibung` akzeptiert wird |
| `test_conventional_commits_invalid_type` | `.githooks/test_conventional_commits_check.py` | Dass ungültiger Type `foo(core): ...` abgelehnt wird |
| `test_conventional_commits_too_short` | `.githooks/test_conventional_commits_check.py` | Dass Nachricht unter 10 Zeichen abgelehnt wird |
| `test_format_code_style_check_calls_dotnet_format` | `.githooks/test_format_code_style_check.py` | Dass `dotnet format --verify-no-changes` aufgerufen wird |
| `test_test_execution_check_runs_dotnet_test` | `.githooks/test_test_execution_check.py` | Dass `dotnet test Tankradar.sln` aufgerufen wird |
| `test_test_execution_check_timeout` | `.githooks/test_test_execution_check.py` | Dass Timeout nach 5 Min erreicht wird und sauberer Exit erfolgt |
| **Manuelle Hook-Tests (nicht automatisiert, sondern als Schritte dokumentiert):** | — | — |
| `test_pre_commit_blocks_main` | Manuelle Test-Anleitung in `docs/help/git-hooks/checks.md` | Dass `git commit` auf `main` blockiert wird |
| `test_pre_push_blocks_staging` | Manuelle Test-Anleitung | Dass `git push` auf `staging` blockiert wird |
| `test_pre_commit_allows_feature_branch` | Manuelle Test-Anleitung | Dass `git commit` auf Feature-Branch erlaubt wird |
| `test_pre_push_runs_tests` | Manuelle Test-Anleitung | Dass Tests vor Push ausgeführt und blockiert werden bei Fehler |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| Alle 11 Tests in `Tankradar.Tests.*` | Keine Anpassung erforderlich, aber sie müssen nach Hook-Installation weiterhin grün durchlaufen (Validierungs-Kriterium in Schritt 8) |

### E2E-Tests (primärer Funktionsnachweis)

Da die Git-Hooks keine Benutzer-UI beeinflussen, sondern ausschließlich den Entwickler-Workflow regeln (Commit/Push-Schritte), sind keine E2E-Tests via FlaUI erforderlich. Die Hooks werden durch manuelle Commits und Push-Operationen validiert (siehe oben unter „Manuelle Hook-Tests").

**Begründung:** Git-Hook-Funktionalität ist durch Black-Box-Tests (Commit durchführen, Hook ausführen oder blockieren) nachgewiesen. Keine App-UI-Interaktion notwendig.

## Offene Punkte

Keine. Alle in Requirement und Inventory aufgelisteten Punkte werden durch den Plan beantwortet:

- **Test-Execution-Validierung:** `test-execution-check.py` führt `dotnet test` aus, blockiert bei Exit ≠ 0, Timeout 5 Min mit Fallback via `HOOK_SKIP_TESTS=1`
- **API-Schlüssel und Secrets:** `forbidden-patterns-check.py` mit Regex-Mustern für Tankerkönig, OpenRouteService, Standard-Secrets, Zertifikate, DB-Dumps, Logdateien
- **Conventional Commits Format:** Types (feat, fix, docs, test, refactor, chore, perf), optionale Scopes, blockierend in pre-push
- **Python venv:** System-Python 3.x, kein venv erforderlich (Standard-Library ausreichend)
- **Installationsmechanik:** `core.hooksPath .githooks`, elegant, keine Symlinks/Kopien nötig (Git 2.9+)
- **Hooks-Deaktivierung:** Umgebungsvariable `SKIP_HOOKS=1` mit stderr-Warnung, oder Git-Standard `--no-verify`
- **Code-Formatierung:** `dotnet format --verify-no-changes` in pre-commit (Warnung, nicht blockierend)
- **XAML vs. Razor:** `translation-check.py` bleibt (funktioniert mit Ressourcen-Dateien), `razor-l10n-check.py` und `razor-usage-check.py` entfallen, XAML-Komponenten-Prüfung nicht implementiert (zu komplex, nicht explizit gefordert)
- **Testlauf gegen aktuellen Code-Stand:** Explizit in Schritt 8 (Testschritt) geplant; Erfolgs-Kriterium: Hooks grün, alle 11 Tests bestanden
