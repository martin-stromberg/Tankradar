using Tankradar.MAUI.Services.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die gerätelokale Speicherung der gewählten Fahrzeugkennung durch <see cref="PreferencesVehicleSelectionStore"/>.
/// </summary>
public class PreferencesVehicleSelectionStoreTests : BaseTest
{
    private readonly FakePreferences _preferences = new();

    /// <summary>
    /// Prüft, dass eine gespeicherte Kennung wieder gelesen wird und nur die Kennung unter dem festen Schlüssel liegt.
    /// </summary>
    [Fact]
    public void Store_SetThenGet_RoundTrips()
    {
        var store = new PreferencesVehicleSelectionStore(_preferences);

        store.Set(42);

        Assert.Equal(42, store.Get());
        Assert.Equal("42", _preferences.Get<string?>(PreferencesVehicleSelectionStore.Key, null));
        Assert.Equal(42, new PreferencesVehicleSelectionStore(_preferences).Get());
    }

    /// <summary>
    /// Prüft, dass das Löschen (z. B. nach einer Wiederherstellung) die Kennung entfernt.
    /// </summary>
    [Fact]
    public void Store_Clear_RemovesValue()
    {
        var store = new PreferencesVehicleSelectionStore(_preferences);
        store.Set(42);

        store.Clear();

        Assert.Null(store.Get());
        Assert.False(_preferences.ContainsKey(PreferencesVehicleSelectionStore.Key));
    }

    /// <summary>
    /// Prüft, dass ein ungültiger gespeicherter Wert als „keine Auswahl“ gelesen wird.
    /// </summary>
    /// <param name="stored">Der gespeicherte, ungültige Wert.</param>
    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("-1")]
    [InlineData("99999999999999999999")]
    public void Store_InvalidStoredValue_ReturnsNull(string stored)
    {
        _preferences.Set(PreferencesVehicleSelectionStore.Key, stored);

        Assert.Null(new PreferencesVehicleSelectionStore(_preferences).Get());
    }

    /// <summary>
    /// Prüft, dass getrennte Bereiche (Testmodus je Testdatenverzeichnis) sich nicht gegenseitig beeinflussen.
    /// </summary>
    [Fact]
    public void Store_SharedName_IsolatesValues()
    {
        var scoped = new PreferencesVehicleSelectionStore(_preferences, "scope");
        var standard = new PreferencesVehicleSelectionStore(_preferences);

        scoped.Set(7);

        Assert.Equal(7, scoped.Get());
        Assert.Null(standard.Get());
    }
}
