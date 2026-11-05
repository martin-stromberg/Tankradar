using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft die Reihenfolge der drei Schlüsselquellen beim Build (<c>TANKRADAR_FUEL_PRICE_API_KEY</c>, <c>FUEL_PRICE_API_KEY</c>, <c>tankerkoenig.local.props</c>) mit Platzhaltern.
/// </summary>
public class ApiKeyBuildTests_Sources
{
    private const string PrimaryPlaceholder = "ZZPLACEHOLDERPRIMARYZZ";
    private const string SecretPlaceholder = "ZZPLACEHOLDERSECRETZZ";
    private const string LocalPlaceholder = "ZZPLACEHOLDERLOCALZZ";

    private static string? BuildAndRead(Action<ApiKeyBuildRunner> prepare, string? primary, string? secret)
    {
        using var runner = new ApiKeyBuildRunner();
        prepare(runner);
        var result = runner.Build(primary, secret);
        Assert.True(result.ExitCode == 0, "Der Testbuild ist fehlgeschlagen.");
        return ApiKeyBuildRunner.ReadEmbeddedKey(result.AssemblyPath);
    }

    /// <summary>
    /// Prüft, dass die Umgebungsvariable <c>TANKRADAR_FUEL_PRICE_API_KEY</c> den beiden anderen Quellen vorgeht.
    /// </summary>
    [Fact]
    public void Build_AllSourcesPresent_PrimaryEnvironmentVariableWins()
    {
        var key = BuildAndRead(runner => runner.WriteLocalProps(LocalPlaceholder), PrimaryPlaceholder, SecretPlaceholder);

        Assert.Equal(PrimaryPlaceholder, key);
    }

    /// <summary>
    /// Prüft, dass <c>FUEL_PRICE_API_KEY</c> der props-Datei vorgeht, wenn die erste Quelle fehlt.
    /// </summary>
    [Fact]
    public void Build_NoPrimary_SecretEnvironmentVariableBeatsLocalProps()
    {
        var key = BuildAndRead(runner => runner.WriteLocalProps(LocalPlaceholder), null, SecretPlaceholder);

        Assert.Equal(SecretPlaceholder, key);
    }

    /// <summary>
    /// Prüft, dass ohne Umgebungsvariablen der Wert der props-Datei übernommen wird.
    /// </summary>
    [Fact]
    public void Build_OnlyLocalProps_UsesLocalProps()
    {
        var key = BuildAndRead(runner => runner.WriteLocalProps(LocalPlaceholder), null, null);

        Assert.Equal(LocalPlaceholder, key);
    }

    /// <summary>
    /// Prüft, dass ohne jede Quelle (und ohne props-Datei) der Build gelingt und ein leerer Schlüssel eingebettet wird.
    /// </summary>
    [Fact]
    public void Build_NoSource_EmbedsEmptyKey()
    {
        var key = BuildAndRead(_ => { }, null, null);

        Assert.Equal(string.Empty, key);
    }
}
