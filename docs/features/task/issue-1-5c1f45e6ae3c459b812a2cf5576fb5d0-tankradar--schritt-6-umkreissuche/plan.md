# Umsetzungsplan: Schritt 6 – Umkreissuche nach aktuellem Standort mit Ergebnisliste (Version 0.1.7)

## Übersicht

Der Bereich „Karte“ (Suche) wird von einem Platzhalter zur funktionalen Umkreissuche: Ein neuer Standortdienst (`ILocationService`) liefert den Gerätestandort nur auf Anforderung und gemäß `GpsUsage`; `MapViewModel` ruft `IFuelPriceService.SearchNearbyAsync` mit Radius (1–25 km, Standard 5 km) und den gewählten Sorten auf und zeigt eine nach Sorte filterbare und sortierbare Ergebnisliste mit Preisen, Alter, Hinweisen und Offline-Hinweis in der Kopfzeile. Zusätzlich wird die Testmodus-Variable `TEST_DATA_PATH` überall in `TANKATLAS_TEST_DATA_PATH` umbenannt, der Mock-Server um Umkreisdaten erweitert, die Plattform-Berechtigungstexte ergänzt und der Zwischenstand 0.1.7 vorbereitet.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Standortzugriff `ILocationService` | Gateway-Interface `GetCurrentLocationAsync(GpsUsage, CancellationToken)` liefert ein `LocationResult` (Status + `GeoPosition`); Implementierungen `MauiLocationService` (MAUI `Permissions`/`Geolocation`) und `TestLocationService` (fester Standort aus Testkonfiguration) | Windows hat kein GPS; Tests brauchen einen deterministischen Standort. `GpsUsage.Never` wird bereits im Dienst abgefangen, bevor irgendeine Plattform-API berührt wird (Fail Secure). |
| Auswahl der Implementierung | Eine Factory in `MauiProgram` (`LocationServiceSelector`, analog `ApiKeyStoreSelector`) wählt `TestLocationService`, nur wenn `TANKATLAS_TEST_DATA_PATH` gesetzt ist und eine gültige `TANKATLAS_TEST_LOCATION` vorliegt; im Testmodus ohne gültige Test-Position wird `LocationStatus.Unavailable` geliefert, nie die echte Plattform | Gleiches Muster wie bei der Schlüsselablage; verhindert, dass Tests echte Standortabfragen auslösen. Produktiv ist die Variable wirkungslos. |
| `GpsUsage.Always` | Wird für die Vordergrundsuche wie `WhileInUse` behandelt (Berechtigung „bei Nutzung“, kein Hintergrundzugriff) | Die Suche braucht nur Vordergrundzugriff; die Info.plist enthält deshalb nur `NSLocationWhenInUseUsageDescription`. |
| Radius | Statisches Value Object `SearchRadius` (Min 1, Max 25 aus `PriceApiOptions.MaxRadiusKm`, Standard 5, `TryParse`); Eingabe als numerisches `Entry`, Wert wird nicht gespeichert | Die Anforderung nennt keine Persistenz; Standard 5 km je Sitzung vermeidet Migration und Einstellungsumbau. Die Prüfung greift vor dem API-Aufruf, `FuelPriceService` bleibt zweite Absicherung. |
| Filter und Sortierung | Reiner Transaction Script `StationResultBuilder.Build(...)` (Statics, ohne UI-Abhängigkeit) erzeugt aus `StationInfo`, Einstellungen, Filter, Sortierung, Uhrzeit die Liste `StationListItem`; Filter und Sortierung werden clientseitig auf dem vorhandenen Ergebnis angewendet, ohne neuen API-Aufruf | Spart Abrufe (Nutzungsbedingungen), ist vollständig unit-testbar. Die Suche selbst fragt immer alle gewählten Sorten ab. |
| Bedeutung des Filters | Filter „Alle“ (Standard) oder eine der in den Einstellungen gewählten Sorten; bei einer Sorte erscheinen nur Stationen, die diese Sorte führen, und die Sortierung nach Preis nutzt deren Preis; die Preiszeilen zeigen weiterhin alle gewählten Sorten in Einstellungsreihenfolge | Entspricht „Preise der gewählten Sorten in der dort festgelegten Reihenfolge“. Ohne Filter bestimmt die erste gewählte Sorte die Preissortierung (wie Schritt 9 es für die Karte vorsieht). Stationen ohne Preis der Sortiersorte stehen am Ende. |
| Wiederverwendung UI-Muster | Auswahlgruppen für Filter und Sortierung als `RadioButton`-Gruppen über die vorhandene Schnittstelle `IChoiceOption` und `SettingsRadio`-Muster (Optionen-Seite); Sortierung mit `ChoiceOptionViewModel<ResultSortOrder>`, Filter mit neuem `FuelFilterOptionViewModel : IChoiceOption`; Karten-Optik wie `SettingsCard` (Border, Surface, `rounded-md`) | Nutzt bestehende, E2E-erprobte Bedienmuster; keine Eingabe interner Kennungen (Sorten werden mit Klartextnamen aus `SettingsTexts.GetLabel` gewählt). |
| Ergebnisliste | `CollectionView` mit `ItemsSource` auf eine unveränderliche Liste (`IReadOnlyList<StationListItem>`), die bei jeder Änderung komplett ersetzt wird (kein `ObservableCollection`) | Bindungen marshallen `PropertyChanged` auf den UI-Thread; Ersetzen der Liste vermeidet Threading-Probleme bei Ereignissen des Verbindungsmonitors. |
| Offline-Hinweis | Banner in der Kopfzeile der Seite (oberhalb der Überschrift) mit `IsOffline = !IConnectionMonitor.IsOnline || Ergebnis.Source == OfflineFallback`; `MapViewModel` abonniert `IConnectionMonitor.ConnectionChanged` und aktualisiert das Banner (Abmeldung in `OnDisappearing`/`Dispose`) | Erfüllt „Kopfzeile zeigt Offline-Hinweis“; die automatische Neuabfrage nach Wiederverbindung gehört laut Projektplan zu den Schritten 8 und 11 und ist hier nicht Teil. |
| Karte/Standardansicht | Die Kartenansicht gehört zu Schritt 9. Schritt 6 zeigt immer die Liste; `ResultView.Map` aus den Einstellungen wird bis dahin als Liste dargestellt, ohne Umschalter | „Karte nur falls schon vorgesehen“ – sie ist es nicht. |
| Keine Persistenz von Koordinaten | `GeoPosition` lebt nur im Arbeitsspeicher des laufenden Suchvorgangs und wird weder in Datenbank, Einstellungen, Protokolle noch Fehlermeldungen geschrieben; Logging des Standorts ist verboten (Test prüft die Logausgabe). Der Preis-Cache speichert wie bisher Tankstellenkoordinaten und Preise, keine Suchposition und keine Distanz. | Sicherheitskapitel der Anforderung. |
| Umbenennung der Variable | Einzige Quelle bleibt die Konstante `TestDataPaths.TestDataPathEnvironmentVariable` mit neuem Wert `TANKATLAS_TEST_DATA_PATH`; kein Alias für den alten Namen | Der alte Name darf den Testmodus nicht mehr aktivieren (Fail Secure). Die Variablen `TANKRADAR_PRICE_API_URL`/`TANKRADAR_PRICE_API_KEY` bleiben unverändert (nicht gefordert, `TANKRADAR_`-Präfix ist projektspezifisch). |

