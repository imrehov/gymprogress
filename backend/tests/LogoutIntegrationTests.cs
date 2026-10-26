using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using GymProgress.Api;
using GymProgress.Api.Data;

namespace GymProgress.Tests;

[TestFixture]
public class LogoutIntegrationTests
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
    public async Task Logout_MissingCookie_ReturnsNoContent_AndEmitsDeletingCookie()
    {
        var response = await _client.PostAsync("/api/v1/auth/logout", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var setCookie = GetRefreshSetCookie(response);
        AssertDeletingCookie(setCookie);

        await using var scope = _factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.That(await database.RefreshTokens.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task Logout_UnknownCookie_ReturnsNoContent_AndChangesNothing()
    {
        await CreateTestUserAsync();
        var activeToken = await LoginAndGetRefreshTokenAsync();

        var response = await SendLogoutAsync("unknown-token");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var setCookie = GetRefreshSetCookie(response);
        AssertDeletingCookie(setCookie);

        await using var scope = _factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokens = await database.RefreshTokens.ToListAsync();

        Assert.That(tokens, Has.Count.EqualTo(1));
        var storedToken = tokens.Single();
        Assert.That(storedToken.TokenHash, Is.EqualTo(TokenService.HashRefreshToken(activeToken)));
        Assert.That(storedToken.RevokedAtUtc, Is.Null);
    }

    [Test]
    public async Task Logout_ValidCookie_ReturnsNoContent_RevokesDatabaseToken_EmitsDeletingCookie_AndCannotRefresh()
    {
        await CreateTestUserAsync();
        var rawToken = await LoginAndGetRefreshTokenAsync();

        var logoutResponse = await SendLogoutAsync(rawToken);

        Assert.That(logoutResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var setCookie = GetRefreshSetCookie(logoutResponse);
        AssertDeletingCookie(setCookie);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var storedToken = await database.RefreshTokens.SingleAsync(token =>
                token.TokenHash == TokenService.HashRefreshToken(rawToken));

            Assert.That(storedToken.RevokedAtUtc, Is.Not.Null);
        }

        var refreshResponse = await SendRefreshAsync(rawToken);
        Assert.That(refreshResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Logout_TwoSeparateLoginSessions_RevokingSessionALeavesSessionBActive()
    {
        await CreateTestUserAsync();
        var sessionAToken = await LoginAndGetRefreshTokenAsync();
        var sessionBToken = await LoginAndGetRefreshTokenAsync();

        var logoutResponse = await SendLogoutAsync(sessionAToken);

        Assert.That(logoutResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokenA = await database.RefreshTokens.SingleAsync(token =>
                token.TokenHash == TokenService.HashRefreshToken(sessionAToken));
            var tokenB = await database.RefreshTokens.SingleAsync(token =>
                token.TokenHash == TokenService.HashRefreshToken(sessionBToken));

            Assert.Multiple(() =>
            {
                Assert.That(tokenA.RevokedAtUtc, Is.Not.Null);
                Assert.That(tokenB.RevokedAtUtc, Is.Null);
            });
        }

        var refreshAResponse = await SendRefreshAsync(sessionAToken);
        Assert.That(refreshAResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        var refreshBResponse = await SendRefreshAsync(sessionBToken);
        Assert.That(refreshBResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    private static void AssertDeletingCookie(string setCookie)
    {
        Assert.That(setCookie, Does.Contain($"{RefreshCookieName}="));
        Assert.That(setCookie, Does.Contain("path=/api/v1/auth").IgnoreCase);
        Assert.That(
            setCookie.Contains("max-age=0", StringComparison.OrdinalIgnoreCase) ||
            setCookie.Contains("expires=", StringComparison.OrdinalIgnoreCase),
            Is.True,
            "Set-Cookie must contain max-age=0 or an expired expires date to delete the cookie.");
    }

    private async Task<AppUser> CreateTestUserAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser
        {
            Email = "logout@example.com",
            UserName = "logout@example.com",
            DisplayName = "Logout User"
        };

        var result = await userManager.CreateAsync(user, "ValidPass1!");
        Assert.That(result.Succeeded, Is.True);
        return user;
    }

    private async Task<string> LoginAndGetRefreshTokenAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new Login.Request("logout@example.com", "ValidPass1!"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return GetRawToken(GetRefreshSetCookie(response));
    }

    private async Task<HttpResponseMessage> SendLogoutAsync(string rawToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/auth/logout");
        request.Headers.Add("Cookie", $"{RefreshCookieName}={rawToken}");

        return await _client.SendAsync(request);
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
