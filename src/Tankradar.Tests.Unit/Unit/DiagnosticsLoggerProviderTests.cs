using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Services.Diagnostics;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass der Diagnose-Logging-Provider Warnungen und Fehler samt Ausnahmetyp in den Puffer schreibt.
/// </summary>
public class DiagnosticsLoggerProviderTests : BaseTest
{
    /// <summary>
    /// Prüft, dass Warnungen mit Kategorie und Meldung im Puffer landen.
    /// </summary>
    [Fact]
    public void Log_Warning_Buffered()
    {
        var buffer = new DiagnosticsLogBuffer(new ManualTimeProvider());
        var logger = new DiagnosticsLoggerProvider(buffer).CreateLogger("Meine.Kategorie");

        logger.LogWarning("Etwas ist schiefgegangen");

        var entry = Assert.Single(buffer.Snapshot());
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("Meine.Kategorie", entry.Category);
        Assert.Contains("schiefgegangen", entry.Message);
    }

    /// <summary>
    /// Prüft, dass Informationsmeldungen nicht gepuffert werden.
    /// </summary>
    [Fact]
    public void Log_Information_Skipped()
    {
        var buffer = new DiagnosticsLogBuffer(new ManualTimeProvider());
        var logger = new DiagnosticsLoggerProvider(buffer).CreateLogger("Kat");

        logger.LogInformation("Nur ein Hinweis");

        Assert.Empty(buffer.Snapshot());
    }

    /// <summary>
    /// Prüft, dass der Typname einer Ausnahme mit abgelegt wird.
    /// </summary>
    [Fact]
    public void Log_Exception_TypeCaptured()
    {
        var buffer = new DiagnosticsLogBuffer(new ManualTimeProvider());
        var logger = new DiagnosticsLoggerProvider(buffer).CreateLogger("Kat");

        logger.LogError(new InvalidOperationException("kaputt"), "Vorgang fehlgeschlagen");

        var entry = Assert.Single(buffer.Snapshot());
        Assert.Equal(nameof(InvalidOperationException), entry.ExceptionType);
    }
}
