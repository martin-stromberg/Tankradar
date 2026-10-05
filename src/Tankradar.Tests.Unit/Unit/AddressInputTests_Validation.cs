using Tankradar.MAUI.Services.Geocoding;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Validierung und Normalisierung der Adresseingabe vor dem Aufruf des externen Dienstes.
/// </summary>
public class AddressInputTests_Validation : BaseTest
{
    /// <summary>
    /// Prüft, dass gängige Eingaben (Ort, PLZ, Straße mit Hausnummer, Umlaute) gültig sind.
    /// </summary>
    /// <param name="input">Die Eingabe.</param>
    [Theory]
    [InlineData("Frankfurt")]
    [InlineData("60311")]
    [InlineData("Hauptstraße 1, 10115 Berlin")]
    [InlineData("Köln-Ehrenfeld")]
    [InlineData("Ulm")]
    [InlineData("St. Ingbert (Saar)")]
    [InlineData("Straße des 17. Juni 1")]
    public void Validate_CommonInput_IsValid(string input)
    {
        Assert.Equal(AddressInputError.None, AddressInput.Validate(input, out var normalized));
        Assert.NotEmpty(normalized);
    }

    /// <summary>
    /// Prüft, dass Leerraum am Rand entfernt und innen zu einem Leerzeichen zusammengefasst wird.
    /// </summary>
    [Fact]
    public void Validate_Whitespace_IsNormalized()
    {
        Assert.Equal(AddressInputError.None, AddressInput.Validate("  Bad \t Homburg\r\n  vor der   Höhe ", out var normalized));

        Assert.Equal("Bad Homburg vor der Höhe", normalized);
    }

    /// <summary>
    /// Prüft, dass leere und nur aus Leerraum oder Satzzeichen bestehende Eingaben als leer gelten.
    /// </summary>
    /// <param name="input">Die Eingabe.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    [InlineData("-- ..")]
    public void Validate_EmptyInput_IsEmpty(string? input)
    {
        Assert.Equal(AddressInputError.Empty, AddressInput.Validate(input, out var normalized));
        Assert.Empty(normalized);
    }

    /// <summary>
    /// Prüft die Mindestlänge.
    /// </summary>
    [Fact]
    public void Validate_TooShort_IsRejected()
    {
        Assert.Equal(AddressInputError.TooShort, AddressInput.Validate(" ab ", out _));
        Assert.Equal(AddressInputError.None, AddressInput.Validate("abc", out _));
    }

    /// <summary>
    /// Prüft die Höchstlänge.
    /// </summary>
    [Fact]
    public void Validate_TooLong_IsRejected()
    {
        Assert.Equal(AddressInputError.None, AddressInput.Validate(new string('a', AddressInput.MaxLength), out _));
        Assert.Equal(AddressInputError.TooLong, AddressInput.Validate(new string('a', AddressInput.MaxLength + 1), out _));
    }

    /// <summary>
    /// Prüft, dass Steuerzeichen, Markup- und Anführungszeichen sowie Anfrageparameter-Trenner abgelehnt werden.
    /// </summary>
    /// <param name="input">Die Eingabe.</param>
    [Theory]
    [InlineData("Berlin\u0000")]
    [InlineData("Berlin<script>")]
    [InlineData("Berlin\"")]
    [InlineData("Berlin&format=xml;")]
    [InlineData("Berlin%00")]
    [InlineData("Berlin\\")]
    [InlineData("Berlin|Hamburg")]
    [InlineData("Berlin‮")]
    public void Validate_InvalidCharacters_AreRejected(string input)
    {
        Assert.Equal(AddressInputError.InvalidCharacters, AddressInput.Validate(input, out var normalized));
        Assert.Empty(normalized);
    }

    /// <summary>
    /// Prüft, dass jeder Fehlergrund über eine Beispieleingabe erreichbar ist.
    /// </summary>
    [Fact]
    public void Validate_CoversAllErrors()
    {
        var reached = new[] { null, "", "ab", new string('a', 500), "a<b>c", "Berlin" }
            .Select(input => AddressInput.Validate(input, out _))
            .ToHashSet();

        Assert.Equal(Enum.GetValues<AddressInputError>().ToHashSet(), reached);
    }
}
