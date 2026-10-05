← [Zurück zur Übersicht](index.md)

# API-Schlüssel hinterlegen

Einen echten Tankerkönig-Schlüssel beantragen Sie selbst unter <https://creativecommons.tankerkoenig.de>. Der Schlüssel steht nie im Quellcode und wird nie committet. Ohne Schlüssel bauen und testen alle Projekte weiterhin; die App zeigt dann nur zuletzt bekannte Preise.

## Lokal (Entwicklungsrechner)

Variante A, Umgebungsvariable vor dem Build:

```powershell
$env:TANKRADAR_FUEL_PRICE_API_KEY = "<Ihr Schlüssel>"
dotnet build src/Tankradar.MAUI -f net10.0-windows10.0.19041.0
```

Variante B, nicht versionierte Datei `tankerkoenig.local.props` im Repository-Root (steht in der `.gitignore`):

```xml
<Project>
  <PropertyGroup>
    <TankerkoenigApiKey>IHR-SCHLUESSEL</TankerkoenigApiKey>
  </PropertyGroup>
</Project>
```

Der Schlüssel gelangt beim Build als Assembly-Metadatum in die App. Beim ersten Start übernimmt die App ihn in die sichere Ablage des Betriebssystems (Windows: Credential Locker, iOS: Keychain) und liest ihn von dort. Wollen Sie einen anderen Schlüssel verwenden, bauen Sie neu und löschen den Eintrag „Tankatlas/PriceApi“ im Windows-Anmeldeinformationsverwaltung bzw. installieren die iOS-App neu.

## GitHub (CI und Releases)

1. Repository → Settings → Secrets and variables → Actions → Secrets → **New repository secret**.
2. Name `FUEL_PRICE_API_KEY`, Wert ist Ihr Schlüssel.
3. Die Workflows reichen das Secret den Build-Schritten als `TANKRADAR_FUEL_PRICE_API_KEY` durch (Windows-Paket und iOS-Build); der Build übernimmt es wie oben beschrieben. Siehe auch die [Einrichtungs-Checkliste](../ci-cd/einrichtung.md).

## Tests

Tests verwenden nie den echten Schlüssel und nie produktive Endpunkte. Der Test-Mock akzeptiert einen festen Test-Schlüssel, der nur im Testmodus (`TEST_DATA_PATH` gesetzt) über `TANKRADAR_PRICE_API_KEY` an die App gegeben wird.

## Hinweis zur Sicherheit des Build-Schlüssels

Der beim Build mitgegebene Schlüssel steht als Metadatum in der ausgelieferten Assembly (Pre-Release, TestFlight) und ist daraus lesbar; er steht nicht im Quellcode, aber im Auslieferungsartefakt. Verwenden Sie deshalb für Builds einen eigenen, jederzeit rotierbaren Schlüssel und tauschen Sie ihn bei Verdacht auf Weitergabe aus.
