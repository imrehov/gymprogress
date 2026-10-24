using System.Net;
using System.Net.Http.Json;
using GymProgress.Api;
using GymProgress.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GymProgress.Tests;

[TestFixture]
public class RegisterIntegrationTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();

        await using var scope = _factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        await database.Database.MigrateAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // 1. Valid registration returns 201 and safe user data
    // 2. The user exists in the database afterward
    // 3. The stored password is hashed, not equal to the submitted password
    [Test]
    public async Task Register_ValidCommand_ReturnsCreated_AndPersistsHashedPassword()
    {
        var command = new Register.Command("john@example.com", "P@ssword123!", "John Doe");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", command);

        // 1. Assert status 201 & safe response
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var body = await response.Content.ReadFromJsonAsync<Register.Response>();
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.Email, Is.EqualTo("john@example.com"));
        Assert.That(body.DisplayName, Is.EqualTo("John Doe"));

        // 2. Assert user exists in DB
        await using var scope = _factory.Services.CreateAsyncScope();

        var database = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var user = await database.Users
            .SingleOrDefaultAsync(user =>
                user.Email == "john@example.com");
        Assert.That(user, Is.Not.Null);

        // 3. Assert password is hashed, and valid

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<AppUser>>();

        var passwordIsValid = await userManager.CheckPasswordAsync(
            user!,
            "P@ssword123!");

        Assert.That(passwordIsValid, Is.True);

        Assert.That(user!.PasswordHash, Is.Not.Null);
    }

    // 4. Invalid email or blank display name returns 400 and creates no user
    [TestCase("notanemail", "ValidPass1!", "John")]
    [TestCase("valid@example.com", "ValidPass1!", "")]
    public async Task Register_InvalidPayload_ReturnsBadRequest_AndCreatesNoUser(
        string email, string password, string displayName)
    {
        var command = new Register.Command(email, password, displayName);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", command);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        await using var scope = _factory.Services.CreateAsyncScope();

        var database = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var userCount = await database.Users.CountAsync();
        Assert.That(userCount, Is.EqualTo(0));
    }

    // 5. Registering the same email twice returns 400 and leaves one user
    [Test]
    public async Task Register_DuplicateEmail_ReturnsBadRequest_AndLeavesOneUser()
    {
        var command1 = new Register.Command("duplicate@example.com", "P@ssword123!", "First User");
        var command2 = new Register.Command("duplicate@example.com", "P@ssword123!", "Second User");

        var response1 = await _client.PostAsJsonAsync("/api/v1/auth/register", command1);
        Assert.That(response1.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var response2 = await _client.PostAsJsonAsync("/api/v1/auth/register", command2);
        Assert.That(response2.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        await using var scope = _factory.Services.CreateAsyncScope();

        var database = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var count = await database.Users.CountAsync(u => u.Email == "duplicate@example.com");
        Assert.That(count, Is.EqualTo(1));
    }
}