## Programmabläufe

### Umkreissuche am aktuellen Standort

1. Der Nutzer öffnet „Karte“; `MapViewModel.OnAppearing` lädt über `ISettingsService.LoadAsync` die Einstellungen (Sorten in Reihenfolge, `GpsUsage`, Standardsortierung) und baut Filter- und Sortieroptionen auf. Es wird noch kein Standort abgefragt.
2. Der Nutzer prüft/ändert den Radius (Standard 5) und löst „Suchen“ aus (`SearchCommand`).
3. `SearchRadius.TryParse` validiert die Eingabe (ganze Zahl 1–25); bei Fehler erscheint eine Meldung (`SearchTexts.RadiusInvalid`), es gibt keinen Standort- und keinen API-Aufruf.
4. `MapViewModel` ruft `ILocationService.GetCurrentLocationAsync(settings.GpsUsage)`:
   - `GpsUsage.Never` -> `LocationStatus.DisabledBySetting`: keine Berechtigungs- und keine Standortabfrage; die Seite zeigt den Hinweis (`SearchTexts.LocationDisabledBySetting`, mit Verweis auf „Optionen“), die Liste bleibt leer.
   - `Always`/`WhileInUse` -> `MauiLocationService` prüft `Permissions.LocationWhenInUse`, fragt bei Bedarf einmal an (iOS-Dialog mit Text aus `Info.plist`), holt dann `Geolocation`-Position mit Zeitlimit. Verweigert -> `PermissionDenied`, Standort nicht ermittelbar/Zeitüberschreitung -> `Unavailable`; jeweils verständlicher Hinweis, keine Ausnahme nach oben.
   - Testmodus: `TestLocationService` liefert den festen Standort (`Available`) für `Always`/`WhileInUse`, für `Never` ebenfalls `DisabledBySetting`.
5. Bei `Available` baut `MapViewModel` eine `StationSearchQuery` (Position, Radius, alle in den Einstellungen gewählten Sorten) und ruft `IFuelPriceService.SearchNearbyAsync` auf; `IsBusy` ist währenddessen gesetzt, ein laufender Aufruf wird bei erneutem Suchen abgebrochen (`CancellationTokenSource`).
6. Das `StationSearchResult` (Stationen, `Source`, `Failure`) wird unverändert in einem privaten Feld gehalten (nur im Speicher).
7. `StationResultBuilder.Build` erzeugt die Liste: Filter anwenden, Preiszeilen in Einstellungsreihenfolge, je Preis `PriceFreshness.FormatAge`/`IsStale`, Hinweise über `StationHints` (`PriceTexts.PriceUnconfirmed`, `PriceTexts.AutomatedStation`), Entfernungstext, Sortierung (Preis aufsteigend, Entfernung aufsteigend, Name alphabetisch kulturabhängig; Gleichstand: Entfernung, dann Name).
8. Banner: `IsOffline` und Hinweis zur Quelle (`OfflineFallback` -> „Es werden die zuletzt bekannten Preise angezeigt“); leere Ergebnisse zeigen einen eigenen Leerzustand („Keine Tankstellen im Umkreis gefunden“).
9. Ändert der Nutzer Filter oder Sortierung, wird nur Schritt 7 mit dem gehaltenen Ergebnis erneut ausgeführt (kein Standort-, kein API-Aufruf). Ein erneutes „Suchen“ fragt den Standort erneut ab.

