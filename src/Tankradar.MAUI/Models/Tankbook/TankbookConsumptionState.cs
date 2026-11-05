namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Zustand der Verbrauchsberechnung eines Tank- oder Ladevorgangs.
/// </summary>
public enum TankbookConsumptionState
{
    /// <summary>
    /// Der Verbrauch wurde berechnet.
    /// </summary>
    Calculated,

    /// <summary>
    /// Teilbetankung: kein eigener Wert, die Menge fließt in den nächsten vollen Vorgang derselben Art ein.
    /// </summary>
    Partial,

    /// <summary>
    /// Erster voller Vorgang dieser Art: Es gibt keinen vorherigen vollen Vorgang als Ausgangsbasis.
    /// </summary>
    NoPreviousFullEntry,

    /// <summary>
    /// Seit dem vorherigen vollen Vorgang wurde keine Strecke gefahren.
    /// </summary>
    NoDistance,
}
