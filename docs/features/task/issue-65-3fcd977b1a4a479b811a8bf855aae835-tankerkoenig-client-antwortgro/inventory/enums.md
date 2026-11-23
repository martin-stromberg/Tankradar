# Enums

## `PriceFailure`
Datei: `src/Tankradar.MAUI/Models/Pricing/PriceFailure.cs`

Fehlergrund der Preis-API; fließt über `PriceApiException.Failure` → `FuelPriceService`-Ergebnisrecords → Anzeigetexte `PriceTexts.FailureText` (`src/Tankradar.MAUI/Resources/Texts/PriceTexts.cs:65-71`) und `SearchTexts` (`src/Tankradar.MAUI/Resources/Texts/SearchTexts.cs:314-320`). Jeder Wert hat bereits einen Anzeigetext.

| Wert | Bedeutung |
|------|-----------|
| `None` | Kein Fehler |
| `Offline` | Keine Netzverbindung laut Betriebssystem |
| `Unreachable` | Trotz Wiederholungen nicht erreichbar (Verbindungsfehler, Zeitüberschreitung, Serverfehler) |
| `ApiKeyMissing` | Kein API-Schlüssel hinterlegt |
| `Rejected` | Anfrage abgelehnt (z. B. ungültiger Schlüssel oder Nutzungsgrenze) |
| `InvalidResponse` | Antwort nicht auswertbar — vorhandener Kandidat für übergroße Antworten („unerwartete Antwort") |
| `EndpointNotConfigured` | Testmodus ohne Endpunkt; Abruf verweigert |

Ein eigener Wert für „Antwort zu groß" existiert nicht.

## `FuelType`
Datei: `src/Tankradar.MAUI/Models/FuelType.cs`

Persistenzvertrag (Namen dürfen nicht umbenannt werden); vom `TankerkoenigClient` beim Parsen von `e5`/`e10`/`diesel` befüllt.

| Wert | Bedeutung |
|------|-----------|
| `SuperE5` | Super E5 |
| `SuperE10` | Super E10 |
| `Diesel` | Diesel |

## `PriceDataSource`
Datei: `src/Tankradar.MAUI/Models/Pricing/PriceDataSource.cs`

Herkunftsangabe in den `FuelPriceService`-Ergebnissen.

| Wert | Bedeutung |
|------|-----------|
| `Live` | Frisch abgerufen |
| `Cache` | Aus dem lokalen Cache innerhalb der Frische |
| `OfflineFallback` | Zuletzt bekannte Daten bei fehlgeschlagenem Abruf |
