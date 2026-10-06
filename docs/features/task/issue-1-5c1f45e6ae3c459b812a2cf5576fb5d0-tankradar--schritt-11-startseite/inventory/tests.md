# Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-06, Beginn 14:29:59 +0200 (Lauf endete gegen 14:37); Wiederholungslauf des Diagnosetests kurz danach
- Branch und Commit-ID: `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-11-startseite`, Commit `ab54fcf46ed875f9a8998fad3310680005b00040`
- Uncommittete Änderungen im getesteten Stand: Es gab keine Änderungen an Quellcode oder Tests (`git status`: nur das untracked Verzeichnis `docs/features/` mit den Planungsartefakten). Tests liefen gegen die unveränderten Quellen.
- Testumgebung und Runtime-/SDK-Versionen: Windows 11 Pro 10.0.26200, .NET SDK 10.0.401, Zielframework `net10.0-windows10.0.19041.0`, Konfiguration `Release`, Umgebungsvariablen `IncludeAndroidTarget=false`, `IncludeIosTarget=false`, `IncludeMacCatalystTarget=false` (Windows-Job-Simulation wie `scripts/local-ci.ps1`); E2E-Tests im Standardmodus der Testbasis (App außerhalb des Bildschirms); Mock-Server statt produktiver Endpunkte; der API-Schlüssel wurde nicht ausgelesen.
- Ermittelte Testsuiten und Quellen der Testbefehle: `src/Tankradar.Tests.Unit`, `src/Tankradar.Tests.Integration`, `src/Tankradar.Tests.E2E` (FlaUI); Befehle aus `scripts/local-ci.ps1` (Schritte 6 und 7) und `.github/workflows/pr-staging-ci.yml` (`dotnet test <Projekt> --configuration Release --no-build`). Abweichung zu `local-ci.ps1`: ohne Coverage-Sammlung (`coverlet.runsettings`) und ohne Format-/Sicherheitsprüfung, weil nur der Teststand ermittelt wird. Node- und Python-Skripttests (`npm test`, Git-Hook-Tests) gehören nicht zum Bereich und wurden nicht ausgeführt.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Build | `dotnet build Tankradar.sln --configuration Release` | Repository-Root | 0 | – | – | – | [build.log](test-results/build.log) |
| Unit | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build --logger "trx;LogFileName=unit.trx"` | Repository-Root | 0 | 693 | 0 | 0 | [unit.log](test-results/unit.log), [unit.trx](test-results/unit.trx) |
| Integration | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build --logger "trx;LogFileName=integration.trx"` | Repository-Root | 0 | 95 | 0 | 0 | [integration.log](test-results/integration.log), [integration.trx](test-results/integration.trx) |
| E2E (FlaUI) | `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build --logger "trx;LogFileName=e2e.trx"` | Repository-Root | 1 | 60 | 1 | 0 | [e2e.log](test-results/e2e.log), [e2e.trx](test-results/e2e.trx) |
| E2E-Wiederholung nur Diagnose-Test | `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build --filter "FullyQualifiedName~DiagnosticsCaptureE2ETests"` | Repository-Root | 1 | 1 | 1 | 0 | [e2e-rerun-diagnostics.log](test-results/e2e-rerun-diagnostics.log), [e2e-rerun-diagnostics.trx](test-results/e2e-rerun-diagnostics.trx) |

Die Läufe laufen über [run.sh](test-results/run.sh); Exit-Codes in [exits.txt](test-results/exits.txt). Der Wiederholungslauf ist getrennt dokumentiert und ersetzt den ursprünglichen Lauf nicht.

### Nachgewiesene bestehende Testfehler

