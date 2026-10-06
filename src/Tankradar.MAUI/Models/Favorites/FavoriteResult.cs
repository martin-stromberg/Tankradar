namespace Tankradar.MAUI.Models.Favorites;

/// <summary>
/// Ergebnis einer Änderung an Favoritengruppen oder Favoriten.
/// </summary>
public enum FavoriteResult
{
    /// <summary>
    /// Die Änderung wurde gespeichert.
    /// </summary>
    Ok,

    /// <summary>
    /// Der Gruppenname ist leer oder zu lang.
    /// </summary>
    InvalidName,

    /// <summary>
    /// Beschreibung oder Notiz sind zu lang.
    /// </summary>
    InvalidText,

    /// <summary>
    /// Eine Gruppe mit diesem Namen gibt es bereits (ohne Beachtung der Groß- und Kleinschreibung).
    /// </summary>
    DuplicateName,

    /// <summary>
    /// Die Gruppe gibt es nicht (mehr).
    /// </summary>
    GroupNotFound,

    /// <summary>
    /// Die Tankstelle ist lokal nicht bekannt und kann daher nicht gespeichert werden.
    /// </summary>
    StationUnknown,

    /// <summary>
    /// Die Tankstelle gehört der Gruppe bereits an.
    /// </summary>
    AlreadyMember,

    /// <summary>
    /// Die Tankstelle gehört der Gruppe nicht an.
    /// </summary>
    NotMember,
}