Beteiligte Klassen/Komponenten: `MapViewModel`, `MapPage`, `ILocationService`, `MauiLocationService`, `TestLocationService`, `SearchRadius`, `StationResultBuilder`, `IFuelPriceService`, `ISettingsService`, `IConnectionMonitor`, `TimeProvider`, `PriceFreshness`, `StationHints`.

### Offline und Verbindungswechsel

1. Konstruktor/`OnAppearing`: `MapViewModel` abonniert `IConnectionMonitor.ConnectionChanged`.
2. Bei Änderung wird `IsOffline` neu berechnet (Banner ein/aus); die angezeigte Liste bleibt unverändert, das Alter der Preise wird bei der nächsten Aufbereitung aktualisiert.
3. Ohne Verbindung liefert `FuelPriceService` `PriceDataSource.OfflineFallback` mit den zuletzt bekannten Preisen samt Alter; die Seite zeigt sie mit Banner.

### Testmodus-Aktivierung

1. `AppDataPathProvider`, `PriceApiOptions.FromEnvironment`, `ApiKeyStoreSelector` und neu `LocationServiceSelector` lesen ausschließlich `TestDataPaths.TestDataPathEnvironmentVariable` (`TANKATLAS_TEST_DATA_PATH`).
2. Eine nur mit dem alten Namen `TEST_DATA_PATH` gesetzte Umgebung aktiviert den Testmodus nicht: regulärer Datenpfad, produktiver Endpunkt, echte Schlüsselablage, echter Standortdienst.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `GeoPosition` (`src/Tankradar.MAUI/Models/Search/GeoPosition.cs`) | Record (Value Object) | Breite/Länge, nur im Speicher, mit Bereichsprüfung |
| `LocationStatus` (`Models/Search/LocationStatus.cs`) | Enum | `Available`, `DisabledBySetting`, `PermissionDenied`, `Unavailable` |
| `LocationResult` (`Models/Search/LocationResult.cs`) | Record | Status + optionale `GeoPosition` |
| `SearchRadius` (`Models/Search/SearchRadius.cs`) | Statische Klasse (Value Object) | `Min`, `Max`, `Default`, `TryParse`, `Validate` |
| `StationListItem` (`Models/Search/StationListItem.cs`) | Record | Id, Name, Entfernung (Wert + Text), Preiszeilen, Hinweise, Öffnungsstatus-Text |
| `StationPriceLine` (`Models/Search/StationPriceLine.cs`) | Record | Sorte, Anzeigename, Preis (Wert und Text mit Euro-Format), Alterstext, `IsStale` |
| `StationResultBuilder` (`Services/Search/StationResultBuilder.cs`) | Statische Klasse (Transaction Script) | Filter, Preiszeilen, Hinweise, Sortierung |
| `ILocationService` (`Services/Location/ILocationService.cs`) | Interface (Gateway) | Standort auf Anforderung gemäß `GpsUsage` |
| `MauiLocationService` (`Services/Location/MauiLocationService.cs`) | Klasse | Berechtigung und Position über MAUI Essentials; kein Logging von Koordinaten |
| `TestLocationService` (`Services/Location/TestLocationService.cs`) | Klasse | Fester Standort aus Testkonfiguration; ohne Koordinaten `Unavailable` |
| `LocationServiceSelector` (`Services/Location/LocationServiceSelector.cs`) | Statische Klasse | Auswahl nach Testmodus, Parsen von `TANKATLAS_TEST_LOCATION` (`lat,lon`, invariant) |
| `FuelFilterOptionViewModel` (`ViewModels/FuelFilterOptionViewModel.cs`) | ViewModel (`IChoiceOption`) | Filteroption „Alle“ bzw. Sorte |
| `StationListItemViewModel` oder direkte Bindung an `StationListItem` | entfällt | `StationListItem` ist direkt bindbar (kompilierte Bindungen), kein zusätzliches ViewModel |
| `SearchTexts` (`Resources/Texts/SearchTexts.cs`) | Statische Textklasse | Alle deutschen Texte der Suche (Überschriften, Hinweise, Fehler, Offline, Leerzustand, Entfernungs- und Preisformat) |

## Änderungen an bestehenden Klassen

### `MapViewModel` (ViewModel)

- **Neue Abhängigkeiten (Konstruktor):** `ISettingsService`, `ILocationService`, `IFuelPriceService`, `IConnectionMonitor`, `TimeProvider`, `ILogger<MapViewModel>`.
- **Neue Eigenschaften:** `RadiusText` (string, Standard „5“), `Stations` (`IReadOnlyList<StationListItem>`), `FuelFilterOptions`, `SortOptions` (`ChoiceOptionViewModel<ResultSortOrder>`), `StatusMessage`/`HasStatusMessage`, `IsOffline`/`OfflineMessage`, `HasResults`, `ShowEmptyState`, `SourceNote`, `LastSearchTask` (für Tests, wie `LastCheckTask`).
- **Neue Befehle/Methoden:** `SearchCommand`, `ApplyFilterAndSort`, Einstellungen laden in `OnAppearing`; Abmeldung vom Ereignis in `OnDisappearing`.
- **Geändert:** Titel „Karte“ bleibt; der parameterlose Konstruktor entfällt.

