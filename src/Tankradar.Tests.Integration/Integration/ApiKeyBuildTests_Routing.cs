using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft die Schlüsselübernahme des OpenRouteService-Schlüssels beim Build (<c>TANKRADAR_ROUTING_API_KEY</c>, <c>ROUTING_API_KEY</c>, <c>routing.local.props</c>):
/// Reihenfolge der Quellen, Log ohne Wert und Trennung vom Tankerkönig-Schlüssel. Es kommen ausschließlich Platzhalter zum Einsatz.
/// </summary>
public class ApiKeyBuildTests_Routing
{
    private const string PrimaryPlaceholder = "ZZPLACEHOLDERROUTEPRIMARYZZ";
    private const string SecretPlaceholder = "ZZPLACEHOLDERROUTESECRETZZ";
    private const string LocalPlaceholder = "ZZPLACEHOLDERROUTELOCALZZ";
    private const string ForeignPlaceholder = "ZZPLACEHOLDERFOREIGNZZ";
    private const string MetadataName = "RoutingApiKey";

    private static string? BuildAndRead(Action<ApiKeyBuildRunner> prepare, string? primary, string? secret, IReadOnlyDictionary<string, string>? extra = null)
    {
        using var runner = new ApiKeyBuildRunner(ApiKeyBuildProfile.Routing);
        prepare(runner);
        var result = runner.Build(primary, secret, extra);
        Assert.True(result.ExitCode == 0, "Der Testbuild ist fehlgeschlagen.");
        return ApiKeyBuildRunner.ReadEmbeddedKey(result.AssemblyPath, MetadataName);
    }

    /// <summary>
    /// Prüft, dass <c>TANKRADAR_ROUTING_API_KEY</c> den beiden anderen Quellen vorgeht.
    /// </summary>
    [Fact]
    public void Build_AllSourcesPresent_PrimaryEnvironmentVariableWins()
    {
        Assert.Equal(PrimaryPlaceholder, BuildAndRead(runner => runner.WriteLocalProps(LocalPlaceholder), PrimaryPlaceholder, SecretPlaceholder));
    }

    /// <summary>
    /// Prüft, dass <c>ROUTING_API_KEY</c> der props-Datei vorgeht, wenn die erste Quelle fehlt.
    /// </summary>
    [Fact]
    public void Build_NoPrimary_SecretEnvironmentVariableBeatsLocalProps()
    {
        Assert.Equal(SecretPlaceholder, BuildAndRead(runner => runner.WriteLocalProps(LocalPlaceholder), null, SecretPlaceholder));
    }

    /// <summary>
    /// Prüft, dass ohne Umgebungsvariablen der Wert aus <c>routing.local.props</c> übernommen wird.
    /// </summary>
    [Fact]
    public void Build_OnlyLocalProps_UsesLocalProps()
    {
        Assert.Equal(LocalPlaceholder, BuildAndRead(runner => runner.WriteLocalProps(LocalPlaceholder), null, null));
    }

    /// <summary>
    /// Prüft, dass ohne jede Quelle der Build gelingt und ein leerer Schlüssel eingebettet wird.
    /// </summary>
    [Fact]
    public void Build_NoSource_EmbedsEmptyKey()
    {
        Assert.Equal(string.Empty, BuildAndRead(_ => { }, null, null));
    }

    /// <summary>
    /// Prüft, dass die Variablen des Tankerkönig-Schlüssels nicht als Routing-Schlüssel übernommen werden.
    /// </summary>
    [Fact]
    public void Build_OnlyTankerkoenigVariables_EmbedsEmptyRoutingKey()
    {
        var extra = new Dictionary<string, string>
        {
            ["TANKRADAR_FUEL_PRICE_API_KEY"] = ForeignPlaceholder,
            ["FUEL_PRICE_API_KEY"] = ForeignPlaceholder,
        };

        Assert.Equal(string.Empty, BuildAndRead(_ => { }, null, null, extra));
    }

    /// <summary>
    /// Prüft, dass umgekehrt der Tankerkönig-Build die Routing-Variablen nicht übernimmt.
    /// </summary>
    [Fact]
    public void Build_TankerkoenigTargets_IgnoreRoutingVariables()
    {
        using var runner = new ApiKeyBuildRunner();
        var extra = new Dictionary<string, string>
        {
            ["TANKRADAR_ROUTING_API_KEY"] = ForeignPlaceholder,
            ["ROUTING_API_KEY"] = ForeignPlaceholder,
        };

        var result = runner.Build(null, null, extra);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, ApiKeyBuildRunner.ReadEmbeddedKey(result.AssemblyPath));
    }

    /// <summary>
    /// Prüft, dass der Schlüssel bei diagnostischer Ausführlichkeit und im Binlog nie erscheint, aber in der Assembly steht.
    /// </summary>
    /// <param name="source">Die Quelle (0 vorrangige Variable, 1 Secret-Variable, 2 lokale Datei).</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Build_AnySource_IsNotLogged(int source)
    {
        using var runner = new ApiKeyBuildRunner(ApiKeyBuildProfile.Routing);
        if (source == 2)
        {
            runner.WriteLocalProps(LocalPlaceholder);
        }

        var placeholder = source switch { 0 => PrimaryPlaceholder, 1 => SecretPlaceholder, _ => LocalPlaceholder };
        var result = runner.Build(source == 0 ? PrimaryPlaceholder : null, source == 1 ? SecretPlaceholder : null);

        Assert.Equal(0, result.ExitCode);
        Assert.NotEmpty(result.BinaryLog);
        Assert.DoesNotContain(placeholder, result.ConsoleLog, StringComparison.Ordinal);
        Assert.DoesNotContain(placeholder, result.BinaryLog, StringComparison.Ordinal);
        Assert.Equal(placeholder, ApiKeyBuildRunner.ReadEmbeddedKey(result.AssemblyPath, MetadataName));
    }
}
