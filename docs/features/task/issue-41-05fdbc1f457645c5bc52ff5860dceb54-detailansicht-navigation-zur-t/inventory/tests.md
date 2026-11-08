# Tests — Bestand und Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-11-09, lokale Zeit UTC+1 (Windows)
- Branch und Commit-ID: `task/issue-41-05fdbc1f457645c5bc52ff5860dceb54-detailansicht-navigation-zur-t` @ `d8686c10a0cb66c52adb99e270ee809ee9615d83`
- Uncommittete Änderungen im getesteten Stand: nur neue Dokumentationsartefakte unter `docs/features/...` (requirement.md, inventory/*, todo.md) — kein Produktivcode geändert
- Testumgebung und Runtime-/SDK-Versionen: .NET SDK 10.0.403, Windows
- Ermittelte Testsuiten und Quellen der Testbefehle: `src/Tankradar.Tests.Unit` (xUnit), `src/Tankradar.Tests.Integration`, `src/Tankradar.Tests.E2E` (FlaUI) — Testbefehle aus `.github/workflows/pr-staging-ci.yml` (Zeilen 146–175) und Aufgaben-Vorgabe; nur das Unit-Testprojekt ist lokal ausführbar (Integration/E2E benötigen gesonderte Umgebung; die Solution baut lokal nicht vollständig wegen fehlender Android/iOS-Workloads — Vorgabe: nur Unit-Testprojekt bauen/testen).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test src/Tankradar.Tests.Unit/Tankradar.Tests.Unit.csproj -c Debug --no-build` (nach `dotnet build` desselben Projekts, 0 Fehler) | Repo-Root | 0 | 2062 | 0 | unbekannt | [baseline-unit-tests.txt](test-results/baseline-unit-tests.txt) |

### Nachgewiesene bestehende Testfehler

Keine — alle 2062 Tests des Ausgangslaufs bestanden.

### Testlücken und Ausführungsprobleme

- `src/Tankradar.Tests.Integration` und `src/Tankradar.Tests.E2E` wurden im Baseline-Lauf nicht ausgeführt (Vorgabe der Umgebung: nur Unit-Testprojekt; E2E benötigt gestartete App via FlaUI, Integrationstests eigene Infrastruktur). Über deren Zustand wird keine Aussage getroffen.
- Für den neuen Karten-Start existiert noch kein Test — der Bereich ist ungetestet.

## Testklassen

### `LauncherLinkOpenerTests`
Datei: `src/Tankradar.Tests.Unit/Unit/LauncherLinkOpenerTests.cs`

- `OpenAsync_TestModeOrNonHttps_DoesNothing` — Testmodus und Nicht-HTTPS öffnen nichts; `null` wirft `ArgumentNullException`. Muster für Tests des neuen Dienstes.

### `StationDetailViewModelTestBase`
Datei: `src/Tankradar.Tests.Unit/Unit/StationDetailViewModelTestBase.cs`

Gemeinsamer Aufbau aller `StationDetailViewModel`-Tests: Fakes `FakeSettingsService`, `FakeFuelPriceService`, `FakeConnectionMonitor`, `ManualTimeProvider`, `RecordingLogger`, `FavoritesFixture`; `CreateStation`, `CreateOrigin` (über `StationResultBuilder.Build`), `AppearAsync`. Neue ViewModel-Tests erweitern diesen Aufbau bzw. nutzen ihn.

### `StationDetailBuilderTests_*` (Auswahl)
Dateien: `src/Tankradar.Tests.Unit/Unit/StationDetailBuilderTests_Content.cs`, `_Design.cs`, `_DisplayName.cs`, `_MissingData.cs`

- `Build_ShowsNameBrandAddressDistanceAndStatus` — prüft aufbereitete Felder inkl. `AddressText`.
- `Build_WholeDayWithRecentDetails_ShowsAutomatedHint` u. a. — Muster: `StationInfo` aufbauen, `Build` aufrufen, `StationDetailItem` prüfen.

### Weitere `StationDetailViewModelTests_*` (Auswahl)
Dateien: `src/Tankradar.Tests.Unit/Unit/StationDetailViewModelTests_Loading.cs`, `_Errors.cs`, `_Offline.cs`, `_Refresh.cs`, `_Concurrency.cs`, `_Favorites.cs`, `_CustomName.cs`, `_RouteDistance.cs`

Themengruppen-Konvention `{Klasse}Tests_{Thema}` — neue Tests für den Navigationsbefehl passen in eine neue Gruppe `StationDetailViewModelTests_Navigation` (Name `TankbookViewModelTests_Navigation` existiert bereits für ein anderes ViewModel — Kollisionsfrei durch eigenen Klassennamen).

## Hilfsmethoden

### `SearchTestData` (`src/Tankradar.Tests.Unit/Unit/Support/`)
- `AllFuels`, `Now` — Testdaten für Sorten/Zeitpunkt; wird von den Builder-Tests genutzt.

### `FavoritesFixture` (`src/Tankradar.Tests.Unit/Unit/Support/`)
- `CreateService()` — Favoritendienst mit Testdatenbank für den `StationFavoritesViewModel`-Aufbau.

### Fake-Dienste (`src/Tankradar.Tests.Unit/Unit/Support/` u. `src/TestSupport/`)
- `FakeSettingsService`, `FakeFuelPriceService`, `FakeConnectionMonitor`, `ManualTimeProvider`, `RecordingLogger<T>` — für einen neuen `IMapLauncher`-Dienst ist ein analoger Fake/Aufzeichnungsfake nötig.
