# Bestandsaufnahme: Schritt 11 – Startseite mit Favoritengruppen und Bereich „In der Nähe“

Analysiert wurden Favoriten (Dienst, ViewModels, Seiten), Preisdienst und Preis-Cache, Standortdienst, Einstellungen, Navigation, Kachel-Cache und die zugehörigen Tests und Testhilfen, bezogen auf `requirement.md`.

## Zusammenfassung

- **Favoriten (Schritt 10) vorhanden:** `IFavoritesService`/`FavoritesService` mit Gruppen, Zuordnung, Notiz/Priorität, `Changed`; `FavoritesPage`/`FavoritesViewModel` zeigt nur Gruppenname, Beschreibung und Anzahl; `FavoriteGroupPage` zeigt Einträge mit Name, Adresse, Notiz, Priorität. Es fehlen auf der Startseite: Preise, Entfernung, Hinweise, Aktualisierung, Offline-Hinweis, Wiederverbindung, „In der Nähe“. Die Gruppenansicht kann keine Detailansicht öffnen (`FavoriteGroupViewModel` kennt `IStationNavigator` nicht).
- **Bausteine zur Wiederverwendung vorhanden:** `StationResultBuilder`/`StationListItem`/`StationPriceLine` (Preiszeilen der gewählten Sorten in Einstellungsreihenfolge, „vor X Min.“, Amber ab 60 min, „Preis unbestätigt“, „Automatentankstelle“, Entfernung), `StationDetailViewModel` (Offline-Hinweis, `ConnectionRestored`-Aktualisierung), `MapViewModel` (Standort, Umkreissuche, Filterchips), `IConnectionMonitor`, `ILocationService` (Testmodus über `TANKATLAS_TEST_LOCATION`), `IStationNavigator`, `PriceRepository.DistanceKm`, lokal gespeicherte Tankstellenpositionen.
- **Fehlt im Preisdienst:** Es gibt keinen Abruf nach Kennungen (`prices.php`) und kein Repository-Lesen mehrerer Tankstellen mit Preisen; `IFuelPriceService` kennt nur Umkreissuche, Einzeldetail und Verfügbarkeitsprüfung. Der Mock-Server `MockTankerkoenigServer` bietet nur `list.php` und `detail.php`.
- **Einstellungen:** `AppSettings`/`UserSettingsEntity`/`SettingsService`/`SettingsViewModel` kennen keine Einstellung für „In der Nähe“; es gibt außer den Spritsorten keine Ein/Aus-Schalter. Eine neue Spalte braucht eine Migration (jüngste: `AddFavorites`).
- **Restpunkte Schritt 10:** `TileCachePolicy` wertet `Age` nicht aus; `HttpTileSource` verwendet bei `304` ohne Caching-Angaben die Rückfallfrist (`TileServerOptions.CacheLifetime`, 7 Tage), `TileMetadata` hat keine Lebensdauer; `FavoritesService.SaveAsync` bildet jede UNIQUE-Verletzung (auch `FavoriteEntries`) als `DuplicateName` ab, `AlreadyMember` kommt nur aus der Vorabprüfung.
- **Dokumentation:** `docs/help/Favoriten`, `Einstellungen`, `Preisdaten`, `Tankstellendetails`, `Suche`, `Navigation` und ADR 0006 (verweist auf Schritt 11) sind vorhanden; `docs/help/Startseite` und ADR 0007 fehlen; `docs/features.md` existiert nicht.
- **Designentwurf** `startseite_favoriten_gruppen` (aus `design-draft/stitch_smart_fuel_charge_tracker.zip`, im Scratchpad entpackt): Gruppen als einklappbare Karten, Tankstellenkacheln mit Preisen und Alter, Hinweise, „Neue Gruppe erstellen“, Bereich „In der Nähe (GPS)“; zusätzlich Elemente ohne Entsprechung in der Anforderung (Tages-Empfehlung, Routenknöpfe, Strom/Ladestationen laut ADR 0001, Tiefstpreis-Kennzeichen, „Sortieren“, Kopfzeile „Vor X Min. aktualisiert“).

Test-Ausgangszustand: Build, 693 Unit- und 95 Integrationstests bestehen; von 61 E2E-Tests schlägt einer fehl (`DiagnosticsCaptureE2ETests.FailingTestBodyProducesScreenshotUiTreeAndErrorFile`, „Der Screenshot zeigt keinen Fensterinhalt (einfarbig)“, im Wiederholungslauf identisch, vermutlich Umgebung). Keine Tests gibt es für Startseite, „In der Nähe“, `prices.php`, `Age`, `304` ohne Angaben, gleichzeitiges Zuordnen und Öffnen der Detailansicht aus der Gruppenansicht. Nicht ausgeführt: Skripttests (Node/Python), Formatprüfung, Sicherheitsprüfung, iOS-Compile, Coverage. Details und Nachweise: [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
