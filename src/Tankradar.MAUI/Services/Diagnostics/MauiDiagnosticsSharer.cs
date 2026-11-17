namespace Tankradar.MAUI.Services.Diagnostics;

/// <summary>
/// Umsetzung von <see cref="IDiagnosticsSharer"/> über <see cref="Share.Default"/> (iOS: Teilen-Blatt mit „In Dateien sichern“, Windows: Freigabe-Dialog).
/// </summary>
public sealed class MauiDiagnosticsSharer : IDiagnosticsSharer
{
    /// <inheritdoc />
    public Task ShareAsync(string fullPath, string title, CancellationToken cancellationToken = default)
    {
        return Share.Default.RequestAsync(new ShareFileRequest(title, new ShareFile(fullPath)));
    }
}
