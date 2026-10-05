using System.Net;
using System.Text;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// HTTP-Handler für Tests: beantwortet Anfragen nacheinander mit vorgegebenen Antworten (die letzte wiederholt sich) und protokolliert die Anfragen. Es findet nie echter Netzwerkverkehr statt.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly List<Func<CancellationToken, Task<HttpResponseMessage>>> _steps = [];
    private int _index;

    /// <summary>
    /// Die empfangenen Anfrageadressen.
    /// </summary>
    public List<Uri> Requests { get; } = [];

    /// <summary>
    /// Hängt eine Antwort mit Statuscode und JSON-Inhalt an.
    /// </summary>
    /// <param name="status">Der Statuscode.</param>
    /// <param name="body">Der Inhalt.</param>
    /// <returns>Der Handler für verkettete Aufrufe.</returns>
    public FakeHttpMessageHandler RespondWith(HttpStatusCode status, string body)
    {
        _steps.Add(_ => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        }));
        return this;
    }

    /// <summary>
    /// Hängt einen Schritt an, der eine Ausnahme auslöst.
    /// </summary>
    /// <param name="exception">Die Ausnahme.</param>
    /// <returns>Der Handler für verkettete Aufrufe.</returns>
    public FakeHttpMessageHandler Throw(Exception exception)
    {
        _steps.Add(_ => throw exception);
        return this;
    }

    /// <summary>
    /// Hängt einen Schritt an, der nie antwortet (bis zum Abbruch).
    /// </summary>
    /// <returns>Der Handler für verkettete Aufrufe.</returns>
    public FakeHttpMessageHandler Hang()
    {
        _steps.Add(async token =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        return this;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request.RequestUri!);
        var step = _steps[Math.Min(_index, _steps.Count - 1)];
        _index++;
        return step(cancellationToken);
    }
}
