namespace GymProgress.Api;

public static class RefreshTokenCookie
{
    public const string Name = "gymprogress.refresh_token";

    public static void Append(
        HttpResponse response,
        string rawToken,
        DateTime expiresAtUtc,
        bool secure)
    {
        response.Cookies.Append(
            Name,
            rawToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = SameSiteMode.Strict,
                Path = "/api/v1/auth",
                Expires = expiresAtUtc,
                IsEssential = true
            });
    }

    public static void Delete(
        HttpResponse response,
        bool secure)
    {
        response.Cookies.Delete(
            Name,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = SameSiteMode.Strict,
                Path = "/api/v1/auth"
            });
    }
}