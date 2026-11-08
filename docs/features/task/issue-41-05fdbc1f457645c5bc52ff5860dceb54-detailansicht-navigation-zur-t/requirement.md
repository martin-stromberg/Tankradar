# Übersetzte Anforderung: Navigation zur Tankstelle aus der Detailansicht

## Fachliche Zusammenfassung

Die Tankstellen-Detailansicht (`StationDetailPage`, `StationDetailViewModel`) wird um eine Schaltfläche „Navigation starten" erweitert, die auf ausdrücklichen Wunsch des Anwenders die installierte Karten-App des Geräts mit der Tankstelle als Ziel öffnet (Navigations-/Routenmodus, `NavigationMode.Driving`). Als Ziel werden primär die Koordinaten der Tankstelle verwendet; Stationen ohne gültige Position fallen auf die Adresse zurück bzw. blenden die Schaltfläche aus.

## Fachlicher Scope

- **Verarbeitungseinheit:** Eine angezeigte Tankstelle in der Detailansicht (`StationDetailItem` mit Position bzw. Adresse).
- **Auslösende Ereignisse:** Nutzeraktion — Antippen der Schaltfläche „Navigation starten" in der Detailansicht. Keine automatischen Auslöser (kein Start beim Laden, keine Hintergrundaktion).
- **Erwartete Wirkung:** Die Karten-App des Geräts öffnet sich mit der Tankstelle als Ziel, idealerweise direkt im Navigations-/Routenmodus (Auto). Der Anwender verlässt dabei die App — die Tankradar-App selbst zeigt keine Routenführung an.
- **Umgang mit fehlenden Objekten:**
  - Gültige Position (`Latitude`/`Longitude`) vorhanden → Ziel per Koordinaten übergeben.
  - Keine gültige Position, aber `AddressText` vorhanden → Ziel per Adresse an die Karten-App übergeben (Geocoding dort).
  - Weder Position noch Adresse → Schaltfläche wird ausgeblendet (kein wirkungsloses, scheinbar aktives Element).
- **Scope-Ausschlüsse:**
  - Keine eigene Routenführung/Turn-by-Turn-Navigation innerhalb der Tankradar-App (die bestehende integrierte Karte/`MapPage` bleibt davon unberührt).
  - Keine Auswahl der Karten-App durch den Anwender innerhalb der App (es gilt die Standard-Karten-App des Geräts).
  - Kein automatischer Start — nur auf ausdrücklichen Wunsch (konsistent zum Offline-first-/Privacy-Ansatz).
  - Keine Konfiguration des Navigationstyps (immer Auto/`NavigationMode.Driving`).

## Betroffene Klassen und Komponenten

- Datenmodell: `StationDetailItem` (`src/Tankradar.MAUI/Models/Search/StationDetailItem.cs`) — neue Positions-/Zielangaben.
- Logik: `StationDetailBuilder` (`src/Tankradar.MAUI/Services/Search/StationDetailBuilder.cs`) — Durchreichen der Koordinaten aus `StationInfo`.
- ViewModel: `StationDetailViewModel` (`src/Tankradar.MAUI/ViewModels/StationDetailViewModel.cs`) — neuer Befehl zum Starten der Navigation; Übergabe der Position beim Aufbau der Anfangsanzeige aus `StationListItem`.
- Interface/Dienst: neuer Dienst analog zu `ILinkOpener`/`LauncherLinkOpener` (`src/Tankradar.MAUI/Services/Navigation/`) — kapselt `Map.OpenAsync` aus .NET MAUI Essentials; im Testmodus wird nie eine externe App gestartet.
- UI: `StationDetailPage.xaml` — neue Schaltfläche (Stil `DetailActionButton`, Texte aus `DetailTexts`).
- DI: `MauiProgram.cs` — Registrierung des neuen Dienstes analog zu `ILinkOpener` (Zeile 109).
- Tests: `src/Tankradar.Tests.Unit/` — neue Testklassen für den Karten-Start-Dienst, die Erweiterung von `StationDetailItem`/`StationDetailBuilder` und den neuen Befehl im `StationDetailViewModel`.

## Implementierungsansatz

- Neuer Dienst (z. B. `IMapLauncher`/`EssentialsMapLauncher`) im Verzeichnis `src/Tankradar.MAUI/Services/Navigation/`, der `Microsoft.Maui.ApplicationModel.Map.OpenAsync` mit `NavigationMode.Driving` kapselt. Konstruktorparameter `testMode` analog zu `LauncherLinkOpener`: im Testmodus geschieht nichts.
- `StationDetailItem` erhält die Zielposition (`Latitude`/`Longitude` als `double?` wie in `StationListItem` bzw. abgeleitete `HasPosition`-Angabe); `StationDetailBuilder.Build` füllt sie aus `StationInfo.Latitude`/`StationInfo.Longitude`; `StationDetailViewModel.Show` übernimmt sie aus `StationListItem`.
- `StationDetailViewModel` erhält einen `ICommand` (z. B. `NavigateCommand`), der den Dienst aufruft; Sichtbarkeit der Schaltfläche über eine `CanNavigate`-/`HasNavigationTarget`-Eigenschaft (Position oder Adresse erforderlich).
- `StationDetailPage.xaml`: Schaltfläche „Navigation starten" mit Stil `DetailActionButton`, Text als Konstante in `DetailTexts`, `AutomationId` nach bestehendem Schema (`Detail.*`).
- Registrierung in `MauiProgram.cs` analog zur bestehenden `ILinkOpener`-Registrierung mit `IsTestMode()`.

## Konfiguration

Keine Konfiguration erforderlich. Der Testmodus folgt dem bestehenden Mechanismus (`TestDataPaths.TestDataPathEnvironmentVariable` via `IsTestMode()`).

## Offene Fragen

Keine — Position als primäres Ziel mit Adress-Fallback sowie Ausblenden bei fehlendem Ziel sind in der Anforderung vorgegeben; die Benennung des Dienstes und der Schaltfläche folgt den Projektkonventionen.
