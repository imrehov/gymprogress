using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

using GymProgress.Api.Data;
using GymProgress.Api;

namespace GymProgress.Tests;

[TestFixture]
public class GetCurrentUserIntegrationTests
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

    [Test]
    public async Task GetCurrentUser_WithoutAccessToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]

    public async Task GetCurrentUser_Me_Endpoint_Works()
    {

        //first log in
        var user = await CreateTestUserAsync("user@example.com", "ValidPass1!");

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new Login.Request("user@example.com", "ValidPass1!"));

        var login = await loginResponse.Content
            .ReadFromJsonAsync<Login.Response>();

        Assert.That(login, Is.Not.Null);

        //then set bearer header
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login!.AccessToken);

        //then call the /me endpoint
        var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var profile = await response.Content
            .ReadFromJsonAsync<GetCurrentUser.Response>();

        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.Id, Is.EqualTo(user.Id));
        Assert.That(profile.Email, Is.EqualTo(user.Email));
        Assert.That(profile.DisplayName, Is.EqualTo(user.DisplayName));

    }
}