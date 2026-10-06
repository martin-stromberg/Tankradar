namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Zeitquelle für Tests mit von Hand gesteuerter Uhr.
/// </summary>
public sealed class ManualTimeProvider : TimeProvider
{
    /// <summary>
    /// Erstellt die Zeitquelle mit dem Startzeitpunkt 2026-10-05 12:00 UTC.
    /// </summary>
    public ManualTimeProvider()
    {
        Now = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
    }

    /// <summary>
    /// Der aktuelle Zeitpunkt.
    /// </summary>
    public DateTimeOffset Now { get; set; }

    /// <summary>
    /// Der aktuelle Zeitpunkt als UTC-<see cref="DateTime"/>.
    /// </summary>
    public DateTime UtcNow => Now.UtcDateTime;

    /// <summary>
    /// Stellt die Uhr vor.
    /// </summary>
    /// <param name="by">Die Dauer.</param>
    public void Advance(TimeSpan by)
    {
        Now += by;
    }

    /// <inheritdoc />
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <inheritdoc />
    public override long GetTimestamp()
    {
        return Now.UtcTicks;
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
    {
        return Now;
    }
}
