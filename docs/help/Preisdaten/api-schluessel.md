← [Zurück zur Übersicht](index.md)

# API-Schlüssel hinterlegen

Einen echten Tankerkönig-Schlüssel beantragen Sie selbst unter <https://creativecommons.tankerkoenig.de>. Der Schlüssel steht nie im Quellcode und wird nie committet. Ohne Schlüssel bauen und testen alle Projekte weiterhin; die App zeigt dann nur zuletzt bekannte Preise.

## Lokal (Entwicklungsrechner)

Der lokale Build sucht den Schlüssel in dieser Reihenfolge; die erste Quelle mit einem Wert gewinnt:

1. Umgebungsvariable `TANKRADAR_FUEL_PRICE_API_KEY`
2. Umgebungsvariable `FUEL_PRICE_API_KEY` (gleicher Name wie das GitHub-Secret)
3. Datei `tankerkoenig.local.props` (Variante B)

Variante A, Umgebungsvariable vor dem Build (eine der beiden Namen genügt):

```powershell
$env:FUEL_PRICE_API_KEY = "<Ihr Schlüssel>"
dotnet build src/Tankradar.MAUI -f net10.0-windows10.0.19041.0
```

Der Schlüssel erscheint dabei weder im Repository noch in Logs oder Build-Ausgaben: Eine kleine Build-Aufgabe (`src/Tankradar.MAUI/MSBuild/TankerkoenigApiKey.targets`) liest die Umgebungsvariablen selbst und schreibt die generierte Quelldatei mit dem Assembly-Metadatum direkt nach `obj/`. Der Wert kommt dadurch in keiner MSBuild-Eigenschaft, keinem Item-Metadatum und keinem Task-Parameter vor und wird auch bei `-v:detailed`, `-v:diag` und in Binlogs (`-bl`) nicht protokolliert; ins Log gelangt nur „Tankerkönig-Schlüssel beim Build vorhanden: ja/nein“. Ein automatisierter Test (`ApiKeyBuildTests_Log`, `ApiKeyBuildTests_Sources`) baut mit `-v:diag` und Binlog und Platzhalterschlüsseln: Der Platzhalter darf nicht im Log stehen, muss aber in der gebauten Assembly vorhanden sein; ein zweiter Test sichert die Reihenfolge der drei Quellen. Die echten Umgebungsvariablen werden im Test aus der Prozessumgebung entfernt.

Variante B, nicht versionierte Datei `tankerkoenig.local.props` im Repository-Root (steht in der `.gitignore`). Die Datei wird von der Build-Aufgabe gelesen, nicht per MSBuild-`Import` eingebunden (ein Import würde im Binlog mitprotokolliert):

```xml
<Project>
  <PropertyGroup>
    <TankerkoenigApiKey>IHR-SCHLUESSEL</TankerkoenigApiKey>
  </PropertyGroup>
</Project>
```

Der Schlüssel gelangt beim Build als Assembly-Metadatum in die App. Der beim Build mitgegebene Schlüssel ist maßgeblich: Beim Start vergleicht die App ihn mit dem Eintrag in der sicheren Ablage des Betriebssystems (Windows: Credential Locker „Tankatlas/PriceApi“, iOS: Keychain) und aktualisiert diesen, wenn er fehlt oder abweicht. Ein Update mit neuem oder rotiertem Schlüssel setzt sich dadurch automatisch durch; manuelles Löschen oder Neuinstallation ist nicht nötig. Wurde beim Build kein Schlüssel mitgegeben, nutzt die App weiter den gespeicherten.

## GitHub (CI und Releases) und öffentliches Repository

Das Repository ist öffentlich. Deshalb darf der Schlüssel nicht in öffentlich herunterladbaren Dateien landen:

| Artefakt | Enthält den Schlüssel? |
|----------|------------------------|
| Windows-Paket `release-win-x64.zip` (Release und Pre-Release) | **Nein.** Es wird ohne Schlüssel gebaut (`scripts/package-windows.ps1` leert die Quellen; der Workflow übergibt keinen Schlüssel). Die App zeigt dort „API-Schlüssel: nicht hinterlegt“ und nur zuletzt bekannte Preise. Windows ist Entwicklungs- und Testplattform: Den Schlüssel hinterlegen Sie lokal beim Build (Variante A oder B) bzw. in der App-Ablage. |
| Signierte iOS-`.ipa` | Ja, zwangsläufig (die App hat kein Backend). Sie wird **nicht** als Workflow-Artefakt hochgeladen; der Upload nach TestFlight ist der einzige Weg. |
| Quellcode, Workflow-Logs | Nein (GitHub maskiert das Secret in Logs; der Build gibt es ohnehin nicht aus). |

