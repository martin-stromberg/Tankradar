# Logik

## `StationDetailBuilder`
Datei: `src/Tankradar.MAUI/Services/Search/StationDetailBuilder.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Build(StationInfo, IReadOnlyList<FuelTypeSelection>, double?, DateTime)` | `public static` | Erzeugt `StationDetailItem` aus `StationInfo`; blendet fehlende Quelldaten aus |
| `FormatOpeningHours(OpeningTimeEntry)` | `private static` | Öffnungszeile aufbereiten |
| `ShortTime(string?)` | `private static` | „HH:mm:ss" → „HH:mm" |

Wichtig: `Build` erhält die `StationInfo` mit `Latitude`/`Longitude`, übergibt sie aber nicht an `StationDetailItem` (das Record hat keine solchen Parameter). Wird aufgerufen von `StationDetailViewModel.RebuildDetail`.

## `StationDetailViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/StationDetailViewModel.cs`

| Methode / Element | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| Konstruktor `(ISettingsService, IFuelPriceService, IConnectionMonitor, TimeProvider, ILogger<StationDetailViewModel>, StationFavoritesViewModel)` | `public` | DI; erzeugt `RefreshCommand`, `SaveCustomNameCommand`, `RemoveCustomNameCommand` |
| `Detail` | `public` (private setter) | Das angezeigte `StationDetailItem`; löst `OnPropertyChanged` für `HasDetail`, `HasCustomName`, `ShowNoPrices` aus |
| `Favorites` | `public` | ViewModel der Karte „Favoritengruppen" |
| `ApplyQueryAttributes(IDictionary<string, object>)` | `public` | Navigationsparameter → `Show(StationListItem)` |
| `Show(StationListItem)` | `public` | Baut `StationDetailItem` direkt aus dem Listeneintrag (Anfangsanzeige vor dem Detailabruf) |
| `OnAppearing()` | `public override` | Abonniert Verbindungsmonitor, startet `LoadAsync` |
| `OnDisappearing()` | `public override` | Abmeldung, bricht Ladevorgang ab |
| `LoadAsync()` | `public` | Detailabfrage über `IFuelPriceService.GetStationDetailAsync` |
| `Apply(StationDetailResult, AppSettings)` | `private` | Übernimmt Ergebnis, ruft `RebuildDetail` |
| `RebuildDetail(AppSettings)` | `private` | Ruft `StationDetailBuilder.Build(_station, settings.FuelTypes, _origin?.DistanceKm, now)` |
| `RefreshCommand`, `SaveCustomNameCommand`, `RemoveCustomNameCommand` | `public ICommand` | Bestehende Befehle |
| `CanRefresh` | `public` | Bedienbarkeit der Aktualisieren-Schaltfläche (`!IsBusy && _connection.IsOnline`) |
| `LastLoadTask`, `LastCustomNameTask` | `public Task` | Für Tests und Abwarten |

Abonnierte Events: `IConnectionMonitor.ConnectionChanged`, `IConnectionMonitor.ConnectionRestored`.

Relevanter Ablauf für die Anforderung: `Show(StationListItem)` (Anfangsanzeige, hat `Latitude`/`Longitude` aus `StationListItem`) und `RebuildDetail` (vollständige Anzeige, hat `Latitude`/`Longitude` aus `StationInfo`). Beide Wege müssten die Position in `StationDetailItem` durchreichen.

## `LauncherLinkOpener` (Referenzmuster)
Datei: `src/Tankradar.MAUI/Services/Navigation/ILinkOpener.cs` (beide Typen in einer Datei)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| Konstruktor `(bool testMode)` | `public` | Testmodus-Schalter |
| `OpenAsync(Uri)` | `public` | `ArgumentNullException.ThrowIfNull`; bei `_testMode` oder nicht-HTTPS-Rückkehr ohne Aktion; sonst `Launcher.Default.OpenAsync(uri)` |

Wird injiziert in `RouteSearchSettingsViewModel` (`_linkOpener`, siehe `RouteSearchSettingsViewModel.cs` Zeile 21). Registrierung: `MauiProgram.cs` Zeile 109 — `builder.Services.AddSingleton<ILinkOpener>(_ => new LauncherLinkOpener(IsTestMode()));`; `IsTestMode()` (Zeile 163) prüft `TestDataPaths.TestDataPathEnvironmentVariable`.

## `StationResultBuilder`
Datei: `src/Tankradar.MAUI/Services/Search/StationResultBuilder.cs`

Erzeugt `StationListItem` aus `StationInfo` — setzt `Latitude`/`Longitude` bereits (daher `double?` im Listeneintrag). Wird in `StationDetailViewModelTestBase.CreateOrigin()` genutzt.

## `StationDetailPage` (UI)
Datei: `src/Tankradar.MAUI/Views/StationDetailPage.xaml`

- Stil `DetailActionButton` (Zeilen 35–49): Primärschaltflächen der Seite („Preise aktualisieren", „Namen speichern" etc.), `MinimumTouchTarget`, `ColorPrimaryTeal`, Disabled-Trigger.
- Texte via `x:Static texts:DetailTexts.*`; `AutomationId`-Schema `Detail.*`.
- Adresse als `Label` (`Detail.Address`, Zeilen 143–147) — nicht interaktiv.
- Seitenende: `Refresh`-Schaltfläche (Zeilen 451–455) und `Detail.Attribution`-Label.
