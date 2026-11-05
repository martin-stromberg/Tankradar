using Tankradar.MAUI.Services;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="IScreenReaderAnnouncer"/> für Tests: hält alle Ansagen in Reihenfolge fest.
/// </summary>
public sealed class FakeScreenReaderAnnouncer : IScreenReaderAnnouncer
{
    /// <summary>
    /// Die angesagten Texte in Reihenfolge.
    /// </summary>
    public List<string> Announcements { get; } = [];

    /// <inheritdoc />
    public void Announce(string text)
    {
        Announcements.Add(text);
    }
}
