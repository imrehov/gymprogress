using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymProgress.Api;
using GymProgress.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GymProgress.Tests;

[TestFixture]
public class LoginIntegrationTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();

        await using var scope = _factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await database.Database.MigrateAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<AppUser> CreateTestUserAsync(
        string email = "login@example.com",
        string password = "ValidPass1!",
        string displayName = "Login User")
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser
        {
            Email = email,
            UserName = email,
            DisplayName = displayName
        };

        var creation = await userManager.CreateAsync(user, password);
        Assert.That(creation.Succeeded, Is.True);

        return user;
    }

    //Successful-login test
    [Test]
    public async Task Login_ValidCredentials_ReturnsOk_SetsSecureCookie_AndPersistsHashedToken()
    {
        var user = await CreateTestUserAsync("login@example.com", "ValidPass1!");
        var request = new Login.Request("login@example.com", "ValidPass1!");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // - Response is 200 OK
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // - Response contains access token, expiry and safe user
        var json = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        Assert.That(root.TryGetProperty("accessToken", out var accessTokenProp), Is.True);
        Assert.That(accessTokenProp.GetString(), Is.Not.Null.And.Not.Empty);

        Assert.That(root.TryGetProperty("accessTokenExpiresAtUtc", out _), Is.True);

        Assert.That(root.TryGetProperty("user", out var userProp), Is.True);
        Assert.That(userProp.GetProperty("id").GetGuid(), Is.EqualTo(user.Id));
        Assert.That(userProp.GetProperty("email").GetString(), Is.EqualTo(user.Email));
        Assert.That(userProp.GetProperty("displayName").GetString(), Is.EqualTo(user.DisplayName));

        // - Response JSON does not contain the refresh token
        Assert.That(root.TryGetProperty("refreshToken", out _), Is.False);
        Assert.That(root.TryGetProperty("rawRefreshToken", out _), Is.False);

        // - Set-Cookie exists and contains attributes
        Assert.That(response.Headers.Contains("Set-Cookie"), Is.True);
        var setCookie = response.Headers.GetValues("Set-Cookie").Single();

        Assert.That(setCookie, Does.Contain("gymprogress.refresh_token="));
        Assert.That(setCookie, Does.Contain("httponly").IgnoreCase);
        Assert.That(setCookie, Does.Contain("secure").IgnoreCase);
        Assert.That(setCookie, Does.Contain("samesite=strict").IgnoreCase);
        Assert.That(setCookie, Does.Contain("path=/api/v1/auth").IgnoreCase);

        // Extract raw refresh token from cookie
        var cookiePair = setCookie.Split(';', 2)[0];
        var rawRefreshToken = cookiePair.Split('=', 2)[1];
        Assert.That(rawRefreshToken, Is.Not.Empty);

        // Database assertions
        await using var scope = _factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tokens = await database.RefreshTokens.ToListAsync();
        Assert.That(tokens, Has.Count.EqualTo(1));

        var storedToken = tokens.Single();
        Assert.That(storedToken.UserId, Is.EqualTo(user.Id));
        Assert.That(storedToken.TokenHash, Is.Not.EqualTo(rawRefreshToken));
        Assert.That(storedToken.TokenHash, Is.EqualTo(TokenService.HashRefreshToken(rawRefreshToken)));
    }

    // Invalid-password test
    [Test]
    public async Task Login_InvalidPassword_ReturnsUnauthorized_AndCreatesNoCookieOrToken()
    {
        await CreateTestUserAsync("login@example.com", "ValidPass1!");
        var request = new Login.Request("login@example.com", "WrongPassword!");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // - 401 Unauthorized
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // - No Set-Cookie header
        Assert.That(response.Headers.Contains("Set-Cookie"), Is.False);

        // - No refresh-token database record and user still exists
        await using var scope = _factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tokensCount = await database.RefreshTokens.CountAsync();
        Assert.That(tokensCount, Is.EqualTo(0));

        var user = await database.Users.SingleOrDefaultAsync(u => u.Email == "login@example.com");
        Assert.That(user, Is.Not.Null);
    }

    // Unknown-email test
    [Test]
    public async Task Login_UnknownEmail_ReturnsUnauthorized_AndCreatesNoCookieOrToken()
    {
        var request = new Login.Request("nonexistent@example.com", "ValidPass1!");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // - 401 Unauthorized
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // - No cookie
        Assert.That(response.Headers.Contains("Set-Cookie"), Is.False);

        // - No token record
        await using var scope = _factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tokensCount = await database.RefreshTokens.CountAsync();
        Assert.That(tokensCount, Is.EqualTo(0));
    }

    // Separate-login-family test
    [Test]
    public async Task Login_TwoSeparateLogins_CreatesTwoRefreshTokens_WithDifferentHashesAndFamilyIds()
    {
        await CreateTestUserAsync("login@example.com", "ValidPass1!");
        var request = new Login.Request("login@example.com", "ValidPass1!");

        var firstResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", request);
        Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var secondResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", request);
        Assert.That(secondResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        await using var scope = _factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tokens = await database.RefreshTokens.OrderBy(t => t.CreatedAtUtc).ToListAsync();
        Assert.That(tokens, Has.Count.EqualTo(2));

        Assert.That(tokens[0].TokenHash, Is.Not.EqualTo(tokens[1].TokenHash));
        Assert.That(tokens[0].FamilyId, Is.Not.EqualTo(tokens[1].FamilyId));
    }

    // Lockout test
    [Test]
    public async Task Login_FiveFailedAttempts_LocksOutAccount_SubsequentCorrectCredentialsReturnUnauthorized()
    {
        await CreateTestUserAsync("login@example.com", "ValidPass1!");

        var wrongPasswordRequest = new Login.Request("login@example.com", "WrongPassword!");
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var wrongResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", wrongPasswordRequest);
            Assert.That(wrongResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        // 6th attempt with correct password
        var correctPasswordRequest = new Login.Request("login@example.com", "ValidPass1!");
        var lockedResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", correctPasswordRequest);

        // Confirm still 401 Unauthorized without exposing lockout details
        Assert.That(lockedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(lockedResponse.Headers.Contains("Set-Cookie"), Is.False);

        // Confirm no refresh token was created in the database
        await using var scope = _factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tokenCount = await database.RefreshTokens.CountAsync();
        Assert.That(tokenCount, Is.EqualTo(0));
    }
}