### `MapPage` (View, `MapPage.xaml` und `.xaml.cs`)

- Platzhalter ersetzen: Offline-Banner (`Search.OfflineBanner`), Überschrift (`MapPage.Headline` bleibt), Karte „Suchradius“ (`Entry` `Search.Radius`, Einheit „km“, Schaltfläche `Search.Submit`), Statusmeldung (`Search.StatusMessage`), Filter- und Sortiergruppen als `RadioButton` (`Search.Filter.{Key}`, `Search.Sort.{Key}`), Ergebnisliste `CollectionView` (`Search.Results`), je Zeile Name (`Search.Station.Name`), Entfernung, Preiszeilen (Preis in `JetBrainsMono`/`price-hero`-Stil nach Designentwurf, veraltete Preise in `ColorTertiaryAmber`), Alterstext, Hinweis-Chips (Amber für „Preis unbestätigt“, Teal/neutral für „Automatentankstelle“), Leerzustand. Stile und Farben ausschließlich aus `DesignSystem.xaml`; Touch-Ziele 44x44, Hell-/Dunkelmodus über `AppThemeBinding`; ScrollView/CollectionView so, dass die Seite bei Fenstergrößen von Windows bedienbar bleibt.
- `.xaml.cs`: Konstruktor bleibt; `OnDisappearing` wird an das ViewModel weitergegeben (Abmeldung vom Verbindungsereignis).

### `BaseViewModel` / `TankradarContentPage`

- **Neue Methode:** virtuelles `OnDisappearing` in `BaseViewModel` (Standard leer); `TankradarContentPage` überschreibt `OnDisappearing` und leitet weiter.

### `MauiProgram` (Klasse)

- `ILocationService` über `LocationServiceSelector` registrieren (Singleton); `MapViewModel` bleibt transient; `UserAgent`-Zeichenfolge unverändert.

### `TestDataPaths` (`src/TestSupport/TestDataPaths.cs`)

- **Geändert:** `TestDataPathEnvironmentVariable` = `"TANKATLAS_TEST_DATA_PATH"`.
- **Neue Konstante:** `TestLocationEnvironmentVariable` = `"TANKATLAS_TEST_LOCATION"` (Format `breite,länge`, invariante Kultur; nur im Testmodus wirksam).

### `AppDataPathProvider`, `PriceApiOptions`, `ApiKeyStoreSelector`

- Nur XML-Kommentare mit dem alten Namen (`<c>TEST_DATA_PATH</c>`) auf `TANKATLAS_TEST_DATA_PATH` ändern; Logik läuft über die Konstante.

### `PriceTexts`

- Hinweis: `GetCheckMessage` bleibt; neue Suchtexte kommen in `SearchTexts`, nicht in `PriceTexts`.

### `MockTankerkoenigServer` (`src/TestSupport`)

- Umkreisdaten: zusätzliche Stationen `StationGamma` (rund 8 km von Berlin-Mitte, Diesel am günstigsten, nur E5/Diesel, Name „Gamma Tankstelle“, nicht durchgehend geöffnet) und `StationDelta` (rund 22 km, alle Sorten, durchgehend offen), beide ausserhalb des 5-km-Standardradius für Alpha/Beta; eine Station `StationEpsilon` (rund 40 km, ausserhalb jedes zulässigen Radius).
- `list.php` wertet `lat`, `lng`, `rad` aus: `dist` aus Haversine berechnet, nur Stationen im Radius, aufsteigend nach `dist`; Anfragen mit `rad` > 25 beantwortet der Mock wie die Quelle mit Fehler.
- Neue Eigenschaften: `LastListRadius`, `LastListLatitude` und `LastListLongitude` (für Tests, die prüfen, dass nie ein Radius über 25 gesendet wird).
- `detail.php` für die neuen Stationen ergänzen.

### E2E-Basis `E2ETestBase`

- Neuer Überlademechanismus `RestartApplication(IReadOnlyDictionary<string,string> additionalEnvironment)`, der die Umgebung für den Neustart ersetzt (gleiches Testdatenverzeichnis), damit ein Szenario „online suchen, dann Preisdienst unerreichbar neu starten“ möglich ist.

### Neue E2E-Basis `SearchE2ETestBase` (`src/Tankradar.Tests.E2E/E2E/FlaUI/SearchE2ETestBase.cs`)

- Startet die App mit Mock-URL, Test-Schlüssel und `TANKATLAS_TEST_LOCATION=52.5200,13.4050`; Hilfsmethoden `OpenSearch`, `SetRadius`, `Submit`, `ReadStationNames`, `SelectSort`, `SelectFilter`, `SetGpsUsage` (über Optionen-Seite, bestehende Hilfsmethoden von `SettingsE2ETestBase`).

### Plattformdateien

