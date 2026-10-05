using System.Net;
using System.Net.Sockets;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// Basis für E2E-Tests der Umkreissuche: startet die App im Testmodus mit Mock-Preisdienst, Test-Schlüssel und festem Teststandort
/// (Berlin-Mitte); produktive Endpunkte und der echte Standortdienst werden nie berührt.
/// </summary>
public abstract class SearchE2ETestBase : SettingsE2ETestBase
{
    /// <summary>
    /// Der feste Teststandort (Berlin-Mitte) im Format der Umgebungsvariable.
    /// </summary>
    protected const string TestLocation = "52.5200,13.4050";

    /// <summary>
    /// Startet die App mit einem neuen Mock-Server und dem festen Teststandort.
    /// </summary>
    protected SearchE2ETestBase()
        : this(new MockTankerkoenigServer())
    {
    }

    private SearchE2ETestBase(MockTankerkoenigServer server)
        : base(CreateEnvironment(server.BaseUrl, TestLocation, MockTankerkoenigServer.AcceptedKey))
    {
        Server = server;
    }

    /// <summary>
    /// Der Mock-Server des Preisdienstes.
    /// </summary>
    protected MockTankerkoenigServer Server { get; }

    /// <summary>
    /// Erstellt die Umgebungsvariablen der App für den Testmodus (Preisdienst-Adresse, Schlüssel und optional der Teststandort).
    /// </summary>
    /// <param name="baseUrl">Die Adresse des Preisdienstes.</param>
    /// <param name="location">Der Teststandort oder <see langword="null"/>, wenn keiner gesetzt werden soll.</param>
    /// <param name="apiKey">Der Test-Schlüssel.</param>
    /// <returns>Die Umgebungsvariablen.</returns>
    protected static Dictionary<string, string> CreateEnvironment(Uri baseUrl, string? location, string apiKey)
    {
        var environment = new Dictionary<string, string>
        {
            [TestDataPaths.PriceApiUrlEnvironmentVariable] = baseUrl.ToString(),
            [TestDataPaths.PriceApiKeyEnvironmentVariable] = apiKey,
        };
        if (location is not null)
        {
            environment[TestDataPaths.TestLocationEnvironmentVariable] = location;
        }

        return environment;
    }

