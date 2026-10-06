using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Geocoding;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Texte der Adresssuche: Beschriftungen der Sucharten, Meldungen zu Eingabefehlern und zur Adressauflösung sowie die Quellenangabe.
/// </summary>
public class SearchTextsTests_Address : BaseTest
{
    /// <summary>
    /// Prüft die Beschriftungen aller Sucharten.
    /// </summary>
    [Fact]
    public void GetModeLabel_CoversAllModes()
    {
        Assert.Equal("Aktueller Standort", SearchTexts.GetModeLabel(SearchMode.CurrentLocation));
        Assert.Equal("Adresse, Ort oder PLZ", SearchTexts.GetModeLabel(SearchMode.Address));
        Assert.All(Enum.GetValues<SearchMode>(), mode => Assert.NotEmpty(SearchTexts.GetModeLabel(mode)));
    }

    /// <summary>
    /// Prüft, dass jeder Eingabefehler eine eigene, nicht leere Meldung hat und „gültig“ keine.
    /// </summary>
    [Fact]
    public void GetAddressInputMessage_HasDistinctMessageForEachError()
    {
        var errors = Enum.GetValues<AddressInputError>().Where(error => error != AddressInputError.None).ToList();

        Assert.Equal(string.Empty, SearchTexts.GetAddressInputMessage(AddressInputError.None));
        Assert.Equal(errors.Count, errors.Select(SearchTexts.GetAddressInputMessage).Distinct().Count());
        Assert.All(errors, error => Assert.NotEmpty(SearchTexts.GetAddressInputMessage(error)));
        Assert.Equal("Bitte eine Adresse, einen Ort oder eine Postleitzahl eingeben.", SearchTexts.GetAddressInputMessage(AddressInputError.Empty));
    }

    /// <summary>
    /// Prüft, dass jeder nicht erfolgreiche Ausgang eine Meldung hat und bei fehlender Verbindung der Offline-Hinweis erscheint.
    /// </summary>
    [Fact]
    public void GetGeocodingMessage_CoversAllStatuses()
    {
        Assert.Equal(string.Empty, SearchTexts.GetGeocodingMessage(GeocodingStatus.Found, isOnline: true));
        foreach (var status in Enum.GetValues<GeocodingStatus>().Where(status => status != GeocodingStatus.Found))
        {
            Assert.NotEmpty(SearchTexts.GetGeocodingMessage(status, isOnline: true));
        }

        Assert.Equal(SearchTexts.GeocodingUnavailable, SearchTexts.GetGeocodingMessage(GeocodingStatus.Unavailable, isOnline: true));
        Assert.Equal(SearchTexts.AddressOffline, SearchTexts.GetGeocodingMessage(GeocodingStatus.Unavailable, isOnline: false));
        Assert.Equal(SearchTexts.AddressNotFound, SearchTexts.GetGeocodingMessage(GeocodingStatus.NotFound, isOnline: false));
    }

    /// <summary>
    /// Prüft die Quellenangabe für OpenStreetMap und das Format des Ortshinweises.
    /// </summary>
    [Fact]
    public void Attribution_AndResolvedPlace_AreFormatted()
    {
        Assert.Contains("OpenStreetMap", SearchTexts.OsmAttribution, StringComparison.Ordinal);
        Assert.Contains("©", SearchTexts.OsmAttribution, StringComparison.Ordinal);
        Assert.Equal("Suche rund um: Berlin", SearchTexts.FormatResolvedPlace("Berlin"));
    }

    /// <summary>
    /// Prüft, dass die Meldungstexte keine technischen Begriffe enthalten.
    /// </summary>
    [Fact]
    public void Messages_AreFreeOfTechnicalTerms()
    {
        foreach (var status in Enum.GetValues<GeocodingStatus>())
        {
            var text = SearchTexts.GetGeocodingMessage(status, isOnline: true);
            Assert.DoesNotContain("Exception", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(status.ToString(), text, StringComparison.Ordinal);
            Assert.DoesNotContain("HTTP", text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
