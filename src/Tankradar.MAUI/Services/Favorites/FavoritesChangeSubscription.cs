namespace Tankradar.MAUI.Services.Favorites;

/// <summary>
/// Meldet einen Besitzer (z. B. ein ViewModel) beim Änderungsereignis des Favoritendienstes an, ohne ihn am Leben zu halten: Der Dienst ist ein Singleton, die Seiten dagegen
/// werden häufig neu erzeugt. Ist der Besitzer nicht mehr erreichbar, meldet sich die Anmeldung beim nächsten Ereignis selbst ab.
/// </summary>
public static class FavoritesChangeSubscription
{
    /// <summary>
    /// Ruft <paramref name="onChanged"/> bei jeder Änderung der Favoriten auf, solange der Besitzer existiert.
    /// </summary>
    /// <typeparam name="T">Der Typ des Besitzers.</typeparam>
    /// <param name="service">Der Favoritendienst.</param>
    /// <param name="owner">Der Besitzer; wird nur schwach referenziert.</param>
    /// <param name="onChanged">Die Reaktion; sie darf den Besitzer nicht erfassen (statisch formulieren) und erhält ihn als Argument.</param>
    public static void Attach<T>(IFavoritesService service, T owner, Action<T> onChanged)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(onChanged);

        // Das Ereignis kommt von einem Threadpool-Thread; die Reaktion läuft im Kontext, in dem sich die Ansicht angemeldet hat (UI-Thread der App).
        var context = SynchronizationContext.Current;
        var weak = new WeakReference<T>(owner);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            if (weak.TryGetTarget(out var target))
            {
                if (context is not null && SynchronizationContext.Current != context)
                {
                    context.Post(_ => onChanged(target), null);
                }
                else
                {
                    onChanged(target);
                }
            }
            else
            {
                service.Changed -= handler;
            }
        };
        service.Changed += handler;
    }
}
