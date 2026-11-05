using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Data;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass Verbindungen mit der Verbindungszeichenfolge der App (<see cref="TankradarDbContext.BuildConnectionString"/>) Fremdschlüssel ausdrücklich einschalten.
/// </summary>
public class ForeignKeysTests_Connection : IDisposable
{
    private readonly TestDatabase _database = new();

    /// <summary>
    /// Prüft <c>PRAGMA foreign_keys</c> = 1 auf einer Verbindung des Kontexts und dass die Verbindungszeichenfolge <c>Foreign Keys=True</c> enthält.
    /// </summary>
    [Fact]
    public async Task AppConnection_HasForeignKeysEnabled()
    {
        var factory = _database.CreateFactory();
        await _database.CreateInitializer(factory).InitializeAsync();
        await using var context = factory.CreateDbContext();

        Assert.True(new SqliteConnectionStringBuilder(context.Database.GetConnectionString()).ForeignKeys);
        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "PRAGMA foreign_keys";
        Assert.Equal(1L, await command.ExecuteScalarAsync());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}