- `src/Tankradar.MAUI/Platforms/iOS/Info.plist`: `NSLocationWhenInUseUsageDescription` = „Ermittlung von Tankstellen in der Nähe“. `Platforms/MacCatalyst/Info.plist` erhält denselben Schlüssel (gleiche Codebasis, vermeidet Absturz bei Standortabfrage).
- `src/Tankradar.MAUI/Platforms/Windows/Package.appxmanifest`: `<DeviceCapability Name="location" />` im Abschnitt Capabilities ergänzen (zusätzlich zu `runFullTrust`).
- `src/Tankradar.MAUI/Platforms/iOS/Resources/PrivacyInfo.xcprivacy`: prüfen, ob für Standort ein Eintrag nötig ist (Standortnutzung ohne Tracking; `NSPrivacyCollectedDataTypes` für „Precise Location“, nicht verknüpft, nicht für Tracking, Zweck App-Funktionalität) und ergänzen.
- `Tankradar.MAUI.csproj`: keine Versionsänderung (die Schrittversion 0.1.7 wird per `-Version` beim Publish und in `changes.log` geführt; die Projektdatei trägt wie bisher 0.1.0).

### Dokumentation und Skripte

- `README.md`: Zeilen mit `TEST_DATA_PATH` auf `TANKATLAS_TEST_DATA_PATH`; Hinweis auf `TANKATLAS_TEST_LOCATION`; Stand Schritt 6.
- `docs/help/Einstellungen/ablauf-technisch.md`, `docs/help/Navigation/ablauf-technisch.md`, `docs/help/Preisdaten/ablauf-technisch.md`, `docs/help/Preisdaten/api-schluessel.md`: neuer Variablenname; neue Anwenderdokumentation `docs/help/Suche/` (index, beschreibung, ablauf-anwender, ablauf-technisch) und Eintrag in `docs/help/index.md`; Hinweis zu Radius 1–25 km, Standortnutzung/Datenschutz.
- `changes.log`: neuer Eintrag oben „Umkreissuche … (Tankradar Schritt 6, Version 0.1.7)“ mit Hinweis auf die Umbenennung (Breaking für lokale Testumgebungen: `TEST_DATA_PATH` wirkt nicht mehr). Bestehende historische Einträge (Zeilen 19 und 58) bleiben unverändert.
- Skripte, Workflows und `.github`: enthalten den Namen nicht (laut `git grep`); trotzdem im Verifikationsschritt erneut mit `git grep -n "TEST_DATA_PATH"` (ohne `docs/projects`) prüfen, Treffer nur noch als Bestandteil von `TANKATLAS_TEST_DATA_PATH` oder in der historischen `changes.log`.
- Zwischenstand: nach Abnahme `scripts/create-review-version.ps1 -Version 0.1.7 -ChangelogFile <Text>` ergibt `review-versions/0.1.7_<Datum>/` (nicht committen; `.gitignore` deckt das Verzeichnis ab).

## Datenbankmigrationen

Keine. Radius und Filter werden nicht gespeichert; die Einstellungen bleiben unverändert; es werden keine Standortdaten persistiert.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Radius (`SearchRadius.TryParse`) | Ganze Zahl, 1 <= r <= 25 (Obergrenze aus `PriceApiOptions.MaxRadiusKm`), invariante Ziffern, keine Dezimalzahlen, kein Leerstring | Meldung `SearchTexts.RadiusInvalid` („Bitte einen Radius von 1 bis 25 km eingeben.“); kein Standort- und kein API-Aufruf |
| Spritsortenfilter | Nur „Alle“ oder eine in den Einstellungen gewählte, definierte Sorte (Auswahl, keine Freitexteingabe) | Nicht auswählbare Werte entstehen nicht; Builder ignoriert/verwirft undefinierte Werte; Suche verlangt weiterhin mindestens eine gewählte Sorte (Einstellungen garantieren das) |
| Standort | Breite -90..90, Länge -180..180, endlich | `GeoPosition`-Prüfung wirft; `MauiLocationService` meldet dann `Unavailable`; `TestLocationService` lehnt ungültige `TANKATLAS_TEST_LOCATION` ab (Unavailable) |
| Suche bei `GpsUsage.Never` | Keine Abfrage von Berechtigung oder Standort | Hinweis `SearchTexts.LocationDisabledBySetting` |
| API-Aufruf | Radius 1..25 und Sorten werden zusätzlich in `FuelPriceService.Validate` geprüft (bestehend) | `ArgumentException` wird in `MapViewModel` abgefangen und als allgemeine Meldung angezeigt |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `TANKATLAS_TEST_DATA_PATH` (Umgebungsvariable, ersetzt `TEST_DATA_PATH`) | Umgebungsvariable | nicht gesetzt | Aktiviert den Testmodus und gibt das isolierte Datenverzeichnis vor |
| `TANKATLAS_TEST_LOCATION` | Umgebungsvariable (nur Testmodus) | nicht gesetzt | Fester Standort `breite,länge` für Tests |
| `NSLocationWhenInUseUsageDescription` | Info.plist (iOS, MacCatalyst) | „Ermittlung von Tankstellen in der Nähe“ | Zweck im Berechtigungsdialog |
| Capability `location` | Package.appxmanifest | gesetzt | Standortzugriff unter Windows |
| `SearchRadius.Default` / `Max` | Konstante | 5 / 25 | Standard- und Höchstradius |

