using GymProgress.Api.Data;
using GymProgress.Api;

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

    [Test]
    public async Task Can_Insert_And_Retrieve_Valid_RefreshToken()
    {
        await using var context = new AppDbContext(_contextOptions);

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = "token-test@example.com",
            Email = "token-test@example.com",
            DisplayName = "Token Test"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var familyId = Guid.NewGuid();
        var token = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = new string('A', 64),
            FamilyId = familyId,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(30),
            CreatedByIp = "127.0.0.1"
        };
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        var savedToken = await context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == token.Id);

        Assert.That(savedToken, Is.Not.Null);
        Assert.That(savedToken!.UserId, Is.EqualTo(user.Id));
        Assert.That(savedToken.FamilyId, Is.EqualTo(familyId));
    }

    [Test]
    public async Task Duplicate_TokenHash_Throws_DbUpdateException()
    {
        await using var context = new AppDbContext(_contextOptions);

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = "token-test@example.com",
            Email = "token-test@example.com",
            DisplayName = "Token Test"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var token1 = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = new string('A', 64),
            FamilyId = Guid.NewGuid(),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(30)
        };
        var token2 = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = new string('A', 64),
            FamilyId = Guid.NewGuid(),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(30)
        };

        context.RefreshTokens.Add(token1);
        await context.SaveChangesAsync();

        context.RefreshTokens.Add(token2);
        Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task Expiration_NotLaterThan_Creation_Throws_DbUpdateException()
    {
        await using var context = new AppDbContext(_contextOptions);

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = "token-test@example.com",
            Email = "token-test@example.com",
            DisplayName = "Token Test"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var token = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = new string('B', 64),
            FamilyId = Guid.NewGuid(),
            CreatedAtUtc = now,
            ExpiresAtUtc = now // Violates CK_RefreshTokens_ExpiresAfterCreation ("ExpiresAtUtc" > "CreatedAtUtc")
        };

        context.RefreshTokens.Add(token);
        Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task Replacement_Without_Revocation_Throws_DbUpdateException()
    {
        await using var context = new AppDbContext(_contextOptions);

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = "token-test@example.com",
            Email = "token-test@example.com",
            DisplayName = "Token Test"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;

        // 1. Create the replacement token first
        var replacementToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = new string('C', 64),
            FamilyId = Guid.NewGuid(),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(30)
        };
        context.RefreshTokens.Add(replacementToken);
        await context.SaveChangesAsync();

        // 2. Create old token referencing replacement but with RevokedAtUtc = null
        var oldToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = new string('D', 64),
            FamilyId = replacementToken.FamilyId,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(30),
            ReplacedByTokenId = replacementToken.Id,
            RevokedAtUtc = null // Violates CK_RefreshTokens_ReplacementRequiresRevocation
        };

        context.RefreshTokens.Add(oldToken);
        Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());

        // 3. Set RevokedAtUtc and verify it can now be saved
        oldToken.RevokedAtUtc = now;
        Assert.DoesNotThrowAsync(async () => await context.SaveChangesAsync());
    }
}
