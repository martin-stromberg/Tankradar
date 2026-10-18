using System.Net;
using System.Net.Sockets;

namespace Tankradar.TestSupport;

/// <summary>
/// Startet einen <see cref="HttpListener"/> auf einem freien Loopback-Port (ausschließlich für Test-Mocks). Der Port wird vorab ermittelt und erst danach gebunden;
/// belegt zwischenzeitlich ein paralleler Testlauf denselben Port, scheitert der Start mit einer <see cref="HttpListenerException"/>.
/// Der Start wird dann mit einem neuen Port mehrfach wiederholt.
/// </summary>
public static class LoopbackHttpListener
{
    /// <summary>
    /// Die Anzahl der Startversuche, bevor der Fehler weitergereicht wird.
    /// </summary>
    public const int DefaultMaxAttempts = 10;

    /// <summary>
    /// Startet einen Listener auf einem freien Loopback-Port.
    /// </summary>
    /// <param name="port">Der Port, auf dem der Listener läuft.</param>
    /// <param name="maxAttempts">Die höchste Anzahl der Startversuche.</param>
    /// <returns>Der gestartete Listener.</returns>
    public static HttpListener Start(out int port, int maxAttempts = DefaultMaxAttempts)
    {
        return Start(FindFreePort, out port, maxAttempts);
    }

    /// <summary>
    /// Startet einen Listener auf dem Port, den <paramref name="selectPort"/> liefert; bei <see cref="HttpListenerException"/> wird mit einem neuen Port wiederholt.
    /// </summary>
    /// <param name="selectPort">Liefert den zu versuchenden Port.</param>
    /// <param name="port">Der Port, auf dem der Listener läuft.</param>
    /// <param name="maxAttempts">Die höchste Anzahl der Startversuche.</param>
    /// <returns>Der gestartete Listener.</returns>
    public static HttpListener Start(Func<int> selectPort, out int port, int maxAttempts = DefaultMaxAttempts)
    {
        ArgumentNullException.ThrowIfNull(selectPort);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        for (var attempt = 1; ; attempt++)
        {
            var candidate = selectPort();
            var listener = new HttpListener();
            try
            {
                listener.Prefixes.Add($"http://127.0.0.1:{candidate}/");
                listener.Start();
                port = candidate;
                return listener;
            }
            catch (HttpListenerException) when (attempt < maxAttempts)
            {
                listener.Close();
                Thread.Sleep(TimeSpan.FromMilliseconds(20 * attempt));
            }
            catch
            {
                listener.Close();
                throw;
            }
        }
    }

    private static int FindFreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        try
        {
            return ((IPEndPoint)probe.LocalEndpoint).Port;
        }
        finally
        {
            probe.Stop();
        }
    }
}
