using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.MAUI.Services.Pricing;

/// <summary>
/// Ein Abruf bei der Preis-API ist fehlgeschlagen. Die Meldung enthält nie den API-Schlüssel oder die vollständige Adresse.
/// </summary>
public sealed class PriceApiException : Exception
{
    /// <summary>
    /// Erstellt die Ausnahme.
    /// </summary>
    /// <param name="failure">Der Fehlergrund.</param>
    /// <param name="message">Die Beschreibung.</param>
    /// <param name="innerException">Die ursächliche Ausnahme, sofern vorhanden.</param>
    public PriceApiException(PriceFailure failure, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = failure;
    }

    /// <summary>
    /// Der Fehlergrund.
    /// </summary>
    public PriceFailure Failure { get; }
}
