using Tankradar.MAUI.Services.Map;

namespace Tankradar.MAUI.Views.Controls;

/// <summary>
/// Daten der Ereignisse <see cref="StationMapView.ViewportChanged"/> (Kartenausschnitt nach einer Bewegung durch den Anwender) und <see cref="StationMapView.ViewportFitted"/> (Kartenausschnitt nach dem Einpassen auf ein neues Ergebnis).
/// </summary>
public sealed class ViewportChangedEventArgs : EventArgs
{
    /// <summary>
    /// Erstellt die Ereignisdaten.
    /// </summary>
    /// <param name="viewport">Der neue Kartenausschnitt.</param>
    public ViewportChangedEventArgs(MapViewport viewport)
    {
        Viewport = viewport;
    }

    /// <summary>
    /// Der neue Kartenausschnitt.
    /// </summary>
    public MapViewport Viewport { get; }
}