1. Repository → Settings → Secrets and variables → Actions → Secrets → **New repository secret**.
2. Name `FUEL_PRICE_API_KEY`, Wert ist Ihr Schlüssel.
3. Nur der iOS-Build (`package-ios`) erhält das Secret als `TANKRADAR_FUEL_PRICE_API_KEY` und übernimmt es wie oben beschrieben. Das Windows-Paketieren (`build-and-package`) bekommt es nicht; `scripts/validate-workflows.py` schlägt fehl, wenn ein Windows-Paketschritt einen Schlüssel erhält oder eine `.ipa` als Artefakt hochgeladen wird. Siehe auch die [Einrichtungs-Checkliste](../ci-cd/einrichtung.md).

## Tests

Tests verwenden nie den echten Schlüssel und nie produktive Endpunkte. Im Testmodus liegt der Schlüssel nie im echten Credential Locker bzw. in der Keychain (isolierte Ablage im Speicher), und ohne `TANKRADAR_PRICE_API_URL` verweigert die App den Abruf mit einer Meldung, statt den produktiven Endpunkt anzusprechen. Der Test-Mock akzeptiert einen festen Test-Schlüssel, der nur im Testmodus (`TANKATLAS_TEST_DATA_PATH` gesetzt) über `TANKRADAR_PRICE_API_KEY` an die App gegeben wird.

## Hinweis zur Sicherheit des Build-Schlüssels

Der beim Build mitgegebene Schlüssel steht als Metadatum in der ausgelieferten Assembly und ist daraus lesbar. Bei der iOS-App (TestFlight) ist das unvermeidbar, weil es kein Backend gibt, das den Schlüssel verwaltet; bei lokalen Windows-Builds liegt die Assembly nur auf Ihrem Rechner (Zwischenstände unter `review-versions/` nicht weitergeben). Er steht nicht im Quellcode und in keinem öffentlich herunterladbaren Windows-Paket. Verwenden Sie für den iOS-Build einen eigenen, jederzeit rotierbaren Schlüssel und tauschen Sie ihn bei Verdacht auf Weitergabe aus (empfohlen: regelmäßige Rotation, neuen Wert im Secret `FUEL_PRICE_API_KEY` hinterlegen).

## Schlüssel des Routingdienstes (OpenRouteService)

Die Routensuche (Schritt 17) braucht einen eigenen, kostenlosen Schlüssel von <https://openrouteservice.org/dev/#/signup>. **Jeder Anwender beantragt ihn selbst**; er wird nicht beim Build
eingebunden (kein Secret, keine Umgebungsvariable, keine `routing.local.props`), denn ein eingebauter Schlüssel wäre aus der öffentlich verfügbaren App auslesbar, und alle Nutzer
teilten sich Kontingent und Missbrauchsrisiko.

- **Eintragen:** **Optionen** → **Routensuche**: Schlüssel in das verdeckte Feld einfügen, **Schlüssel speichern**. Mit **Schlüssel prüfen** schickt die App eine kleine Testanfrage
  (feste Koordinaten, keine eigenen Daten) und meldet verständlich: gültig, abgelehnt, Dienst nicht erreichbar oder Kontingent aufgebraucht.
- **Aktivieren:** Der Schalter **Routensuche aktivieren** ist erst bedienbar, wenn ein Schlüssel gespeichert ist. Nur bei aktivierter Routensuche ist die Suchart „Route“ sichtbar, und nur dann
  gehen Aufrufe an OpenRouteService. Der Wechsel wirkt ohne Neustart. **Schlüssel entfernen** schaltet die Routensuche aus.
- **Ablage:** sichere Ablage des Geräts (Windows: Credential Locker „Tankatlas/RoutingApi“, Schalter „Tankatlas/RoutingEnabled“; iOS: Keychain „tankatlas.routing-api-key“ und „tankatlas.routing-enabled“).
  Der Schlüssel wird nie angezeigt, protokolliert oder in Meldungen und Adressen übernommen (er steht nur im Anfragekopf `Authorization`).
- **Sicherung:** Schlüssel und Schalter gehören nicht zur lokalen Sicherung. Nach einer Wiederherstellung auf einem Gerät ohne Schlüssel ist die Routensuche aus; auf demselben Gerät bleibt sie unverändert.
- **Adresse:** `https://api.heigit.org/openrouteservice/` (Directions: `v2/directions/driving-car`). Die frühere Adresse `api.openrouteservice.org` wird vom Anbieter Anfang November 2026 abgeschaltet
  (<https://ask.openrouteservice.org/t/deprecating-api-openrouteservice-org-in-favour-of-api-heigit-org/7912>).
- **CI und Windows-Paket:** Weder die Workflows noch `scripts/package-windows.ps1` kennen einen Routing-Schlüssel; das Secret `ROUTING_API_KEY` wird nicht mehr benötigt (ein vorhandenes kann gelöscht werden).
- **Tests:** Im Testmodus belegt `TANKRADAR_ROUTING_TEST_API_KEY` die Testablage im Speicher vor (Schlüssel gesetzt, Routensuche eingeschaltet); ohne die Variable startet die App ohne Schlüssel.
  Das Schlüsselfeld ist im Testmodus unverdeckt, damit die UI-Automation es beschreiben kann. Mock: `MockOpenRouteServiceServer`, nie ein echter Schlüssel.
