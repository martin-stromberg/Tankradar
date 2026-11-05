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
    /// Wertet die Kopfzeilen einer Antwort aus. Die Gültigkeit ist die Lebensdauer abzüglich des bereits vergangenen Alters (<paramref name="age"/>), mindestens 0.
    /// </summary>
    /// <param name="cacheControl">Die Angabe <c>Cache-Control</c> oder <see langword="null"/>.</param>
    /// <param name="expires">Die Angabe <c>Expires</c> oder <see langword="null"/>.</param>
    /// <param name="serverDate">Die Angabe <c>Date</c> des Servers oder <see langword="null"/> (Bezug für <paramref name="expires"/>).</param>
    /// <param name="now">Der aktuelle Zeitpunkt.</param>
    /// <param name="fallback">Die Gültigkeitsdauer, wenn der Server keine Angabe macht.</param>
    /// <param name="age">Die Angabe <c>Age</c> des Servers oder <see langword="null"/>; negative Werte gelten als 0 und bei <c>no-cache</c>/<c>no-store</c> ist sie ohne Wirkung.</param>
    /// <returns>Der Zeitpunkt, bis zu dem die Kachel ohne Rückfrage gilt, ob sie nicht gespeichert werden darf, die Lebensdauer vor Abzug des Alters und ob die Angabe vom Server stammt.</returns>
    public static TileFreshness Evaluate(CacheControlHeaderValue? cacheControl, DateTimeOffset? expires, DateTimeOffset? serverDate, DateTimeOffset now, TimeSpan fallback, TimeSpan? age = null)
    {
        if (cacheControl is { NoStore: true })
        {
            return new TileFreshness(now, NoStore: true, TimeSpan.Zero, IsExplicit: true);
        }

        if (cacheControl is { NoCache: true })
        {
            // Speichern ist erlaubt, vor jeder Verwendung wird beim Server rückgefragt.
            return new TileFreshness(now, NoStore: false, TimeSpan.Zero, IsExplicit: true);
        }

        if (cacheControl?.MaxAge is { } maxAge)
        {
            return Create(now, Clamp(maxAge), age, isExplicit: true);
        }

        if (expires is { } expiresAt)
        {
            // Läuft die Uhr des Servers anders als die lokale, zählt die Spanne zwischen Date und Expires des Servers.
            var lifetime = serverDate is { } date ? expiresAt - date : expiresAt - now;
            return Create(now, Clamp(lifetime), age, isExplicit: true);
        }

        return Create(now, fallback, age, isExplicit: false);
    }

    /// <summary>
    /// Berechnet die Frische aus einer bekannten Lebensdauer abzüglich des Alters (mindestens 0).
    /// </summary>
    /// <param name="now">Der aktuelle Zeitpunkt.</param>
    /// <param name="lifetime">Die Lebensdauer vor Abzug des Alters.</param>
    /// <param name="age">Die Angabe <c>Age</c> oder <see langword="null"/>.</param>
    /// <param name="isExplicit">Gibt an, ob die Lebensdauer aus Angaben des Servers stammt.</param>
    /// <returns>Die Frische.</returns>
    public static TileFreshness Create(DateTimeOffset now, TimeSpan lifetime, TimeSpan? age, bool isExplicit)
    {
        var elapsed = age is { } value && value > TimeSpan.Zero ? value : TimeSpan.Zero;
        var remaining = lifetime - elapsed;
        return new TileFreshness(now + (remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero), NoStore: false, lifetime, isExplicit);
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
/// <param name="Lifetime">Die Lebensdauer vor Abzug des Alters oder <see langword="null"/>, wenn unbekannt.</param>
/// <param name="IsExplicit">Gibt an, dass die Lebensdauer aus <c>Cache-Control</c> oder <c>Expires</c> der Antwort abgeleitet wurde.</param>
/// <returns>Der Wert.</returns>
public readonly record struct TileFreshness(DateTimeOffset ExpiresUtc, bool NoStore, TimeSpan? Lifetime = null, bool IsExplicit = false);
