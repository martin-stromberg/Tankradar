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

Der Schlüssel gelangt beim Build als Assembly-Metadatum in die App. Der beim Build mitgegebene Schlüssel ist maßgeblich: Beim Start vergleicht die App ihn mit dem Eintrag in der sicheren Ablage des Betriebssystems (Windows: Credential Locker „Tankatlas/PriceApi“, iOS: Keychain) und aktualisiert diesen, wenn er fehlt oder abweicht. Ein Update mit neuem oder rotiertem Schlüssel setzt sich dadurch automatisch durch; manuelles Löschen oder Neuinstallation ist nicht nötig. Wurde beim Build kein Schlüssel mitgegeben, nutzt die App weiter den gespeicherten.

## GitHub (CI und Releases)

1. Repository → Settings → Secrets and variables → Actions → Secrets → **New repository secret**.
2. Name `FUEL_PRICE_API_KEY`, Wert ist Ihr Schlüssel.
3. Die Workflows reichen das Secret den Build-Schritten als `TANKRADAR_FUEL_PRICE_API_KEY` durch (Windows-Paket und iOS-Build); der Build übernimmt es wie oben beschrieben. Siehe auch die [Einrichtungs-Checkliste](../ci-cd/einrichtung.md).

## Tests

Tests verwenden nie den echten Schlüssel und nie produktive Endpunkte. Im Testmodus liegt der Schlüssel nie im echten Credential Locker bzw. in der Keychain (isolierte Ablage im Speicher), und ohne `TANKRADAR_PRICE_API_URL` verweigert die App den Abruf mit einer Meldung, statt den produktiven Endpunkt anzusprechen. Der Test-Mock akzeptiert einen festen Test-Schlüssel, der nur im Testmodus (`TANKATLAS_TEST_DATA_PATH` gesetzt) über `TANKRADAR_PRICE_API_KEY` an die App gegeben wird.

## Hinweis zur Sicherheit des Build-Schlüssels

Der beim Build mitgegebene Schlüssel steht als Metadatum in der ausgelieferten Assembly (Pre-Release, TestFlight) und ist daraus lesbar; er steht nicht im Quellcode, aber im Auslieferungsartefakt. Verwenden Sie deshalb für Builds einen eigenen, jederzeit rotierbaren Schlüssel und tauschen Sie ihn bei Verdacht auf Weitergabe aus.
