using GymProgress.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace GymProgress.Tests;

[TestFixture]
public class DbIntegrationTests
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<AppDbContext> _contextOptions = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;


        using var context = new AppDbContext(_contextOptions);
        await context.Database.MigrateAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    [Test]
    public async Task CanConnect_And_QueryDb_ReturnsTrue()
    {

        await using var context = new AppDbContext(_contextOptions);

        var canConnect = await context.Database.CanConnectAsync();

        Assert.That(canConnect, Is.True);
    }

    [Test]
    public async Task Db_Contains_Identity_Schema()
    {
        var expectedTables = new[]
        {
            "AspNetUsers",
            "AspNetRoles",
            "AspNetUserRoles",
            "AspNetUserClaims",
            "AspNetUserLogins",
            "AspNetUserTokens",
            "AspNetRoleClaims"
        };

        var existingTables = new List<string>();

        await using var command = _connection.CreateCommand();

        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE 'AspNet%';";

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            existingTables.Add(reader.GetString(0));
        }

        Assert.That(existingTables, Is.EquivalentTo(expectedTables));
    }

    [Test]
    public async Task Db_Contains_RefreshTokens()
    {
        var expectedTables = new[]
        {
            "RefreshTokens"
        };

        var existingTables = new List<string>();

        await using var command = _connection.CreateCommand();

        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='RefreshTokens';";

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            existingTables.Add(reader.GetString(0));
        }

        Assert.That(existingTables, Is.EquivalentTo(expectedTables));
    }
}
