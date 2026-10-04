# Abnahmeprüfung – Entwicklungsschritt 2

## Ergebnis

**Status:** Abweichungen gefunden

Geprüft wurden der Diff `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar...HEAD` (26 Dateien) sowie
`.githooks/`, `src/`, `docs/help/git-hooks/`, `docs/help/index.md` und `README.md`. Die Prüfung erfolgte am Code
und durch praktische Aufrufe ohne Seiteneffekte:

- `sh .githooks/pre-commit` (nichts gestaged): Exit 0, aber mit Formatierungs-Warnung (siehe unten).
- `sh .githooks/pre-push` mit simulierten Refs:
  - Push auf `refs/heads/main` und `refs/heads/staging`: Exit 1 (blockiert, korrekt).
  - Push auf einen Feature-Ref: Exit 0. Stub-, Enum-, Forbidden-Patterns-, Commit-Format- und Testprüfung
    liefen durch, `dotnet test Tankradar.sln` ergab 11/11 grün (Unit 7, Integration 3, E2E 1).
- Einzelskripte:
  - `no-notimplemented-check.py --all --strict`, `enum-coverage-check.py --all --strict`,
    `forbidden-patterns-check.py --all --strict` und `conventional-commits-check.py` liefen ohne Befund
    (11 Commits seit `main` gültig).
  - `translation-check.py --all` meldet „No .resx files found“.
  - **`csproj-xmldoc-check.py --all` endet mit Exit 1.**
  - **`format-code-style-check.py --strict` endet mit Exit 1.**
- Python-Hook-Tests (`python -m unittest discover -p "test_*.py"` in `.githooks/`): 31 Tests, OK.
- Commit-Nachrichten gegen `validate_message` geprüft: Die Projekt-Typen `plan:`, `feat:`, `chore:`,
  `merge:`, `fix:`, `docs:` und `test:` (mit/ohne Scope) werden akzeptiert. Der bisherige Merge-Commit
  `44725a2 merge: Entwicklungsschritt 1 …` (zwei Eltern, `--no-ff`) besteht die Prüfung.
- `forbidden-patterns-check.check_file` mit Testdateien im Scratchpad geprüft:
  - Erkannt werden `.pfx`, `.db`, `.log` > 1 MB, PEM-Inhalte, `<SigningKey>`-Werte sowie `*_API_KEY=…` und
    `ApiKey = "…"` in `.cs`/`.env`/`.py`.
  - Nicht erkannt werden die in Abweichung 4 genannten Fälle.

Erfüllt sind:

- Versionierte Hooks unter `.githooks/` mit Installationsskripten für Windows (`.cmd`) und Unix (`.sh`),
  die jeweils `core.hooksPath` setzen.
- Aus der Vorlage übernommen: XML-Doku-Check, Stub-Check, Enum-Coverage, Übersetzungs-/Ressourcen-Check
  sowie der Branch-Schutz für `main`/`staging` bei Commit und Push. Die Razor-Checks sind entfallen, was
  zulässig ist.
- Neu hinzugekommen: Validierung der Commit-Nachrichten im Conventional-Commits-Format (blockierend vor dem
  Push) und Testausführung vor dem Push (blockierend, Timeout 5 Min.).
- Die Dokumentation liegt unter `docs/help/git-hooks/` (README, installation, checks, index). README und
  `docs/help/index.md` verweisen darauf.

## Abweichungen

- [ ] **Auf dem aktuellen Stand laufen die Hooks nicht ohne Beanstandung durch (Formatierung).**
  - **Anforderung:** „Auf dem aktuellen Stand des Repositorys müssen die Hooks ohne Beanstandung
    durchlaufen.“
  - **Ist-Stand:** `dotnet format Tankradar.sln --verify-no-changes`, aufgerufen über
    `format-code-style-check.py`, meldet 38 `WHITESPACE`-Fehler in 6 Dateien:
    - `src/Tankradar.MAUI/Platforms/Android/MainApplication.cs`
    - `Platforms/MacCatalyst/AppDelegate.cs`
    - `Platforms/MacCatalyst/Program.cs`
    - `Platforms/Windows/App.xaml.cs`
    - `Platforms/iOS/AppDelegate.cs`
    - `Platforms/iOS/Program.cs`
  - **Folge:** Der `pre-commit`-Hook gibt deshalb bei jedem Commit die Warnung „meldet
    Formatierungsabweichungen in Tankradar.sln“ aus. Die Beanstandung wird nur deshalb nicht zum Abbruch,
    weil der Check nie blockiert (siehe nächster Punkt).

- [ ] **Formatierung und Code-Stil werden nicht sichergestellt, nur gemeldet.**
  - **Anforderung:** Die Hooks sollen „vor Commit und Push sicherstellen, dass der Code konsistent,
    formatiert und fehlerfrei ist“. Die Prüfung von Formatierung und Code-Stil wird ausdrücklich verlangt.
  - **Ist-Stand:** `format-code-style-check.py` läuft nur in `pre-commit`, und zwar immer mit Exit 0
    (Warn-Modus). `pre-push` ruft den Check gar nicht auf. Unformatierter Code kann damit ungehindert
    committet und gepusht werden, wie der aktuelle Stand zeigt. Die `--strict`-Option des Skripts wird von
    keinem Hook genutzt.

