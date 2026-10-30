# Bestandsaufnahme: Doppeltipp-Zentrierung und fließender Zwei-Finger-Zoom auf der Kartenansicht

Analysiert wurde der Kartenbereich (`StationMapView` und die Kartenmathematik `MapViewport`) im Stand von Branch `task/issue-19-karte-gesten-doppeltipp-pinch`, Commit `4fee2a30d6d394a888032515bf47147ec2949067`, bezogen auf die Anforderung [requirement.md](requirement.md).

## Zusammenfassung

- **Vorhanden:** Die Kartenmathematik ([`MapViewport`](inventory/logic.md)) hat `ZoomBy`, `ToLatLon`, `Create`, `Fit` und `PanByPixels`, die sich für Doppeltipp und Pinch wiederverwenden lassen. Das Mausziehen und Mausrad sind unter Windows umgesetzt ([`MapPointerTracker`](inventory/logic.md), `StationMapView.Pointer.cs`).
- **Vorhanden, aber für die Anforderung nicht passend:** `ZoomBy` behält die Kartenmitte bei; ein Zoom um einen Bildschirmpunkt (Fixpunkt) existiert nicht. Die Kneifgeste (`OnPinchUpdated`) zoomt erst bei `Completed` um eine ganze Stufe, mit festen Schwellen 1,3 / 0,77; eine fließende Skalierung der Kachelebene gibt es nicht.
- **Fehlt offensichtlich:** kein `TapGestureRecognizer` mit `NumberOfTapsRequired = 2` in `StationMapView.xaml`; keine Doppelklick-Erkennung im Windows-Teil; keine Methoden `ZoomAtPoint` / `ScaleAtPoint` (in der Anforderung nur als Vorschlag genannt); keine Umrechnung Faktor zu Zoomstufe.
- **Maßgebliche Semantik von `PinchGestureUpdatedEventArgs.Scale`:** Die XML-Dokumentation des installierten Pakets `Microsoft.Maui.Controls.Core` 10.0.120 beschreibt `Scale` als „relative scale of the pinch gesture **since the last update**. Default is 1.“ Das spricht für **inkrementelle** Werte. Der bestehende Code übernimmt aber jeden Wert mit `_pinchScale = e.Scale` (kein Multiplizieren). Bei inkrementeller Semantik sähe die Auswertung bei `Completed` nur den letzten Einzelschritt, nicht die Gesamtgeste. Das ist eine Ableitung aus der Doku, nicht durch einen Lauf auf Windows bestätigt. Details: [logic.md](inventory/logic.md#streitpunkt-scale-semantik).
- **Ungeklärt:** Die Einheit von `ScaleOrigin` ist in der Doku nicht angegeben (relativ oder absolut). Ob der `TapGestureRecognizer` unter Windows Mausklicks erhält, wenn `OnPointerPressed` `e.Handled = true` setzt, ist aus dem Code allein nicht entscheidbar.
- **Konflikt mit Markierungen:** Die Tankstellenmarkierungen sind `Button`-Elemente über der Gestenfläche. Ein Doppeltipp auf eine Markierung trifft daher zuerst den Button (siehe Offene Fragen der Anforderung, Punkt 1).
- **E2E-Einschränkung:** Die E2E-Quelltextregel (`E2EInputPolicyTests_NoFocus`) verbietet `.Click(` / `.DoubleClick(` / `Mouse.` in E2E-Quellen. Eine Doppelklick-Prüfung über Mausbedienung ist damit im E2E-Projekt nicht zulässig; vorhandene Tests bedienen die Karte nur über Schaltflächen und UI-Automation-Muster. Es gibt keinen E2E-Test für Geste (Doppeltipp oder Kneifen).
- **Testlücken:** Keine Unit-Tests für Fixpunkt-Zoom, Faktor-zu-Stufe-Umrechnung oder die Pinch-Auswertung. Bestehende Tests decken nur `ZoomBy` (mit Mittenerhalt und Grenzen) und Mausziehen/Mausrad ab.

### Test-Ausgangszustand vor der Umsetzung

- Build (Release) erfolgreich, 0 Warnungen, 0 Fehler.
- Unit: **2016 bestanden, 0 fehlgeschlagen, 0 übersprungen**, Exit-Code 0.
- Integration: **297 bestanden, 0 fehlgeschlagen, 0 übersprungen**, Exit-Code 0.
- E2E (173 Tests laut CI) lokal **nicht ausgeführt**; die letzte CI-Auswertung (PR #26) war grün. Kein lokaler Nachweis.
- Keine bekannten Testfehler im Ausgangszustand. Verbleibende Lücken und Nachweise: [tests.md](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md): Ereignisdaten, Pixel- und Kachelstrukturen
- [Logik](inventory/logic.md): `MapViewport`, `StationMapView` (Gesten, Zeiger, Ereignisse), `MapPointerTracker`
- [Enums](inventory/enums.md): `MapFitMode`
- [Interfaces](inventory/interfaces.md): `ITileSource`
- [Tests](inventory/tests.md): Test-Ausgangszustand, Testläufe, bestehende Testfälle, Testlücken
