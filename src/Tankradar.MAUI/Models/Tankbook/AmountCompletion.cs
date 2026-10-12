namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ergebnis der Ergänzung von Menge, Preis pro Einheit und Gesamtbetrag: Aus zwei Werten ergibt sich der dritte; bei drei Werten wird die Abweichung geprüft.
/// </summary>
/// <param name="Quantity">Die Menge (angegeben oder berechnet).</param>
/// <param name="UnitPrice">Der Preis pro Einheit (angegeben oder berechnet).</param>
/// <param name="TotalAmount">Der Gesamtbetrag (angegeben oder berechnet).</param>
/// <param name="ComputedField">Der berechnete Wert; <see langword="null"/>, wenn nichts berechnet wurde.</param>
/// <param name="HasMismatch">Gibt an, ob drei angegebene Werte nicht zueinander passen.</param>
/// <param name="ExpectedTotal">Der rechnerische Gesamtbetrag (Menge × Preis) bei einer Abweichung, sonst <see langword="null"/>.</param>
/// <param name="IsMissing">Gibt an, ob weniger als zwei Werte angegeben sind.</param>
/// <returns>Der Wert.</returns>
public sealed record AmountCompletion(
    decimal? Quantity,
    decimal? UnitPrice,
    decimal? TotalAmount,
    AmountField? ComputedField,
    bool HasMismatch,
    decimal? ExpectedTotal,
    bool IsMissing);
