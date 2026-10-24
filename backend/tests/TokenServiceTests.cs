using System.IdentityModel.Tokens.Jwt;
using System.Text;
using GymProgress.Api;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GymProgress.Tests;

[TestFixture]
public class TokenServiceTests
{
    private IOptions<JwtOptions> _options = null!;
    private DateTimeOffset _fixedNow;
    private TokenService _service = null!;
    private AppUser _testUser = null!;

    [SetUp]
    public void SetUp()
    {
        _options = Options.Create(new JwtOptions
        {
            Issuer = "GymProgress.Tests",
            Audience = "GymProgress.Tests.Client",
            SigningKey = new string('A', 64),
            AccessTokenMinutes = 15,
            RefreshTokenDays = 30
        });

        _fixedNow = new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
        _service = new TokenService(_options, new FixedTimeProvider(_fixedNow));

        _testUser = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            UserName = "user@example.com",
            DisplayName = "Test User"
        };
    }

    [Test]
    public void IssueTokens_Generates_Valid_RefreshToken()
    {
        var issued = _service.IssueTokens(_testUser, "127.0.0.1");

        Assert.That(issued.RawRefreshToken, Is.Not.Empty);
        Assert.That(issued.RefreshToken.TokenHash, Is.Not.EqualTo(issued.RawRefreshToken));
        Assert.That(issued.RefreshToken.TokenHash, Has.Length.EqualTo(64));
        Assert.That(issued.RefreshToken.TokenHash, Is.EqualTo(TokenService.HashRefreshToken(issued.RawRefreshToken)));
        Assert.That(issued.RefreshToken.UserId, Is.EqualTo(_testUser.Id));
        Assert.That(issued.RefreshToken.CreatedAtUtc, Is.EqualTo(_fixedNow.UtcDateTime));
        Assert.That(issued.RefreshToken.ExpiresAtUtc, Is.EqualTo(_fixedNow.UtcDateTime.AddDays(30)));
    }

    [Test]
    public void IssueTokens_Generates_Random_Tokens_And_Preserves_Family_On_Rotation()
    {
        var first = _service.IssueTokens(_testUser, null);
        var second = _service.IssueTokens(_testUser, null);

        Assert.That(second.RawRefreshToken, Is.Not.EqualTo(first.RawRefreshToken));
        Assert.That(second.RefreshToken.TokenHash, Is.Not.EqualTo(first.RefreshToken.TokenHash));
        Assert.That(second.RefreshToken.FamilyId, Is.Not.EqualTo(first.RefreshToken.FamilyId));

        var rotated = _service.IssueTokens(_testUser, null, first.RefreshToken.FamilyId);
        Assert.That(rotated.RefreshToken.FamilyId, Is.EqualTo(first.RefreshToken.FamilyId));
    }

    [Test]
    public void IssueTokens_Generates_Cryptographically_Valid_AccessToken()
    {
        var issued = _service.IssueTokens(_testUser, "127.0.0.1");

        var handler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Value.Issuer,

            ValidateAudience = true,
            ValidAudience = _options.Value.Audience,

        ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Convert.FromHexString(
                    _options.Value.SigningKey)),

            RequireSignedTokens = true,
            RequireExpirationTime = true,

            // Expiry is asserted separately against the fixed clock
            ValidateLifetime = false
        };

        var principal = handler.ValidateToken(
            issued.AccessToken,
            validationParameters,
            out var validatedToken);

        Assert.That(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, Is.EqualTo(_testUser.Id.ToString()));
        Assert.That(principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value, Is.EqualTo(_testUser.Email));
        Assert.That(principal.FindFirst("display_name")?.Value, Is.EqualTo(_testUser.DisplayName));
        Assert.That(validatedToken, Is.TypeOf<JwtSecurityToken>());

        var jwt = (JwtSecurityToken)validatedToken;
        Assert.That(jwt.ValidTo, Is.EqualTo(_fixedNow.UtcDateTime.AddMinutes(15)).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void HashRefreshToken_Is_Deterministic_And_Produces_Expected_Format()
    {
        var first = TokenService.HashRefreshToken("known-token");
        var second = TokenService.HashRefreshToken("known-token");

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first, Has.Length.EqualTo(64));
        Assert.That(first, Does.Match("^[0-9A-F]{64}$"));
    }
}