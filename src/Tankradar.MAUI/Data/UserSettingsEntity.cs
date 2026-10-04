using Tankradar.MAUI.Models;

namespace Tankradar.MAUI.Data;

/// <summary>
/// Tabelle <c>UserSettings</c>: die einzelne Zeile (Id 1) mit den skalaren Einstellungen. Enum-Werte werden als Name gespeichert.
/// </summary>
public class UserSettingsEntity
{
    /// <summary>
    /// Die Id der einzigen Zeile.
    /// </summary>
    public const int SingletonId = 1;

    /// <summary>
    /// Primärschlüssel; immer <see cref="SingletonId"/>.
    /// </summary>
    public int Id { get; set; } = SingletonId;

    /// <summary>
    /// Name des <see cref="Models.GpsUsage"/>-Werts.
    /// </summary>
    public string GpsUsage { get; set; } = string.Empty;

    /// <summary>
    /// Name des <see cref="Models.ResultView"/>-Werts.
    /// </summary>
    public string ResultView { get; set; } = string.Empty;

    /// <summary>
    /// Name des <see cref="Models.ResultSortOrder"/>-Werts.
    /// </summary>
    public string ResultSortOrder { get; set; } = string.Empty;

    /// <summary>
    /// Übernimmt die skalaren Einstellungen aus <paramref name="settings"/> in diese Zeile.
    /// </summary>
    /// <param name="settings">Die zu speichernden Einstellungen.</param>
    public void Update(AppSettings settings)
    {
        GpsUsage = settings.GpsUsage.ToString();
        ResultView = settings.ResultView.ToString();
        ResultSortOrder = settings.ResultSortOrder.ToString();
    }
}
