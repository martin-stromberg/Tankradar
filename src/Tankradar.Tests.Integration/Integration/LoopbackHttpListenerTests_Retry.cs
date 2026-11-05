using System.Net;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass der Start der Mock-Server bei einem zwischenzeitlich belegten Port mit einem neuen Port wiederholt wird.
/// </summary>
public class LoopbackHttpListenerTests_Retry
{
    /// <summary>
    /// Prüft, dass ein belegter erster Port zu einem zweiten Versuch mit neuem Port führt.
    /// </summary>
    [Fact]
    public void Start_PortTaken_RetriesWithNewPort()
    {
        var blocker = LoopbackHttpListener.Start(out var takenPort);
        try
        {
            var freeProbe = LoopbackHttpListener.Start(out var freePort);
            freeProbe.Close();
            var offered = new Queue<int>([takenPort, takenPort, freePort]);
            var calls = 0;

            var listener = LoopbackHttpListener.Start(
                () =>
                {
                    calls++;
                    return offered.Dequeue();
                },
                out var port);

            try
            {
                Assert.Equal(freePort, port);
                Assert.Equal(3, calls);
                Assert.True(listener.IsListening);
            }
            finally
            {
                listener.Close();
            }
        }
        finally
        {
            blocker.Close();
        }
    }

    /// <summary>
    /// Prüft, dass der Fehler nach allen Versuchen weitergereicht wird.
    /// </summary>
    [Fact]
    public void Start_PortAlwaysTaken_ThrowsAfterMaxAttempts()
    {
        var blocker = LoopbackHttpListener.Start(out var takenPort);
        try
        {
            var calls = 0;

            Assert.Throws<HttpListenerException>(() => LoopbackHttpListener.Start(
                () =>
                {
                    calls++;
                    return takenPort;
                },
                out _,
                maxAttempts: 3));

            Assert.Equal(3, calls);
        }
        finally
        {
            blocker.Close();
        }
    }

    /// <summary>
    /// Prüft, dass mehrere gleichzeitig gestartete Mock-Server unterschiedliche Ports erhalten.
    /// </summary>
    [Fact]
    public void ParallelStarts_GetDistinctPorts()
    {
        var servers = new MockTileServer[8];
        try
        {
            Parallel.For(0, servers.Length, i => servers[i] = new MockTileServer());

            Assert.Equal(servers.Length, servers.Select(server => server.BaseUrl.Port).Distinct().Count());
        }
        finally
        {
            foreach (var server in servers)
            {
                server?.Dispose();
            }
        }
    }
}
