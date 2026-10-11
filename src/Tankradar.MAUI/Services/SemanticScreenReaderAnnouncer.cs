using Microsoft.Extensions.Logging;

namespace Tankradar.MAUI.Services;

/// <summary>
/// Umsetzung von <see cref="IScreenReaderAnnouncer"/> über <see cref="SemanticScreenReader"/> von .NET MAUI.
/// </summary>
public sealed class SemanticScreenReaderAnnouncer : IScreenReaderAnnouncer
{
    private readonly ILogger<SemanticScreenReaderAnnouncer> _logger;

    /// <summary>
    /// Erstellt den Dienst.
    /// </summary>
    /// <param name="logger">Logger (protokolliert nie den angesagten Text).</param>
    public SemanticScreenReaderAnnouncer(ILogger<SemanticScreenReaderAnnouncer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void Announce(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        try
        {
            SemanticScreenReader.Default.Announce(text);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            // Eine Ansage ist eine Hilfe; fehlt die Plattformunterstützung, bleibt die Bedienung unverändert möglich.
            _logger.LogDebug("Die Ansage für die Sprachausgabe ist nicht möglich ({ExceptionType}).", ex.GetType().Name);
        }
    }
}