- [ ] **Die Prüfungen der Vorlage für XML-Dokumentation und Übersetzungen wurden abgeschwächt, und der
  aktuelle Stand besteht den XML-Doku-Check nicht.**
  - **Vorlage** (`external-sources/githooks/pre-commit`, `set -eu`): `translation-check.py` und
    `csproj-xmldoc-check.py` blockieren den Commit bei Verstößen.
  - **Ist-Stand:** Beide laufen in `pre-commit` nur als Warnung (`warn_check`). In `pre-push` sind sie nicht
    enthalten. Sie blockieren also an keiner Stelle.
  - **Befund auf dem aktuellen Stand:** `csproj-xmldoc-check.py --all` meldet für
    `src/Tankradar.MAUI/Tankradar.MAUI.csproj` den Fehler „CS1591 ist nicht als Fehler konfiguriert“ (Exit 1).
    Laut Kommentar im csproj ist CS1591 dort bewusst nicht als Fehler gesetzt. Die Hook-Regel wurde aber
    weder angepasst noch eine begründete Ausnahme im Check hinterlegt.
  - **Warum es unbemerkt bleibt:** Der Befund ist nur verdeckt, weil der Check in `pre-commit` ausschließlich
    gestagte Dateien und nur im Warn-Modus prüft.
  - **Folge:** Die Anforderungen „Übernommen werden die Prüfungen der Vorlage“ und „ohne Beanstandung
    durchlaufen“ sind hier nicht erfüllt.

- [ ] **Die Prüfung auf verbotene Muster und Dateien ist lückenhaft gegenüber den genannten Kategorien.**
  - **Anforderung:** Geprüft werden sollen insbesondere API-Schlüssel (Kraftstoffpreis- und Routing-Dienst),
    Zertifikate und Signierungsdaten, Datenbank-Dumps und Logdateien. Die Originalanforderung verlangt unter
    „Git-Sicherheit“: „Keine sensiblen Dateien im Repository: … Logdateien“.
  - Die praktische Prüfung von `forbidden-patterns-check.py` zeigt folgende Lücken:
    - **API-Schlüssel in JSON-Notation** (z. B. `"TankerkoenigApiKey": "0000…"` in einer `appsettings.json`)
      werden nicht erkannt. Die Regexe verlangen `=`, JSON verwendet `:`. Gerade eine JSON-Konfiguration ist
      laut Vorgehensentscheidung der vorgesehene lokale Ablageort der Schlüssel.
    - **iOS-Signierungsdaten** wie `.mobileprovision` (Provisioning Profiles) und `.p8` (App Store Connect
      API-Key) werden nicht erkannt. Die Signierung über den Apple Developer Account ist fester Bestandteil
      des Projekts.
    - **Datenbank-Dumps im SQL-Format** (`.sql`, z. B. `dump.sql`) werden nicht erkannt. Erfasst werden nur
      `.db`, `.sqlite` und `.sqlite3`.
    - **Logdateien** werden nur ab 1 MB beanstandet, kleinere `.log`-Dateien passieren. Die Anforderung
      schließt Logdateien ohne Größengrenze aus.

## Hinweise

- **Automatisierte Commits:** Die Commits dieses Projekts werden nicht blockiert. `plan`, `feat`, `chore`,
  `merge`, `fix`, `docs` und `test` sind erlaubt. Bei den Mindestlängen und der Groß-/Kleinschreibung wurde
  bewusst auf deutsche Betreffzeilen Rücksicht genommen. `pre-commit` blockiert nur Commits auf
  `main`/`staging`.
- **Merge-Commits:** `--no-ff`-Merges mit der Projektkonvention `merge: …` bestehen die Prüfung. Die
  Git-Standardnachricht „Merge branch '…' into …“ wird dagegen in `pre-push` als Formatverstoß abgelehnt.
  Solange die Merges des Workflows mit `-m "merge: …"` erfolgen, ist das unkritisch. Es sollte aber
  dokumentiert sein.
- **Conventional-Commits-Syntax:** Die Breaking-Change-Markierung `feat!:` sowie die Typen `ci`, `build`,
  `style` und `revert` werden abgelehnt. Für die automatische Versionierung (Schritt 3) und für CI-Commits
  kann das relevant werden.
- **Geprüfter Commit-Bereich:** `conventional-commits-check.py` prüft `main..HEAD` und nicht den tatsächlich
  gepushten Ref-Bereich aus stdin. Beim Push eines anderen Refs als `HEAD` wird daher der falsche Bereich
  geprüft.
- **Dateimodus der Hooks:** Alle Hook-Dateien liegen mit Modus `100644` im Index, also nicht ausführbar. Unter
  Unix scheitert der dokumentierte Aufruf `./.githooks/install-hooks.sh` daher an fehlenden Rechten, ohne
  `sh`-Präfix oder vorheriges `chmod`. Erst das Installationsskript setzt die Ausführungsrechte.
- **Code-Stil-Regeln:** Es gibt keine `.editorconfig`. `dotnet format` prüft daher nur gegen die
  Standardregeln. Projektspezifische Code-Stil-Regeln sind nicht definiert.
- **Übersetzungsprüfung:** `translation-check.py` findet derzeit keine `.resx`-Dateien und prüft damit
  faktisch nichts. Hartkodierte Texte in XAML werden nicht geprüft; ein Ersatz für `razor-l10n-check.py`
  fehlt. Laut Anforderung ist das zulässig („entfallen oder … ersetzt“). Die Vorgehensentscheidung
  „Oberflächentexte werden zentral gepflegt, damit die Übersetzungsprüfung der Git-Hooks greift“ wird
  dadurch aktuell aber nicht wirksam.
- **Testlaufzeit:** `pre-push` führt auch die FlaUI-E2E-Tests aus. Dabei startet die App. Die Laufzeit liegt
  derzeit bei rund 6 s, deutlich unter dem Timeout.
