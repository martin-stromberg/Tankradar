using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Navigation;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Aufzeichnender <see cref="IMapOpener"/> für Tests: hält die angeforderten Ziele fest und startet nie eine externe App.
/// </summary>
public sealed class FakeMapOpener : IMapOpener
{
    /// <summary>
    /// Die angeforderten Positionsziele (Position und Name je Aufruf).
    /// </summary>
    public List<(GeoPosition Position, string Name)> PositionTargets { get; } = [];

    /// <summary>
    /// Die angeforderten Adressziele (Adresse und Name je Aufruf).
    /// </summary>
    public List<(string Address, string Name)> AddressTargets { get; } = [];

    /// <summary>
    /// Die Ausnahme, die der nächste Aufruf wirft; <see langword="null"/> für keinen Fehler.
    /// </summary>
    public Exception? Failure { get; set; }

    /// <inheritdoc />
    public Task OpenAsync(GeoPosition position, string name)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(name);
        if (Failure is { } failure)
        {
            return Task.FromException(failure);
        }

        PositionTargets.Add((position, name));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OpenAsync(string address, string name)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(name);
        if (Failure is { } failure)
        {
            return Task.FromException(failure);
        }

        AddressTargets.Add((address, name));
        return Task.CompletedTask;
    }
}
