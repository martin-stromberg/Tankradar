# Umsetzungsplan: Navigation zur Tankstelle aus der Detailansicht

## Übersicht

Die Detailansicht erhält eine Schaltfläche „Navigation starten", die auf Anwenderwunsch die Karten-App des Geräts mit der Tankstelle als Ziel öffnet (`Map.OpenAsync` mit `NavigationMode.Driving`). Dazu werden die Koordinaten von `StationInfo`/`StationListItem` über `StationDetailBuilder` bzw. `StationDetailViewModel.Show` bis ins `StationDetailItem` durchgereicht; ein neuer Dienst `IMapOpener`/`EssentialsMapOpener` kapselt den externen Aufruf (Testmodus: keine Aktion). Fallback ohne Position: die Adresse; ohne beides wird die Schaltfläche ausgeblendet.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Eine angezeigte Tankstelle in der Detailansicht (`StationDetailItem`) |
| Auslösende Ereignisse | Antippen der Schaltfläche „Navigation starten" (nur Nutzeraktion) |
| Erwartete Wirkung | Karten-App des Geräts öffnet sich mit der Tankstelle als Ziel im Routenmodus (Auto) |
| Umgang mit fehlenden Objekten | Position vorhanden → Koordinaten; keine Position, aber Adresse → Adresse; weder noch → Schaltfläche ausgeblendet |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine eigene Routenführung in der App; die integrierte `MapPage` bleibt unverändert | Anforderung verlangt Fremd-App-Start | offen |
| 2 | Keine Auswahl der Karten-App (immer Standard-App des Geräts) | `Map.OpenAsync` nutzt die System-Maps-App | offen |
| 3 | Kein konfigurierbares Transportmittel (immer `NavigationMode.Driving`) | Anforderung nennt nur „Navigations-/Routenmodus"; Zielgruppe ist Autofahrer | offen |
| 4 | Die Adresse ist nicht als tippbares Element umgesetzt — nur die Schaltfläche | Anforderung nennt die Schaltfläche als Beispiel und lässt die Umsetzung frei | offen |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Karten-Start-Dienst | `IMapOpener` + `EssentialsMapOpener` in einer Datei `IMapOpener.cs` unter `Services/Navigation/`, `testMode`-Flag im Konstruktor | Deckt sich mit dem Muster `ILinkOpener`/`LauncherLinkOpener` (Interface und Implementierung in einer Datei, Singleton-Registrierung mit `IsTestMode()`) |
| Position im Detailmodell | `StationDetailItem` erhält `GeoPosition? Position` (statt zwei `double?`-Felder) | `GeoPosition` existiert bereits (`Models/Search`), garantiert Wertebereiche und vermeidet ungültige Zustände |
| Zentrale Positionsprüfung | `StationInfo` und `StationListItem` erhalten je eine abgeleitete `Position`-Eigenschaft (`GeoPosition?`); `StationResultBuilder` und `RouteStationSearchService` werden darauf umgestellt | Die Regel „`GeoPosition.TryCreate` und nicht (0,0)" ist derzeit zweimal dupliziert; die Eigenschaft zentralisiert sie und dient direkt als Quelle für `StationDetailItem.Position` |
| Zielart im Interface | Zwei Methoden: `OpenAsync(GeoPosition, string)` und `OpenAsync(string address, string)` | Spiegelt die beiden Übergabewege von `Map.OpenAsync` (Koordinaten via `Location`, Adresse via `Placemark`) ohne eigenes Zielmodell |
| Schaltflächenposition | Eigene Schaltfläche direkt unter der Station-Karte (vor der Überschrift „Kraftstoffe"), Stil `DetailActionButton` | Gut erreichbar und prominent; Binding direkt am ViewModel möglich (innerhalb der Karte wäre der `BindingContext` das `StationDetailItem`) |
| Fehlerfall beim Öffnen | `StatusMessage` mit neuem Text `DetailTexts.NavigationFailed` | Vorhandene Meldungs-Infrastruktur der Seite (`Detail.StatusMessage`) wird wiederverwendet; stilles Fehlschlagen wäre eine wirkungslose Aktion |
| Name-Kollision `Map` | Alias `using EssentialsMap = Microsoft.Maui.ApplicationModel.Map;` in `IMapOpener.cs` | Der Namespace `Tankradar.MAUI.Services.Map` würde `Map` sonst als Namespace auflösen |

## Programmabläufe

### Navigation starten (Happy Path, mit Position)

1. Anwender tippt „Navigation starten" (`Detail.Navigate`) in der Detailansicht.
2. `StationDetailViewModel.NavigateCommand` setzt `LastNavigationTask = NavigateAsync()`.
3. `NavigateAsync` liest `Detail`; `Detail.Position` ist gesetzt → `_mapOpener.OpenAsync(position, Detail.Name)`.
4. `EssentialsMapOpener.OpenAsync` ruft `EssentialsMap.Default.OpenAsync(new Location(...), new MapLaunchOptions { Name = name, NavigationMode = NavigationMode.Driving })` (außer Testmodus).
5. Die Karten-App öffnet sich mit der Tankstelle als Ziel im Routenmodus.

Beteiligte Klassen/Komponenten: `StationDetailViewModel`, `IMapOpener`, `EssentialsMapOpener`, `StationDetailItem`, `GeoPosition`.

### Navigation starten (Fallback, nur Adresse)

1.–2. wie Happy Path.
3. `Detail.Position` ist `null`, `Detail.HasAddress` ist `true` → `_mapOpener.OpenAsync(Detail.AddressText, Detail.Name)`.
4. `EssentialsMapOpener` öffnet die Karten-App mit einem `Placemark`, dessen `Thoroughfare` die formatierte Adresszeile trägt (Geocoding in der Karten-App).

Beteiligte Klassen/Komponenten: `StationDetailViewModel`, `IMapOpener`, `EssentialsMapOpener`.

### Fehler beim Öffnen / kein Ziel

1. `CanNavigate` ist `false`, wenn `Detail` weder `Position` noch `HasAddress` liefert → die Schaltfläche ist nicht sichtbar.
2. Wirft der Betriebssystem-Aufruf (z. B. keine Karten-App installiert) → `NavigateAsync` protokolliert den Ausnahmetyp und setzt `StatusMessage = DetailTexts.NavigationFailed`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `IMapOpener` | Interface (in `IMapOpener.cs`) | Vertrag zum Öffnen der Karten-App mit Koordinaten- oder Adressziel |
| `EssentialsMapOpener` | Klasse (in `IMapOpener.cs`) | Implementierung über `Microsoft.Maui.ApplicationModel.Map`; im Testmodus ohne Aktion |
| `FakeMapOpener` | Test-Hilfsklasse (`src/Tankradar.Tests.Unit/Unit/Support/FakeMapOpener.cs`) | Zeichnet `OpenAsync`-Aufrufe auf (Zielart, Position/Adresse, Name) |

## Änderungen an bestehenden Klassen

### `StationInfo` (Datenmodellklasse)

- **Neue Eigenschaft:** `Position` (`GeoPosition?`, berechnet) — `GeoPosition.TryCreate(Latitude, Longitude, out var p) && !(0,0)` ? `p` : `null`. Zentralisiert die bisher private Regel `StationResultBuilder.HasValidPosition`.

### `StationListItem` (Datenmodellklasse, Record)

- **Neue Eigenschaft:** `Position` (`GeoPosition?`, berechnet) — `HasPosition ? GeoPosition.TryCreate(Latitude.Value, Longitude.Value, out var p) ? p : null : null` (vereinfacht: `TryCreate` über den bereits geprüften Werten).

### `StationDetailItem` (Datenmodellklasse, Record)

- **Neue Positionsparameter:** `GeoPosition? Position = null` — als letzter optionaler Parameter (nach `CustomName`).
- **Neue Eigenschaft:** `HasPosition` (`bool`) — `Position is not null`.

### `StationResultBuilder` (Logik)

- **Geänderte Methode:** `CreateItem` — `Latitude`/`Longitude` werden aus `station.Position` gefüllt; private Methode `HasValidPosition` entfällt (Regel liegt in `StationInfo.Position`).

### `RouteStationSearchService` (Logik)

- **Geänderte Stelle:** Positionsprüfung (Zeile ~106) — `station.Position is not null` statt dupliziertem `GeoPosition.TryCreate` + (0,0)-Vergleich.

### `StationDetailBuilder` (Logik)

- **Geänderte Methode:** `Build` — übergibt `current.Position` an den neuen `StationDetailItem`-Parameter.

### `StationDetailViewModel` (ViewModel)

- **Neuer Konstruktorparameter:** `IMapOpener mapOpener` — nach `connection`.
- **Neue Eigenschaften:** `CanNavigate` (`bool`, `Detail is { } d && (d.HasPosition || d.HasAddress)`), `LastNavigationTask` (`Task`, für Tests/Abwarten, wie `LastLoadTask`).
- **Neuer Befehl:** `NavigateCommand` (`ICommand`) — `new Command(() => LastNavigationTask = NavigateAsync())`.
- **Neue Methode:** `NavigateAsync` (`private`) — Position → Koordinatenaufruf, sonst Adresse → Adressaufruf; Fehler → LogWarning (nur Ausnahmetyp) + `StatusMessage = DetailTexts.NavigationFailed`.
- **Geänderte Stelle:** `Detail`-Setter — zusätzlich `OnPropertyChanged(nameof(CanNavigate))`.
- **Geänderte Methode:** `Show(StationListItem)` — übergibt `origin.Position` an den neuen `StationDetailItem`-Parameter.

### `DetailTexts` (Textklasse)

- **Neue Konstanten:** `Navigate = "Navigation starten"`, `NavigationFailed = "Die Karten-App konnte nicht geöffnet werden."`

### `StationDetailPage` (XAML)

- **Neues Element:** `Button` „Navigation starten" (`{x:Static texts:DetailTexts.Navigate}`), `Command="{Binding NavigateCommand}"`, `IsVisible="{Binding CanNavigate}"`, `Style="{StaticResource DetailActionButton}"`, `AutomationId="Detail.Navigate"` — eingefügt zwischen der Station-Karte (`Border` mit `Detail.Address`/Info-Box) und der Überschrift „Kraftstoffe".

### `MauiProgram` (DI)

- **Neue Registrierung:** `builder.Services.AddSingleton<IMapOpener>(_ => new EssentialsMapOpener(IsTestMode()));` — direkt neben der `ILinkOpener`-Registrierung (Zeile 109).

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `StationInfo.Position` / `StationListItem.Position` | Wertebereich via `GeoPosition.TryCreate`, zusätzlich (0,0) ungültig | `Position` ist `null` → Adress-Fallback bzw. Ausblenden |
| `EssentialsMapOpener.OpenAsync(string address, …)` | Leere/Whitespace-Adresse | Rückkehr ohne Aktion (nie ein sinnloser App-Start) |
| `EssentialsMapOpener.OpenAsync(…)` | `null`-Argumente | `ArgumentNullException` (Konvention aus `LauncherLinkOpener`) |

## Konfigurationsänderungen

Keine (der Testmodus nutzt den bestehenden `IsTestMode()`-Mechanismus).

## Seiteneffekte und Risiken

- **`StationInfo.Position` / `StationListItem.Position`:** neue berechnete Eigenschaft — `StationResultBuilder` und `RouteStationSearchService` werden auf sie umgestellt; das Verhalten bleibt identisch (gleiche Regel), bestehende Tests bleiben grün.
- **`StationDetailItem`-Signatur:** neuer optionaler Parameter am Ende — beide Erzeugstellen (`StationDetailBuilder.Build`, `StationDetailViewModel.Show`) werden angepasst; `with`-Ausdrücke (`detail with { DistanceText = … }`) bleiben kompatibel.
- **`StationDetailViewModel`-Konstruktor:** neuer Parameter `IMapOpener` — DI-Registrierung und `StationDetailViewModelTestBase` müssen angepasst werden; alle bestehenden ViewModel-Tests betroffen (Kompilieren).
- **Testmodus:** `EssentialsMapOpener` startet im Testmodus keine Fremd-App — E2E-Tests können die Schaltfläche antippen, ohne dass eine Karten-App aufgeht.

## Umsetzungsreihenfolge

1. **`StationInfo.Position` ergänzen und `StationResultBuilder`/`RouteStationSearchService` umstellen**
   - Voraussetzungen: `GeoPosition` vorhanden (`Models/Search/GeoPosition.cs`)
   - Beschreibung: berechnete `Position`-Eigenschaft; beide Prüfstellen auf `Position` umstellen.

2. **`StationListItem.Position` ergänzen**
   - Voraussetzungen: `GeoPosition` vorhanden
   - Beschreibung: berechnete `Position`-Eigenschaft aus `Latitude`/`Longitude`.

3. **`StationDetailItem` erweitern (`Position`, `HasPosition`)**
   - Voraussetzungen: `GeoPosition` vorhanden
   - Beschreibung: optionaler Parameter + abgeleitete Eigenschaft.

4. **`StationDetailBuilder.Build` anpassen**
   - Voraussetzungen: Schritte 1 und 3
   - Beschreibung: `current.Position` durchreichen.

5. **`IMapOpener`/`EssentialsMapOpener` anlegen**
   - Voraussetzungen: `GeoPosition` vorhanden; .NET-MAUI-Essentials (`Map`, `Location`, `Placemark`, `MapLaunchOptions`, `NavigationMode`) im Projekt enthalten
   - Beschreibung: Interface + Implementierung in `Services/Navigation/IMapOpener.cs`; Alias für `Map` wegen Namenskonflikt.

6. **`DetailTexts` erweitern**
   - Voraussetzungen: keine
   - Beschreibung: `Navigate`, `NavigationFailed`.

7. **`StationDetailViewModel` erweitern (`IMapOpener`, `NavigateCommand`, `CanNavigate`, `NavigateAsync`, `LastNavigationTask`)**
   - Voraussetzungen: Schritte 2, 3, 5, 6
   - Beschreibung: Konstruktorparameter, Befehl, Meldung bei Fehler, `Show` übergibt `origin.Position`, `Detail`-Setter meldet `CanNavigate`.

8. **`StationDetailPage.xaml`: Schaltfläche einfügen**
   - Voraussetzungen: Schritte 6 und 7
   - Beschreibung: `Detail.Navigate`-Button zwischen Station-Karte und „Kraftstoffe".

9. **`MauiProgram`: `IMapOpener` registrieren**
   - Voraussetzungen: Schritt 5
   - Beschreibung: Singleton neben `ILinkOpener`.

10. **Test-Hilfsklasse `FakeMapOpener` + `StationDetailViewModelTestBase` anpassen**
    - Voraussetzungen: Schritte 5 und 7
    - Beschreibung: Fake anlegen, `ViewModel`-Konstruktion um das Fake ergänzen.

11. **Unit-Tests schreiben**
    - Voraussetzungen: Schritte 1–10
    - Beschreibung: neue Testklassen gemäß Abschnitt „Tests".

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `EssentialsMapOpenerTests` | `src/Tankradar.Tests.Unit/Unit/EssentialsMapOpenerTests.cs` | Testmodus und leere Adresse starten nie eine App (kein Fehler, kein Start); `null`-Argumente werfen `ArgumentNullException` |
| `StationDetailItemTests_Navigation` | `src/Tankradar.Tests.Unit/Unit/StationDetailItemTests_Navigation.cs` | `HasPosition` aus `Position`; `StationDetailBuilder.Build` setzt `Position` bei gültigen Koordinaten und `null` bei (0,0)/ungültigen |
| `StationDetailViewModelTests_Navigation` | `src/Tankradar.Tests.Unit/Unit/StationDetailViewModelTests_Navigation.cs` | `CanNavigate` mit Position/nur Adresse/ohne beides; `NavigateCommand` ruft `FakeMapOpener` mit Koordinaten bzw. Adresse; Fehler → `StatusMessage`; Testmodus-Konformität (Fake statt echtem Start) |
| `StationInfoTests_Position` (oder Erweiterung bestehender Gruppe) | `src/Tankradar.Tests.Unit/Unit/StationInfoTests_Position.cs` | `Position` bei gültigen/ungültigen/(0,0)-Koordinaten |
| `FakeMapOpener` | `src/Tankradar.Tests.Unit/Unit/Support/FakeMapOpener.cs` | Aufzeichnender `IMapOpener`-Fake (Aufrufliste, Zielart) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `StationDetailViewModelTestBase` (alle `StationDetailViewModelTests_*` indirekt) | Neuer Konstruktorparameter `IMapOpener` → `FakeMapOpener` im Aufbau |
| Tests, die `StationInfo` mit Standard-Koordinaten 0/0 erzeugen und `StationListItem.Latitude`/`Longitude` erwarten (z. B. `StationResultBuilderTests_*`, `CreateOrigin` im TestBase) | Keine Verhaltensänderung — `CreateOrigin` erzeugt Stationen ohne Koordinaten (0/0 → `Position` bleibt `null`, wie bisher `Latitude`/`Longitude` `null` waren); nur Kompilieren sicherstellen |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Detailansicht zeigt „Navigation starten" bedienbar und der Klick stürzt nicht ab (Testmodus: kein Fremd-App-Start) | `src/Tankradar.Tests.E2E/E2E/FlaUI/DetailE2ETests_Navigate.cs` (neu) | Schaltfläche sichtbar/aktiv, tippbar; App bleibt auf der Detailseite | Nachweis, dass die Schaltfläche in der echten UI erreichbar und angebunden ist |
| Pflicht | Station ohne Position und ohne Adresse zeigt die Schaltfläche nicht | `DetailE2ETests_Navigate.cs` | Ausblendregel | Sichtbarkeitsregel ist anwendersichtbar |

Welche bestehenden E2E-Tests müssen angepasst werden? Keine — `DetailE2ETests_Open` bleibt unverändert.

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path | Antippen von „Navigation starten" | Detailansicht geöffnet, Tankstelle mit gültiger Position | Button → `NavigateAsync` → `EssentialsMapOpener` → `Map.OpenAsync` (Driving) | Karten-App öffnet sich mit Tankstelle als Ziel im Routenmodus | E2E (Button vorhanden/bedienbar) + Unit (Aufruf mit Koordinaten) + manuelle Abnahme |
| 2 | Adress-Fallback | Antippen von „Navigation starten" | Tankstelle ohne gültige Position, aber mit `AddressText` | Button → `OpenAsync(address, name)` | Karten-App öffnet sich mit Adresssuche | Unit-Test (`FakeMapOpener` mit Adresse) |
| 3 | Kein Ziel | – | Tankstelle ohne Position und ohne Adresse | `CanNavigate == false` | Schaltfläche nicht sichtbar; keine wirkungslose Aktion | Unit-Test + E2E-Sichtbarkeit |
| 4 | Fehler beim Start | Antippen, OS wirft | Detailansicht geöffnet | `NavigateAsync` fängt Ausnahme | `Detail.StatusMessage` zeigt „Die Karten-App konnte nicht geöffnet werden." | Unit-Test (Fake wirft) |
| 5 | Testmodus | Antippen im Testmodus | `TestDataPathEnvironmentVariable` gesetzt | `EssentialsMapOpener` kehrt sofort zurück | Keine Fremd-App wird gestartet | Unit-Test `EssentialsMapOpenerTests` |

## Offene Punkte

Keine.