## Seiteneffekte und Risiken

- **Umbenennung der Variable:** Lokale Entwicklungsumgebungen, die `TEST_DATA_PATH` selbst gesetzt hatten (z. B. eigene Skripte), müssen umgestellt werden; E2E-Tests setzen sie über die Konstante und sind unmittelbar betroffen. Risiko: veraltete Dokumentation; daher komplette Dokuprüfung per `git grep`.
- **`MapViewModel`-Konstruktor:** `ViewModelTests_PageTitles` (parameterlose Erzeugung) und eventuelle andere Stellen, die `MapViewModel` direkt erstellen, brechen; DI-Registrierung neuer Dienste in `MauiProgram` nötig, sonst Startfehler der App (E2E-Navigationstest fängt das ab).
- **Mock-Server:** Alpha/Beta erhalten berechnete statt fester Entfernungen; bestehende Tests, die die festen Werte (0,1 / 1,4 km) oder nur zwei Stationen erwarten (`PriceServiceMockServerTests_Flow`, `PriceServiceE2ETests_*`, ggf. `TankerkoenigClientTests_*`), müssen auf die berechneten Werte bzw. die Radiusfilterung angepasst werden. Die Probe-Suche (Radius 1 bei Berlin) darf weiter Alpha liefern.
- **Windows und Standort:** Die Windows-App ist ungepackt (`WindowsPackageType=None`); der echte Geolocator kann dort von den Systemeinstellungen abhängen. Deshalb nutzen alle automatisierten Tests den festen Teststandort, und `MauiLocationService` fängt alle Fehler als `Unavailable`/`PermissionDenied` ab.
- **Datenschutz:** Koordinaten dürfen in keiner Protokollzeile und keiner Ausnahmemeldung erscheinen; `ILogger`-Aufrufe in `MauiLocationService`/`MapViewModel` protokollieren nur Statuswerte. Test prüft das mit einem Log-Sammler.
- **Altersanzeige:** Die Quelle liefert keine eigenen Preiszeitstempel; das Alter beginnt mit dem Abruf (`FuelPrice.RetrievedUtc`). Live-Ergebnisse zeigen daher „vor 0 Min.“. Veraltete Preise (Amber, „Preis unbestätigt“) treten praktisch nur bei Cache-/Offline-Daten auf; E2E kann das nicht über die Uhr erzeugen, daher Abdeckung auf Unit-Ebene mit `ManualTimeProvider`.
- **Preis-Cache:** `FuelPriceService` filtert beim Cache-Treffer erneut nach Sorten; da die Suche immer alle gewählten Sorten abfragt, bleibt der Such-Cache-Schlüssel (Position, Radius) wirksam.

## Umsetzungsreihenfolge

1. **Variable umbenennen**
   - Voraussetzungen: Keine.
   - Beschreibung: `TestDataPaths.TestDataPathEnvironmentVariable` auf `TANKATLAS_TEST_DATA_PATH`, neue Konstante `TestLocationEnvironmentVariable`; XML-Kommentare in `ApiKeys.cs`, `PriceApiOptions.cs`; Test (Unit), dass der alte Name `TEST_DATA_PATH` den Testmodus in `AppDataPathProvider`, `PriceApiOptions.FromEnvironment` und `ApiKeyStoreSelector` nicht aktiviert und der neue Name es tut; Dokumentation, README, Hilfetexte anpassen; `git grep`-Prüfung.
2. **Modelle und Text**
   - Voraussetzungen: Schritt 1 nicht nötig.
   - Beschreibung: `GeoPosition`, `LocationStatus`, `LocationResult`, `SearchRadius`, `StationPriceLine`, `StationListItem`, `SearchTexts` anlegen.
3. **Ergebnisaufbereitung**
   - Voraussetzungen: Schritt 2, bestehende `PriceFreshness`, `StationHints`, `SettingsTexts`.
   - Beschreibung: `StationResultBuilder` mit Filter, Preiszeilen, Hinweisen, Alter, Sortierung (Unit-Tests parallel).
4. **Standortdienst**
   - Voraussetzungen: Schritt 1 (Konstante `TestLocationEnvironmentVariable`), Schritt 2.
   - Beschreibung: `ILocationService`, `MauiLocationService`, `TestLocationService`, `LocationServiceSelector`; DI-Registrierung in `MauiProgram`; Plattformdateien (Info.plist iOS/MacCatalyst, Package.appxmanifest, Privacy-Manifest).
5. **Mock-Server erweitern**
   - Voraussetzungen: Keine (parallel möglich).
   - Beschreibung: Stationen, Radius- und Positionsauswertung, `LastList*`-Eigenschaften, Detailantworten; bestehende Tests anpassen.
6. **ViewModel**
   - Voraussetzungen: Schritte 2 bis 4; `BaseViewModel.OnDisappearing` und `TankradarContentPage.OnDisappearing` angelegt.
   - Beschreibung: `FuelFilterOptionViewModel`, `MapViewModel` (Suche, Filter, Sortierung, Banner, Verbindungsereignis); `ViewModelTests_PageTitles` anpassen.