| Test-ID inkl. Testfall | Suite / Dateipfad | Fehlerbild / Fehlermeldung | Lauf und Nachweis |
|-----------------------|------------------|---------------------------|-------------------|
| `Tankradar.Tests.E2E.E2E.FlaUI.DiagnosticsCaptureE2ETests.FailingTestBodyProducesScreenshotUiTreeAndErrorFile` | E2E, `src/Tankradar.Tests.E2E/E2E/FlaUI/DiagnosticsCaptureE2ETests.cs` (Zeile 59 `AssertScreenshotShowsWindowContent`, aufgerufen in Zeile 30) | „Der Screenshot zeigt keinen Fensterinhalt (einfarbig).“ Der Test prüft die Diagnoseerfassung (Screenshot, UI-Baum, Fehlerdatei) bei einem absichtlich fehlschlagenden Testkörper; der Screenshot der außerhalb des Bildschirms laufenden App war einfarbig. Der Fehler trat im ursprünglichen Lauf und im Wiederholungslauf identisch auf (vermutlich Umgebung: Sitzung/Anzeige der Entwicklungsmaschine; Ursache nicht untersucht) | E2E-Lauf [e2e.log](test-results/e2e.log) (Zeilen 4–9) und [e2e-rerun-diagnostics.log](test-results/e2e-rerun-diagnostics.log) |

Keine weiteren Fehler: alle übrigen 60 E2E-Tests, alle 693 Unit- und 95 Integrationstests bestanden. Ein Fehler darf nur dann als „preexisting“ gelten, wenn derselbe Testfall mit demselben Fehlerbild hier nachgewiesen ist (nur obiger Test).

### Testlücken und Ausführungsprobleme

- Keine übersprungenen oder abgebrochenen Tests; Build ohne Fehler.
- Nicht ausgeführt (außerhalb des Bereichs bzw. nicht erforderlich für den Ausgangszustand): Node-Skripttests (`npm test`), Python-Hook-Tests (`.githooks/test_*.py`, `scripts/test_validate_workflows.py`), `dotnet format`-Prüfung, Sicherheitsprüfung, iOS-Compile-Prüfung, Coverage-Messung. Aussagen zum Zustand dieser Prüfungen sind damit nicht möglich.
- E2E-Tests der Startseite, des Bereichs „In der Nähe“, der Einstellung „In der Nähe“ und des Öffnens der Detailansicht aus der Gruppenansicht fehlen (Funktion existiert noch nicht). Es gibt keine Tests für `Age` im Kachel-Cache, für `304` ohne Caching-Angaben und für gleichzeitiges Zuordnen derselben Tankstelle zur selben Gruppe (bisher nur sequentieller `AlreadyMember`-Test). Es gibt keinen Mock für `prices.php`.

## Testklassen

### Unit (`src/Tankradar.Tests.Unit/Unit`)

#### `FavoritesViewModelTests_Groups`
- `Load_WithoutGroups_ShowsEmptyHintOnlyAfterLoading` — Hinweis nur nach dem Laden
- `Load_ShowsGroupsAndOpensSelectedGroup` — Gruppenliste und Öffnen der Gruppenansicht
- `Create_AddsGroupAndClosesForm`, `Create_InvalidOrDuplicate_ShowsMessage`, `Load_StorageFailure_ShowsMessage`, `Create_StorageFailure_ShowsMessage` — Anlegen und Fehlerfälle

#### `FavoriteGroupViewModelTests_Group`
- `Load_ShowsGroupAndEntries`, `Load_EmptyOrMissingGroup`, `Load_WithoutGroup_DoesNothing`, `Edit_RenamesGroupAndChangesDescription`, `Edit_DuplicateName_ShowsMessageAndCancelKeepsName`, `Delete_RequiresConfirmation`, `Delete_StorageFailure_ShowsMessage`, `EditEntry_SavesNoteAndPriorityAndReorders`, `EditEntry_PreselectsStoredValuesAndCancelKeeps`, `EditEntry_TooLongNote_ShowsMessage`, `Load_StorageFailure_ShowsMessageWithoutLoggingNames` — Gruppenansicht; kein Test zum Öffnen der Detailansicht

#### `FavoritesChangeTests_Notification`
- `Service_RaisesChangedOnlyForSavedChanges`, `StationFavorites_FollowsRenameAndDeleteMadeElsewhere`, `FavoritesList_FollowsChangesAfterFirstLoad`, `Subscription_DoesNotKeepOwnerAlive` — Änderungsbenachrichtigung

#### `FavoritesServiceTests_Groups`, `_Entries`, `_Membership`
- Gruppenoperationen, Einträge, Zuordnung (`AddStation_MultipleGroups_AllowedDuplicateInGroupRejected` prüft sequentiell `AlreadyMember`), Anlegen und Hinzufügen, Entfernen

