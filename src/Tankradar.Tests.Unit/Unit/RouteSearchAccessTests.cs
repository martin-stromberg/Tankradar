using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Routing;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Schlüssel und Schalter der Routensuche: Die Routensuche ist nur mit Schlüssel und eingeschaltetem Schalter aktiv, ohne Aktivierung gibt es keinen Schlüssel
/// für Aufrufe, und der Schlüssel taucht nie in Protokollen auf.
/// </summary>
public class RouteSearchAccessTests : BaseTest
{
    private const string Key = "ZZPLATZHALTER-SCHLUESSEL-ZZ";

    private readonly FakeApiKeyStore _keyStore = new();
    private readonly FakeApiKeyStore _enabledStore = new();
    private readonly RecordingLogger<RouteSearchAccess> _logger = new();

    private RouteSearchAccess Create()
    {
        return new RouteSearchAccess(_keyStore, _enabledStore, _logger);
    }

    /// <summary>
    /// Prüft den Ausgangszustand: kein Schlüssel, nicht aktiviert, kein Schlüssel für Aufrufe.
    /// </summary>
    [Fact]
    public async Task Initial_HasNoKeyAndIsDisabled()
    {
        var access = Create();

        Assert.False(await access.HasKeyAsync());
        Assert.False(await access.IsEnabledAsync());
        Assert.Null(await access.GetApiKeyAsync());
        Assert.Null(await access.GetStoredKeyAsync());
    }

    /// <summary>
    /// Prüft, dass das Speichern den Schlüssel ablegt, die Routensuche aber nicht von selbst einschaltet und weiterhin keinen Schlüssel für Aufrufe liefert.
    /// </summary>
    [Fact]
    public async Task SaveKeyAsync_StoresKeyButDoesNotEnable()
    {
        var access = Create();
        var changes = 0;
        access.Changed += (_, _) => changes++;

        await access.SaveKeyAsync("  " + Key + " ");

        Assert.True(await access.HasKeyAsync());
        Assert.Equal(Key, await access.GetStoredKeyAsync());
        Assert.False(await access.IsEnabledAsync());
        Assert.Null(await access.GetApiKeyAsync());
        Assert.Equal(1, changes);
    }

    /// <summary>
    /// Prüft, dass ein unzulässiger Schlüssel nicht gespeichert wird.
    /// </summary>
    /// <param name="key">Der Schlüssel.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("mit leerzeichen")]
    [InlineData("zeilen\r\numbruch")]
    public async Task SaveKeyAsync_InvalidKey_Throws(string key)
    {
        var access = Create();

        await Assert.ThrowsAsync<ArgumentException>(() => access.SaveKeyAsync(key));

        Assert.Equal(0, _keyStore.SetCount);
    }

    /// <summary>
    /// Prüft, dass sich die Routensuche ohne Schlüssel nicht einschalten lässt.
    /// </summary>
    [Fact]
    public async Task SetEnabledAsync_WithoutKey_IsRefused()
    {
        var access = Create();

        Assert.False(await access.SetEnabledAsync(true));

        Assert.False(await access.IsEnabledAsync());
        Assert.Equal(0, _enabledStore.SetCount);
    }

    /// <summary>
    /// Prüft Einschalten und Ausschalten mit Schlüssel: Nur im eingeschalteten Zustand liefert der Zugang den Schlüssel für Aufrufe.
    /// </summary>
    [Fact]
    public async Task SetEnabledAsync_WithKey_TogglesAvailabilityAndApiKey()
    {
        var access = Create();
        await access.SaveKeyAsync(Key);
        var changes = 0;
        access.Changed += (_, _) => changes++;

        Assert.True(await access.SetEnabledAsync(true));

        Assert.True(await access.IsEnabledAsync());
        Assert.Equal(Key, await access.GetApiKeyAsync());

        Assert.True(await access.SetEnabledAsync(false));

        Assert.False(await access.IsEnabledAsync());
        Assert.Null(await access.GetApiKeyAsync());
        Assert.Equal(Key, await access.GetStoredKeyAsync());
        Assert.Equal(2, changes);
    }

    /// <summary>
    /// Prüft, dass das Entfernen des Schlüssels die Routensuche ausschaltet und ein späteres Speichern sie nicht wieder einschaltet.
    /// </summary>
    [Fact]
    public async Task RemoveKeyAsync_DisablesRouteSearch()
    {
        var access = Create();
        await access.SaveKeyAsync(Key);
        await access.SetEnabledAsync(true);
        var changes = 0;
        access.Changed += (_, _) => changes++;

        await access.RemoveKeyAsync();

        Assert.False(await access.HasKeyAsync());
        Assert.False(await access.IsEnabledAsync());
        Assert.Null(await access.GetApiKeyAsync());
        Assert.Equal(1, changes);

        await access.SaveKeyAsync(Key);

        Assert.False(await access.IsEnabledAsync());
    }

    /// <summary>
    /// Prüft, dass ein Schalter, der noch gesetzt ist, während der Schlüssel fehlt (z. B. nach einer Wiederherstellung auf einem anderen Gerät), die Routensuche nicht aktiviert.
    /// </summary>
    [Fact]
    public async Task SwitchOnWithoutKey_IsNotEnabled()
    {
        _enabledStore.Stored = "1";

        var access = Create();

        Assert.False(await access.IsEnabledAsync());
        Assert.Null(await access.GetStoredKeyAsync());
    }

