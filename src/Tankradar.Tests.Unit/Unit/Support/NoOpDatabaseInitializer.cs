using Tankradar.MAUI.Services;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="IDatabaseInitializer"/> für Tests, bei denen das Schema bereits angelegt ist.
/// </summary>
public sealed class NoOpDatabaseInitializer : IDatabaseInitializer
{
    /// <inheritdoc />
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
}
