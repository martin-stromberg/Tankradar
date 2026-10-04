# Abnahmeprüfung – Entwicklungsschritt 3

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

**Abweichung aus 3.5 behoben:**

- Neues gemeinsames Skript `scripts/check-vulnerabilities.mjs`:
  - Es ruft `dotnet list <Solution> package --vulnerable --include-transitive --no-restore --format json` auf.
  - Es wertet ausschließlich das JSON aus, also die Einträge unter `vulnerabilities` bei direkten und transitiven Paketen.
  - Exit-Codes: 0 = keine Funde, 1 = Funde, 2 = Aufruf- oder Ausgabefehler.
- Die CI-Action `.github/actions/security-scan` und der lokale Prüflauf `scripts/local-ci.ps1` (Schritt „Sicherheitsprüfung (Abhängigkeiten)“, bricht bei Exit-Code ≠ 0 ab) nutzen dasselbe Skript. Der Bericht heißt jetzt `vulnerable-packages.json` und ist gitignoriert.
- Praktisch nachgeprüft mit einem Wegwerfprojekt im Scratchpad außerhalb des Repos, das `Newtonsoft.Json 12.0.1` referenziert. Die SDK-Ausgabe ist deutschsprachig (Restore meldet „NU1903 … Hoch Schweregrad“).
  - Ergebnis: „Anfaellige Pakete gefunden (1): Newtonsoft.Json 12.0.1 … High GHSA-5crp-9r3c-p9vr“, **Exit 1**.
  - Das Projekt wurde danach entfernt.
- Zusatzprobe mit derselben Kopie und einer `nuget.config`, deren einzige Quelle nicht erreichbar ist: `dotnet list` endet mit Exit 1, das Skript meldet das und liefert **Exit 2**. Ein Quellenfehler wird also nicht als „keine Funde“ gewertet.
- Gegen `Tankradar.sln` gibt es keine Funde (Exit 0). Der JSON-Bericht enthält alle vier Projekte.

**Node in allen Jobs mit Sicherheitsprüfung vorhanden:** Die Action `security-scan` wird in drei Jobs genutzt:
- `static checks` in `pr-staging-ci.yml`
- `static checks` in `staging-ci.yml`
- `security-scan` in `security-scan.yml`; dort wurde `actions/setup-node@v7` mit Node 24 neu ergänzt.

In allen drei Jobs gilt:
- `setup-node` (Node 24) läuft vor dem Scan.
- Vor dem Scan laufen `dotnet restore Tankradar.sln -p:Configuration=Release` unter der Job-Umgebung. Der Aufruf mit `--no-restore` findet also einen passenden Restore-Zustand vor.

Andere Jobs rufen das Skript nicht auf.

**Windows-Jobs nachgestellt:** Betroffen sind `static checks` und `build & test` in PR und staging, `prerelease`, `release` und `security-scan`. Alle teilen dieselbe Job-Umgebung: `IncludeAndroidTarget`, `IncludeIosTarget` und `IncludeMacCatalystTarget` sind `false`, `IncludeWindowsTarget` ist ungesetzt. Nachgestellt in einer frischen `pwsh`-Sitzung, alle Schritte mit Exit 0:

| Prüfung | Ergebnis |
|---|---|
| `TargetFrameworks` | `;net10.0-windows10.0.19041.0` |
| `pwsh -NoProfile -File scripts/test-ios-deployment.ps1` | „iOS-Deployment-Pruefung erfolgreich.“ |
| `dotnet restore Tankradar.sln -p:Configuration=Release` | OK |
| `npm test` | OK |
| `dotnet format … --verify-no-changes --no-restore --severity error` | OK |
| Security-Scan wie in der Action (`bash`: `node scripts/check-vulnerabilities.mjs --run Tankradar.sln …`) | „Keine anfaelligen Pakete gefunden.“ |
| `dotnet build -c Release --no-restore -p:TreatWarningsAsErrors=true` | 0 Warnungen, 0 Fehler |
| `dotnet build -c Release --no-restore` | OK |
| Unit- und Integrationstests (`--no-build`) | 15/15 und 3/3 |
| Release-Gate (`dotnet test` Release ohne `--no-build`) | OK |
| `scripts/package-windows.ps1 -Version 0.1.0-rc.1` (Ausgabe in den Scratchpad, danach gelöscht) | `release-win-x64.zip` (ca. 92 MB) und `update.json` |

