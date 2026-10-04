# Abnahmeprüfung – Entwicklungsschritt 3

## Ergebnis

**Status:** Abweichungen gefunden

## Abweichungen

- [ ] **Die lokale Sicherheitsprüfung erkennt auf dem Entwicklungsrechner keine anfälligen Pakete.** Die Anforderung verlangt einen gleichwertigen lokalen Prüflauf, „der dieselben Prüfungen auf dem Entwicklungsrechner ausführt“. Dazu gehört ausdrücklich die Prüfung der Abhängigkeiten auf bekannte Sicherheitslücken.

  So entscheidet `scripts/local-ci.ps1` (Z. 140–143) über einen Fund:
  - Das Skript sucht in der Ausgabe von `dotnet list … package --vulnerable` nur nach den englischen Texten `has the following vulnerable packages|Severity`.
  - `dotnet list` endet auch bei Funden mit Exit-Code 0. Die Textsuche ist also das einzige Kriterium.

  Das .NET SDK auf dem Entwicklungsrechner gibt seine Meldungen aber auf Deutsch aus. Praktisch nachgeprüft mit einem Wegwerfprojekt im Scratchpad, das `Newtonsoft.Json 12.0.1` referenziert (GHSA-5crp-9r3c-p9vr, High):
  - Die Ausgabe lautet „Für das Projekt "vuln" liegen die folgenden anfälligen Pakete vor.“, die Spalte heißt „Schweregrad“.
  - Der Ausdruck aus `local-ci.ps1` liefert darauf `False`.

  Der lokale Schritt „Sicherheitsprüfung (Abhängigkeiten)“ meldet unter einer deutschsprachigen SDK-Ausgabe also immer „OK“, auch wenn anfällige Pakete vorhanden sind. Die Pipeline erkennt solche Funde dagegen, denn die Runner geben Englisch aus und die Action `security-scan` greift.

  Behebung, eine von zwei Möglichkeiten:
  - Die Sprache für den Aufruf festlegen, z. B. `DOTNET_CLI_UI_LANGUAGE=en`.
  - Die Ausgabe sprachunabhängig auswerten, z. B. mit `--format json` und Prüfung auf `vulnerabilities`.

  Diese Abweichung besteht seit der ersten Umsetzung. Die früheren Prüfrunden haben sie nicht erkannt, weil dort jeweils keine anfälligen Pakete vorhanden waren.

## Hinweise

**Abweichung aus 3.4 behoben, keine Regression festgestellt:**

- **Lokale iOS-Compile-Prüfung:** `local-ci.ps1` hat jetzt einen eigenen Schritt „iOS-Compile-Prüfung (net10.0-ios)“. Er läuft mit `IncludeIosTarget=true`, `IncludeAndroidTarget=false` und `IncludeMacCatalystTarget=false` sowie mit `-p:RuntimeIdentifier=iossimulator-arm64 -p:TreatWarningsAsErrors=true -p:MauiXamlInflator=XamlC`. Ist die iOS-Workload nicht installiert, wird der Schritt übersprungen.
- **Variablen werden wiederhergestellt:** Die `Include*Target`-Variablen werden in einem `try/finally` gesichert und zurückgesetzt. `Set-ProcessEnv` entfernt dabei vorher ungesetzte Variablen wirklich, statt sie als leere Zeichenfolge zu hinterlassen.
- **Doku und Kommentare aktualisiert:**
  - `docs/help/ci-cd/lokaler-pruefung.md`: Tabelle und Fließtext
  - `docs/help/ci-cd/README.md`: „nur Windows-Ziel“
  - `docs/help/ci-cd/workflows.md`
  - Kommentar im Schritt `Static analysis` in `pr-staging-ci.yml`
  - Der Skriptkopf von `local-ci.ps1` beschreibt das neue Verhalten.

**Praxisprüfung `scripts/local-ci.ps1` (vollständig, inklusive E2E), aufgerufen per Dot-Source in derselben `pwsh`-Sitzung:**

| Lauf | Werte vorher | Werte nachher | Ergebnis |
|---|---|---|---|
| 1 | alle vier `Include*Target` ungesetzt | alle vier ungesetzt (`<null>`) | Exit 0; alle Schritte OK |
| 2 | alle vier `Include*Target` auf `'true'` | alle vier `'true'` | Exit 0; alle Schritte OK |

In beiden Läufen waren alle Schritte OK, nur das Paket war mangels `-Package` übersprungen:
- Unit 15/15, Integration 3/3
- Abdeckung 73,2 % bei einer Schwelle von 70 %
- FlaUI-E2E 3/3
- iOS-Compile-Prüfung OK

Die Werte von `IncludeAndroidTarget`, `IncludeIosTarget`, `IncludeMacCatalystTarget` und `IncludeWindowsTarget` sind nach dem Lauf jeweils identisch zu vorher.

