using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.MAUI.Services.Routing;

/// <summary>
/// Gemeinsame Übertragung zum Routingdienst: HTTP-Client (ohne Weiterleitungen) und Drosselung, die von der Routenberechnung und der Schlüsselprüfung geteilt werden,
/// damit beide zusammen den Mindestabstand zum Dienst einhalten.
/// </summary>
/// <param name="Client">Der HTTP-Client.</param>
/// <param name="Throttle">Die Drosselung der Anfragen an den Routingdienst.</param>
/// <returns>Der Wert.</returns>
public sealed record RoutingTransport(HttpClient Client, RequestThrottle Throttle);
