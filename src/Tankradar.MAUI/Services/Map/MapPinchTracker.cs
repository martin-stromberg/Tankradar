namespace Tankradar.MAUI.Services.Map;

/// <summary>
/// Hält den Zustand einer Kneifgeste: den Gesamtfaktor (Produkt der Einzelfaktoren seit dem letzten Update) und den letzten Gestenmittelpunkt.
/// Plattformunabhängig, damit die Auswertung ohne Oberfläche prüfbar ist.
/// </summary>
public sealed class MapPinchTracker
{
    /// <summary>
    /// Gibt an, ob gerade eine Kneifgeste läuft.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Der Gesamtfaktor der laufenden Geste (1 ohne Geste).
    /// </summary>
    /// <returns>Der Wert.</returns>
    public double Factor { get; private set; } = 1;

    /// <summary>
    /// Die horizontale Lage des letzten Gestenmittelpunkts in Pixeln.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public double OriginX { get; private set; }

    /// <summary>
    /// Die vertikale Lage des letzten Gestenmittelpunkts in Pixeln.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public double OriginY { get; private set; }

    /// <summary>
    /// Beginnt eine Geste am angegebenen Mittelpunkt.
    /// </summary>
    /// <param name="originX">Horizontale Lage des Mittelpunkts in Pixeln.</param>
    /// <param name="originY">Vertikale Lage des Mittelpunkts in Pixeln.</param>
    public void Start(double originX, double originY)
    {
        IsActive = true;
        Factor = 1;
        OriginX = originX;
        OriginY = originY;
    }

    /// <summary>
    /// Meldet einen Einzelfaktor seit dem letzten Update und den aktuellen Mittelpunkt; ohne laufende Geste oder bei ungültigem Faktor ohne Wirkung auf den Faktor.
    /// </summary>
    /// <param name="scale">Der Einzelfaktor (muss endlich und größer als 0 sein).</param>
    /// <param name="originX">Horizontale Lage des Mittelpunkts in Pixeln.</param>
    /// <param name="originY">Vertikale Lage des Mittelpunkts in Pixeln.</param>
    public void Update(double scale, double originX, double originY)
    {
        if (!IsActive)
        {
            return;
        }

        OriginX = originX;
        OriginY = originY;
        if (double.IsFinite(scale) && scale > 0)
        {
            Factor *= scale;
        }
    }

    /// <summary>
    /// Beendet die Geste und setzt den Faktor zurück.
    /// </summary>
    public void End()
    {
        IsActive = false;
        Factor = 1;
    }
}
