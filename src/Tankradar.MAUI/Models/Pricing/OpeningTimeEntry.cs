namespace Tankradar.MAUI.Models.Pricing;

/// <summary>
/// Ein Öffnungszeitenabschnitt einer Tankstelle, wie ihn die Quelle liefert.
/// </summary>
/// <param name="Text">Bezeichnung des Abschnitts (z. B. „Mo-Fr“).</param>
/// <param name="Start">Beginn im Format HH:mm:ss.</param>
/// <param name="End">Ende im Format HH:mm:ss.</param>
/// <returns>Der Wert.</returns>
public sealed record OpeningTimeEntry(string Text, string Start, string End);
