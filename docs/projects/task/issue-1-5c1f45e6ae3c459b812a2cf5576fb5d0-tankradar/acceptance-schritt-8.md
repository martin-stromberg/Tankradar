# Abnahmeprüfung – Entwicklungsschritt 8

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

### Behebung der Abweichungen aus `acceptance-schritt-8.1.md`

- **Designabweichungen der Detailansicht: behoben.**
  - `StationDetailPage.xaml` setzt den Entwurf `tankstellen_details_favoriten` jetzt weitgehend um:
    - Kopfkarte mit Marken-Chip, Chip „Live-Preise“ bzw. Preisalter, Chip „Preis unbestätigt“
    - Info-Box mit Symbolen (`near_me`, `schedule`) für Entfernung und Öffnungsstatus sowie Chip „Automat 24/7“
    - Preiskarten mit hochgestellter dritter Nachkommastelle und „€/L“ in `price-hero`
    - Rückkehr nur über den Zurück-Pfeil der Shell; die eigene „Zurück“-Schaltfläche ist entfallen
  - `docs/adr/0004-station-detail-design-deviations.md` hält alle verbleibenden Abweichungen mit Begründung bzw. Zielschritt fest:
    - Favoriten → Schritt 10, Tankbuch → Schritt 13, Navigation und Anfahrtszeit → Schritt 17
    - Preisverlauf, Spartipp und Trend entfallen: keine Datenquelle
    - „Ausstattung & Services“ entfällt, ebenso EV (ADR 0001)
    - Sortenuntertitel, Preisfarbe und Symbole weichen ab
    - Ergänzt wurden „Preise aktualisieren“, Quellenhinweis, Quellenangabe und die Öffnungszeiten-Karte
  - `docs/help/Tankstellendetails/beschreibung.md` verweist auf ADR 0004.
  - Nicht ausdrücklich im ADR erwähnt sind Kleinigkeiten:
    - App-Logo und Profil-Avatar in der Kopfleiste (Shell-weit)
    - „Geöffnet“ ohne „bis 23:00 Uhr“
- **Schlüssel im MSBuild-Log: behoben.**
  - Das csproj setzt keine Eigenschaft und kein `AssemblyAttribute`-Item mehr. `MSBuild/TankerkoenigApiKey.targets` liest Umgebung und props-Datei in einer `RoslynCodeTaskFactory`-Inline-Task selbst und schreibt `TankerkoenigApiKey.g.cs` direkt nach `$(IntermediateOutputPath)` (`obj/`).
    - Parameter der Task sind nur Pfade und das Bool-Ergebnis.
    - Ins Log gelangt nur „vorhanden: ja/nein“.
    - Die Warnung bei ungültigem XML und mögliche Ausnahmen enthalten den Wert nicht.
    - Die props-Datei wird nicht mehr per `<Import>` eingebunden.
    - Die Task läuft bei jedem Build (keine Inputs/Outputs) und schreibt die Datei nur bei geänderter Ausgabe. Ein Schlüsselwechsel kommt dadurch an, ohne unnötige Neukompilierung.
  - **Eigene Gegenprobe am echten MAUI-Projekt** (Platzhalter `ZZREVIEWPLACEHOLDERZZ` in `FUEL_PRICE_API_KEY`, echte Variablen per `env -u` entfernt): `dotnet build … -v:diag -bl`
    - Konsole und entpackte Binlog: 0 Treffer.
    - Treffer nur in `obj/…/TankerkoenigApiKey.g.cs` sowie in der gebauten `Tankradar.MAUI.dll` und `.pdb`.
  - `ApiKeyBuildTests_Log` und `ApiKeyBuildTests_Sources` (7 Tests) sichern dasselbe automatisiert ab:
    - diag-Log und Binlog für alle drei Quellen
    - Reihenfolge der Quellen
    - leerer Schlüssel ohne Quelle
    - Gezielter Lauf: 7/7 grün.

### Prüfpunkte (a)–(d)