7. **Seite**
   - Voraussetzungen: Schritt 6, `DesignSystem.xaml`-Stile.
   - Beschreibung: `MapPage.xaml`/`.cs` gemäß Designentwurf, AutomationIds, Hell/Dunkel, Touch-Ziele.
8. **Tests Unit/Integration**
   - Voraussetzungen: Schritte 3 bis 6.
   - Beschreibung: Testklassen laut Abschnitt Tests, Testhelfer (`FakeLocationService`, Log-Sammler).
9. **E2E**
   - Voraussetzungen: Schritte 5 bis 7, `RestartApplication`-Überladung.
   - Beschreibung: `SearchE2ETestBase`, E2E-Tests; Navigationstest prüfen.
10. **Dokumentation und Release**
    - Voraussetzungen: Schritte 1 bis 9.
    - Beschreibung: `docs/help/Suche/`, `docs/help/index.md`, README, `changes.log` (Version 0.1.7); danach `scripts/create-review-version.ps1 -Version 0.1.7 ...`; Gesamtlauf `dotnet build`/`dotnet test` Release.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| Alter Name aktiviert Testmodus nicht (je `AppDataPathProvider`, `PriceApiOptions`, `ApiKeyStoreSelector`) | `TestModeVariableTests_Rename` | `TEST_DATA_PATH` allein -> regulärer Pfad, `EndpointNotConfigured` false, echte Ablage; `TANKATLAS_TEST_DATA_PATH` -> Testmodus; Konstante hat den neuen Wert |
