using Tankradar.MAUI.Models.Search;

namespace Tankradar.MAUI.Services.Geocoding;

/// <summary>
/// Der Ausgang einer Adressauflösung.
/// </summary>
public enum GeocodingStatus
{
    /// <summary>
    /// Die Adresse wurde in eine Position umgewandelt.
    /// </summary>
    Found,

    /// <summary>
    /// Zur Eingabe wurde kein Ort gefunden.
    /// </summary>
    NotFound,

    /// <summary>
    /// Die Eingabe war ungültig; es wurde keine Anfrage gesendet.
    /// </summary>
    InvalidInput,

    /// <summary>
    /// Der Dienst ist nicht erreichbar oder hat nicht rechtzeitig geantwortet (auch bei fehlender Verbindung).
    /// </summary>
    Unavailable,

    /// <summary>
    /// Der Dienst hat die Anfrage abgelehnt (z. B. wegen der Nutzungsrichtlinie).
    /// </summary>
    Rejected,

    /// <summary>
    /// Die Antwort des Dienstes war nicht auswertbar.
    /// </summary>
    InvalidResponse,

    /// <summary>
    /// Im Testmodus ist kein Dienst angegeben; es wurde keine Anfrage gesendet.
    /// </summary>
    EndpointNotConfigured,
}

/// <summary>
/// Das Ergebnis einer Adressauflösung. Position und Ortsname leben nur im Arbeitsspeicher und werden nie gespeichert oder protokolliert.
/// </summary>
public sealed class GeocodingResult
{
    private GeocodingResult(GeocodingStatus status, GeoPosition? position, string? placeName)
    {
        Status = status;
        Position = position;
        PlaceName = placeName;
    }

    /// <summary>
    /// Der Ausgang der Auflösung.
    /// </summary>
    public GeocodingStatus Status { get; }

    /// <summary>
    /// Die gefundene Position; nur bei <see cref="GeocodingStatus.Found"/> gesetzt.
    /// </summary>
    public GeoPosition? Position { get; }

    /// <summary>
    /// Der Anzeigename des gefundenen Ortes (aus dem Dienst); nur bei <see cref="GeocodingStatus.Found"/> möglich.
    /// </summary>
    public string? PlaceName { get; }

    /// <summary>
    /// Erstellt ein erfolgreiches Ergebnis.
    /// </summary>
    /// <param name="position">Die Position.</param>
    /// <param name="placeName">Der Anzeigename oder <see langword="null"/>.</param>
    /// <returns>Das Ergebnis.</returns>
    public static GeocodingResult Success(GeoPosition position, string? placeName)
    {
        ArgumentNullException.ThrowIfNull(position);
        return new GeocodingResult(GeocodingStatus.Found, position, placeName);
    }

    /// <summary>
    /// Erstellt ein Ergebnis ohne Position.
    /// </summary>
    /// <param name="status">Der Grund; nicht <see cref="GeocodingStatus.Found"/>.</param>
    /// <returns>Das Ergebnis.</returns>
    public static GeocodingResult Failure(GeocodingStatus status)
    {
        if (status == GeocodingStatus.Found)
        {
            throw new ArgumentException("Ein Fehlschlag darf nicht den Status 'Found' tragen.", nameof(status));
        }

        return new GeocodingResult(status, null, null);
    }

    /// <summary>
    /// Gibt nur den Status zurück, damit weder Position noch Ortsname versehentlich protokolliert werden.
    /// </summary>
    /// <returns>Der Status als Text.</returns>
    public override string ToString()
    {
        return Status.ToString();
    }
}
