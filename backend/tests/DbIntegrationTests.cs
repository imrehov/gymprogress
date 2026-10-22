using Api.Shared.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Tests;

[TestFixture]
public class DbIntegrationTests
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<AppDbContext> _contextOptions = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;


        using var context = new AppDbContext(_contextOptions);
        context.Database.EnsureCreated();
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
}