#### `StationDetailViewModelTests_Offline`
- `Offline_ShowsLastKnownDataWithAgeAndBanner`, `Offline_UnknownStation_KeepsListDataAndBanner`, `ConnectionRestored_RefreshesStaleDataAndClearsBanner`, `ConnectionRestored_FreshData_DoesNotQueryAgain`, `ConnectionRestored_StalePricesWhileOnline_QueriesAgain`, `ConnectionRestored_AfterDisappearing_IsIgnored`, `ConnectionLost_ShowsBannerImmediately` — Vorbild für das Offline- und Wiederverbindungsverhalten der Startseite

#### `FuelPriceServiceTests_Search`, `_Offline`, `_Detail`, `_DetailAge`, `_StorageFailure`
- Umkreissuche, Offline-Fallback (`SearchNearbyAsync_Offline_ReturnsLastKnownPricesWithAge`, `_AnyApiFailure_FallsBack`, `_AfterConnectionRestored_FetchesLiveAgain`, `CheckAvailabilityAsync_MapsOutcomes`), Detail, Speicherfehler; nichts zum Abruf nach Kennungen

#### `TankerkoenigClientTests_Parsing`, `_Retry`, `_EndpointNotConfigured`
- `SearchAsync_ParsesStationsAndPrices`, `SearchAsync_BuildsHttpsRequestWithInvariantNumbers`, `GetDetailAsync_ParsesOpeningTimesAndWholeDay`, `GetDetailAsync_InvalidId_ThrowsBeforeRequest`, `SearchAsync_BadBodies_AreClassified` — Parser; nichts zu `prices.php`

#### `HttpTileSourceTests_Fetch`, `_HttpCaching`, `_NoStoreAndExpires`
- `GetTile_MaxAge_OverridesFallbackLifetime`, `GetTile_WithoutCacheHeaders_UsesSevenDayFallback`, `GetTile_ExpiresHeader_IsHonored`, `GetTile_ExpiredWithETag_SendsConditionalRequestAndAccepts304` (304 mit eigenem `max-age`), `GetTile_ExpiredWithLastModified_SendsIfModifiedSince`, `GetTile_ExpiredAndChanged_ReplacesTile`, `GetTile_NoStoreAndNoCache_AreRespected`, `GetTile_RevalidationFails_UsesStaleTile`, `Evaluate_EdgeCases` (ruft `TileCachePolicy.Evaluate` mit fünf Argumenten), `GetTile_InvalidExpires_IsTreatedAsExpired`, `GetTile_NoStoreAfterEarlierCopy_RemovesStoredFiles` — Kein Test zu `Age` oder zu `304` ohne Caching-Angaben

#### `SettingsViewModelTests_Loading`, `_Changes`; `SettingsServiceTests_LoadSave`; `AppSettingsTests_Defaults`, `_Validation`; `SettingsEnumTests_Persistence`
- Laden/Speichern der Einstellungen und Enum-Persistenz; `SelectGps_SavesImmediately`, `ToggleFuel_SavesImmediately`, `MoveUp_ChangesOrderAndSaves` usw.

#### `MapViewModelTests_*`, `StationResultBuilderTests_*`, `StationDetailBuilderTests_*`, `StationHintsTests_Derivation`, `PriceFreshnessTests_Age`
- Such-/Kartenansicht, Filter und Sortierung, Aufbereitung von Preiszeilen, Alter, Hinweise (wiederverwendbare Bausteine für die Startseite)

#### `ViewModelTests_PageTitles`
- Seitentitel aller ViewModels (inkl. `FavoritesViewModel`)

