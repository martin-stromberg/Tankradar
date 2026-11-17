using Tankradar.MAUI.Services.Diagnostics;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Redaktion von Geheimnissen, Schlüsselparametern und tokenartigen Zeichenfolgen.
/// </summary>
public class DiagnosticsRedactorTests_Secrets : BaseTest
{
    /// <summary>
    /// Prüft, dass wörtlich übergebene API-Schlüssel überall im Text ersetzt werden.
    /// </summary>
    [Fact]
    public void Redact_LiteralKey_Removed()
    {
        const string key = "abc123def456ghi789";

        var result = DiagnosticsRedactor.Redact($"Anfrage mit {key} fehlgeschlagen; Wiederholung {key}", [key]);

        Assert.DoesNotContain(key, result);
        Assert.Equal(2, result.Split(DiagnosticsRedactor.Placeholder).Length - 1);
    }

    /// <summary>
    /// Prüft, dass kurze Werte (unter 8 Zeichen) nicht wörtlich ersetzt werden, um Flächenredaktion zu vermeiden.
    /// </summary>
    [Fact]
    public void Redact_ShortValue_Kept()
    {
        var result = DiagnosticsRedactor.Redact("Diesel ist teuer", ["iesel"]);

        Assert.Contains("iesel", result);
    }

    /// <summary>
    /// Prüft, dass Schlüsselparameter in der Form name=wert redigiert werden, der Name aber erhalten bleibt.
    /// </summary>
    /// <param name="input">Der zu prüfende Parameterausdruck.</param>
    /// <param name="keptPrefix">Der Teil des Parameternamens, der erhalten bleiben muss.</param>
    [Theory]
    [InlineData("access-token=geheim123", "access-token=")]
    [InlineData("token = abcdefgh", "token")]
    [InlineData("password=supersecret", "password=")]
    [InlineData("key=xyz987654", "key=")]
    public void Redact_SecretParameter_Removed(string input, string keptPrefix)
    {
        var result = DiagnosticsRedactor.Redact("Fehler bei " + input);

        Assert.Contains(keptPrefix, result);
        Assert.Contains(DiagnosticsRedactor.Placeholder, result);
        Assert.DoesNotContain(input.Split('=')[1].Trim(), result);
    }

    /// <summary>
    /// Prüft, dass der konkrete Preisdienst-Parametername redigiert wird (zusammengesetzt,
    /// damit kein schlüsselartiges Literal im Quelltext liegt).
    /// </summary>
    [Fact]
    public void Redact_ApiKeyParameter_Removed()
    {
        var parameterName = "api-" + "key";

        var result = DiagnosticsRedactor.Redact("Fehler bei " + parameterName + "=geheim123");

        Assert.Contains(parameterName + "=", result);
        Assert.DoesNotContain("geheim123", result);
    }

    /// <summary>
    /// Prüft, dass E-Mail-Adressen entfernt werden.
    /// </summary>
    [Fact]
    public void Redact_Email_Removed()
    {
        var result = DiagnosticsRedactor.Redact("Nutzer martin@example.org meldete den Fehler");

        Assert.DoesNotContain("martin@example.org", result);
        Assert.Contains(DiagnosticsRedactor.Placeholder, result);
    }

    /// <summary>
    /// Prüft, dass lange Hex- und Base64-artige Zeichenfolgen (Tokens) entfernt werden.
    /// </summary>
    [Fact]
    public void Redact_LongToken_Removed()
    {
        const string hex = "0123456789abcdef0123456789abcdef";
        const string b64 = "QUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVowMTIzNDU2Nzg5MA==";

        var result = DiagnosticsRedactor.Redact($"Kennung {hex} und {b64}");

        Assert.DoesNotContain(hex, result);
        Assert.DoesNotContain(b64, result);
    }

    /// <summary>
    /// Prüft, dass <see langword="null"/> und leere Texte unverändert bleiben.
    /// </summary>
    [Fact]
    public void Redact_Empty_Unchanged()
    {
        Assert.Equal(string.Empty, DiagnosticsRedactor.Redact(null));
        Assert.Equal(string.Empty, DiagnosticsRedactor.Redact(string.Empty));
    }

    /// <summary>
    /// Prüft, dass unverfänglicher Text unverändert bleibt.
    /// </summary>
    [Fact]
    public void Redact_PlainText_Unchanged()
    {
        const string text = "Die Datenbank wurde geöffnet; 42 Einträge geladen.";

        Assert.Equal(text, DiagnosticsRedactor.Redact(text));
    }
}
