namespace FlaUI.Core.Exceptions;

/// <summary>
/// Nachbildung der gleichnamigen Ausnahme der UI-Automation (das Unit-Testprojekt verweist nicht auf FlaUI); <c>TransientWait</c> erkennt die Ausnahme typisiert; im E2E-Projekt bindet derselbe Code an den echten FlaUI-Typ, eine Umbenennung dort fällt beim Kompilieren auf.
/// </summary>
public sealed class ElementNotAvailableException : Exception
{
    /// <summary>
    /// Erstellt die Ausnahme.
    /// </summary>
    /// <param name="message">Die Meldung.</param>
    public ElementNotAvailableException(string message)
        : base(message)
    {
    }
}
