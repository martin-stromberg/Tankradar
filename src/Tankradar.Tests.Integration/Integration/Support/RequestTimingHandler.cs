using System.Diagnostics;

namespace Tankradar.Tests.Integration.Integration.Support;

/// <summary>
/// Hält clientseitig fest, wann jede Anfrage abgesendet wird (Stopwatch-Zeitstempel beim Eintritt in <see cref="SendAsync"/>).
/// So hängt die Prüfung der Drosselung nicht von der Empfangszeit am Mock-Server ab.
/// </summary>
public sealed class RequestTimingHandler : DelegatingHandler
{
    private readonly List<long> _sendTimes = [];

    /// <summary>
    /// Erstellt den Handler mit dem inneren Handler, der die Anfragen tatsächlich sendet.
    /// </summary>
    /// <param name="innerHandler">Der innere Handler.</param>
    public RequestTimingHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
    }

    /// <summary>
    /// Die Stopwatch-Zeitstempel (<see cref="Stopwatch.GetTimestamp"/>) aller gesendeten Anfragen in Sendereihenfolge.
    /// </summary>
    public IReadOnlyList<long> SendTimes
    {
        get
        {
            lock (_sendTimes)
            {
                return [.. _sendTimes];
            }
        }
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (_sendTimes)
        {
            _sendTimes.Add(Stopwatch.GetTimestamp());
        }

        return base.SendAsync(request, cancellationToken);
    }
}
