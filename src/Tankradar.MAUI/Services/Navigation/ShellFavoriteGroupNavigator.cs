namespace Tankradar.MAUI.Services.Navigation;

/// <summary>
/// Umsetzung von <see cref="IFavoriteGroupNavigator"/> über die Shell-Navigation der App.
/// </summary>
public sealed class ShellFavoriteGroupNavigator : IFavoriteGroupNavigator
{
    /// <summary>
    /// Die Route der Gruppenansicht.
    /// </summary>
    public const string GroupRoute = "favoritegroup";

    /// <summary>
    /// Der Name des Navigationsparameters, der die Kennung der Gruppe trägt.
    /// </summary>
    public const string GroupIdParameter = "groupId";

    /// <inheritdoc />
    public Task OpenGroupAsync(long groupId)
    {
        var parameters = new Dictionary<string, object> { [GroupIdParameter] = groupId };
        return Shell.Current.GoToAsync(GroupRoute, parameters);
    }

    /// <inheritdoc />
    public Task CloseAsync()
    {
        return Shell.Current.GoToAsync("..");
    }
}
