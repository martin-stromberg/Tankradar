namespace Tankradar.MAUI.Models.Map;

/// <summary>
/// Eine Lage im Kartenausschnitt in Pixeln, vom linken oberen Rand aus.
/// </summary>
/// <param name="X">Der Abstand vom linken Rand.</param>
/// <param name="Y">Der Abstand vom oberen Rand.</param>
/// <returns>Der Wert.</returns>
public readonly record struct ScreenPoint(double X, double Y);
