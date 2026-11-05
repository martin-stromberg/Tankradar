using Microsoft.Maui.ApplicationModel;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Location;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass der Standortdienst die Einstellung „Nie“ beachtet, bei Berechtigungs- und Standortproblemen einen Status statt einer Ausnahme liefert und nichts protokolliert, was den Standort verrät.
/// </summary>
public class LocationServiceTests_Permission : BaseTest
{
    private static readonly GeoPosition Berlin = new(52.5200123, 13.4050456);

    private sealed class Platform
    {
        public int PermissionCalls { get; private set; }

        public int PositionCalls { get; private set; }

        public Func<Task<bool>> Permission { get; set; } = () => Task.FromResult(true);

        public Func<Task<GeoPosition?>> Position { get; set; } = () => Task.FromResult<GeoPosition?>(Berlin);

        public Task<bool> EnsurePermission(CancellationToken token)
        {
            PermissionCalls++;
            return Permission();
        }

        public Task<GeoPosition?> GetPosition(CancellationToken token)
        {
            PositionCalls++;
            return Position();
        }
    }

    private static MauiLocationService Create(Platform platform, RecordingLogger<MauiLocationService>? logger = null)
    {
        return new MauiLocationService(logger ?? new RecordingLogger<MauiLocationService>(), platform.EnsurePermission, platform.GetPosition);
    }

    /// <summary>
    /// Prüft, dass bei „Nie“ weder Berechtigung noch Standort abgefragt werden.
    /// </summary>
    [Fact]
    public async Task Never_DoesNotTouchPlatform()
    {
        var platform = new Platform();

        var result = await Create(platform).GetCurrentLocationAsync(GpsUsage.Never);

        Assert.Equal(LocationStatus.DisabledBySetting, result.Status);
        Assert.Null(result.Position);
        Assert.Equal(0, platform.PermissionCalls);
        Assert.Equal(0, platform.PositionCalls);
    }

    /// <summary>
    /// Prüft, dass ein undefinierter Einstellungswert wie „Nie“ behandelt wird (Fail Secure).
    /// </summary>
    [Fact]
    public async Task UndefinedUsage_IsTreatedAsDisabled()
    {
        var platform = new Platform();

        var result = await Create(platform).GetCurrentLocationAsync((GpsUsage)99);

        Assert.Equal(LocationStatus.DisabledBySetting, result.Status);
        Assert.Equal(0, platform.PermissionCalls);
    }

    /// <summary>
    /// Prüft, dass „Immer“ und „Nur bei Nutzung“ den Standort liefern.
    /// </summary>
    /// <param name="usage">Die Einstellung.</param>
    [Theory]
    [InlineData(GpsUsage.Always)]
    [InlineData(GpsUsage.WhileInUse)]
    public async Task AllowedUsage_ReturnsPosition(GpsUsage usage)
    {
        var platform = new Platform();

        var result = await Create(platform).GetCurrentLocationAsync(usage);

        Assert.Equal(LocationStatus.Available, result.Status);
        Assert.Same(Berlin, result.Position);
        Assert.Equal(1, platform.PermissionCalls);
        Assert.Equal(1, platform.PositionCalls);
    }

    /// <summary>
    /// Prüft, dass eine verweigerte Berechtigung den Standort nicht abfragt.
    /// </summary>
    [Fact]
    public async Task PermissionDenied_ReturnsStatusWithoutPositionRequest()
    {
        var platform = new Platform { Permission = () => Task.FromResult(false) };

        var result = await Create(platform).GetCurrentLocationAsync(GpsUsage.WhileInUse);

        Assert.Equal(LocationStatus.PermissionDenied, result.Status);
        Assert.Equal(0, platform.PositionCalls);
    }

    /// <summary>
    /// Prüft, dass eine PermissionException als verweigert gemeldet wird.
    /// </summary>
    [Fact]
    public async Task PermissionException_ReturnsPermissionDenied()
    {
        var platform = new Platform { Position = () => throw new PermissionException("verweigert") };

        var result = await Create(platform).GetCurrentLocationAsync(GpsUsage.WhileInUse);

        Assert.Equal(LocationStatus.PermissionDenied, result.Status);
    }