    /// <summary>
    /// Liefert die Adresse eines Ports, auf dem niemand lauscht (nicht erreichbarer Preisdienst).
    /// </summary>
    /// <returns>Die Adresse.</returns>
    protected static Uri ClosedPortUrl()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return new Uri($"http://127.0.0.1:{port}/json/");
    }

    /// <summary>
    /// Wechselt auf die Suche („Karte“) und wartet, bis Einstellungen geladen und Filter- und Sortieroptionen sichtbar sind.
    /// </summary>
    protected void OpenSearch()
    {
        NavigateToTab("Karte", "MapPage.Headline");
        WaitForAutomationId("Search.Radius");
        WaitForAutomationId("Search.Filter.All");
        WaitForAutomationId("Search.Sort.Price");
    }

    /// <summary>
    /// Trägt den Radius ein.
    /// </summary>
    /// <param name="text">Der Text des Radiusfeldes.</param>
    protected void SetRadius(string text)
    {
        WaitForAutomationId("Search.Radius").Patterns.Value.Pattern.SetValue(text);
    }

    /// <summary>
    /// Liefert den Text des Radiusfeldes.
    /// </summary>
    /// <returns>Der Text.</returns>
    protected string ReadRadius()
    {
        return WaitForAutomationId("Search.Radius").Patterns.Value.Pattern.Value.Value;
    }

    /// <summary>
    /// Löst die Suche aus.
    /// </summary>
    protected void Submit()
    {
        WaitForAutomationId("Search.Submit").Patterns.Invoke.Pattern.Invoke();
    }

    /// <summary>
    /// Wartet, bis die Ergebnisliste genau die angegebenen Tankstellen in dieser Reihenfolge zeigt.
    /// </summary>
    /// <param name="expected">Die erwarteten Namen von oben nach unten.</param>
    protected void WaitForStationNames(params string[] expected)
    {
        var last = string.Empty;
        WaitUntil(
            () =>
            {
                var names = ReadTexts("Search.Station.Name");
                last = string.Join(" | ", names);
                return names.SequenceEqual(expected);
            },
            $"Die Ergebnisliste zeigt nicht die erwarteten Tankstellen. Erwartet: {string.Join(" | ", expected)}; zuletzt: {last}");
    }

    /// <summary>
    /// Wartet, bis die Statusmeldung der Suche den Text zeigt.
    /// </summary>
    /// <param name="expected">Der erwartete Text.</param>
    protected void WaitForStatusMessage(string expected)
    {
        var last = string.Empty;
        WaitUntil(
            () =>
            {
                last = MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("Search.StatusMessage"))?.Name ?? string.Empty;
                return last == expected;
            },
            $"Die Statusmeldung der Suche lautet nicht '{expected}', zuletzt: '{last}'.");
    }

    /// <summary>
    /// Liefert die Texte aller Elemente mit der AutomationId von oben nach unten.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <returns>Die Texte.</returns>
    protected IReadOnlyList<string> ReadTexts(string automationId)
    {
        return FindAllOrdered(automationId).Select(element => element.Name).ToList();
    }

    /// <summary>
    /// Liefert die Elemente mit der AutomationId in Dokumentreihenfolge (bei der senkrechten Liste von oben nach unten).
    /// Elemente außerhalb des sichtbaren Bereichs melden keine Position, daher wird nicht nach Koordinaten sortiert.
    /// </summary>
    /// <param name="automationId">Die AutomationId.</param>
    /// <returns>Die Elemente.</returns>
    protected IReadOnlyList<AutomationElement> FindAllOrdered(string automationId)
    {
        return MainWindow.FindAllDescendants(cf => cf.ByAutomationId(automationId));
    }

    /// <summary>
    /// Liefert die Texte der Elemente mit den angegebenen AutomationIds je Tankstelle: Schlüssel ist der Name der Tankstelle,
    /// Werte sind die Texte der zugeordneten Elemente in Dokumentreihenfolge. Die Zuordnung folgt der Reihenfolge in der Liste (Elemente gehören zur davor stehenden Tankstelle).
    /// </summary>
    /// <param name="automationIds">Die AutomationIds der zuzuordnenden Elemente.</param>
    /// <returns>Je Tankstelle die Texte der zugehörigen Elemente.</returns>
    protected IReadOnlyList<(string Station, string Text)> TextsByStation(params string[] automationIds)
    {
        var ids = new[] { "Search.Station.Name" }.Concat(automationIds).ToHashSet();
        var elements = MainWindow.FindAllDescendants(cf => new OrCondition(ids.Select(id => (ConditionBase)cf.ByAutomationId(id)).ToArray()));
        var result = new List<(string, string)>();
        var current = string.Empty;
        foreach (var element in elements)
        {
            if (element.AutomationId == "Search.Station.Name")
            {
                current = element.Name;
            }
            else
            {
                result.Add((current, element.Name));
            }
        }

        return result;
    }

    /// <summary>
    /// Liefert die Namen der Tankstellen, denen die Elemente mit der AutomationId zugeordnet sind (je Element ein Eintrag).
    /// </summary>
    /// <param name="automationId">Die AutomationId der zuzuordnenden Elemente.</param>
    /// <returns>Je Element der Name der zugehörigen Tankstelle.</returns>
    protected IReadOnlyList<string> StationNamesOf(string automationId)
    {
        return TextsByStation(automationId).Select(entry => entry.Station).ToList();
    }

    /// <summary>
    /// Wählt die Sortierung („Price“, „Distance“, „Name“).
    /// </summary>
    /// <param name="key">Der Schlüssel der Sortierung.</param>
    protected void SelectSort(string key)
    {
        SelectRadio("Search.Sort." + key);
    }

    /// <summary>
    /// Wählt den Spritsortenfilter („All“ oder Name der Sorte).
    /// </summary>
    /// <param name="key">Der Schlüssel des Filters.</param>
    protected void SelectFilter(string key)
    {
        SelectRadio("Search.Filter." + key);
    }

    /// <summary>
    /// Stellt in den Optionen die Standortnutzung ein („Always“, „WhileInUse“, „Never“) und wechselt zurück auf die Suche.
    /// </summary>
    /// <param name="key">Der Schlüssel der Standortnutzung.</param>
    protected void SetGpsUsage(string key)
    {
        OpenSettings();
        SelectRadio("Settings.Gps." + key);
        OpenSearch();
    }

    /// <inheritdoc />
    protected override void Cleanup()
    {
        Server.Dispose();
    }
}
