using Microsoft.Data.Sqlite;
using Tankradar.MAUI.Data;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft <see cref="TankradarDbContext.BuildConnectionString"/>: Fremdschlüssel ausdrücklich eingeschaltet, Pooling wahlweise aus.
/// </summary>
public class TankradarDbContextTests_ConnectionString : BaseTest
{
    /// <summary>
    /// Prüft, dass die Verbindungszeichenfolge den Pfad und <c>Foreign Keys=True</c> enthält und das Pooling nicht abschaltet.
    /// </summary>
    [Fact]
    public void BuildConnectionString_EnablesForeignKeys()
    {
        var path = Path.Combine("C:", "daten", "tankatlas.db");

        var builder = new SqliteConnectionStringBuilder(TankradarDbContext.BuildConnectionString(path));

        Assert.Equal(path, builder.DataSource);
        Assert.True(builder.ForeignKeys);
        Assert.True(builder.Pooling);
    }

    /// <summary>
    /// Prüft, dass das Pooling für Tests abgeschaltet werden kann und die Fremdschlüssel dabei eingeschaltet bleiben.
    /// </summary>
    [Fact]
    public void BuildConnectionString_WithoutPooling()
    {
        var builder = new SqliteConnectionStringBuilder(TankradarDbContext.BuildConnectionString("tankatlas.db", pooling: false));

        Assert.False(builder.Pooling);
        Assert.True(builder.ForeignKeys);
    }
}
