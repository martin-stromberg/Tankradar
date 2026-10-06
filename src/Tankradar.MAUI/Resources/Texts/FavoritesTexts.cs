using System.Globalization;
using Tankradar.MAUI.Models.Favorites;

namespace Tankradar.MAUI.Resources.Texts;

/// <summary>
/// Zentrale deutsche UI-Texte der Favoritengruppen (Detailansicht, Gruppenübersicht und Gruppenansicht).
/// </summary>
public static class FavoritesTexts
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>
    /// Titel des Bereichs „Favoriten“.
    /// </summary>
    public const string PageTitle = "Favoriten";

    /// <summary>
    /// Titel der Gruppenansicht.
    /// </summary>
    public const string GroupPageTitle = "Favoritengruppe";

    /// <summary>
    /// Überschrift der Gruppenliste.
    /// </summary>
    public const string GroupsHeading = "Favoritengruppen";

    /// <summary>
    /// Hinweis, dass es noch keine Gruppen gibt.
    /// </summary>
    public const string NoGroups = "Noch keine Favoritengruppen. Öffne eine Tankstelle und füge sie in der Detailansicht über „Zu Favoriten hinzufügen“ einer Gruppe hinzu, oder lege hier eine Gruppe an.";

    /// <summary>
    /// Hinweis, dass die Gruppe keine Tankstellen enthält.
    /// </summary>
    public const string GroupEmpty = "Diese Gruppe enthält noch keine Tankstellen. Füge Tankstellen in ihrer Detailansicht hinzu.";

    /// <summary>
    /// Beschriftung „Neue Gruppe“.
    /// </summary>
    public const string NewGroup = "Neue Gruppe";

    /// <summary>
    /// Beschriftung des Namensfelds.
    /// </summary>
    public const string NameLabel = "Name der Gruppe";

    /// <summary>
    /// Beschriftung des Beschreibungsfelds.
    /// </summary>
    public const string DescriptionLabel = "Beschreibung, optional";

    /// <summary>
    /// Beschriftung „Anlegen“.
    /// </summary>
    public const string Create = "Gruppe anlegen";

    /// <summary>
    /// Beschriftung „Speichern“.
    /// </summary>
    public const string Save = "Speichern";

    /// <summary>
    /// Beschriftung „Abbrechen“.
    /// </summary>
    public const string Cancel = "Abbrechen";

    /// <summary>
    /// Beschriftung „Gruppe bearbeiten“.
    /// </summary>
    public const string EditGroup = "Gruppe bearbeiten";

    /// <summary>
    /// Beschriftung „Gruppe löschen“.
    /// </summary>
    public const string DeleteGroup = "Gruppe löschen";

    /// <summary>
    /// Beschriftung der Bestätigung des Löschens.
    /// </summary>
    public const string ConfirmDelete = "Ja, löschen";

    /// <summary>
    /// Beschriftung „Gruppe öffnen“.
    /// </summary>
    public const string OpenGroup = "Gruppe öffnen";

    /// <summary>
    /// Hinweis für Bedienhilfen zum Öffnen einer Gruppe.
    /// </summary>
    public const string OpenGroupHint = "Zeigt die Tankstellen der Gruppe";

    /// <summary>
    /// Beschriftung „Bearbeiten“ eines Eintrags.
    /// </summary>
    public const string EditEntry = "Notiz und Priorität";

    /// <summary>
    /// Beschriftung der Notiz.
    /// </summary>
    public const string NoteLabel = "Notiz, optional";

    /// <summary>
    /// Beschriftung der Priorität.
    /// </summary>
    public const string PriorityLabel = "Priorität";

    /// <summary>
    /// Überschrift der Karte „Favoritengruppen“ in der Detailansicht.
    /// </summary>
    public const string DetailHeading = "Favoritengruppen";

    /// <summary>
    /// Einleitung der zugeordneten Gruppen in der Detailansicht.
    /// </summary>
    public const string AssignedTo = "Bereits zugeordnet zu:";

    /// <summary>
    /// Hinweis, dass die Tankstelle keiner Gruppe angehört.
    /// </summary>
    public const string NotAssigned = "Noch keiner Favoritengruppe zugeordnet.";

    /// <summary>
    /// Beschriftung der Schaltfläche zum Hinzufügen.
    /// </summary>
    public const string AddToFavorites = "Zu Favoriten hinzufügen";

    /// <summary>
    /// Beschriftung der Schaltfläche zum Entfernen.
    /// </summary>
    public const string RemoveFromFavorites = "Aus Favoriten entfernen";

    /// <summary>
    /// Aufforderung, eine bestehende Gruppe zu wählen.
    /// </summary>
    public const string ChooseGroup = "Bestehende Gruppe wählen:";

    /// <summary>
    /// Aufforderung, eine neue Gruppe zu benennen.
    /// </summary>
    public const string OrNewGroup = "Oder neue Gruppe anlegen:";

    /// <summary>
    /// Beschriftung „Anlegen und hinzufügen“.
    /// </summary>
    public const string CreateAndAdd = "Anlegen und hinzufügen";

    /// <summary>
    /// Aufforderung, die Gruppen zum Entfernen zu wählen.
    /// </summary>
    public const string ChooseRemove = "Aus welchen Gruppen soll die Tankstelle entfernt werden?";

    /// <summary>
    /// Beschriftung „Entfernen“.
    /// </summary>
    public const string ConfirmRemove = "Entfernen";

    /// <summary>
    /// Hinweis, dass die Tankstelle schon allen Gruppen angehört.
    /// </summary>
    public const string AllGroupsAssigned = "Die Tankstelle gehört bereits allen Gruppen an.";

    /// <summary>
    /// Fehlermeldung beim Laden.
    /// </summary>
    public const string LoadFailed = "Die Favoriten konnten nicht geladen werden.";

    /// <summary>
    /// Fehlermeldung beim Speichern.
    /// </summary>
    public const string SaveFailed = "Die Änderung konnte nicht gespeichert werden.";

    /// <summary>
    /// Formatiert die Zahl der Tankstellen einer Gruppe („1 Tankstelle“, „3 Tankstellen“).
    /// </summary>
    /// <param name="count">Die Zahl.</param>
    /// <returns>Der Text.</returns>
    public static string FormatStationCount(int count)
    {
        return count == 1 ? "1 Tankstelle" : string.Create(German, $"{count} Tankstellen");
    }

    /// <summary>
    /// Formatiert die Rückfrage vor dem Löschen einer Gruppe.
    /// </summary>
    /// <param name="name">Der Name der Gruppe.</param>
    /// <param name="stationCount">Die Zahl der Tankstellen in der Gruppe.</param>
    /// <returns>Der Text.</returns>
    public static string FormatDeleteQuestion(string name, int stationCount)
    {
        var effect = stationCount == 0
            ? "Sie enthält keine Tankstellen."
            : string.Create(German, $"Die Zuordnung von {FormatStationCount(stationCount)} zu dieser Gruppe entfällt; in anderen Gruppen bleiben sie erhalten.");
        return string.Create(German, $"Gruppe „{name}“ wirklich löschen? {effect}");
    }

    /// <summary>
    /// Liefert die Beschriftung einer Priorität.
    /// </summary>
    /// <param name="priority">Die Priorität.</param>
    /// <returns>Der Text.</returns>
    public static string GetPriorityLabel(FavoritePriority priority)
    {
        return priority switch
        {
            FavoritePriority.None => "Keine",
            FavoritePriority.Low => "Niedrig",
            FavoritePriority.Medium => "Mittel",
            FavoritePriority.High => "Hoch",
            _ => "Keine",
        };
    }

    /// <summary>
    /// Formatiert die Angabe der Priorität eines Eintrags („Priorität: Hoch“).
    /// </summary>
    /// <param name="priority">Die Priorität.</param>
    /// <returns>Der Text.</returns>
    public static string FormatPriority(FavoritePriority priority)
    {
        return $"{PriorityLabel}: {GetPriorityLabel(priority)}";
    }

    /// <summary>
    /// Liefert die Meldung zu einem Ergebnis, das nicht <see cref="FavoriteResult.Ok"/> ist.
    /// </summary>
    /// <param name="result">Das Ergebnis.</param>
    /// <returns>Der Text; leer bei <see cref="FavoriteResult.Ok"/>.</returns>
    public static string GetResultMessage(FavoriteResult result)
    {
        return result switch
        {
            FavoriteResult.Ok => string.Empty,
            FavoriteResult.InvalidName => string.Create(German, $"Bitte einen Namen mit 1 bis {FavoriteLimits.MaxNameLength} Zeichen eingeben."),
            FavoriteResult.InvalidText => string.Create(German, $"Der Text ist zu lang (Beschreibung höchstens {FavoriteLimits.MaxDescriptionLength}, Notiz höchstens {FavoriteLimits.MaxNoteLength} Zeichen)."),
            FavoriteResult.DuplicateName => "Eine Gruppe mit diesem Namen gibt es bereits.",
            FavoriteResult.GroupNotFound => "Die Gruppe gibt es nicht mehr.",
            FavoriteResult.StationUnknown => "Die Tankstelle ist lokal nicht gespeichert. Bitte erneut suchen und die Details öffnen.",
            FavoriteResult.AlreadyMember => "Die Tankstelle gehört der Gruppe bereits an.",
            FavoriteResult.NotMember => "Die Tankstelle gehört der Gruppe nicht (mehr) an.",
            _ => SaveFailed,
        };
    }
}
