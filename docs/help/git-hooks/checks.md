← [Zurück zur Übersicht](README.md)

# Git-Hooks — Alle Prüfungen im Detail

## Übersicht

| Check | Läuft in | Modus | Prüft | Skript |
|---|---|---|---|---|
| Branch-Schutz | pre-commit, pre-push | blockierend | Keine direkten Commits/Pushes auf `main`/`staging` | im Hook-Skript selbst |
| Übersetzungs-Konsistenz | pre-commit, pre-push (`--all`) | blockierend | Lokalisierungsschlüssel über alle `.resx`-Dateien konsistent | `translation-check.py` |
| XML-Dokumentation | pre-commit, pre-push (`--all`) | blockierend | `<summary>`, `<param>`, `<returns>`, `<typeparam>`, `<response>` an öffentlichen/internen Membern | `csproj-xmldoc-check.py` |
| Platzhalter-Implementierungen | pre-commit (Warnung), pre-push (blockierend, `--all --strict`) | siehe links | Keine `NotImplementedException`/reine throw-Stubs | `no-notimplemented-check.py` |
| Enum-Testabdeckung | pre-commit (Warnung), pre-push (blockierend, `--all --strict`) | siehe links | Alle öffentlichen/internen Enum-Werte in Tests verwendet | `enum-coverage-check.py` |
| Code-Formatierung | pre-commit, pre-push (`--strict`) | blockierend | `dotnet format --verify-no-changes` | `format-code-style-check.py` |
| Verbotene Muster / Secrets | pre-commit, pre-push (`--all --strict`) | blockierend | API-Schlüssel, Zertifikate, Signierungsdaten, DB-Dumps, Logdateien | `forbidden-patterns-check.py` |
| Commit-Nachrichten-Format | pre-push | blockierend | Conventional-Commits-Format für gepushte Commits | `conventional-commits-check.py` |
| Testausführung | pre-push | blockierend | `dotnet test Tankradar.sln` (Timeout 5 Min.) | `test-execution-check.py` |

## Branch-Schutz

Direkte Commits und Pushes auf `main` und `staging` werden blockiert (Exit-Code 1), unabhängig
von allen anderen Prüfungen. Änderungen an diesen Branches erfolgen ausschließlich über Feature-
Branches und Pull Requests.

Beispielausgabe:

```
FEHLER: Direkte Commits auf 'main' sind nicht erlaubt. Bitte einen Feature-Branch verwenden und per Pull Request mergen.
```

**Behebung:** Feature-Branch anlegen (`git checkout -b feature/...`) und dort committen.

## translation-check.py

Prüft, ob lokalisierbare Ressourcenschlüssel über alle Sprachvarianten eines `.resx`-Pakets
konsistent sind, sowie die ResX-Header-Struktur und deutsche Übersetzungen auf typische
Umlaut-Transliterationen (`ae`/`oe`/`ue` statt `ä`/`ö`/`ü`).
- **pre-commit:** nur gestaffelte Dateien, blockierend.
- **pre-push:** `--all`, gesamtes Repository, blockierend.

Aktuell enthält Tankatlas keine `.resx`-Dateien, daher meldet der Check "No .resx files found; nothing to check."

**Behebung:** Fehlende Schlüssel in der jeweiligen Sprachdatei ergänzen; ResX-Header nicht von
Hand verändern (durch den Ressourcen-Editor von Visual Studio erzeugen lassen).

## csproj-xmldoc-check.py

Prüft für gestaffelte `.cs`-Dateien (in `pre-commit`) bzw. das gesamte Repository (in `pre-push`),
ob dokumentierte Member (die bereits ein `<summary>`-Tag haben) auch vollständige `<param>`-,
`<typeparam>`- und `<returns>`-Tags besitzen, sowie für die zugehörigen `.csproj`-Dateien, ob
`GenerateDocumentationFile` und `CS1591` als Fehler konfiguriert sind. In `Tankradar.MAUI` ist
`CS1591` als Fehler konfiguriert; `Resources/DesignSystem.xaml` ist mit `x:ClassModifier="Internal"`
markiert, damit der XAML-SourceGenerator keinen undokumentierten öffentlichen Typ erzeugt.

Beispielausgabe:

```
ERROR: unvollständige XML-Dokumentation in src/Tankradar.MAUI/Services/AppConfiguration.cs:
  Zeile 12 (GetBundleId): fehlendes <returns>-Tag
```

**Behebung:** Fehlende Tags in der `///`-Dokumentation ergänzen.

## no-notimplemented-check.py

