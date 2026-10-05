← [Zurück zur Übersicht](index.md)

# Kraftstoffpreise — Technischer Ablauf

## Übersicht

Alle Komponenten liegen im Projekt `Tankradar.MAUI` unter `Services/Pricing/` und `Models/Pricing/`; die Registrierung erfolgt in `MauiProgram.AddPriceServices`.

| Komponente | Aufgabe |
|------------|---------|
| `FuelPriceService` (`IFuelPriceService`) | Fassade: Umkreissuche, Tankstellendetails, Verfügbarkeitsprüfung; entscheidet zwischen Cache, Live-Abruf und Offline-Rückfall |
| `TankerkoenigClient` (`ITankerkoenigClient`) | HTTP-Zugriff auf `list.php` und `detail.php`, Auswertung der JSON-Antworten, Zeitlimit, Wiederholung, Drosselung |
| `RequestThrottle`, `IDelay` | Mindestabstand zwischen Anfragen; austauschbares Warten für Tests |
| `PriceRepository` (`IPriceRepository`) | Persistenz in SQLite (`Stations`, `PriceEntries`), letzte Preise, Umkreis per Haversine |
| `ConnectionMonitor` (`IConnectionMonitor`) | Online/Offline-Zustand und Ereignis `ConnectionRestored` über `INetworkStatusSource` (MAUI-`Connectivity`) |
| `ApiKeyProvider` (`IApiKeyProvider`), `IApiKeyStore` | Build-Schlüssel ist maßgeblich und aktualisiert bei Abweichung die sichere Ablage, sonst Schlüssel aus der Ablage; im Testmodus isolierte `InMemoryApiKeyStore` (`ApiKeyStoreSelector`); Windows: `CredentialLockerApiKeyStore`, sonst `SecureStorageApiKeyStore` (iOS-Keychain) |
| `PriceApiOptions` | Basisadresse (HTTPS-Pflicht), Zeitlimit, Versuche, Abstände, Cache-Dauer, maximaler Radius |
| `PriceFreshness`, `StationHints` | Aktualitätsbewertung (ab 60 Minuten veraltet), „vor X Min.“, abgeleitete Hinweise |
| `DataSourceViewModel` | Karte „Datenquelle“ in den Optionen |

## Ablauf einer Umkreissuche

1. `FuelPriceService.SearchNearbyAsync` validiert die Anfrage (Breite/Länge, Radius 1 bis `MaxRadiusKm` = 25, mindestens eine Sorte) und wirft sonst `ArgumentException`, bevor etwas abgerufen wird.
2. Cache: Wurde dieselbe Suche (Position auf drei Nachkommastellen gerundet, Radius) in den letzten fünf Minuten live abgerufen (nur im Arbeitsspeicher), liefert `PriceRepository.FindNearbyAsync` die Daten mit `PriceDataSource.Cache`.
3. Ist `IConnectionMonitor.IsOnline` falsch, folgt der Rückfall (`OfflineFallback`, `PriceFailure.Offline`).
4. Sonst ruft `TankerkoenigClient.SearchAsync` ab: Schlüssel über `IApiKeyProvider`, Drosselung, Anfrage mit eigenem Zeitlimit; Netz-, Zeitlimit- und 5xx/429-Fehler werden bis `MaxAttempts` mit Wartezeit `RetryBaseDelay · 2^(n-1)` wiederholt, andere 4xx sofort als `Rejected` gemeldet. Antworten mit `ok:false` ergeben `Rejected`, ungültiges JSON `InvalidResponse`.
5. Erfolg: `PriceRepository.SaveAsync` legt die Stationen an bzw. aktualisiert sie und fügt je Preis eine Zeile in `PriceEntries` mit Abrufzeitpunkt hinzu (nichts wird überschrieben; Historie bleibt). Ergebnis: `Live`, gefiltert auf die gewünschten Sorten, nach Entfernung sortiert.
6. Fehler (`PriceApiException`): Rückfall auf `FindNearbyAsync` (letzte bekannte Preise, Entfernung neu berechnet) mit `OfflineFallback` und dem Fehlergrund. Es entsteht keine Ausnahme für den Aufrufer.

Tankstellendetails (`GetStationDetailAsync`) folgen demselben Muster; der Cache gilt, wenn `DetailsUpdatedUtc` jünger als fünf Minuten ist. Ein späterer Listenabruf überschreibt gespeicherte Detailangaben (Öffnungszeiten) nicht.

## Datenmodell (Migration `AddPriceCache`)

- `Stations`: `Id` (UUID, Primärschlüssel), Name, Marke, Adresse, Position der Tankstelle, `WholeDay`, `OpeningTimesJson`, `DetailsUpdatedUtc`, `LastSeenUtc`.
- `PriceEntries`: `Id`, `StationId` (Fremdschlüssel, Kaskade), `FuelTypeKey` (Name von `FuelType`), `Price`, `RetrievedUtc`; Index auf `StationId, FuelTypeKey, RetrievedUtc`.
- Die Migration legt nur neue Tabellen an; Einstellungen bestehender Installationen bleiben unberührt (Test `PriceCacheMigrationTests_Upgrade`).
- Die Position des Anwenders wird nie gespeichert.

## Konfiguration und Testmodus

- Produktiver Endpunkt: `https://creativecommons.tankerkoenig.de/json/`. HTTP ist unzulässig.
- Nur wenn `TANKATLAS_TEST_DATA_PATH` gesetzt ist (Testmodus), überschreibt `TANKRADAR_PRICE_API_URL` den Endpunkt (HTTP nur für Loopback, kurze Wartezeiten) und `TANKRADAR_PRICE_API_KEY` den Schlüssel (nie gespeichert). Ohne `TANKRADAR_PRICE_API_URL` verweigert der Client im Testmodus den Abruf (`PriceFailure.EndpointNotConfigured`, kein Rückfall auf den produktiven Endpunkt). So starten E2E-Tests die App gegen den `MockTankerkoenigServer` (`src/TestSupport`) oder gegen einen unerreichbaren Port (Offline-Betrieb).
- Kein Test spricht produktive Endpunkte an.

## E2E-Robustheit (UIA-Timeouts)

`TransientRetry` (`src/TestSupport`) wiederholt den Aufbau der UI-Automation, die Abfrage des Hauptfensters und die Suche der Reiter bei `TimeoutException` bzw. `COMException` 0x80131505 höchstens dreimal mit Pause und meldet danach klar „UI-Automation hat auch nach 3 Versuchen nicht rechtzeitig geantwortet“. Alle anderen Fehler (Assertions, fehlende Elemente, Programmfehler) werden nicht wiederholt und nicht verdeckt.

## Hinweise

- `PriceEntries` wächst bewusst mit jedem Live-Abruf (jede Zeile ist ein Preisstand mit Zeitstempel für spätere Auswertungen); eine Bereinigung ist in diesem Schritt nicht vorgesehen. Die Abfrage des jeweils letzten Standes nutzt den Index auf `StationId, FuelTypeKey, RetrievedUtc`.
- Ein Speicherfehler nach erfolgreichem Abruf wird protokolliert; die frischen Daten werden trotzdem geliefert.
- Der HTTP-Client folgt keinen Weiterleitungen, damit der Schlüssel in der Anfrageadresse nie an einen anderen Host gelangt.