**iOS-Compile-Prüfung mit `TreatWarningsAsErrors=true` ist stabil und maskiert nichts:**
- **Sauberer Build:** In einer Kopie ohne `bin`/`obj` im Scratchpad lieferte exakt der Befehl aus `local-ci.ps1` 0 Fehler und 1 Warnung MAUI1001. Dabei entsteht `Tankradar.MAUI.dll` für `net10.0-ios/iossimulator-arm64`.
- **Gegenprobe:** Eine eingeschleuste C#-Warnung (CS0168 in `Platforms/iOS`) wird zum Fehler `error CS0168`, der Build schlägt mit Exit 1 fehl. Der C#-Code des iOS-Ziels wird also tatsächlich kompiliert, und Compiler-Warnungen werden als Fehler behandelt.
- **Warum MAUI1001 eine Warnung bleibt:**
  - MAUI1001 ist eine MSBuild-Task-Warnung aus `Microsoft.Maui.Controls.targets`. `TreatWarningsAsErrors` wirkt nur auf Compiler-Warnungen, `MSBuildTreatWarningsAsErrors` ist nicht gesetzt.
  - Es gibt kein `NoWarn` und kein `WarningsNotAsErrors`. Die Warnung bleibt sichtbar.
  - Dasselbe Verhalten gilt für den Schritt `Static analysis` der Pipeline.
- Der unsignierte iOS-Build der CI (`package-ios`) läuft ohne `TreatWarningsAsErrors`. Der lokale Schritt ist damit strenger als die Pipeline, was unschädlich ist.

**Windows-Jobs nachgestellt:** Betroffen sind `static checks` und `build & test` in PR und staging, `prerelease`, `release` und `security-scan`. Alle haben dieselbe Job-Umgebung, nämlich `IncludeAndroidTarget`, `IncludeIosTarget` und `IncludeMacCatalystTarget` jeweils `false`. Ergebnisse:

| Prüfung | Ergebnis |
|---|---|
| `pwsh -NoProfile -File scripts/test-ios-deployment.ps1` | „iOS-Deployment-Pruefung erfolgreich.“, Exit 0 |
| `TargetFrameworks` | `;net10.0-windows10.0.19041.0` |
| `dotnet restore Tankradar.sln -p:Configuration=Release` | OK |
| `dotnet format --verify-no-changes --no-restore --severity error` | OK |
| `dotnet build -c Release --no-restore -p:TreatWarningsAsErrors=true` | 0 Warnungen, 0 Fehler |
| Unit- und Integrationstests mit Coverage | 15/15 und 3/3 |
| `dotnet list … --vulnerable` | keine Funde |
| `scripts/package-windows.ps1 -Version 0.1.0-rc.1 -Tag v0.1.0-rc.1` (Ausgabe in den Scratchpad) | `release-win-x64.zip` (ca. 92 MB) und `update.json` mit Version, SHA-256 und Größe |

Nicht ausgeführt wurden:
- `dotnet workload restore`, um keine Workloads zu verändern
- alle macOS- und GitHub-gebundenen Teile: signierter iOS-Build, TestFlight, `gh release`

**Weitere Läufe:**
- `npm test`: 24/24.
- `python scripts/validate-workflows.py`: OK für 7 Workflows und alle lokalen Actions. `actionlint` ist lokal nicht vorhanden.

**Kleinere Beobachtungen, nicht abnahmerelevant:**
- **`IncludeWindowsTarget` wird nicht gesetzt:** Die Windows-Job-Simulation und der iOS-Schritt setzen `IncludeWindowsTarget` nicht explizit auf `true`. Ist diese Variable in der Sitzung auf `false` vorbelegt, baut die Simulation das MAUI-Projekt ohne Zielframework. Das ist ein Randfall, die Pipeline setzt die Variable ebenfalls nicht.
- **Nachwirkung des iOS-Schritts:** Der iOS-Schritt restauriert das MAUI-Projekt zuletzt mit iOS-Ziel. Ein folgender `dotnet build --no-restore` ohne vorherigen Restore kann deshalb andere Assets vorfinden. Der nächste Lauf von `local-ci.ps1` restauriert zuerst und war im zweiten Lauf unauffällig.
- **Gelöschte Arbeitsdokumente:** Der Commit `4e55610` löscht außerdem die Arbeitsdokumente unter `docs/features/…--schritt-3-ci-cd/`, also `plan.md`, `requirement.md`, `review*.md`, `test-results.md` und `todo.md`. Für die Anforderung ist das unerheblich, war aber nicht Gegenstand der Nachbesserung.

**Übrige Anforderungspunkte unverändert erfüllt, wie in 3.2 bis 3.4 bewertet:**
- Branch-Modell mit `verify-pr-source`
- Promotion- und Backmerge-Workflows
- Versionierung: Start bei 0.1.0, keine automatische Anhebung auf 1.0, RC-Suffix
- Windows-ZIP ohne Installer
- Secrets und Variablen ausschließlich über GitHub
- iOS unsigniert ohne Fehlschlag, gesteuert über `IOS_SIGNING_ENABLED` und `IOS_*`
- `scripts/iOS-Deployment.ps1` mit allen Aktionen und `TANKRADAR_IOS_*`
- `package-ios` nach Vorlage
- Android-Ziel in allen Windows-Jobs ausgeblendet
- Einrichtungs-Checkliste
- Bundle-ID `de.martinstromberg.tankradar`
- `drafts/` nicht versioniert