Verbietet `NotImplementedException` sowie Methoden/Konstruktoren/Accessoren, deren gesamter
Body nur aus einem `throw`-Statement besteht (Platzhalter-Stubs).

- **pre-commit:** nur gestaffelte Dateien, Warnung, blockiert nicht.
- **pre-push:** `--all --strict`, gesamtes Repository, blockiert.

Beispielausgabe (strict):

```
ERROR: verbotene Platzhalter-Implementierung in src/Tankradar.MAUI/Services/FooService.cs:
  Zeile 20: NotImplementedException verwendet — throw new NotImplementedException();
```

**Behebung:** Member vollständig implementieren oder noch nicht anlegen.

## enum-coverage-check.py

Prüft, ob alle Werte jedes öffentlichen/internen Enums in mindestens einer Testdatei
vorkommen.

- **pre-commit:** Warnung, blockiert nicht.
- **pre-push:** `--all --strict`, blockiert.

Beispielausgabe (strict):

```
ERROR: unvollständige Enum-Testabdeckung:
  FuelType: Enum-Werte nicht in Tests abgedeckt: Diesel, E10 (src/Tankradar.MAUI/Models/FuelType.cs)
```

**Behebung:** Fehlende Enum-Werte in einer Testdatei referenzieren (z. B. in einem
`[Theory]`/`[InlineData]`-Test).

## format-code-style-check.py

Ruft `dotnet format Tankradar.sln --verify-no-changes` auf und blockiert bei Abweichungen.
- **pre-commit:** prüft immer die gesamte Solution (nicht nur gestagte Dateien), blockierend (`--strict`)
- **pre-push:** prüft ebenfalls die gesamte Solution, blockierend (`--strict`)

Die verbindliche Stilgrundlage ist `.editorconfig` im Repository-Root.

Beispielausgabe:

```
ERROR: "dotnet format --verify-no-changes" meldet Formatierungsabweichungen in Tankradar.sln:
  .../MainApplication.cs(12,1): error WHITESPACE: Korrigieren Sie die Leerraumformatierung. ...
```

**Behebung:** Lokal `dotnet format Tankradar.sln` ausführen und die Änderungen committen.

## forbidden-patterns-check.py

Sucht nach versehentlich committeten Secrets und problematischen Dateien:

- **API-Schlüssel:** z. B. `FUEL_API_KEY=`, `ROUTING_KEY=`, `MAPBOX_TOKEN=`,
  `NOMINATIM_TOKEN=` sowie generische Muster wie `api_key=`, `routing_secret=`
  (case-insensitive). Zusätzlich JSON-Notation (z. B. `"TankerkoenigApiKey": "..."`) mit
  typischen Geheimnis-Feldnamen. Zuweisungen an Umgebungsvariablen-Referenzen (`$env:...`,
  `Environment.GetEnvironmentVariable(...)`, `os.environ`, …) gelten nicht als Fund, da sie
  keine Klartext-Secrets enthalten.
- **Zertifikate/Signierungsdaten:** Dateiendungen `.pfx`, `.p12`, `.keystore`, `.jks`, `.pem`,
  `.cer`; PEM-Klartextinhalte (`-----BEGIN CERTIFICATE-----` u. ä.); konkrete (nicht
  platzhalterartige) Werte in `<SigningKey>`/`<CertificateThumbprint>` in `.csproj`-Dateien.
- **iOS-Signierungsdaten:** Dateiendungen `.mobileprovision`, `.p8`.
- **Datenbank-Dumps:** Dateiendungen `.db`, `.sqlite`, `.sqlite3`, `.sql`.
- **Logdateien:** Alle `.log`-Dateien ohne Größenlimit (Ausnahme: `changes.log` im Repo-Root).

Es werden nur versionierbare (nicht von `.gitignore` ausgeschlossene) Dateien geprüft.

- **pre-commit:** nur gestaffelte Dateien, blockierend (`--strict`).
- **pre-push:** `--all --strict`, gesamtes Repository, blockiert.

Beispielausgabe (strict):

```
ERROR: verbotene Muster gefunden (Secrets, Zertifikate, DB-Dumps, iOS-Signierungsdateien, Logdateien):
  src/Tankradar.MAUI/appsettings.local.json:3: möglicher API-Schlüssel gefunden (FUEL_API_KEY=abc123...)
  secrets/prod.pfx: verbotene Zertifikats-/Schlüsseldatei (.pfx)
```