| Radius gültig/ungültig (1, 5, 25, 0, 26, -1, 5,5, „abc“, leer, sehr groß) | `SearchRadiusTests_Validation` | Grenzen 1 und 25, Standard 5 |
| Filter: „Alle“, eine Sorte, Sorte ohne Treffer | `StationResultBuilderTests_Filter` | Stationen ohne gewählte Sorte entfallen, Preiszeilen bleiben in Einstellungsreihenfolge |
| Preiszeilen-Reihenfolge entspricht den Einstellungen, abgewählte Sorten fehlen | `StationResultBuilderTests_PriceLines` | Reihenfolge und Format (`1,859 €`, deutsche Kultur) |
| Sortierung Preis/Entfernung/Name inkl. Gleichstand und fehlendem Preis | `StationResultBuilderTests_Sorting` | Stabile Reihenfolge, Stationen ohne Sortierpreis am Ende, Standardsortierung aus Einstellungen |
| Altersanzeige „vor X Min.“ und Amber-Grenze (59 / 60 / 61 Minuten, Zukunft) | `StationResultBuilderTests_Age` | `StationPriceLine.AgeText`, `IsStale` |
| Hinweise „Preis unbestätigt“ / „Automatentankstelle“ nur wo zutreffend | `StationResultBuilderTests_Hints` | Ableitung über `StationHints` |
| `GpsUsage.Never` fragt Standort nie ab | `LocationServiceTests_Permission` | `TestLocationService` und Fake-Plattform: keine Aufrufe, Status `DisabledBySetting` |
| Berechtigung verweigert / Standort nicht verfügbar / Zeitüberschreitung | `LocationServiceTests_Permission` | `PermissionDenied`/`Unavailable` statt Ausnahme (mit abstrahierter Plattformschicht `ILocationPlatform` oder Delegates) |
| Auswahl der Implementierung im Test-/Normalmodus, ungültige Test-Position, alter Variablenname | `LocationServiceSelectorTests_TestMode` | Testmodus -> `TestLocationService`, sonst Plattformdienst; ungültige Werte -> `Unavailable` |
| Suche ohne Standort ruft `IFuelPriceService` nicht auf; ungültiger Radius ruft weder Standort noch API auf | `MapViewModelTests_Search` | Reihenfolge und Abbruchbedingungen |
| Erfolgreiche Suche übergibt Radius, Position, alle gewählten Sorten; Radius nie > 25 | `MapViewModelTests_Search` | `StationSearchQuery`-Inhalt |
| Filter-/Sortierwechsel ohne erneuten Standort-/API-Aufruf | `MapViewModelTests_FilterSort` | Zähler der Fakes |
| Standardsortierung aus Einstellungen vorgewählt; `ResultView.Map` zeigt Liste | `MapViewModelTests_Settings` | Initialzustand |
| Offline-Banner bei `OfflineFallback` und bei `IsOnline=false`; Wechsel über `ConnectionChanged`; Abmeldung bei `OnDisappearing` | `MapViewModelTests_Offline` | `IsOffline`, `OfflineMessage` |
| Keine Roh-Koordinaten in Logs/Status/Ausnahmen | `MapViewModelTests_Privacy` | Log-Sammler enthält weder Breite noch Länge |
| Keine Standortdaten im Preis-Cache und in den Einstellungen nach einer Suche | `SearchPrivacyTests_Persistence` (Integration) | Datenbankinhalt nach Suche: nur Tankstellen und Preise, keine Position, keine Distanz |
| Suche gegen `MockTankerkoenigServer`: Radiusfilterung, Sortierung, gesendeter `rad` (5 und 25), keine Anfrage bei ungültigem Radius | `SearchMockServerTests_Flow` (Integration) | End-to-End über Dienst + Repository + Mock |
| Offline-Rückfall nach erfolgreicher Suche (Mock beendet) liefert Cache-Daten | `SearchMockServerTests_Offline` (Integration) | `PriceDataSource.OfflineFallback`, Alter |
| Plattformdateien: `Info.plist` enthält den Schlüssel mit Text „Ermittlung von Tankstellen in der Nähe“; `Package.appxmanifest` enthält `location` | `PlatformManifestTests_Location` (Unit, liest die Dateien relativ zum Repository) | Konfiguration vorhanden |
| Hilfsmethoden: `FakeLocationService`, `RecordingLogger`, `StationFactory` | `Unit/Support` | Testdaten und Zähler |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `ViewModelTests_PageTitles` | `MapViewModel` hat Abhängigkeiten; Erzeugung mit Fakes |
| `PriceServiceMockServerTests_Flow`, `TankerkoenigClientTests_*` (soweit sie Mock-Daten nutzen) | Mock liefert Radiusfilter und berechnete Entfernungen |
| `PriceServiceE2ETests_Reachable`/`_Unreachable`, `SettingsE2ETestBase` | Mock-Änderung, neue Konstante für Testmodus, ggf. `E2ETestBase`-Überladung |
| `AppDataPathProviderTests_DataDirectoryResolution`, `ApiKeyStoreSelectorTests_TestMode`, `PriceApiOptionsTests_Validation` | verwenden die Konstante; Wert ändert sich (inhaltlich nur Namensprüfung ergänzen) |
| `NavigationE2ETests` | Karten-Seite hat neue Inhalte; `MapPage.Headline` bleibt erhalten |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Suche mit festem Standort und Standardradius 5 km: Liste zeigt Alpha und Beta (nicht Gamma/Delta) mit Name, Entfernung, Preisen der gewählten Sorten in Einstellungsreihenfolge, „vor 0 Min.“ und Hinweis „Automatentankstelle“ bei Alpha | `SearchE2ETests_Basic` | Umkreissuche, Ergebnisdarstellung, Hinweise, Altersanzeige | Nur die laufende App zeigt Verdrahtung aus Standortdienst, Dienst, Cache und XAML |
| Pflicht | Radius 25 km: Gamma und Delta erscheinen, Epsilon nicht; Mock sah `rad=25`; Radius 26 und 0 zeigen Fehlermeldung ohne API-Anfrage (`MockRequestCount` unverändert) | `SearchE2ETests_Radius` | Radius 1–25, Validierung vor API-Aufruf | Eingabe und Fehlermeldung sind UI-Verhalten |
| Pflicht | Filter „Diesel“: Beta (kein Diesel) verschwindet; Sortierung Preis/Entfernung/Name ändert die Reihenfolge; Standardsortierung aus Optionen (z. B. Name) ist beim Öffnen vorgewählt | `SearchE2ETests_FilterSort` | Filterung, Sortierung, Standard aus Einstellungen | Zusammenspiel Einstellungen und Suche |
| Pflicht | `GpsUsage` „Nie“ (in Optionen gesetzt): Suche liefert Hinweis, Liste leer, Mock erhält keine Anfrage | `SearchE2ETests_GpsNever` | Keine Abfrage bei „Nie“, Hinweis | Datenschutzregel muss im echten Ablauf wirken |
| Pflicht | Reihenfolge der Preiszeilen folgt den Einstellungen (Reihenfolge ändern, neu suchen) und abgewählte Sorten fehlen | `SearchE2ETests_FuelOrder` | Preise in Einstellungsreihenfolge | Verknüpfung Optionen und Suche |
| Pflicht | Offline: online suchen (Cache füllen), App mit nicht erreichbarem Preisdienst neu starten (gleiche Daten), erneut suchen: zuletzt bekannte Preise mit Alter und Offline-Banner in der Kopfzeile | `SearchE2ETests_Offline` | Offline-Betrieb und Kopfzeilenhinweis | Fallback und Banner nur im echten Ablauf prüfbar |
| Pflicht | Alter Variablenname: App-Start nur mit `TEST_DATA_PATH` (und Mock-URL) nutzt nicht das Testverzeichnis/den Testmodus; ohne neuen Namen kein Testmodus | `TestModeRenameE2ETests` | Umbenennung der Variable | Nachweis in der echten App, nicht nur in der Einheit; muss das reguläre Datenverzeichnis des Entwicklers schonen (E2E nutzt dafür ein eigenes, nicht das Benutzerprofil: Test prüft nur, dass kein Mock-Zugriff erfolgt und die Suche den Hinweis „Preisdienst nicht verfügbar/Standort nicht verfügbar“ zeigt) |
| Empfohlen | Altersanzeige Amber: Nachweis nur über Unit-Tests | – | Amber ab 60 Min. | Die Uhr lässt sich im E2E nicht verschieben; UIA liefert keine Farbe zuverlässig |

Bestehende E2E-Tests, die angepasst werden: `NavigationE2ETests` (Karten-Seite lädt mit allen neuen Diensten), `PriceServiceE2ETests_Reachable` und `_Unreachable` (Mock-Änderungen, Konstante), `SettingsE2ETestBase`/`E2ETestBase` (neue Konstante, Überladung `RestartApplication`).

## Offene Punkte

Keine.
