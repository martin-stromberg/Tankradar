namespace Tankradar.MAUI.Models.Favorites;

/// <summary>
/// Ergebnis einer Änderung, die eine Gruppe liefert (z. B. Anlegen).
/// </summary>
/// <param name="Result">Das Ergebnis der Änderung.</param>
/// <param name="Group">Die angelegte oder geänderte Gruppe; <see langword="null"/>, wenn <paramref name="Result"/> nicht <see cref="FavoriteResult.Ok"/> ist.</param>
/// <returns>Der Wert.</returns>
public sealed record FavoriteGroupResult(FavoriteResult Result, FavoriteGroup? Group);
