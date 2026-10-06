using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass der Schlüssel bei diagnostischer Ausführlichkeit und im Binlog nie im MSBuild-Log erscheint, aber in der gebauten Assembly steht (nur Platzhalter, nie der echte Schlüssel).
/// </summary>
public class ApiKeyBuildTests_Log
{
    private const string PrimaryPlaceholder = "ZZPLACEHOLDERPRIMARYZZ";
    private const string SecretPlaceholder = "ZZPLACEHOLDERSECRETZZ";
    private const string LocalPlaceholder = "ZZPLACEHOLDERLOCALZZ";

    private static void AssertNotLogged(ApiKeyBuildResult result, string placeholder)
    {
        Assert.Equal(0, result.ExitCode);
        Assert.NotEmpty(result.BinaryLog);
        Assert.DoesNotContain(placeholder, result.ConsoleLog, StringComparison.Ordinal);
        Assert.DoesNotContain(placeholder, result.BinaryLog, StringComparison.Ordinal);
        Assert.Equal(placeholder, ApiKeyBuildRunner.ReadEmbeddedKey(result.AssemblyPath));
    }

    /// <summary>
    /// Prüft den Schlüssel aus <c>TANKRADAR_FUEL_PRICE_API_KEY</c>.
    /// </summary>
    [Fact]
    public void Build_PrimaryEnvironmentVariable_IsNotLogged()
    {
        using var runner = new ApiKeyBuildRunner();

        AssertNotLogged(runner.Build(PrimaryPlaceholder, null), PrimaryPlaceholder);
    }

    /// <summary>
    /// Prüft den Schlüssel aus <c>FUEL_PRICE_API_KEY</c>.
    /// </summary>
    [Fact]
    public void Build_SecretEnvironmentVariable_IsNotLogged()
    {
        using var runner = new ApiKeyBuildRunner();

        AssertNotLogged(runner.Build(null, SecretPlaceholder), SecretPlaceholder);
    }

    /// <summary>
    /// Prüft den Schlüssel aus <c>tankerkoenig.local.props</c>.
    /// </summary>
    [Fact]
    public void Build_LocalProps_IsNotLogged()
    {
        using var runner = new ApiKeyBuildRunner();
        runner.WriteLocalProps(LocalPlaceholder);

        AssertNotLogged(runner.Build(null, null), LocalPlaceholder);
    }
}
