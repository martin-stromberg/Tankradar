namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ein Punkt des Verbrauchsverlaufs.
/// </summary>
/// <param name="OccurredAt">Der Zeitpunkt des Vorgangs (lokale Wanduhrzeit).</param>
/// <param name="Value">Der Verbrauch pro 100 km (L bzw. kWh).</param>
/// <param name="IsFromStart">Gibt an, ob der Wert ab dem Start-Kilometerstand berechnet wurde (Annahme: Tank beim Start voll).</param>
/// <returns>Der Wert.</returns>
public sealed record TrendPoint(DateTime OccurredAt, decimal Value, bool IsFromStart = false);
