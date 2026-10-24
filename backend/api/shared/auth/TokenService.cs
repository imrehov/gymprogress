using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GymProgress.Api;

public sealed record IssuedTokens(
    string AccessToken,
    string RawRefreshToken, //goes into the cookie
    RefreshToken RefreshToken); //refreshtoken.tokenhash goes into sqlite
public sealed class TokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public TokenService(
        IOptions<JwtOptions> options,
        TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public IssuedTokens IssueTokens(
        AppUser user,
        string? createdByIp,
        Guid? familyId = null)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new(
                "display_name",
                user.DisplayName),

            new(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now)
                    .ToUnixTimeSeconds()
                    .ToString(),
                ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(
                JwtRegisteredClaimNames.Email,
                user.Email));
        }

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SigningKey));

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(
                _options.AccessTokenMinutes),
            signingCredentials: new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256));

        var accessToken =
            new JwtSecurityTokenHandler().WriteToken(jwt);

        var rawRefreshToken = WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(64));

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashRefreshToken(rawRefreshToken),
            FamilyId = familyId ?? Guid.NewGuid(),
            CreatedAtUtc = now,
            CreatedByIp = createdByIp,
            ExpiresAtUtc = now.AddDays(
                _options.RefreshTokenDays)
        };

        return new IssuedTokens(
            accessToken,
            rawRefreshToken,
            refreshToken);
    }

    public static string HashRefreshToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }
}