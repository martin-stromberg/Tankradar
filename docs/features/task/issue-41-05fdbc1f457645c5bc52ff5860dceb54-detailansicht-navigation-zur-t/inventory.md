# Bestandsaufnahme: Navigation zur Tankstelle aus der Detailansicht

Analyse des Detailansichts-Bereichs (`StationDetailPage`, `StationDetailViewModel`, `StationDetailItem`, `StationDetailBuilder`) sowie des Dienst-Musters rund um `ILinkOpener`, bezogen auf die Anforderung, aus der Detailansicht heraus die Karten-App mit der Tankstelle als Navigationsziel zu starten.

## Zusammenfassung

- `StationInfo` liefert `Latitude`/`Longitude` als `double` (nicht nullable); `StationListItem` führt sie als `double?` mit abgeleiteter `HasPosition`-Eigenschaft — Stationen ohne gültige Position erscheinen nicht auf der Karte.
- `StationDetailItem` führt derzeit **keine** Positionsangaben — nur `AddressText`. Die Koordinaten müssen von `StationInfo`/`StationListItem` durchgereicht werden.
- `StationDetailViewModel.Show` baut die Anfangsanzeige direkt aus `StationListItem`; `RebuildDetail` nutzt `StationDetailBuilder.Build` mit `_station` (`StationInfo`).
- Ein Dienst-Muster für externe App-Aufrufe existiert: `ILinkOpener`/`LauncherLinkOpener` in `ILinkOpener.cs` — ein Interface plus Implementierung mit `testMode`-Flag, das im Testmodus nichts tut. Registrierung in `MauiProgram.cs` Zeile 109 mit `IsTestMode()`.
- `Microsoft.Maui.ApplicationModel.Map` (Essentials) wird bisher nirgends verwendet — ein neuer Dienst muss `Map.OpenAsync` kapseln.
- Die Detailansicht hat einen einheitlichen Schaltflächen-Stil `DetailActionButton`; UI-Texte liegen zentral in `DetailTexts`; `AutomationId`s folgen dem Schema `Detail.*`.
- Es gibt keine bestehende Funktion, die eine externe Karten-App öffnet; die integrierte `MapPage` ist ein eigener Bereich (Tile-Karte, keine Fremd-App).

Test-Ausgangszustand: `dotnet test src/Tankradar.Tests.Unit/Tankradar.Tests.Unit.csproj -c Debug --no-build` — 2062 Tests, alle bestanden, keine bekannten Fehlschläge (siehe [tests.md](inventory/tests.md)).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
