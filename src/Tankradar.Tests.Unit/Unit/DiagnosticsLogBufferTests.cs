using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Services.Diagnostics;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den begrenzten Diagnosepuffer: Mindeststufe, Kapazität und Reihenfolge.
/// </summary>
public class DiagnosticsLogBufferTests : BaseTest
{
    /// <summary>
    /// Prüft, dass Einträge unterhalb der Mindeststufe verworfen werden.
    /// </summary>
    [Fact]
    public void Add_BelowMinimum_Skipped()
    {
        var buffer = new DiagnosticsLogBuffer(new ManualTimeProvider());

        buffer.Add(LogLevel.Information, "Kat", "Hinweis", null);
        buffer.Add(LogLevel.Debug, "Kat", "Debug", null);

        Assert.Empty(buffer.Snapshot());
    }

    /// <summary>
    /// Prüft, dass Warnungen und Fehler in chronologischer Reihenfolge gehalten werden.
    /// </summary>
    [Fact]
    public void Snapshot_Chronological()
    {
        var time = new ManualTimeProvider();
        var buffer = new DiagnosticsLogBuffer(time);
        buffer.Add(LogLevel.Warning, "A", "eins", null);
        time.Advance(TimeSpan.FromSeconds(5));
        buffer.Add(LogLevel.Error, "B", "zwei", "InvalidOperationException");

        var snapshot = buffer.Snapshot();

        Assert.Equal(2, snapshot.Count);
        Assert.Equal("eins", snapshot[0].Message);
        Assert.Equal("zwei", snapshot[1].Message);
        Assert.Equal("InvalidOperationException", snapshot[1].ExceptionType);
        Assert.True(snapshot[0].Timestamp < snapshot[1].Timestamp);
    }

    /// <summary>
    /// Prüft, dass bei vollem Puffer die ältesten Einträge verworfen werden.
    /// </summary>
    [Fact]
    public void Add_OverCapacity_DropsOldest()
    {
        var buffer = new DiagnosticsLogBuffer(new ManualTimeProvider(), capacity: 3);
        for (var i = 0; i < 5; i++)
        {
            buffer.Add(LogLevel.Warning, "K", "Eintrag " + i, null);
        }

        var snapshot = buffer.Snapshot();

        Assert.Equal(3, snapshot.Count);
        Assert.Equal("Eintrag 2", snapshot[0].Message);
        Assert.Equal("Eintrag 4", snapshot[2].Message);
    }

    /// <summary>
    /// Prüft, dass eine Kapazität unter 1 abgelehnt wird.
    /// </summary>
    [Fact]
    public void Ctor_ZeroCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiagnosticsLogBuffer(new ManualTimeProvider(), capacity: 0));
    }
}
