using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Lässt den Nutzer ein Belegfoto auswählen oder aufnehmen (in der App über die Systemauswahl, im Testmodus über eine vorgegebene Datei).
/// </summary>
public interface IReceiptPhotoPicker
{
    /// <summary>
    /// Gibt an, ob das Gerät Fotos aufnehmen kann.
    /// </summary>
    bool IsCaptureSupported { get; }

    /// <summary>
    /// Lässt ein Foto aus der Fotomediathek auswählen.
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Foto oder <see langword="null"/>, wenn der Nutzer abbricht.</returns>
    Task<PickedReceiptPhoto?> PickAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lässt ein Foto mit der Kamera aufnehmen.
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Foto oder <see langword="null"/>, wenn der Nutzer abbricht oder keine Aufnahme möglich ist.</returns>
    Task<PickedReceiptPhoto?> CaptureAsync(CancellationToken cancellationToken = default);
}
