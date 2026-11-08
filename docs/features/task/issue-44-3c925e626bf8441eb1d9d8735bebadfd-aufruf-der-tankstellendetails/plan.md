# Umsetzungsplan — Aufruf der Tankstellendetails (Issue #44)

## Ziel

Das Antippen einer Tankstellenkachel in der Favoritenansicht öffnet die Detailansicht — in Favoritengruppen und im Bereich „In der Nähe“, auf allen Plattformen, wie bereits in der Kartenansicht.

## Ursache und Lösungsansatz

Der transparente Overlay-`Button` (`Opacity="0.01"`) ist auf iOS für `hitTest` unsichtbar (`alpha < 0.01` als `float`) und empfängt dort keine echten Berührungen. Die bewährte Lösung der Kartenansicht (`StationBottomSheet`): ein `TapGestureRecognizer` auf dem `Border` für echte Zeigereingabe, der Overlay-`Button` bleibt für Bedienhilfen und UI-Automation. Genau diese Kombination wird in `StationTile` (wieder)hergestellt und in `FavoriteGroupPage` ergänzt. Doppelte Auslösung (Geste und Schaltfläche) ist in allen ViewModels durch `_isOpeningStation` abgesichert.

## Scope-Klärung

- **Im Scope:** `StationTile` (deckt Favoritengruppen und „In der Nähe“ der Favoritenansicht ab).
- **Ebenfalls im Scope (gleiche Fehlerklasse):** `FavoriteGroupPage` — dieselbe Overlay-only-Konstruktion über dem Stationsnamen; die Gruppenansicht ist der unmittelbare Fortsetzungspfad der Favoritenbedienung und zeigt denselben Defekt auf iOS.
- **Scope-Ausschlüsse:** keine.

## Umsetzungsschritte

1. `src/Tankradar.MAUI/Views/Controls/StationTile.xaml`: `TapGestureRecognizer Command="{Binding OpenCommand}"` als `Border.GestureRecognizers` des `TileBorder` ergänzen (Stand vor `0506c99`); der Overlay-`Button` `OpenButton` bleibt unverändert (AutomationId `{Prefix}.Open`, Semantik).
2. `src/Tankradar.MAUI/Views/FavoriteGroupPage.xaml`: Im `Grid` des Stationsnamens (Zeile ~134) `Grid.GestureRecognizers` mit `TapGestureRecognizer Command="{Binding OpenCommand}"` ergänzen; der Overlay-`Button` (`FavoriteGroup.Entry.Open`) bleibt.
3. Struktureller Regressionstest `src/Tankradar.Tests.Unit/Unit/StationTileGestureTests.cs`: per `XDocument` prüfen, dass `StationTile.xaml` am `TileBorder` einen `TapGestureRecognizer` mit `Command="{Binding OpenCommand}"` trägt und dass `FavoriteGroupPage.xaml` im Namens-`Grid` ebenfalls einen `TapGestureRecognizer` mit derselben Bindung trägt — verhindert das Wiederentfernen der echten Zeigerbedienung (wie in `0506c99` geschehen, ohne dass die Invoke-E2E-Tests es bemerkten).
4. `changes.log`: Eintrag der Korrektur (Format der bisherigen Einträge).
5. Dokumentation: `docs/help/Startseite/` und `docs/help/Favoriten/` prüfen — das antippbare Verhalten ist bereits beschrieben (Kachel öffnet Details); nur ergänzen, falls es fehlt. `docs/RELEASE_NOTES.md` prüfen und pflegen.

## Tests je Akzeptanzkriterium

| Akzeptanzkriterium | Test |
|--------------------|------|
| Antippen der Kachel einer Favoritengruppe öffnet Detailansicht | E2E `HomeE2ETests_OpenDetail.OpenStationTile_ShowsDetail` (Invoke-Pfad, vorhanden) + Strukturtest (Zeiger-Pfad abgesichert) |
| Antippen einer Nähe-Kachel öffnet Detailansicht | E2E `HomeE2ETests_Nearby.OpenNearbyStation_ShowsDetail` (Invoke-Pfad, vorhanden) + Strukturtest |
| Bedienhilfen unverändert | Strukturtest: `OpenButton` mit `Command`, `SemanticProperties` bleiben; vorhandene E2E-Tests `NearbySwitch_ShowsSectionWithStationsByDistance`, `OpenStationTile_ShowsDetail` grün |
| Kein doppeltes Öffnen | Unit-Test `OpenStation_NavigatesWithStationAndGuardsDoubleOpen`, `Entries_OpenDetailAndOfferNoFavoriteActions` (vorhanden) |
| Negative: kein Eingriff in Scrollen/sonstige Gesten | E2E-Bestandstests der Startseite (`HomeE2ETests_*`) grün; Recognizer nur auf der Kachel, nicht auf Layout-Containern |

## Reale End-to-End-Abnahme

Die Auslösung echter Zeigereingabe ist im Off-Screen-Betrieb des E2E-Laufs nicht möglich; unter Windows wurde der echte Mausklick auf die Kachel im Vordergrundbetrieb ausprobiert (mit und ohne Recognizer) — ohne Recognizer funktioniert der Overlay-`Button` unter Windows bereits, die Plattformlücke betrifft iOS (`alpha < 0.01`-Grenze des `hitTest`). Die iOS-Abnahme erfolgt durch den Anwender im TestFlight-Build; die strukturelle Absicherung liegt im neuen Unit-Test.
