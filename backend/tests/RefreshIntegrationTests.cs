using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using GymProgress.Api;
using GymProgress.Api.Data;

namespace GymProgress.Tests;

[TestFixture]
public class RefreshIntegrationTests
{
    private const string RefreshCookieName = "gymprogress.refresh_token";

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

    [Test]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/v1/auth/refresh", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        await using var scope = _factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.That(await database.RefreshTokens.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task Refresh_WithUnknownToken_ReturnsUnauthorized_AndCreatesNoToken()
    {
        var response = await SendRefreshAsync("unknown-refresh-token");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        await using var scope = _factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.That(await database.RefreshTokens.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task Refresh_WithActiveToken_RotatesTokenAndPreservesFamily()
    {
        await CreateTestUserAsync();
        var oldRawToken = await LoginAndGetRefreshTokenAsync();

        var response = await SendRefreshAsync(oldRawToken);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<Refresh.Response>();
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.AccessToken, Is.Not.Empty);
        Assert.That(body.AccessTokenExpiresAtUtc, Is.GreaterThan(DateTime.UtcNow));

        var setCookie = GetRefreshSetCookie(response);
        Assert.That(setCookie, Does.Contain("httponly").IgnoreCase);
        Assert.That(setCookie, Does.Contain("secure").IgnoreCase);
        Assert.That(setCookie, Does.Contain("samesite=strict").IgnoreCase);
        Assert.That(setCookie, Does.Contain("path=/api/v1/auth").IgnoreCase);

        var newRawToken = GetRawToken(setCookie);
        Assert.That(newRawToken, Is.Not.EqualTo(oldRawToken));

        await using var scope = _factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokens = await database.RefreshTokens.ToListAsync();

        Assert.That(tokens, Has.Count.EqualTo(2));

        var oldToken = tokens.Single(token =>
            token.TokenHash == TokenService.HashRefreshToken(oldRawToken));
        var newToken = tokens.Single(token =>
            token.TokenHash == TokenService.HashRefreshToken(newRawToken));

        Assert.Multiple(() =>
        {
            Assert.That(oldToken.RevokedAtUtc, Is.Not.Null);
            Assert.That(oldToken.ReplacedByTokenId, Is.EqualTo(newToken.Id));
            Assert.That(newToken.RevokedAtUtc, Is.Null);
            Assert.That(newToken.FamilyId, Is.EqualTo(oldToken.FamilyId));
        });
    }

    [Test]
    public async Task Refresh_WhenRotatedTokenIsReused_RevokesActiveFamilyToken()
    {
        await CreateTestUserAsync();
        var oldRawToken = await LoginAndGetRefreshTokenAsync();

        var rotationResponse = await SendRefreshAsync(oldRawToken);
        Assert.That(rotationResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var newRawToken = GetRawToken(GetRefreshSetCookie(rotationResponse));

        var reuseResponse = await SendRefreshAsync(oldRawToken);

        Assert.That(reuseResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        await using var scope = _factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var newToken = await database.RefreshTokens.SingleAsync(token =>
            token.TokenHash == TokenService.HashRefreshToken(newRawToken));

        Assert.That(newToken.RevokedAtUtc, Is.Not.Null);
    }

    [Test]
    public async Task Refresh_WithExpiredToken_ReturnsUnauthorized_AndCreatesNoReplacement()
    {
        var user = await CreateTestUserAsync();
        const string rawToken = "expired-refresh-token";
        var now = DateTime.UtcNow;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            database.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = TokenService.HashRefreshToken(rawToken),
                FamilyId = Guid.NewGuid(),
                CreatedAtUtc = now.AddDays(-2),
                ExpiresAtUtc = now.AddDays(-1)
            });
            await database.SaveChangesAsync();
        }

        var response = await SendRefreshAsync(rawToken);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationDatabase = verificationScope.ServiceProvider
            .GetRequiredService<AppDbContext>();
        Assert.That(await verificationDatabase.RefreshTokens.CountAsync(), Is.EqualTo(1));
    }

    private async Task<AppUser> CreateTestUserAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser
        {
            Email = "refresh@example.com",
            UserName = "refresh@example.com",
            DisplayName = "Refresh User"
        };

        var result = await userManager.CreateAsync(user, "ValidPass1!");
        Assert.That(result.Succeeded, Is.True);
        return user;
    }

    private async Task<string> LoginAndGetRefreshTokenAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new Login.Request("refresh@example.com", "ValidPass1!"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return GetRawToken(GetRefreshSetCookie(response));
    }

    private async Task<HttpResponseMessage> SendRefreshAsync(string rawToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/auth/refresh");
        request.Headers.Add("Cookie", $"{RefreshCookieName}={rawToken}");

        return await _client.SendAsync(request);
    }

    private static string GetRefreshSetCookie(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(
                $"{RefreshCookieName}=",
                StringComparison.Ordinal));

    private static string GetRawToken(string setCookie) =>
        setCookie.Split(';', 2)[0].Split('=', 2)[1];
}