**`scripts/local-ci.ps1` vollständig inklusive E2E:** Aufgerufen per `&` in je einer `pwsh`-Sitzung, Werte vor und nach dem Lauf ausgegeben.

| Lauf | Werte vorher | Werte nachher | Ergebnis |
|---|---|---|---|
| 1 | alle vier `Include*Target` ungesetzt (`<null>`) | alle vier `<null>` | Exit 0 |
| 2 | Android, iOS und MacCatalyst `'true'`, Windows `'false'` | identisch zu vorher | Exit 0 |

In beiden Läufen waren alle Schritte OK, nur das Paket war mangels `-Package` übersprungen:
- Node-Tests und Workflow-Validierung
- iOS-Deployment-Skript, zweimal
- Restore, Formatprüfung und Sicherheitsprüfung („Keine anfaelligen Pakete gefunden.“)
- Statische Analyse
- Unit 15/15, Integration 3/3
- Abdeckung 73,2 % bei einer Schwelle von 70 %
- FlaUI-E2E 3/3
- iOS-Compile-Prüfung

**Randfälle aus 3.5 behoben:**
- **Vorbelegtes `IncludeWindowsTarget=false`:** Lauf 2 baut trotzdem korrekt, weil die Windows-Simulation `IncludeWindowsTarget=true` setzt.
- **Restore-Zustand nach dem iOS-Schritt:** Nach dem Lauf enthält `obj/project.assets.json` der MAUI-App wieder die Windows-Ziele `net10.0-windows10.0.19041.0` und `…/win-x64`. Der iOS-Schritt stellt den Windows-Restore also wieder her.

**Weitere Läufe:**
- `npm test`: 28/28, davon 4 neue Tests für `check-vulnerabilities`.
- `python scripts/validate-workflows.py`: OK für 7 Workflows und alle lokalen Actions. `actionlint` ist lokal nicht vorhanden.

**Keine Regression festgestellt:** Der Nachbesserungs-Commit `235c5d5` ändert nur diese Dateien:
- Action `security-scan`
- `security-scan.yml`, wo `setup-node` ergänzt wurde
- `.gitignore`
- `local-ci.ps1`
- zwei Hilfeseiten
- das neue Skript samt Tests

Die übrigen Anforderungspunkte sind unverändert erfüllt, wie in 3.2 bis 3.5 bewertet:
- Branch-Modell mit `verify-pr-source`
- Promotion- und Backmerge-Workflows
- Versionierung: Start bei 0.1.0, keine automatische Anhebung auf 1.0, RC-Suffix
- Windows-ZIP ohne Installer
- Secrets und Variablen ausschließlich über GitHub
- iOS unsigniert ohne Fehlschlag, gesteuert über `IOS_SIGNING_ENABLED` und `IOS_*`
- `scripts/iOS-Deployment.ps1` mit allen Aktionen und `TANKRADAR_IOS_*`
- `package-ios` nach Vorlage
- Android-Ziel in den Windows-Jobs ausgeblendet
- Einrichtungs-Checkliste
- Bundle-ID `de.martinstromberg.tankradar`
- `drafts/` nicht versioniert

**Nicht ausgeführt:**
- `dotnet workload restore`, um keine Workloads zu verändern
- alle macOS- und GitHub-gebundenen Teile: signierter iOS-Build, TestFlight, `gh release`, Upload von Artefakten

**Kleinere Beobachtungen, nicht abnahmerelevant:**
- **Node-Zweck in der Doku unvollständig:** In `docs/help/ci-cd/lokaler-pruefung.md` (Voraussetzungen) steht als Zweck von Node.js nur „Skripttests, Abdeckungsprüfung“. Die Sicherheitsprüfung benötigt Node jetzt ebenfalls.
- **Fehlender Bericht bei Abbruch:** Bei einem Abbruch mit Exit 2 schreibt das Skript keine `vulnerable-packages.json`. Der Schritt „Upload report“ der Action findet dann keine Datei. `upload-artifact` warnt in diesem Fall nur, der Job schlägt bereits im Scan-Schritt korrekt fehl.
