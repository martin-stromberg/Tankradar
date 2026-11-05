using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Chronologische Ordnung der Tank- und Ladevorgänge: nach Zeitpunkt, bei gleichem Zeitpunkt nach Kilometerstand, zuletzt nach Kennung.
/// Der Schlüssel ist eindeutig, auch wenn mehrere Vorgänge in derselben Minute liegen.
/// </summary>
public static class TankbookEntryOrder
{
    /// <summary>
    /// Vergleicht aufsteigend (ältester Vorgang zuerst).
    /// </summary>
    public static IComparer<TankbookEntry> Ascending
    {
        get { return AscendingComparer.Instance; }
    }

    /// <summary>
    /// Vergleicht zwei Vorgänge aufsteigend nach (<see cref="TankbookEntry.OccurredAt"/>, <see cref="TankbookEntry.OdometerKm"/>, <see cref="TankbookEntry.Id"/>).
    /// </summary>
    /// <param name="left">Der erste Vorgang.</param>
    /// <param name="right">Der zweite Vorgang.</param>
    /// <returns>Kleiner 0, wenn <paramref name="left"/> früher liegt; 0 bei gleichem Schlüssel; sonst größer 0.</returns>
    public static int Compare(TankbookEntry left, TankbookEntry right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        var result = left.OccurredAt.CompareTo(right.OccurredAt);
        if (result != 0)
        {
            return result;
        }

        result = left.OdometerKm.CompareTo(right.OdometerKm);
        return result != 0 ? result : left.Id.CompareTo(right.Id);
    }

    private sealed class AscendingComparer : IComparer<TankbookEntry>
    {
        public static readonly AscendingComparer Instance = new();

        public int Compare(TankbookEntry? x, TankbookEntry? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            return y is null ? 1 : TankbookEntryOrder.Compare(x, y);
        }
    }
}
