# ADR 0020: Build- und Actions-Governance

## Status

Angenommen (2026-11-17)

## Kontext

Das Architektur- und Sicherheitsreview (#62, Folgeissue #69) hat festgestellt:
Die drei Testprojekte pinnen identische Paketversionen jeweils lokal, externe
GitHub Actions werden über Major-Tags statt Commit-SHAs referenziert, und es
fehlt eine `global.json` für einheitliche SDK-Auflösung. Gleichzeitig existiert
bereits der wöchentliche `review-nuget-packages`-Workflow, der ein Prüf-Issue
für die Analyse erzeugt, aber selbst keine Update-PRs aufmacht.

## Entscheidungen

### Zentrale Paketverwaltung

`Directory.Packages.props` im Repository-Root mit
`ManagePackageVersionsCentrally=true`: Sämtliche `PackageReference` führen keine
`Version` mehr; alle `PackageVersion`-Einträge liegen zentral in dieser Datei.
Die `MauiVersion`-Property zieht ebenfalls dorthin (inklusive ihrer
Begründung aus dem MAUI-csproj, siehe ADR 0016). Damit ist "ein Ort, eine
Version" gegeben und Dependabot kann Update-PRs auf genau eine Datei erzeugen.

### SDK-Band

`global.json` mit `"version": "10.0.400"` und `"rollForward": "latestPatch"`
bindet lokale Builds und Tooling an das 10.0.4xx-Band (verwendete Patches,
keine ungewollten Band-Sprünge). Die CI-Workflows installieren via
`actions/setup-dotnet` `dotnet-version: '10.0.4xx'` dasselbe Band; beide Seiten
sind damit auf dasselbe SDK-Band festgelegt. Ein Wechsel des Bands erfolgt
bewusst durch gleichzeitige Anpassung von `global.json` und den Workflows.

### Automatisierte Update-Vorschläge

`.github/dependabot.yml` richtet wöchentliche Updates für `nuget`
(`/`, erfasst `Directory.Packages.props` und alle Projekte) und
`github-actions` (`/`, erfasst `.github/workflows`) gegen den Zielbranch
`staging` ein. Patch-/Minor-Updates werden gruppiert; Major-Updates kommen
einzeln und werden gesondert bewertet (Konvention aus Issue #68). Der bestehende
wöchentliche Review-Workflow bleibt unverändert als analytische Ergänzung.

### Action-Pinning: bewusst Tag-Pinning statt Commit-SHAs

Externe Actions bleiben auf Major-Tags (`@v5`/`@v6`/`@v7`) gepinnt; ein
vollständiges SHA-Pinning wird in diesem Schritt bewusst zurückgestellt.

Begründung:

- Alle verwendeten externen Actions stammen aus dem `actions/*`-Namespace
  (GitHub-eigene, verifizierte Publisher); es werden keine Actions aus
  Einzel-Repositories Dritter verwendet.
- Dependabot hält die Tags aktuell und erzeugt bei Sicherheitsupdates
  automatisch PRs; das verbleibende Risiko wiederveröffentlichter Tags ist
  bei `actions/*` als gering einzustufen.
- SHA-Pinning erhöht den Pflegeaufwand (unleserliche `uses:`-Zeilen,
  Kommentar-Pflicht für den zugehörigen Tag) und verlangsamt die
  Update-Automation, ohne hier ein unmittelbar höheres Restrisiko zu adressieren.
- Die Workflow-Berechtigungen sind bereits restriktiv gesetzt (Befund #62),
  was die Angriffsfläche veränderbarer Tags zusätzlich begrenzt.

Neubewertung: Sobald Actions von Drittanbietern außerhalb `actions/*` (oder
andere nicht verifizierte Publisher) aufgenommen werden, sind diese einzeln auf
Commit-SHAs zu pinnen. Dependabot aktualisiert auch SHA-gepinnte Actions.

## Konsequenzen

- Builds sind reproduzierbarer (eine Versionsquelle, definiertes SDK-Band).
- Security- und Patchupdates erreichen das Repository als PRs statt nur als
  Review-Aufgaben; die Bewertungslogik (Majors separat prüfen) bleibt bestehen.
- Bestehende Release-/CI-Pipeline bleibt unverändert; einzige Anpassung ist das
  SDK-Band `10.0.x` → `10.0.4xx`.
