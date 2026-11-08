# Anforderung

**Quelle:** GitHub Issue #44 „Aufruf der Tankstellendetails“ (martin-stromberg/Tankradar)

## Kundenanforderung (Original)

> In der Favoritenansicht lässt sich die Detailansicht der dort gelisteten Tankstellen nicht aufrufen. Das gilt für Tankstellen aus den Favoritengruppen und auch für die nahegelegenen.
>
> Anforderung:
> Tippt der Anwender auf ein Tankstellenpanel, so soll die Detailansicht dieser Tankstelle geöffnet werden, so wird das auch in der Kartenansicht der Fall ist.

## Übersetzte Anforderung

In der Startansicht „Favoriten“ (`FavoritesPage`) werden Tankstellen als Kacheln (`StationTile`) dargestellt — sowohl in den Favoritengruppen als auch im Bereich „In der Nähe“. Das Antippen bzw. Anklicken einer solchen Kachel öffnet derzeit nicht die Detailansicht der Tankstelle.

**Soll-Verhalten:**

- Tippt/klickt der Anwender auf eine Tankstellenkachel in einer Favoritengruppe, öffnet sich die Detailansicht (`stationdetail`) genau dieser Tankstelle.
- Tippt/klickt der Anwender auf eine Tankstellenkachel im Bereich „In der Nähe“, öffnet sich ebenfalls die Detailansicht dieser Tankstelle.
- Das Verhalten entspricht dem der Kartenansicht (`StationBottomSheet`-Trefferliste), wo das Antippen eines Trefferpanels die Detailansicht öffnet.
- Die Bedienung über Bedienhilfen (Sprachausgabe/UI-Automation) bleibt erhalten.

## Akzeptanzkriterien

1. Ein Antippen (echte Zeigereingabe, nicht nur UI-Automation-Invoke) einer Tankstellenkachel in einer Favoritengruppe öffnet die Detailansicht mit Name und Preisen dieser Tankstelle.
2. Ein Antippen einer Tankstellenkachel im Bereich „In der Nähe“ öffnet die Detailansicht dieser Tankstelle.
3. Die AutomationIds und Bedienhilfen-Eigenschaften der Kacheln bleiben unverändert (`{Prefix}.Open`, `SemanticProperties`).
4. Ein versehentliches doppeltes Öffnen (Kachel und Schaltfläche melden dasselbe Antippen) führt zu genau einem Navigationsvorgang.
