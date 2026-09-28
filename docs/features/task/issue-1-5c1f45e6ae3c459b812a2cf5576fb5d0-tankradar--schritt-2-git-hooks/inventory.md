# Bestandsaufnahme: Git Hooks für Qualitätssicherung im MAUI-Projekt

Diese Bestandsaufnahme dokumentiert den bestehenden Zustand des Tankradar-Projekts bezogen auf die Anforderung zur Einrichtung lokaler Git-Hooks (Schritt 2: Git-Hooks für Qualitätssicherung).

## Zusammenfassung

### Was ist vorhanden

- **Projektstruktur:** Tankradar MAUI Single-Project-Anwendung (.NET 10.0) mit 4 Test-Suites (Unit, Integration, E2E)
- **Testabdeckung:** 11 Tests bestehen erfolgreich (7 Unit + 3 Integration + 1 E2E), Baseline vor Hook-Implementierung erfasst
- **Template-Hooks:** Versionierte Git-Hook-Vorlagen aus privatem Repository (martin-stromberg/Pattern-Collection) unter `docs/projects/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar/inventory/external-sources/githooks/` vorhanden
- **Bestandteile der Template:**
  - `pre-commit` und `pre-push` Shell-Skripte
  - 6 Python-Check-Skripte: `csproj-xmldoc-check.py`, `no-notimplemented-check.py`, `enum-coverage-check.py`, `translation-check.py`, `razor-l10n-check.py`, `razor-usage-check.py`
  - Installation-Skripte für Windows (`install-hooks.cmd`) und Unix (`install-hooks.sh`)
  - Dokumentation zur Funktionsweise

### Was fehlt noch

- **`.githooks/`-Verzeichnis:** Muss im Repository erstellt und mit aktualisierten/angepassten Hook-Dateien bestückt werden
- **Neue Python-Check-Skripte (Anforderung):**
  - `format-code-style-check.py` — Code-Formatierung (dotnet format)
  - `forbidden-patterns-check.py` — API-Schlüssel, Zertifikate, DB-Dumps, Logdateien
  - `conventional-commits-check.py` — Commit-Message-Format-Validierung
  - `test-execution-check.py` — Validierung lokaler Testausführung vor Push
- **XAML-Äquivalente:** Template-Razor-Checks müssen für XAML adaptiert oder ersetzt werden
- **Hook-Installationsmechanik:** `git config --local core.hooksPath .githooks` muss gesetzt werden
- **Dokumentation:** Setup- und Troubleshooting-Dokumentation unter `docs/help/git-hooks/` erforderlich

### Test-Ausgangszustand

**Baseline erfasst am 2026-09-28 23:16:23 UTC+02:00:**
- Branch: `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-2-git-hooks`
- Commit: 6d2f984 (chore: Schritt 1 abgenommen und gemergt, Schritt 2 gestartet)
- Tests: 11 bestanden, 0 fehlgeschlagen, 0 übersprungen
- SDK: .NET 10.0.401, xUnit.net VSTest Adapter v3.1.4

Detaillierter Bericht: [Tests-Ausgangszustand](inventory/tests.md)

---

## Details

### [Externe Quellen: Template-Hooks und Python-Checks](inventory/external-sources.md)

Dokumentation der vorhandenen Hook-Vorlage aus dem privaten Repository:
- Struktur der `pre-commit` und `pre-push` Shell-Skripte
- Alle 6 Python-Check-Skripte: Zweck, Funktionsweise, Parameter, Exit-Codes
- Installationsmechanismus (`core.hooksPath`)
- Anwendbarkeit auf MAUI/XAML (Razor-Checks entfallen, C#-Checks anwendbar)

### [Projektstruktur: Tankradar MAUI-Projekt](inventory/project-structure.md)

Umfassende Dokumentation der bestehenden Projektstruktur:
- Verzeichnishierarchie (src/, docs/, scripts/, .git/)
- Projektdateien (Tankradar.sln, .csproj-Konfigurationen)
- Test-Projekte und deren Framework (xUnit, FlaUI)
- NuGet-Dependencies und Python-Dependencies
- Testdaten-Isolation über `TEST_DATA_PATH`
- Geplante `.githooks/`-Struktur für Schritt 2
- Besonderheiten für MAUI (Android/iOS-Build, Razor vs. XAML)

### [Tests: Ausgangszustand vor Implementierung](inventory/tests.md)

Nachweis der Test-Baseline für Vergleiche nach Hook-Implementierung:
- Test-Lauf-Details (Zeitpunkt, Branch, Commit, Umgebung)
- Testkommando und Exit-Codes
- Testklassen und -methoden (Unit, Integration, E2E)
- Hilfsmethoden und Test-Infrastruktur
- Ausführliche Logs: [dotnet-test-baseline.log](inventory/test-results/dotnet-test-baseline.log)

---

## Offene Punkte und Unklarheiten

### Aus Requirement übernommen (zu klären vor Implementierung)

1. **Razor vs. XAML:** Template enthält Razor-spezifische Checks. XAML-Äquivalente erforderlich oder Checks entfallen?
2. **Test-Execution-Validierung:** Wie wird Erfolg lokaler Testausführung nachgewiesen?
3. **Forbidden Patterns:** Konkrete Regex/Muster für API-Schlüssel, Zertifikate, DB-Dumps?
4. **Conventional Commits:** Erlaubte Types und Scopes für dieses Projekt?
5. **Python venv:** System-Python oder Virtual Environment?
6. **Hooks-Deaktivierung:** Notfall-Fallback-Mechanik?
7. **Code-Formatierung:** dotnet format Wrapper oder Alternative?

### Aus Analyse ermittelt

- **SecretScan.csproj / MarkdownLinkCheck.csproj:** Template sucht diese Projekte. Sollen sie hinzugefügt werden?
- **CI-Integration:** GitHub Actions geplant?
- **XML-Dokumentation:** Erzwingung in Tankradar.MAUI.csproj?

---

## Zusammenfassung: Nächste Schritte

1. Template-Hooks kopieren und anpassen
2. Neue Python-Checks implementieren (format, forbidden-patterns, conventional-commits, test-execution)
3. .githooks-Verzeichnis versionieren und aktivieren
4. Dokumentation unter docs/help/git-hooks/ erstellen
5. Tests validieren (11 Tests Baseline vor Hook-Installation)

---

## Dateipfade (absolute)

| Bereich | Pfad |
|---------|------|
| **Bestandsaufnahme** | `D:\Repositories\softwareschmiede\5c1f45e6-ae3c-459b-812a-2cf5576fb5d0\docs\features\task\issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-2-git-hooks\inventory\` |
| **Anforderungen** | `D:\Repositories\softwareschmiede\5c1f45e6-ae3c-459b-812a-2cf5576fb5d0\docs\features\task\issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-2-git-hooks\requirement.md` |
| **Template-Hooks** | `D:\Repositories\softwareschmiede\5c1f45e6-ae3c-459b-812a-2cf5576fb5d0\docs\projects\task\issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar\inventory\external-sources\githooks\` |
| **Hauptprojekt** | `D:\Repositories\softwareschmiede\5c1f45e6-ae3c-459b-812a-2cf5576fb5d0\src\Tankradar.MAUI\` |
| **Test-Projekte** | `D:\Repositories\softwareschmiede\5c1f45e6-ae3c-459b-812a-2cf5576fb5d0\src\Tankradar.Tests.*\` |