    /// <summary>
    /// Prüft, dass ein nicht lesbarer Speicher als „kein Schlüssel“ bzw. „aus“ gilt und nur der Typ der Ausnahme protokolliert wird, nie der Schlüssel.
    /// </summary>
    [Fact]
    public async Task UnreadableStores_AreTreatedAsOffAndNeverLogTheKey()
    {
        _keyStore.Stored = Key;
        _enabledStore.Stored = "1";
        var access = Create();
        Assert.True(await access.IsEnabledAsync());

        _keyStore.ThrowOnGet = true;
        _enabledStore.ThrowOnGet = true;

        Assert.False(await access.HasKeyAsync());
        Assert.False(await access.IsEnabledAsync());
        Assert.Null(await access.GetApiKeyAsync());
        Assert.Contains("InvalidOperationException", _logger.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain(Key, _logger.AllText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass der Schalter zuerst entfernt wird: Scheitert das Entfernen des Schlüssels, ist die Routensuche trotzdem aus.
    /// </summary>
    [Fact]
    public async Task RemoveKeyAsync_WhenKeyRemovalFails_SwitchIsAlreadyOff()
    {
        var access = Create();
        await access.SaveKeyAsync(Key);
        await access.SetEnabledAsync(true);
        _keyStore.ThrowOnRemove = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => access.RemoveKeyAsync());

        Assert.False(await access.IsEnabledAsync());
        Assert.Null(await access.GetApiKeyAsync());
    }

    /// <summary>
    /// Prüft die Umgebung außerhalb des Testmodus: Es werden die echten Ablagen verwendet, und die Testvariable wird ignoriert.
    /// </summary>
    [Fact]
    public async Task Create_OutsideTestMode_UsesPlatformStores()
    {
        var created = new List<string>();
        var access = RouteSearchAccess.Create(
            name => name == TestDataPaths.RoutingTestApiKeyEnvironmentVariable ? Key : null,
            () =>
            {
                created.Add("key");
                return _keyStore;
            },
            () =>
            {
                created.Add("switch");
                return _enabledStore;
            },
            _logger);

        Assert.Equal(["key", "switch"], created);
        Assert.False(await access.HasKeyAsync());
        Assert.Equal(0, _keyStore.SetCount);
    }

    /// <summary>
    /// Prüft den Testmodus: Die echten Ablagen werden nie erzeugt; ein Test-Schlüssel belegt Schlüssel und Schalter im Speicher vor, ohne ihn war die Routensuche aus.
    /// </summary>
    [Fact]
    public async Task Create_TestMode_UsesMemoryAndSeedsFromTestKey()
    {
        Func<IApiKeyStore> forbidden = () => throw new InvalidOperationException("Die echte Ablage darf im Testmodus nicht erzeugt werden.");

        var seeded = RouteSearchAccess.Create(
            name => name switch
            {
                var n when n == TestDataPaths.TestDataPathEnvironmentVariable => "testdata",
                var n when n == TestDataPaths.RoutingTestApiKeyEnvironmentVariable => Key,
                _ => null,
            },
            forbidden,
            forbidden,
            _logger);
        var unseeded = RouteSearchAccess.Create(name => name == TestDataPaths.TestDataPathEnvironmentVariable ? "testdata" : null, forbidden, forbidden, _logger);
        var invalid = RouteSearchAccess.Create(
            name => name switch
            {
                var n when n == TestDataPaths.TestDataPathEnvironmentVariable => "testdata",
                var n when n == TestDataPaths.RoutingTestApiKeyEnvironmentVariable => "mit leerzeichen",
                _ => null,
            },
            forbidden,
            forbidden,
            _logger);

        Assert.True(await seeded.IsEnabledAsync());
        Assert.Equal(Key, await seeded.GetApiKeyAsync());
        Assert.False(await unseeded.HasKeyAsync());
        Assert.False(await unseeded.IsEnabledAsync());
        Assert.False(await invalid.HasKeyAsync());
        Assert.Throws<ArgumentNullException>(() => RouteSearchAccess.Create(null!, forbidden, forbidden, _logger));
    }

    /// <summary>
    /// Prüft die Schreibweise des Schlüssels.
    /// </summary>
    /// <param name="input">Die Eingabe.</param>
    /// <param name="expected">Der erwartete Fehler.</param>
    [Theory]
    [InlineData(null, RoutingKeyError.Empty)]
    [InlineData("  ", RoutingKeyError.Empty)]
    [InlineData("abc def", RoutingKeyError.InvalidCharacters)]
    [InlineData("schlüssel", RoutingKeyError.InvalidCharacters)]
    [InlineData("a\tb", RoutingKeyError.InvalidCharacters)]
    [InlineData("5b3ce3597851110001cf6248abcdef", RoutingKeyError.None)]
    [InlineData(" eyJvcmciOiI1YjNjZQ== ", RoutingKeyError.None)]
    public void RoutingKeyFormat_Validate_ClassifiesInput(string? input, RoutingKeyError expected)
    {
        Assert.Equal(expected, RoutingKeyFormat.Validate(input, out var key));
        Assert.Equal(expected == RoutingKeyError.None ? input!.Trim() : string.Empty, key);
    }

    /// <summary>
    /// Prüft die Längengrenze des Schlüssels.
    /// </summary>
    [Fact]
    public void RoutingKeyFormat_Validate_RejectsTooLongKey()
    {
        Assert.Equal(RoutingKeyError.None, RoutingKeyFormat.Validate(new string('a', RoutingKeyFormat.MaxLength), out _));
        Assert.Equal(RoutingKeyError.TooLong, RoutingKeyFormat.Validate(new string('a', RoutingKeyFormat.MaxLength + 1), out _));
    }
}
