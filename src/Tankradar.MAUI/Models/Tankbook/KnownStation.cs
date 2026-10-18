namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Eine bekannte Tankstelle zur Auswahl im Formular eines Tank- oder Ladevorgangs.
/// </summary>
/// <param name="StationId">Die Kennung einer Favoriten-Tankstelle; <see langword="null"/> bei einem bereits verwendeten, frei eingegebenen Namen. Wird nie angezeigt.</param>
/// <param name="Name">Der Name.</param>
/// <param name="Place">Der Ort; <see langword="null"/>, wenn keiner bekannt ist.</param>
/// <param name="Detail">Das Unterscheidungsmerkmal gleichnamiger Tankstellen: der Stammdatenname, sonst Straße und Hausnummer; <see langword="null"/>, wenn keines bekannt ist.</param>
/// <returns>Der Wert.</returns>
public sealed record KnownStation(string? StationId, string Name, string? Place, string? Detail = null);