### Integration (`src/Tankradar.Tests.Integration/Integration`)
- `FavoritesMigrationTests_Upgrade` — `Initialize_FromSchemaBeforeFavorites_KeepsExistingDataAndAddsTables`, `Favorites_SurviveRestartOnRealDatabaseFile`, `Database_RejectsDuplicateGroupNamesCaseInsensitive`, `Database_CascadesGroupDeleteAndProtectsStations`
- `PriceCacheMigrationTests_Upgrade`, `DatabaseInitializerTests_FirstStart`/`_Upgrade`, `MigrationsTests_ModelSync` (`Model_HasNoPendingChanges`)
- `SettingsServiceTests_RestartPersistence` — Round-Trip aller Enum-Werte durch eine Datei
- `PriceServiceMockServerTests_Flow` — `Search_AgainstMock_ReturnsStationsAndPrices`, `Detail_AgainstMock_DerivesAutomatedStationFromOpeningTimes`, `Search_TransientServerErrors_AreRetried`, `Search_SlowServer_TimesOutAndFallsBackToCache`, `Search_ServerGone_ReturnsLastKnownPrices`, `Search_KeyProblems_AreReportedWithoutThrowing`, `Search_ConnectionRestored_FetchesLiveAgain`
- `PriceRepositoryTests_*`, `SearchMockServerTests_*`, `SearchPrivacyTests_Logging`/`_Persistence`, `AddressSearchPrivacyTests_Persistence`, `TileSourceMockServerTests_Flow`, `ApiKeyBuildTests_*`

### E2E (`src/Tankradar.Tests.E2E/E2E/FlaUI`)
- `FavoritesE2ETests_Flow.SearchOpenDetailAddToNewGroupRenameAndRemove`; `FavoritesE2ETests_Groups`: `CreateGroupWithDescription_DeleteAsksForConfirmation`, `CreateGroup_DuplicateName_ShowsMessage`, `MultipleGroups_RemoveAsksWhichGroup`, `EntryNoteAndPriority_AreSavedAndShown`, `Favorites_SurviveApplicationRestart`
- `SettingsE2ETests_Defaults` (`FirstStart_ShowsDefaultValues`, `Settings_ProvideAllControls_WithoutElectricOptions`), `_FuelTypes`, `_Persistence` (`ChangedChoices_SurviveRestart`)
- `SearchE2ETests_*` (u. a. `_Offline`: `UnreachableService_ShowsLastKnownPricesWithAgeAndBanner`, `_GpsNever`: `GpsNever_ShowsHintAndSendsNoRequest`), `DetailE2ETests_Open`, `MapE2ETests_*`, `PriceServiceE2ETests_Reachable`/`_Unreachable`, `NavigationE2ETests`, `StartFailureCleanupE2ETests`, `DiagnosticsCaptureE2ETests`

## Hilfsmethoden

### `FavoritesFixture` (`Unit/Support`)
- Stationen `StationA`–`StationC` (Alpha, Beta, Citra mit Positionen), `Service`, `CreateService()`, `Clock` (`ManualTimeProvider`), SQLite im Speicher über `InMemoryDbContextFactory` (ohne Interceptor-Unterstützung)

### Fakes und Stubs (`Unit/Support`)
- `FakeFuelPriceService` (`Result`, `DetailResult`, `Queries`, `DetailRequests`, `DetailGate`), `FakeLocationService` (`Result`, `Calls`, `Gate`), `FakeConnectionMonitor` (`Change(bool)`), `FakeNetworkStatusSource`, `FakeSettingsService`, `FakeStationNavigator`, `FakeFavoriteGroupNavigator`, `StubTankerkoenigClient`, `FakeHttpMessageHandler`, `ManualTimeProvider`, `RecordingDelay`, `ListLogger`, `StationFactory`, `SearchTestData`

### `MockTankerkoenigServer` (`src/TestSupport`)
- `list.php` und `detail.php` für Alpha, Beta, Gamma, Delta, Epsilon und generierte Stationen (`AddGeneratedStations`), `EnqueueStatuses`, `ResponseDelay`, `ListRequests`, `DetailRequests`, `TotalRequests`, `LastRequest`, `AcceptedKey`

### E2E-Basisklassen
- `E2ETestBase` (`NavigateToTab`, `RestartApplication`, `RunWithDiagnostics`), `SettingsE2ETestBase`, `SearchE2ETestBase` (`CreateEnvironment`, `ClosedPortUrl`, `OpenSearch`, `Submit`, `FindAllOrdered`, `ReadTexts`, Mock-Server `Server`, `Geocoding`, `Tiles`, Testposition `52.5200,13.4050`), `FavoritesE2ETestBase` (`OpenStationDetail`, `Press`, `Type`, `WaitForTexts`, `AddToNewGroup`, `OpenFavorites`, `OpenGroup`, `SelectedNames`, `PressNamed`)
