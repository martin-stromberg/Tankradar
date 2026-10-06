using System.Net.Http.Headers;

namespace Tankradar.MAUI.Services.Map;

/// <summary>
/// Ermittelt aus den HTTP-Caching-Angaben des Kachelservers (<c>Cache-Control</c>, <c>Expires</c>), wie lange eine Kachel gültig ist und ob sie gespeichert werden darf.
/// Fehlt jede Angabe, gilt der Rückfall (pauschal sieben Tage).
/// </summary>
public static class TileCachePolicy
{
    /// <summary>
    /// Die längste Gültigkeitsdauer, die übernommen wird (ein Jahr), damit abwegige Angaben keine Kachel dauerhaft festhalten.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static readonly TimeSpan MaxLifetime = TimeSpan.FromDays(365);

    /// <summary>
    /// Wertet die Kopfzeilen einer Antwort aus.
    /// </summary>
    /// <param name="cacheControl">Die Angabe <c>Cache-Control</c> oder <see langword="null"/>.</param>
    /// <param name="expires">Die Angabe <c>Expires</c> oder <see langword="null"/>.</param>
    /// <param name="serverDate">Die Angabe <c>Date</c> des Servers oder <see langword="null"/> (Bezug für <paramref name="expires"/>).</param>
    /// <param name="now">Der aktuelle Zeitpunkt.</param>
    /// <param name="fallback">Die Gültigkeitsdauer, wenn der Server keine Angabe macht.</param>
    /// <returns>Der Zeitpunkt, bis zu dem die Kachel ohne Rückfrage gilt, und ob sie nicht gespeichert werden darf.</returns>
    public static TileFreshness Evaluate(CacheControlHeaderValue? cacheControl, DateTimeOffset? expires, DateTimeOffset? serverDate, DateTimeOffset now, TimeSpan fallback)
    {
        if (cacheControl is { NoStore: true })
        {
            return new TileFreshness(now, NoStore: true);
        }

        if (cacheControl is { NoCache: true })
        {
            // Speichern ist erlaubt, vor jeder Verwendung wird beim Server rückgefragt.
            return new TileFreshness(now, NoStore: false);
        }

        if (cacheControl?.MaxAge is { } maxAge)
        {
            return new TileFreshness(now + Clamp(maxAge), NoStore: false);
        }

        if (expires is { } expiresAt)
        {
            // Läuft die Uhr des Servers anders als die lokale, zählt die Spanne zwischen Date und Expires des Servers.
            var lifetime = serverDate is { } date ? expiresAt - date : expiresAt - now;
            return new TileFreshness(now + Clamp(lifetime), NoStore: false);
        }

        return new TileFreshness(now + fallback, NoStore: false);
    }

    private static TimeSpan Clamp(TimeSpan lifetime)
    {
        if (lifetime < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return lifetime > MaxLifetime ? MaxLifetime : lifetime;
    }
}

/// <summary>
/// Ergebnis von <see cref="TileCachePolicy"/>: Gültigkeit einer Kachel und Speicherverbot.
/// </summary>
/// <param name="ExpiresUtc">Der Zeitpunkt, bis zu dem die Kachel ohne Rückfrage verwendet werden darf.</param>
/// <param name="NoStore">Gibt an, dass die Kachel nicht auf dem Gerät gespeichert werden darf.</param>
/// <returns>Der Wert.</returns>
public readonly record struct TileFreshness(DateTimeOffset ExpiresUtc, bool NoStore);
