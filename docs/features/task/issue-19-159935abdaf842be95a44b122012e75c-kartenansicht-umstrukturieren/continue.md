# Offene Aufgaben

Erstellt am: 2026-10-30
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

- [ ] `StationMapView.Viewport` (öffentliche Leseeigenschaft vom Typ `MapViewport`) fehlt. Hinweis: Die Eigenschaft wurde in Durchlauf 2 auf Befund aus `review-code.1.md` als ungenutzt entfernt; der Ausschnitt erreicht das ViewModel über `ViewportChangedEventArgs`/`ViewportFitted`. Auflösung: Plan-Eintrag streichen (bevorzugt) oder Eigenschaft ergänzen (Tasks-Zeile 49).

## Code-Review-Befunde

- [ ] `MapViewModel.AreaReload.cs`: Zeitgeber `_areaReloadTimer` wird in `OnDisappearing` nicht freigegeben und tickt bis zu 60 s auf der unsichtbaren Seite weiter. `StopAreaReloadTimer()` einführen, in `OnDisappearing` aufrufen, in `OnAppearing` bei laufender Sperre neu starten; Unit-Test ergänzen.
- [ ] `MapViewModel.AreaReload.cs`: `AreaReloadRemainingSeconds` ist öffentlich, aber nirgends gebunden. In lokale Variable umwandeln; Tests prüfen `AreaReloadHint`.
- [ ] `MapViewModelTests_AreaReloadBlock.cs`: Weitergabe des Timer-Ticks über `SynchronizationContext.Post` ist nicht getestet (Tests verlassen sich auf xUnits `AsyncTestSyncContext`). Test-`SynchronizationContext` mit Warteschlange ergänzen und Test schreiben.

## Fehlgeschlagene Tests

- [ ] E2E-Suite (FlaUI) — Lauf ungültig: `Tankradar.MAUI.runtimeconfig.json` fehlte nach dem MAUI-Build im win-x64-Ausgabeordner, alle Tests scheiterten beim App-Start („hostpolicy.dll nicht gefunden“). E2E nach sauberem Neubau erneut ausführen (Reihenfolge: Unit/Integration bauen+testen → zuletzt `dotnet build src/Tankradar.MAUI -f net10.0-windows10.0.19041.0 -c Debug` → Existenz von runtimeconfig.json prüfen → `dotnet test src/Tankradar.Tests.E2E --no-build`). Unit 2012/2012 und Integration 297/297 waren grün; der letzte gültige E2E-Lauf (Implementierung Durchlauf 3) war 173/173.
