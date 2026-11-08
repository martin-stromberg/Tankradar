# Interfaces

## `ILinkOpener`
Datei: `src/Tankradar.MAUI/Services/Navigation/ILinkOpener.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `OpenAsync` | `Uri uri` | `Task` | Öffnet eine HTTPS-Adresse im Browser; nur auf Anwenderwunsch |

Referenzmuster für den neuen Dienst: Interface in derselben Datei wie die Implementierung, XML-Doku auf Deutsch, `testMode`-Konstruktorparameter, Registrierung als Singleton in `MauiProgram`.

## `IStationNavigator`
Datei: `src/Tankradar.MAUI/Services/Navigation/IStationNavigator.cs` (+ `ShellStationNavigator.cs`)

Definiert die In-App-Navigation zur Detailansicht (`StationParameter` als Schlüssel des Navigationsparameters). Nicht betroffen — Kontext: `StationListItem` gelangt als Objekt in die Detailansicht.

## `IFuelPriceService`
Datei: `src/Tankradar.MAUI/Services/Pricing/` (Interface, genutzt von `StationDetailViewModel`)

| Methode | Zweck |
|---------|-------|
| `GetStationDetailAsync(string, CancellationToken)` | Detailabfrage → `StationDetailResult` (enthält `StationInfo`) |
| `SetCustomNameAsync(string, string)` | Eigener Anzeigename speichern/entfernen → `StationNameResult` |

## `IConnectionMonitor`
Datei: `src/Tankradar.MAUI/Services/`

Liefert `IsOnline`, Events `ConnectionChanged`, `ConnectionRestored`. Für die Anforderung nicht relevant — der Karten-App-Start ist eine lokale Aktion ohne Netzwerkabhängigkeit.

## MAUI Essentials (extern, noch nicht referenziert)

`Microsoft.Maui.ApplicationModel.Map` (`Map.Default.OpenAsync`) mit `Location`, `MapLaunchOptions` (`NavigationMode.Driving`) bzw. `Placemark` für Adressen — wird bisher im Projekt nicht verwendet; ein neuer Dienst muss es kapseln. Hinweis: In Dateien mit `using Tankradar.MAUI.Services.Map` oder im gleichnamigen Namespace kann der Typname `Map` kollidieren — Alias (z. B. `using EssentialsMap = Microsoft.Maui.ApplicationModel.Map;`) empfohlen.