- **(a) Schlüssel in Logs, Ausgaben und auf macOS**
  - Kein Weg ins Log gefunden. Erfasst ist er nur in `obj/` und in den Build-Ausgaben (`bin/`).
  - Beobachtung: Der Schlüssel steht auch in der `.pdb`, vermutlich weil die generierte Quelldatei dort eingebettet ist. Das betrifft nur lokale bzw. iOS-Builds, die ohnehin den Schlüssel enthalten. In CI wird keine PDB als Artefakt veröffentlicht, das Windows-Paket enthält keinen Schlüssel.
  - Setzt jemand `MSBUILDLOGALLENVIRONMENTVARIABLES=1`, protokolliert MSBuild alle Umgebungsvariablen. Das ist eine bewusste Opt-in-Diagnose außerhalb des Projekts; die Doku erwähnt es nicht.
  - **macOS ist lokal nicht nachweisbar.** Die Task nutzt Standardmittel (`RoslynCodeTaskFactory`, `$(MSBuildToolsPath)`, UTF-8 ohne BOM, `Path.GetDirectoryName`), die plattformübergreifend funktionieren. Pfade mit `\` in String-Parametern (`$(IntermediateOutputPath)`, Standardpfad der props-Datei) normalisiert MSBuild unter Unix üblicherweise.
    - Für den CI-iOS-Build ist die props-Datei irrelevant, weil der Schlüssel aus der Umgebung kommt.
    - Ein grüner macOS-Lauf mit der neuen Task steht noch aus. Empfehlung: im ersten Staging-Lauf prüfen, ob „vorhanden: ja“ erscheint.
- **(b) Windows-Paket und IPA-Artefakt**
  - In `build-and-package/action.yml` gibt es keinen Schlüssel-Input mehr; `TANKRADAR_FUEL_PRICE_API_KEY` und `FUEL_PRICE_API_KEY` werden explizit leer gesetzt.
  - `release.yml` und `staging-ci.yml` übergeben an diese Action keinen Schlüssel.
  - `scripts/package-windows.ps1` entfernt zusätzlich beide Umgebungsvariablen und blendet die props-Datei über `-p:TankerkoenigLocalPropsFile=<nicht existierende Datei>` aus.
  - **Praktisch geprüft:** `package-windows.ps1` mit gesetzten Platzhaltern in beiden Variablen.
    - Exit-Code 0.
    - 0 Treffer im Paketlog und im gesamten Publish-Verzeichnis.
  - Die Schritte „Upload signed IPA“ sind in beiden Workflows entfernt. Ein `upload-artifact` mit `.ipa` existiert nirgends mehr; die GitHub-Releases laden nur `release-win-x64.zip` und `update.json` hoch.
  - `validate-workflows.py` prüft das. Synthetisch nachgewiesen: Ein Schlüssel-Input und ein `.ipa`-Upload werden erkannt. Die Prüfung ist allerdings musterbasiert und erkennt nicht:
    - Verzeichnis-Uploads, die eine `.ipa` enthalten könnten
    - `gh release`-Uploads von `.ipa`
    - ein FUEL-Secret als Wert eines `routing`-Inputs
  - Die heutigen Workflows sind unabhängig davon korrekt.
- **(c) iOS-Build:** `package-ios` erhält in `staging-ci.yml` (`ios-prerelease`) und `release.yml` (`ios`) weiterhin `fuel-price-api-key: ${{ secrets.FUEL_PRICE_API_KEY }}` und setzt im Schritt „Build iOS“ `TANKRADAR_FUEL_PRICE_API_KEY`. Die Inline-Task liest genau diese Variable mit Vorrang.
- **(d) Detailansicht:** entspricht dem Entwurf, soweit fachlich und datenseitig möglich. Die Abweichungen sind in ADR 0004 nachvollziehbar dokumentiert (siehe oben).

### Gesamtanforderung (keine Regression)

- **Fachlich:**
  - Name, Adresse und Entfernung
  - Preise der aktivierten Sorten in der Reihenfolge der Einstellungen, mit „vor X Min.“ und Amber ab 60 Minuten
  - „Preis unbestätigt“ und „Automat 24/7“
  - Öffnungszeiten mit Stand
  - Keine Zahlungsangaben, weil die Quelle keine liefert (in der Hilfe dokumentiert)
  - Fehlende Angaben werden ausgeblendet
  - Kein Strom bzw. EV
- **Offline:**
  - Der Offline-Hinweis steht jetzt fest oberhalb des scrollenden Inhalts (Detail- und Suchseite), wie für eine Kopfzeile gefordert.
  - „Preise aktualisieren“ ist offline deaktiviert (`CanRefresh`).
  - Nach `ConnectionRestored` folgt die automatische Aktualisierung bei Offline-Stand oder veralteten Preisen.
- **Zusätze 1, 2, 4 und 5:** unverändert erfüllt; siehe `acceptance-schritt-8.1.md`.
- **Zusatz 3** (Übernahme aus `FUEL_PRICE_API_KEY`): erfüllt und getestet. **Zusatz 6:** erfüllt.
- **Dokumentation:** aktualisiert in `api-schluessel.md` (Tabelle zu den Artefakten, Rotationsempfehlung), `ci-cd/README.md`, `ci-cd/einrichtung.md`, `ci-cd/workflows.md` und `README.md`.

### Praktische Verifikation

- **`scripts/local-ci.ps1`:** einmal vollständig ausgeführt, ohne `FUEL_PRICE_API_KEY` und `TANKRADAR_FUEL_PRICE_API_KEY` in der Prozessumgebung. Exit-Code 0, alle Schritte OK:
  - Pipeline-Skripte und Workflow-Validierung
  - iOS-Deployment-Skript
  - Restore, Format, Sicherheitsprüfung, Build mit Warnungen als Fehler
  - Unit-Tests 539/539, Integrationstests 89/89 (inkl. der 7 `ApiKeyBuildTests`), Abdeckung 95,3 %
  - FlaUI-E2E 44/44, off-screen, 2 min 19 s
  - iOS-Compile-Prüfung
  - Das Windows-Paket wurde dort standardmäßig übersprungen und separat geprüft (siehe (b)).
- **Einzelne Skripte und Tests:**
  - `scripts/test-ios-deployment.ps1`: Exit-Code 0
  - `scripts/validate-workflows.py`: OK (7 Workflows); `actionlint` ist nicht installiert
  - `ApiKeyBuildTests` gezielt: 7/7 grün
- **Zwischenstand `review-versions/0.1.12_2026-10-06/`:**
  - Vorhanden mit `bin/` und `CHANGELOG.md`; der Changelog beschreibt die Nachbesserung von Schritt 8.
  - Per `.gitignore` ausgeschlossen.
  - Kurzstart mit temporärem `TANKATLAS_TEST_DATA_PATH` und `TANKATLAS_TEST_WINDOW=offscreen`: Das Fenster „Tankatlas“ öffnete sich.
  - Danach wurde der Prozess gezielt per Pfad beendet und das temporäre Verzeichnis entfernt; es läuft keine Instanz mehr.
- **Nebenwirkung der Prüfung:** Die Debug-Ausgabe `src/Tankradar.MAUI/obj|bin/Debug/net10.0-windows…` enthält nach der Gegenprobe den Platzhalterschlüssel statt eines Schlüssels. Der nächste lokale Build überschreibt ihn.
- Keine echten Endpunkte aufgerufen, keine Commits, kein Push.
- Das unversionierte `docs/features/` ist weiterhin vorhanden (Restbestand, nicht Teil dieses Schritts).
