namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Der Grund, aus dem die Fotoauswahl oder die Kamera nicht genutzt werden kann.
/// </summary>
public enum ReceiptPhotoAccessProblem
{
    /// <summary>
    /// Der Nutzer hat den Zugriff verweigert oder er ist (z. B. durch Einschränkungen) nicht erlaubt.
    /// </summary>
    PermissionDenied,

    /// <summary>
    /// Die Funktion steht auf diesem Gerät nicht zur Verfügung.
    /// </summary>
    NotSupported,
}

/// <summary>
/// Meldet, dass ein Belegfoto wegen fehlender Berechtigung oder fehlender Geräteunterstützung nicht ausgewählt oder aufgenommen werden konnte.
/// Kapselt die Plattformausnahmen, damit das ViewModel nicht von .NET MAUI abhängt.
/// </summary>
public sealed class ReceiptPhotoAccessException : Exception
{
    /// <summary>
    /// Erstellt die Ausnahme.
    /// </summary>
    /// <param name="problem">Der Grund.</param>
    /// <param name="innerException">Die ursprüngliche Plattformausnahme.</param>
    public ReceiptPhotoAccessException(ReceiptPhotoAccessProblem problem, Exception? innerException = null)
        : base($"Zugriff auf Belegfoto nicht möglich ({problem}).", innerException)
    {
        Problem = problem;
    }

    /// <summary>
    /// Der Grund.
    /// </summary>
    public ReceiptPhotoAccessProblem Problem { get; }
}