**Behebung:** Datei/Zeile aus dem Commit entfernen, Secret über eine nicht versionierte
Konfigurationsquelle (z. B. `*.env`, Umgebungsvariable, User Secrets) bereitstellen und die
Datei ggf. in `.gitignore` aufnehmen. Bereits gepushte Secrets zusätzlich beim jeweiligen
Anbieter rotieren.

## conventional-commits-check.py

Läuft ausschließlich im `pre-push`-Hook (nicht beim Commit) und validiert alle
Commit-Nachrichten der tatsächlich gepushten Commits gegen das Format:

```
type(scope): subject
```

`(scope)` ist optional. Erlaubte `type`-Werte: `feat`, `fix`, `docs`, `test`, `refactor`,
`chore`, `perf`, `plan` (Planungscommit), `merge` (Merge-Commit), `ci`, `build`, `style`,
`revert`. `subject` muss mindestens 10 Zeichen lang sein.

**Merge-Commits** müssen ebenfalls diesem Format folgen, also mit `merge: …` beginnen (z. B.
`git merge --no-ff -m "merge: Entwicklungsschritt 3 - CI/CD-Pipeline"`). Die Git-Standardnachricht
„Merge branch …“ wird abgelehnt.

**Breaking Changes:** Werden durch `!` vor dem Doppelpunkt (z. B. `feat!: ...` oder
`feat(scope)!: ...`) oder durch einen `BREAKING CHANGE:`-Footer im Commit-Body markiert.
Wird `!` verwendet, sollte ein `BREAKING CHANGE:`-Footer folgen (fehlender Footer gibt nur
einen Hinweis, blockiert nicht).

Beispielausgabe:

```
ERROR: Commit a1b2c3d4 "foo: zu kurz":
  -> unbekannter Type 'foo' (erlaubt: build, chore, ci, docs, feat, fix, merge, perf, plan, refactor, revert, style, test)
  -> Nachricht zu kurz (8 Zeichen, mindestens 10 erforderlich)
```

**Behebung:** Commit-Nachricht mit `git commit --amend` (letzter Commit) oder interaktivem
Rebase (`git rebase -i`) korrigieren.

## test-execution-check.py

Führt `dotnet test <Solution> --nologo --verbosity quiet` aus und blockiert den Push, wenn
Tests fehlschlagen oder ein Zeitlimit überschritten wird.

- **Timeout:** 5 Minuten (Standard), überschreibbar über die Umgebungsvariable
  `TEST_TIMEOUT_SECONDS=<Sekunden>`.
- **Notfall-Fallback:** `HOOK_SKIP_TESTS=1 git push ...` überspringt die Testausführung (mit
  Warnung auf stderr).

Beispielausgabe (Fehlschlag):

```
FEHLER: Tests fehlgeschlagen (Exit-Code 1, nach 6s).
```

Beispielausgabe (Timeout):

```
FEHLER: Testlauf nach 300s abgebrochen (Timeout: 300s).
Hinweis: HOOK_SKIP_TESTS=1 kann als Notfall-Fallback gesetzt werden, um die Testausführung zu überspringen.
```

**Behebung:** Fehlschlagende Tests lokal mit `dotnet test Tankradar.sln` reproduzieren und
beheben; bei echtem Timeout-Problem die Ursache klären (z. B. hängender E2E-Test) statt
dauerhaft `HOOK_SKIP_TESTS=1` zu verwenden.

## Manuelle Integrationstests

Die folgenden Abläufe lassen sich jederzeit manuell nachvollziehen, um die Hook-Installation zu
validieren (siehe auch [`installation.md`](installation.md#validierung)):

| Test | Ablauf | Erwartetes Ergebnis |
|---|---|---|
| Commit auf Feature-Branch | `git commit --allow-empty -m "test: Hooks prüfen"` auf einem Feature-Branch | Commit gelingt, Warnungen ggf. sichtbar |
| Commit auf `main` blockiert | `git checkout main && git commit --allow-empty -m "test: sollte blockiert werden"` | Commit wird mit Exit-Code 1 abgelehnt |
| Push auf `staging` blockiert | `git push origin HEAD:staging` von einem Feature-Branch aus | Push wird mit Exit-Code 1 abgelehnt |
| Push führt Tests aus | `git push` auf einem Feature-Branch | Testlauf wird angestoßen; bei Fehlschlag wird der Push blockiert |

Direkter Aufruf der Hook-Skripte ohne echten Commit/Push (z. B. zur Fehlersuche):

```bash
bash .githooks/pre-commit
echo "refs/heads/<branch> <sha> refs/heads/<branch> 0000000000000000000000000000000000000000" | bash .githooks/pre-push origin <remote-url>
```
