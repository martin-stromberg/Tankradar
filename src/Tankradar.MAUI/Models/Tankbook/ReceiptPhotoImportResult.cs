namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ergebnis der Übernahme eines Belegfotos in die Ablage.
/// </summary>
/// <param name="FileName">Der Dateiname in der Ablage; <see langword="null"/> bei einem Fehler.</param>
/// <param name="Error">Der Fehler oder <see cref="ReceiptPhotoError.None"/>.</param>
/// <returns>Der Wert.</returns>
public sealed record ReceiptPhotoImportResult(string? FileName, ReceiptPhotoError Error);