    /// <summary>
    /// Prüft, dass fehlender Standort, Zeitüberschreitung und andere Fehler als „nicht verfügbar“ gemeldet werden.
    /// </summary>
    [Fact]
    public async Task PositionProblems_ReturnUnavailable()
    {
        var missing = new Platform { Position = () => Task.FromResult<GeoPosition?>(null) };
        var timeout = new Platform { Position = () => throw new TimeoutException() };
        var unsupported = new Platform { Position = () => throw new FeatureNotSupportedException() };
        var broken = new Platform { Permission = () => throw new InvalidOperationException("kaputt") };

        Assert.Equal(LocationStatus.Unavailable, (await Create(missing).GetCurrentLocationAsync(GpsUsage.Always)).Status);
        Assert.Equal(LocationStatus.Unavailable, (await Create(timeout).GetCurrentLocationAsync(GpsUsage.Always)).Status);
        Assert.Equal(LocationStatus.Unavailable, (await Create(unsupported).GetCurrentLocationAsync(GpsUsage.Always)).Status);
        Assert.Equal(LocationStatus.Unavailable, (await Create(broken).GetCurrentLocationAsync(GpsUsage.Always)).Status);
    }

    /// <summary>
    /// Prüft, dass ein Abbruch durch den Aufrufer weitergereicht und nicht als Status verschluckt wird.
    /// </summary>
    [Fact]
    public async Task Cancellation_IsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var platform = new Platform { Position = () => throw new OperationCanceledException(cancellation.Token) };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Create(platform).GetCurrentLocationAsync(GpsUsage.Always, cancellation.Token));
    }

    /// <summary>
    /// Prüft, dass weder bei Erfolg noch bei Fehlern Koordinaten ins Protokoll gelangen.
    /// </summary>
    [Fact]
    public async Task Logging_NeverContainsCoordinates()
    {
        var logger = new RecordingLogger<MauiLocationService>();
        var failing = new Platform { Position = () => throw new InvalidOperationException($"Fehler bei {Berlin.Latitude}, {Berlin.Longitude}") };

        await Create(new Platform(), logger).GetCurrentLocationAsync(GpsUsage.Always);
        await Create(new Platform { Permission = () => Task.FromResult(false) }, logger).GetCurrentLocationAsync(GpsUsage.Always);
        await Create(failing, logger).GetCurrentLocationAsync(GpsUsage.Always);

        Assert.DoesNotContain("52.52", logger.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("13.405", logger.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("52,52", logger.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("13,405", logger.AllText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass der Testdienst bei „Nie“ keinen Standort liefert und sonst den festen Standort bzw. „nicht verfügbar“ ohne Konfiguration.
    /// </summary>
    [Fact]
    public async Task TestLocationService_HonorsUsageAndConfiguration()
    {
        var configured = new TestLocationService(Berlin);
        var unconfigured = new TestLocationService(null);

        Assert.Equal(LocationStatus.DisabledBySetting, (await configured.GetCurrentLocationAsync(GpsUsage.Never)).Status);
        Assert.Same(Berlin, (await configured.GetCurrentLocationAsync(GpsUsage.WhileInUse)).Position);
        Assert.Same(Berlin, (await configured.GetCurrentLocationAsync(GpsUsage.Always)).Position);
        Assert.Equal(LocationStatus.Unavailable, (await unconfigured.GetCurrentLocationAsync(GpsUsage.WhileInUse)).Status);
        Assert.Equal(LocationStatus.DisabledBySetting, (await unconfigured.GetCurrentLocationAsync(GpsUsage.Never)).Status);
    }

    /// <summary>
    /// Prüft die Bereichsprüfung der Position und dass sie sich nicht als Text mit Koordinaten ausgibt.
    /// </summary>
    [Fact]
    public void GeoPosition_ValidatesRangeAndHidesCoordinatesInText()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPosition(91, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPosition(0, 181));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPosition(double.NaN, 0));
        Assert.DoesNotContain("52", Berlin.ToString(), StringComparison.Ordinal);
    }
}
